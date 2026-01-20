using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;

namespace FlowClaude.App.ViewModels;

/// <summary>
/// ViewModel for the chat interface with AI agent
/// </summary>
public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private Workspace? _workspace;
    
    [ObservableProperty]
    private string _currentMessage = "";
    
    [ObservableProperty]
    private bool _isProcessing = false;
    
    [ObservableProperty]
    private ChatMode _currentMode = ChatMode.Agent;
    
    [ObservableProperty]
    private bool _hasPendingPlan = false;
    
    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();
    public ObservableCollection<ToolCallViewModel> ActiveToolCalls { get; } = new();
    
    private readonly IChatRepository _chatRepository;
    private readonly IAgentService _agentService;
    private readonly IGitService _gitService;
    private Guid? _currentChatId;
    private CancellationTokenSource? _processingCts;
    
    public ICommand SendMessageCommand { get; }
    public ICommand SwitchModeCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ApprovePlanCommand { get; }
    public ICommand ClearChatCommand { get; }
    
    public ChatViewModel(
        IChatRepository chatRepository,
        IAgentService agentService,
        IGitService gitService)
    {
        _chatRepository = chatRepository;
        _agentService = agentService;
        _gitService = gitService;
        
        SendMessageCommand = new AsyncRelayCommand<string>(SendMessageAsync);
        SwitchModeCommand = new RelayCommand(() => CurrentMode = CurrentMode == ChatMode.Agent ? ChatMode.Plan : ChatMode.Agent);
        CancelCommand = new RelayCommand(CancelProcessing);
        ApprovePlanCommand = new AsyncRelayCommand(ApprovePlanAsync);
        ClearChatCommand = new RelayCommand(ClearChat);
    }
    
    public void SetWorkspace(Workspace? workspace)
    {
        Workspace = workspace;
        LoadChatHistoryAsync();
    }
    
    private async void LoadChatHistoryAsync()
    {
        if (Workspace == null) return;
        
        Messages.Clear();
        
        var chats = await _chatRepository.GetByWorkspaceIdAsync(Workspace.Id);
        foreach (var chat in chats.OrderByDescending(c => c.CreatedAt).Take(1))
        {
            _currentChatId = chat.Id;
            CurrentMode = chat.Mode;
            HasPendingPlan = chat.HasPendingPlan;
            
            foreach (var message in chat.Messages)
            {
                Messages.Add(new ChatMessageViewModel
                {
                    Role = message.Role,
                    Content = message.Content,
                    Timestamp = message.CreatedAt
                });
            }
        }
    }
    
    private async Task SendMessageAsync(object? parameter)
    {
        string userMessage;
        
        if (parameter is string paramMessage && !string.IsNullOrWhiteSpace(paramMessage))
        {
            userMessage = paramMessage;
        }
        else if (string.IsNullOrWhiteSpace(CurrentMessage) || Workspace == null || IsProcessing)
        {
            return;
        }
        else
        {
            userMessage = CurrentMessage;
            CurrentMessage = "";
        }
        
        IsProcessing = true;
        
        // Add user message to UI
        Messages.Add(new ChatMessageViewModel
        {
            Role = MessageRole.User,
            Content = userMessage,
            Timestamp = DateTime.UtcNow
        });
        
        _processingCts = new CancellationTokenSource();
        
        try
        {
            // Create or get chat
            if (!_currentChatId.HasValue)
            {
                var chat = new Chat
                {
                    WorkspaceId = Workspace.Id,
                    Title = userMessage.Length > 50 ? userMessage[..50] + "..." : userMessage,
                    Mode = CurrentMode
                };
                chat = await _chatRepository.CreateAsync(chat);
                _currentChatId = chat.Id;
            }
            
            // Add user message to database
            await _chatRepository.AddMessageAsync(_currentChatId.Value, new ChatMessage
            {
                ChatId = _currentChatId.Value,
                Role = MessageRole.User,
                Content = userMessage
            });
            
            // Send to agent
            var responseContent = new System.Text.StringBuilder();
            var assistantMessage = new ChatMessageViewModel
            {
                Role = MessageRole.Assistant,
                Content = "",
                Timestamp = DateTime.UtcNow,
                IsStreaming = true
            };
            Messages.Add(assistantMessage);
            
            await foreach (var evt in _agentService.SendMessageAsync(new AgentRequest
            {
                Message = userMessage,
                WorkspaceId = Workspace.Id,
                Mode = CurrentMode
            }, _processingCts.Token))
            {
                if (evt is AgentMessageEvent msgEvt)
                {
                    responseContent.Append(msgEvt.Content);
                    assistantMessage.Content = responseContent.ToString();
                }
                else if (evt is AgentToolCallEvent toolEvt)
                {
                    HandleToolCallEvent(toolEvt);
                }
                else if (evt is AgentErrorEvent errorEvt)
                {
                    assistantMessage.IsError = true;
                    assistantMessage.Content += $"\n❌ Error: {errorEvt.Error}";
                }
                else if (evt is AgentCompletedEvent completedEvt)
                {
                    assistantMessage.IsStreaming = false;
                    IsProcessing = false;
                    HasPendingPlan = false;
                    
                    if (!completedEvt.Success)
                    {
                        assistantMessage.IsError = true;
                    }
                }
            }
            
            // Save assistant message
            if (_currentChatId.HasValue)
            {
                await _chatRepository.AddMessageAsync(_currentChatId.Value, new ChatMessage
                {
                    ChatId = _currentChatId.Value,
                    Role = MessageRole.Assistant,
                    Content = responseContent.ToString()
                });
            }
        }
        catch (OperationCanceledException)
        {
            Messages.LastOrDefault()!.IsStreaming = false;
            Messages.LastOrDefault()!.Content += "\n\n⚫ Cancelled";
        }
        catch (Exception ex)
        {
            Messages.LastOrDefault()!.IsStreaming = false;
            Messages.LastOrDefault()!.IsError = true;
            Messages.LastOrDefault()!.Content += $"\n❌ Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            _processingCts?.Dispose();
            _processingCts = null;
        }
    }
    
    private void HandleToolCallEvent(AgentToolCallEvent toolEvt)
    {
        var existing = ActiveToolCalls.FirstOrDefault(t => t.ToolName == toolEvt.ToolName);
        
        if (toolEvt.Status == ToolCallStatus.Running)
        {
            if (existing == null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    ActiveToolCalls.Add(new ToolCallViewModel
                    {
                        ToolName = toolEvt.ToolName,
                        Input = toolEvt.Input,
                        Status = toolEvt.Status,
                        Timestamp = toolEvt.Timestamp
                    });
                });
            }
            else
            {
                existing.Status = toolEvt.Status;
            }
        }
        else if (toolEvt.Status == ToolCallStatus.Completed || toolEvt.Status == ToolCallStatus.Error)
        {
            if (existing != null)
            {
                existing.Status = toolEvt.Status;
                existing.Output = toolEvt.Output;
                
                // Remove after a delay
                Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ActiveToolCalls.Remove(existing);
                    });
                });
            }
            
            if (toolEvt.Status == ToolCallStatus.Error)
            {
                var lastMessage = Messages.LastOrDefault();
                lastMessage!.IsError = true;
            }
        }
    }
    
    private void CancelProcessing()
    {
        _processingCts?.Cancel();
    }
    
    private async Task ApprovePlanAsync()
    {
        if (!_currentChatId.HasValue) return;
        
        HasPendingPlan = false;
        CurrentMode = ChatMode.Agent;
        
        await SendMessageAsync(null);
    }
    
    private void ClearChat()
    {
        Messages.Clear();
        ActiveToolCalls.Clear();
        _currentChatId = null;
    }
}

public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private MessageRole _role;
    
    [ObservableProperty]
    private string _content = "";
    
    [ObservableProperty]
    private DateTime _timestamp;
    
    [ObservableProperty]
    private bool _isStreaming;
    
    [ObservableProperty]
    private bool _isError;
    
    public bool IsUser => Role == MessageRole.User;
    public bool IsAssistant => Role == MessageRole.Assistant;
    public bool IsSystem => Role == MessageRole.System;
}

public partial class ToolCallViewModel : ObservableObject
{
    [ObservableProperty]
    private string _toolName = "";
    
    [ObservableProperty]
    private string? _input;
    
    [ObservableProperty]
    private string? _output;
    
    [ObservableProperty]
    private ToolCallStatus _status;
    
    [ObservableProperty]
    private DateTime _timestamp;
    
    public bool IsPending => Status == ToolCallStatus.Pending;
    public bool IsRunning => Status == ToolCallStatus.Running;
    public bool IsCompleted => Status == ToolCallStatus.Completed;
    public bool IsError => Status == ToolCallStatus.Error;
}
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;

namespace FlowClaude.App.ViewModels;

/// <summary>
/// ViewModel for the integrated terminal
/// </summary>
public partial class TerminalViewModel : ObservableObject
{
    [ObservableProperty]
    private Workspace? _workspace;
    
    [ObservableProperty]
    private string _currentDirectory = "";
    
    [ObservableProperty]
    private bool _isConnected = false;
    
    [ObservableProperty]
    private bool _isProcessing = false;
    
    public ObservableCollection<TerminalTabViewModel> Tabs { get; } = new();
    
    private TerminalTabViewModel? _activeTab;
    private readonly ITerminalService _terminalService;
    private CancellationTokenSource? _readCts;
    
    public ICommand NewTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand SelectTabCommand { get; }
    public ICommand WriteCommand { get; }
    public ICommand ClearCommand { get; }
    
    public TerminalTabViewModel? ActiveTab
    {
        get => _activeTab;
        set
        {
            if (SetProperty(ref _activeTab, value) && value != null)
            {
                CurrentDirectory = value.CurrentDirectory;
            }
        }
    }
    
    public TerminalViewModel(ITerminalService terminalService)
    {
        _terminalService = terminalService;
        
        NewTabCommand = new RelayCommand(NewTab);
        CloseTabCommand = new RelayCommand<TerminalTabViewModel>(CloseTab);
        SelectTabCommand = new RelayCommand<TerminalTabViewModel>(vm => ActiveTab = vm);
        WriteCommand = new RelayCommand<string>(WriteToTerminal);
        ClearCommand = new RelayCommand(ClearTerminal);
        
        // Create initial tab
        NewTab();
    }
    
    public void SetWorkspace(Workspace? workspace)
    {
        _workspace = workspace;
        if (workspace != null && Tabs.Count > 0)
        {
            Tabs[0].CurrentDirectory = workspace.WorktreePath ?? workspace.BranchName ?? "/";
            CurrentDirectory = Tabs[0].CurrentDirectory;
        }
    }
    
    private void NewTab()
    {
        var tab = new TerminalTabViewModel
        {
            CurrentDirectory = _workspace?.WorktreePath ?? "/",
            Title = "Terminal"
        };
        
        tab.OnOutput += (sender, output) =>
        {
            // Parse cwd from output
            var cwd = _terminalService.GetCwd(output);
            if (!string.IsNullOrEmpty(cwd))
            {
                tab.CurrentDirectory = cwd;
                if (ActiveTab == tab)
                    CurrentDirectory = cwd;
            }
        };
        
        Tabs.Add(tab);
        ActiveTab = tab;
        
        ConnectTabAsync(tab);
    }
    
    private async void ConnectTabAsync(TerminalTabViewModel tab)
    {
        try
        {
            var session = await _terminalService.CreateSessionAsync(
                tab.Id,
                _workspace?.Id.ToString() ?? "",
                tab.CurrentDirectory);
            
            tab.CurrentDirectory = session.Cwd;
            tab.IsConnected = true;
            IsConnected = true;
            
            // Start reading output
            _readCts = new CancellationTokenSource();
            _ = ReadOutputAsync(tab, _readCts.Token);
        }
        catch (Exception ex)
        {
            tab.Output += $"\x1b[31m[Failed to start terminal: {ex.Message}]\x1b[0m\r\n";
            tab.IsConnected = false;
        }
    }
    
    private async Task ReadOutputAsync(TerminalTabViewModel tab, CancellationToken ct)
    {
        // In a real implementation, this would subscribe to the terminal output stream
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(100, ct);
            // Output is handled via events from TerminalService
        }
    }
    
    private void CloseTab(TerminalTabViewModel? tab)
    {
        if (tab == null) return;
        
        _terminalService.KillAsync(tab.Id);
        Tabs.Remove(tab);
        
        if (Tabs.Count == 0)
        {
            NewTab();
        }
        else if (ActiveTab == tab)
        {
            ActiveTab = Tabs.LastOrDefault();
        }
    }
    
    private void WriteToTerminal(string? command)
    {
        if (ActiveTab == null || string.IsNullOrEmpty(command)) return;
        
        _terminalService.WriteAsync(ActiveTab.Id, command + "\n");
        ActiveTab.CommandBuffer = command;
    }
    
    private void ClearTerminal()
    {
        ActiveTab?.Clear();
    }
    
    public void Dispose()
    {
        _readCts?.Cancel();
        _readCts?.Dispose();
        
        foreach (var tab in Tabs)
        {
            _terminalService.KillAsync(tab.Id);
        }
        Tabs.Clear();
    }
}

public partial class TerminalTabViewModel : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString();
    
    [ObservableProperty]
    private string _title = "Terminal";
    
    [ObservableProperty]
    private string _currentDirectory = "/";
    
    [ObservableProperty]
    private string _output = "";
    
    [ObservableProperty]
    private string _commandBuffer = "";
    
    [ObservableProperty]
    private bool _isConnected;
    
    [ObservableProperty]
    private bool _isProcessing;
    
    [ObservableProperty]
    private bool _isActive;
    
    public ICommand? CloseCommand { get; set; }
    
    public event EventHandler<string>? OnOutput;
    
    public void Clear()
    {
        Output = "";
    }
    
    public void AppendOutput(string text)
    {
        Output += text;
        OnOutput?.Invoke(this, text);
    }
}
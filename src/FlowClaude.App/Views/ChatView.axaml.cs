using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FlowClaude.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlowClaude.App.Views;

/// <summary>
/// Chat interface for interacting with the AI agent
/// </summary>
public partial class ChatView : UserControl
{
    private ChatViewModel _viewModel = null!;
    private ScrollViewer? _scrollViewer;

    public ChatView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _scrollViewer = this.FindControl<ScrollViewer>("MessagesScroll");
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Get ViewModel from DI
        if (DataContext is ChatViewModel vm)
        {
            _viewModel = vm;
        }
        else
        {
            var provider = Program.Services.BuildServiceProvider();
            _viewModel = provider.GetRequiredService<ChatViewModel>();
            DataContext = _viewModel;
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            _viewModel.SendMessageCommand.Execute(null);
        }
    }
}

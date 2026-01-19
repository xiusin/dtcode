using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FlowClaude.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlowClaude.App.Views;

/// <summary>
/// Integrated terminal view with xterm-style interface
/// </summary>
public partial class TerminalView : UserControl
{
    private TerminalViewModel _viewModel = null!;

    public TerminalView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TerminalViewModel vm)
        {
            _viewModel = vm;
        }
        else
        {
            var provider = Program.Services.BuildServiceProvider();
            _viewModel = provider.GetRequiredService<TerminalViewModel>();
            DataContext = _viewModel;
        }
    }

    private void OnCommandKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            var command = textBox.Text;
            if (!string.IsNullOrEmpty(command))
            {
                _viewModel.WriteCommand.Execute(command);
                textBox.Text = "";
            }
            e.Handled = true;
        }
    }
}

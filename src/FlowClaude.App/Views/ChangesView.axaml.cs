using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlowClaude.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlowClaude.App.Views;

/// <summary>
/// View showing git file changes and diffs
/// </summary>
public partial class ChangesView : UserControl
{
    private ChangesViewModel _viewModel = null!;

    public ChangesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, System.EventArgs e)
    {
        if (DataContext is ChangesViewModel vm)
        {
            _viewModel = vm;
        }
        else
        {
            var provider = Program.Services.BuildServiceProvider();
            _viewModel = provider.GetRequiredService<ChangesViewModel>();
            DataContext = _viewModel;
        }
    }
}
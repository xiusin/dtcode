using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FlowClaude.App.ViewModels;
using FlowClaude.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace FlowClaude.App.Views;

/// <summary>
/// Main window of the application with Fluent Design styling
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        // Set main window reference for dialogs
        Program.MainWindow = this;
        
        // Get services from DI container
        var provider = Program.Services.BuildServiceProvider();
        _viewModel = provider.GetRequiredService<MainWindowViewModel>();
        DataContext = _viewModel;
        
        InitializeComponent();
        
        // Setup window events
        Opened += (s, e) => OnOpened();
        Closing += (s, e) => OnClosing();
        
        // Handle key presses
        KeyDown += OnKeyDown;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Get references to window control buttons and attach handlers
        var minimizeBtn = this.FindControl<Button>("MinimizeButton");
        var maximizeBtn = this.FindControl<Button>("MaximizeButton");
        var closeBtn = this.FindControl<Button>("CloseButton");
        
        if (minimizeBtn != null) minimizeBtn.Click += OnMinimizeClick;
        if (maximizeBtn != null) maximizeBtn.Click += OnMaximizeClick;
        if (closeBtn != null) closeBtn.Click += OnCloseClick;
    }

    private void OnOpened()
    {
        // Apply theme
        ApplyTheme(_viewModel.IsDarkTheme);
    }

    private void OnClosing()
    {
        // Cleanup
    }

    private void OnTitleBarDoubleTap(object? sender, TappedEventArgs e)
    {
        // Double-click title bar to maximize
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Handle global shortcuts
        if (e.Key == Key.W && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            // Cmd+W - Close
            e.Handled = false;
        }
        else if (e.Key == Key.B && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            // Cmd+B - Toggle sidebar
            _viewModel.ToggleSidebarCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ApplyTheme(bool isDark)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = isDark
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
        }
    }
    
    private void OnProjectPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Core.Entities.Project project)
        {
            _viewModel.SelectProjectCommand.Execute(project);
        }
    }
    
    private void OnWorkspacePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Core.Entities.Workspace workspace)
        {
            _viewModel.SelectWorkspaceCommand.Execute(workspace);
        }
    }
}

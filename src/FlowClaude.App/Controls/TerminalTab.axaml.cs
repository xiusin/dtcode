using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace FlowClaude.App.Controls;

/// <summary>
/// Terminal tab item with close button
/// </summary>
public partial class TerminalTab : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<TerminalTab, string>(nameof(Title));

    public static readonly StyledProperty<string> CurrentDirectoryProperty =
        AvaloniaProperty.Register<TerminalTab, string>(nameof(CurrentDirectory));

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<TerminalTab, bool>(nameof(IsActive));

    public static readonly StyledProperty<bool> IsConnectedProperty =
        AvaloniaProperty.Register<TerminalTab, bool>(nameof(IsConnected));

    public static readonly AvaloniaProperty CloseCommandProperty =
        AvaloniaProperty.Register<TerminalTab, System.Windows.Input.ICommand>(nameof(CloseCommand));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string CurrentDirectory
    {
        get => GetValue(CurrentDirectoryProperty);
        set => SetValue(CurrentDirectoryProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool IsConnected
    {
        get => GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    public System.Windows.Input.ICommand? CloseCommand { get; set; }

    public TerminalTab()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Parent handles selection
    }
}

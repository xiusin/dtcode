using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace FlowClaude.App.Controls;

/// <summary>
/// Suggestion chip for quick prompts
/// </summary>
public partial class SuggestionChip : UserControl
{
    public static readonly StyledProperty<string> ContentProperty =
        AvaloniaProperty.Register<SuggestionChip, string>(nameof(Content));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<SuggestionChip, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<SuggestionChip, object?>(nameof(CommandParameter));

    public string Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public SuggestionChip()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Command?.Execute(CommandParameter ?? Content);
    }
}
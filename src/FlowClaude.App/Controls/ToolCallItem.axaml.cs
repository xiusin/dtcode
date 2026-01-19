using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlowClaude.Core.Entities;

namespace FlowClaude.App.Controls;

/// <summary>
/// Tool call execution indicator
/// </summary>
public partial class ToolCallItem : UserControl
{
    public static readonly StyledProperty<string> ToolNameProperty =
        AvaloniaProperty.Register<ToolCallItem, string>(nameof(ToolName));

    public static readonly StyledProperty<string?> InputProperty =
        AvaloniaProperty.Register<ToolCallItem, string?>(nameof(Input));

    public static readonly StyledProperty<string?> OutputProperty =
        AvaloniaProperty.Register<ToolCallItem, string?>(nameof(Output));

    public static readonly StyledProperty<ToolCallStatus> StatusProperty =
        AvaloniaProperty.Register<ToolCallItem, ToolCallStatus>(nameof(Status));

    public string ToolName
    {
        get => GetValue(ToolNameProperty);
        set => SetValue(ToolNameProperty, value);
    }

    public string? Input
    {
        get => GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public string? Output
    {
        get => GetValue(OutputProperty);
        set => SetValue(OutputProperty, value);
    }

    public ToolCallStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public ToolCallItem()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlowClaude.Core.Entities;

namespace FlowClaude.App.Controls;

/// <summary>
/// Chat message bubble with Fluent Design styling
/// </summary>
public partial class ChatMessageView : UserControl
{
    public static readonly StyledProperty<MessageRole> RoleProperty =
        AvaloniaProperty.Register<ChatMessageView, MessageRole>(nameof(Role));

    public static readonly StyledProperty<string> ContentProperty =
        AvaloniaProperty.Register<ChatMessageView, string>(nameof(Content));

    public static readonly StyledProperty<bool> IsStreamingProperty =
        AvaloniaProperty.Register<ChatMessageView, bool>(nameof(IsStreaming));

    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<ChatMessageView, bool>(nameof(IsError));

    public static readonly StyledProperty<DateTime> TimestampProperty =
        AvaloniaProperty.Register<ChatMessageView, DateTime>(nameof(Timestamp));

    public MessageRole Role
    {
        get => GetValue(RoleProperty);
        set => SetValue(RoleProperty, value);
    }

    public string Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public bool IsStreaming
    {
        get => GetValue(IsStreamingProperty);
        set => SetValue(IsStreamingProperty, value);
    }

    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    public DateTime Timestamp
    {
        get => GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }

    public ChatMessageView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FlowClaude.Core.Entities;
using System.Windows.Input;

namespace FlowClaude.App.Controls;

/// <summary>
/// File change list item with Fluent Design styling
/// </summary>
public partial class FileChangeItem : UserControl
{
    public static readonly StyledProperty<string> FilePathProperty =
        AvaloniaProperty.Register<FileChangeItem, string>(nameof(FilePath));

    public static readonly StyledProperty<ChangeStatus> StatusProperty =
        AvaloniaProperty.Register<FileChangeItem, ChangeStatus>(nameof(Status));

    public static readonly StyledProperty<string> StatusTextProperty =
        AvaloniaProperty.Register<FileChangeItem, string>(nameof(StatusText));

    public static readonly StyledProperty<object> StatusIconProperty =
        AvaloniaProperty.Register<FileChangeItem, object>(nameof(StatusIcon));

    public static readonly StyledProperty<int> AdditionsProperty =
        AvaloniaProperty.Register<FileChangeItem, int>(nameof(Additions));

    public static readonly StyledProperty<int> DeletionsProperty =
        AvaloniaProperty.Register<FileChangeItem, int>(nameof(Deletions));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<FileChangeItem, bool>(nameof(IsSelected));

    public static readonly StyledProperty<ICommand?> SelectCommandProperty =
        AvaloniaProperty.Register<FileChangeItem, ICommand?>(nameof(SelectCommand));

    public string FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public ChangeStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public object StatusIcon
    {
        get => GetValue(StatusIconProperty);
        set => SetValue(StatusIconProperty, value);
    }

    public int Additions
    {
        get => GetValue(AdditionsProperty);
        set => SetValue(AdditionsProperty, value);
    }

    public int Deletions
    {
        get => GetValue(DeletionsProperty);
        set => SetValue(DeletionsProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public bool HasChanges => Additions > 0 || Deletions > 0;

    public FileChangeItem()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SelectCommand?.Execute(this);
    }
}

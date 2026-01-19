using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FlowClaude.Core.Entities;

namespace FlowClaude.App.Controls;

/// <summary>
/// Workspace list item with Fluent Design styling
/// </summary>
public partial class WorkspaceItem : UserControl
{
    public static readonly StyledProperty<Workspace> WorkspaceProperty =
        AvaloniaProperty.Register<WorkspaceItem, Workspace>(nameof(Workspace));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<WorkspaceItem, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsPinnedProperty =
        AvaloniaProperty.Register<WorkspaceItem, bool>(nameof(IsPinned));
    
    public static readonly StyledProperty<bool> IsHoveredProperty =
        AvaloniaProperty.Register<WorkspaceItem, bool>(nameof(IsHovered));

    public static readonly StyledProperty<System.Windows.Input.ICommand?> SelectCommandProperty =
        AvaloniaProperty.Register<WorkspaceItem, System.Windows.Input.ICommand?>(nameof(SelectCommand));

    public static readonly StyledProperty<System.Windows.Input.ICommand?> ArchiveCommandProperty =
        AvaloniaProperty.Register<WorkspaceItem, System.Windows.Input.ICommand?>(nameof(ArchiveCommand));

    public Workspace Workspace
    {
        get => GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsPinned
    {
        get => GetValue(IsPinnedProperty);
        set => SetValue(IsPinnedProperty, value);
    }
    
    public bool IsHovered
    {
        get => GetValue(IsHoveredProperty);
        set => SetValue(IsHoveredProperty, value);
    }

    public System.Windows.Input.ICommand? SelectCommand
    {
        get => GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public System.Windows.Input.ICommand? ArchiveCommand
    {
        get => GetValue(ArchiveCommandProperty);
        set => SetValue(ArchiveCommandProperty, value);
    }

    public WorkspaceItem()
    {
        InitializeComponent();
        
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        IsHovered = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        IsHovered = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SelectCommand?.Execute(Workspace);
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        // Show context menu
        var menu = new ContextMenu();
        menu.Items.Add(new MenuItem { Header = "Rename", Command = null });
        menu.Items.Add(new MenuItem { Header = "Archive", Command = ArchiveCommand, CommandParameter = Workspace });
        menu.Open(this);
        e.Handled = true;
    }
}

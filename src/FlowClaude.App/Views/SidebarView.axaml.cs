using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlowClaude.App.Views;

/// <summary>
/// Sidebar navigation panel showing projects and workspaces
/// </summary>
public partial class SidebarView : UserControl
{
    public SidebarView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

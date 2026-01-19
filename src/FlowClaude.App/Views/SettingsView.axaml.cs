using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlowClaude.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FlowClaude.App.Views;

/// <summary>
/// Settings dialog view
/// </summary>
public partial class SettingsView : UserControl
{
    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<SettingsView, ICommand?>(nameof(CloseCommand));

    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public SettingsView()
    {
        InitializeComponent();
        
        // Get ViewModel
        var provider = Program.Services.BuildServiceProvider();
        if (provider.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel vm)
        {
            DataContext = vm;
            CloseCommand = vm.CloseSettingsCommand;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
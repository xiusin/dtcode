using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using FlowClaude.App.Views;
using System;

namespace FlowClaude.App;

/// <summary>
/// Application entry point
/// </summary>
public partial class App : Application
{
    public override void Initialize()
    {
        Console.WriteLine("App.Initialize called");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.WriteLine("App.OnFrameworkInitializationCompleted called");
        
        // Register styles
        Styles.Add(new FluentTheme());
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            Console.WriteLine("Desktop lifetime detected");
            var window = new MainWindow();
            desktopLifetime.MainWindow = window;
            desktopLifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
            Console.WriteLine("MainWindow shown");
        }
        else
        {
            Console.WriteLine("No desktop lifetime available");
        }

        base.OnFrameworkInitializationCompleted();
    }
}

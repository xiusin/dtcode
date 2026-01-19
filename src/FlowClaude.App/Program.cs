using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using FlowClaude.App.ViewModels;
using FlowClaude.App.Views;
using FlowClaude.Core.Interfaces;
using FlowClaude.Infrastructure.Database;
using FlowClaude.Infrastructure.Git;
using FlowClaude.Infrastructure.Terminal;
using FlowClaude.Agent;

namespace FlowClaude.App;

/// <summary>
/// Main application entry point
/// </summary>
public class Program
{
    public static IServiceCollection Services { get; private set; } = null!;
    private static IServiceProvider _serviceProvider = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("FlowClaude starting...");
        
        // Configure services
        ConfigureServices();
        
        Console.WriteLine("Services configured.");
        
        // Build application
        var app = BuildAvaloniaApp();
        Console.WriteLine("App built, starting...");
        app.StartWithClassicDesktopLifetime(args);
        
        Console.WriteLine("App started.");
    }

    private static void ConfigureServices()
    {
        Services = new ServiceCollection();
        
        // Database path - use local app data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = System.IO.Path.Combine(appDataPath, "FlowClaude", "flowclaude.db");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
        Services.AddSingleton(dbPath);
        
        // API Configuration - try to get from environment or config
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "";
        Services.AddSingleton(apiKey);
        
        // Add HttpClient with factory
        Services.AddHttpClient("ClaudeClient", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.DefaultRequestHeaders.Add("anthropic-dangerous-direct-browser-access", "true");
        });
        
        Services.AddSingleton<IAgentService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ClaudeClient");
            return new ClaudeAgentService(apiKey, null);
        });
        
        // ViewModels
        Services.AddSingleton<MainWindowViewModel>();
        Services.AddSingleton<ChatViewModel>();
        Services.AddSingleton<TerminalViewModel>();
        Services.AddSingleton<ChangesViewModel>();
        
        // Core Services
        Services.AddSingleton<IProjectRepository, SqliteProjectRepository>();
        Services.AddSingleton<IWorkspaceRepository, SqliteWorkspaceRepository>();
        Services.AddSingleton<IChatRepository, SqliteChatRepository>();
        Services.AddSingleton<IGitService, GitService>();
        Services.AddSingleton<ITerminalService, TerminalService>();
        
        _serviceProvider = Services.BuildServiceProvider();
    }

    private static void AppMain(Application app, string[] args)
    {
        app.Name = "FlowClaude";
        
        // Register styles
        app.Styles.Add(new FluentTheme());
        
        // The window will be created by App.OnFrameworkInitializationCompleted()
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect();
            
        // Try to detect OS and configure accordingly
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX))
        {
            // macOS configuration
            builder.UseAvaloniaNative();
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux))
        {
            // Linux configuration
            builder.UseX11();
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            // Windows configuration
            builder.UseWin32();
        }
        
        return builder
            .WithInterFont()
            .LogToTrace();
    }
}

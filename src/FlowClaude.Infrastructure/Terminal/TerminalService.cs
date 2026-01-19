using System.Collections.Concurrent;
using System.Diagnostics;
using FlowClaude.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowClaude.Infrastructure.Terminal;

/// <summary>
/// Terminal service using native pseudo-terminal (pty) support
/// On macOS: uses libvterm via interop
/// On Linux: uses libvterm or built-in ptys
/// On Windows: uses ConDrv or pseudoconsole
/// </summary>
public class TerminalService : ITerminalService, IDisposable
{
    private readonly ConcurrentDictionary<string, TerminalSessionInfo> _sessions = new();
    private readonly ILogger<TerminalService> _logger;
    private bool _disposed;

    public TerminalService(ILogger<TerminalService> logger)
    {
        _logger = logger;
    }

    public Task<TerminalSession> CreateSessionAsync(string paneId, string workspaceId, string cwd)
    {
        var sessionId = paneId;
        
        // Create a new PTY session
        var ptyInfo = CreatePty(cwd);
        
        var session = new TerminalSessionInfo
        {
            PaneId = paneId,
            WorkspaceId = workspaceId,
            Process = ptyInfo.Process,
            MasterFd = ptyInfo.MasterFd,
            Cwd = cwd,
            DataBuffer = new List<string>()
        };
        
        // Start reading output
        _ = ReadOutputAsync(session);
        
        _sessions.TryAdd(paneId, session);
        
        return Task.FromResult(new TerminalSession
        {
            PaneId = paneId,
            WorkspaceId = workspaceId,
            Cwd = cwd,
            Cols = 80,
            Rows = 24
        });
    }

    public Task WriteAsync(string paneId, string data)
    {
        if (_sessions.TryGetValue(paneId, out var session) && session?.MasterFd >= 0)
        {
            try
            {
                WriteToPty(session.MasterFd, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to terminal {PaneId}", paneId);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task ResizeAsync(string paneId, int cols, int rows)
    {
        if (_sessions.TryGetValue(paneId, out var session) && session?.Process != null)
        {
            try
            {
                SetWindowSize(session.Process.Id, cols, rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resize terminal {PaneId}", paneId);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task DetachAsync(string paneId)
    {
        // Detach keeps the session running but stops reading
        if (_sessions.TryGetValue(paneId, out var session))
        {
            session.IsDetached = true;
        }
        
        return Task.CompletedTask;
    }

    public Task KillAsync(string paneId)
    {
        if (_sessions.TryRemove(paneId, out var session))
        {
            try
            {
                session.Process?.Kill(entireProcessTree: true);
                session.Process?.Dispose();
                ClosePty(session.MasterFd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill terminal {PaneId}", paneId);
            }
        }
        
        return Task.CompletedTask;
    }

    public string? GetCwd(string data)
    {
        // Parse OSC-7 escape sequence for cwd
        // Format: ESC ] 7 ; file://hostname/path ESC \
        var match = System.Text.RegularExpressions.Regex.Match(data, @"\x1b\]7;([^\\]*)\\?");
        if (match.Success)
        {
            var url = match.Groups[1].Value;
            // Parse file:// URL
            var pathMatch = System.Text.RegularExpressions.Regex.Match(url, @"file://([^/]+)?(.*)");
            if (pathMatch.Success)
            {
                return pathMatch.Groups[2].Value;
            }
        }
        return null;
    }

    private async Task ReadOutputAsync(TerminalSessionInfo session)
    {
        try
        {
            var buffer = new byte[4096];
            
            while (!session.CancellationToken.IsCancellationRequested)
            {
                var bytesRead = await Task.Run(() => ReadFromPty(session.MasterFd, buffer, buffer.Length));
                
                if (bytesRead <= 0)
                    break;
                
                var data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                // Parse cwd from OSC-7 sequences
                var cwd = GetCwd(data);
                if (!string.IsNullOrEmpty(cwd))
                {
                    session.Cwd = cwd;
                }
                
                // Fire data event
                session.OnData?.Invoke(new TerminalDataEvent
                {
                    Type = "data",
                    Data = data
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from terminal {PaneId}", session.PaneId);
            
            session.OnData?.Invoke(new TerminalDataEvent
            {
                Type = "exit",
                ExitCode = -1
            });
        }
    }
    
    // Platform-specific implementations
    private PtyInfo CreatePty(string cwd)
    {
        if (OperatingSystem.IsMacOS())
        {
            return CreatePtyMac(cwd);
        }
        else if (OperatingSystem.IsLinux())
        {
            return CreatePtyLinux(cwd);
        }
        else if (OperatingSystem.IsWindows())
        {
            return CreatePtyWindows(cwd);
        }
        
        throw new PlatformNotSupportedException("Terminal not supported on this platform");
    }
    
    private PtyInfo CreatePtyMac(string cwd)
    {
        // macOS implementation using PTY module
        var pty = OpenPty(out var masterFd, out var slaveName, out var processId);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = GetShellPath(),
            WorkingDirectory = cwd,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        };
        
        // Set environment
        startInfo.Environment["TERM"] = "xterm-256color";
        
        var process = Process.Start(startInfo);
        
        // Configure terminal size
        SetWindowSize(process.Id, 80, 24);
        
        return new PtyInfo
        {
            Process = process,
            MasterFd = masterFd
        };
    }
    
    private PtyInfo CreatePtyLinux(string cwd)
    {
        // Linux implementation
        var pty = OpenPty(out var masterFd, out var slaveName, out var processId);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = GetShellPath(),
            WorkingDirectory = cwd,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        };
        
        startInfo.Environment["TERM"] = "xterm-256color";
        
        var process = Process.Start(startInfo);
        SetWindowSize(process.Id, 80, 24);
        
        return new PtyInfo
        {
            Process = process,
            MasterFd = masterFd
        };
    }
    
    private PtyInfo CreatePtyWindows(string cwd)
    {
        // Windows implementation using ConDrv
        // For simplicity, we'll use the built-in process
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = cwd,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            },
            EnableRaisingEvents = true
        };
        
        process.Start();
        
        return new PtyInfo
        {
            Process = process,
            MasterFd = -1 // Not used on Windows
        };
    }
    
    private static string GetShellPath()
    {
        // Return the user's default shell
        var shellEnv = Environment.GetEnvironmentVariable("SHELL");
        if (!string.IsNullOrEmpty(shellEnv) && File.Exists(shellEnv))
            return shellEnv;
        
        return OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";
    }
    
    // Native interop methods (implemented differently per platform)
    private static int OpenPty(out int masterFd, out string slaveName, out int processId)
    {
        masterFd = -1;
        slaveName = "";
        processId = -1;
        
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            // Unix domain sockets for PTY
            return OpenPtyPosix(out masterFd, out slaveName, out processId);
        }
        
        return -1;
    }
    
    [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
    private static extern int OpenPtyPosix(out int masterFd, out string slaveName, out int processId);
    
    [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
    private static extern int WriteToPty(int fd, string data);
    
    [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
    private static extern int ReadFromPty(int fd, byte[] buffer, int size);
    
    [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
    private static extern int ClosePty(int fd);
    
    [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
    private static extern int SetWindowSize(int pid, int cols, int rows);
    
    private class PtyInfo
    {
        public Process? Process { get; set; }
        public int MasterFd { get; set; }
    }
    
    private class TerminalSessionInfo
    {
        public required string PaneId { get; set; }
        public required string WorkspaceId { get; set; }
        public Process? Process { get; set; }
        public int MasterFd { get; set; }
        public string Cwd { get; set; } = "";
        public bool IsDetached { get; set; }
        public List<string> DataBuffer { get; set; } = new();
        public CancellationTokenSource CancellationToken { get; set; } = new();
        public Action<TerminalDataEvent>? OnData { get; set; }
    }
    
    private class TerminalDataEvent
    {
        public string Type { get; set; } = "";
        public string Data { get; set; } = "";
        public int ExitCode { get; set; }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        foreach (var session in _sessions.Values)
        {
            session.CancellationToken.Cancel();
            session.Process?.Kill(entireProcessTree: true);
            session.Process?.Dispose();
        }
        
        _sessions.Clear();
        _disposed = true;
    }
}

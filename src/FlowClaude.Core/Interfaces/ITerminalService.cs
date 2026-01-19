namespace FlowClaude.Core.Interfaces;

/// <summary>
/// Service interface for terminal operations
/// </summary>
public interface ITerminalService
{
    /// <summary>
    /// Create a new terminal session
    /// </summary>
    Task<TerminalSession> CreateSessionAsync(string paneId, string workspaceId, string cwd);
    
    /// <summary>
    /// Write data to a terminal session
    /// </summary>
    Task WriteAsync(string paneId, string data);
    
    /// <summary>
    /// Resize a terminal session
    /// </summary>
    Task ResizeAsync(string paneId, int cols, int rows);
    
    /// <summary>
    /// Detach from a session (keeps it running)
    /// </summary>
    Task DetachAsync(string paneId);
    
    /// <summary>
    /// Kill a terminal session
    /// </summary>
    Task KillAsync(string paneId);
    
    /// <summary>
    /// Get the current working directory from OSC-7 sequences
    /// </summary>
    string? GetCwd(string data);
}

public class TerminalSession
{
    public required string PaneId { get; init; }
    public required string WorkspaceId { get; init; }
    public required string Cwd { get; init; }
    public int Cols { get; init; }
    public int Rows { get; init; }
    public string? SerializedState { get; init; }
}

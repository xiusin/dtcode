namespace FlowClaude.Core.Entities;

/// <summary>
/// Represents a file change in the workspace
/// </summary>
public class GitFileChange
{
    public required string FilePath { get; init; }
    public ChangeStatus Status { get; init; }
    public string? OriginalContent { get; init; }
    public string? ModifiedContent { get; init; }
    public int? Additions { get; init; }
    public int? Deletions { get; init; }
}

public enum ChangeStatus
{
    Added,
    Modified,
    Deleted,
    Renamed,
    Untracked
}

/// <summary>
/// Represents a git commit in the worktree
/// </summary>
public class GitCommit
{
    public required string Hash { get; init; }
    public required string Message { get; init; }
    public required string Author { get; init; }
    public DateTime Date { get; init; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
}

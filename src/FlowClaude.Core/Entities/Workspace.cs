namespace FlowClaude.Core.Entities;

/// <summary>
/// Represents a workspace (chat session) that runs in an isolated git worktree
/// </summary>
public class Workspace
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? BranchName { get; init; }
    public string? WorktreePath { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    
    public WorkspaceStats? Stats { get; set; }
}

public class WorkspaceStats
{
    public int FileCount { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
}

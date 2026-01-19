using FlowClaude.Core.Entities;

namespace FlowClaude.Core.Interfaces;

/// <summary>
/// Service interface for git operations
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Get the root directory of a git repository
    /// </summary>
    Task<string> GetGitRootAsync(string path);
    
    /// <summary>
    /// Check if a directory is a git repository
    /// </summary>
    Task<bool> IsGitRepositoryAsync(string path);
    
    /// <summary>
    /// Create a new worktree for isolated development
    /// </summary>
    Task<WorktreeResult> CreateWorktreeAsync(string mainRepoPath, string branchName, string worktreePath, string? startPoint = null);
    
    /// <summary>
    /// Remove a worktree
    /// </summary>
    Task<bool> RemoveWorktreeAsync(string mainRepoPath, string worktreePath);
    
    /// <summary>
    /// Check if a worktree exists
    /// </summary>
    Task<bool> WorktreeExistsAsync(string mainRepoPath, string worktreePath);
    
    /// <summary>
    /// Get the current branch name
    /// </summary>
    Task<string?> GetCurrentBranchAsync(string repoPath);
    
    /// <summary>
    /// Get the default branch name (main/master)
    /// </summary>
    Task<string> GetDefaultBranchAsync(string repoPath);
    
    /// <summary>
    /// Get all file changes in the repository
    /// </summary>
    Task<IEnumerable<GitFileChange>> GetFileChangesAsync(string repoPath);
    
    /// <summary>
    /// Get diff between current changes and HEAD
    /// </summary>
    Task<string> GetDiffAsync(string repoPath, string? baseBranch = null);
    
    /// <summary>
    /// Stage and commit all changes
    /// </summary>
    Task<CommitResult> CommitChangesAsync(string repoPath, string message);
    
    /// <summary>
    /// Get git status summary
    /// </summary>
    Task<GitStatus> GetStatusAsync(string repoPath);
    
    /// <summary>
    /// Detect git remote info (owner, provider, repo)
    /// </summary>
    Task<GitRemoteInfo?> DetectRemoteInfoAsync(string repoPath);
}

public class WorktreeResult
{
    public bool Success { get; set; }
    public string? WorktreePath { get; set; }
    public string? Branch { get; set; }
    public string? Error { get; set; }
}

public class CommitResult
{
    public bool Success { get; set; }
    public string? CommitHash { get; set; }
    public string? Error { get; set; }
}

public class GitStatus
{
    public bool HasUncommittedChanges { get; set; }
    public bool HasUnpushedCommits { get; set; }
    public string? CurrentBranch { get; set; }
}

public class GitRemoteInfo
{
    public string? Owner { get; set; }
    public string? Provider { get; set; }
    public string? Repo { get; set; }
}

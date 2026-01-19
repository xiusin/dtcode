using System.Diagnostics;
using System.Text.RegularExpressions;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;

namespace FlowClaude.Infrastructure.Git;

/// <summary>
/// Git operations service using git command line
/// </summary>
public class GitService : IGitService
{
    private static readonly string[] DefaultBranchNames = ["main", "master", "develop", "trunk"];
    
    public async Task<string> GetGitRootAsync(string path)
    {
        await Task.Yield();
        
        var result = RunGitCommand(path, "rev-parse", "--show-toplevel");
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Not a git repository: {path}");
        
        return result.StandardOutput.Trim();
    }

    public async Task<bool> IsGitRepositoryAsync(string path)
    {
        await Task.Yield();
        
        var result = RunGitCommand(path, "rev-parse", "--is-inside-work-tree");
        return result.ExitCode == 0;
    }

    public async Task<WorktreeResult> CreateWorktreeAsync(string mainRepoPath, string branchName, string worktreePath, string? startPoint = null)
    {
        await Task.Yield();
        
        try
        {
            // Ensure parent directory exists
            var parentDir = Path.GetDirectoryName(worktreePath);
            if (!string.IsNullOrEmpty(parentDir))
                Directory.CreateDirectory(parentDir);
            
            // Determine the commit to base on
            string commitRef = startPoint ?? "HEAD";
            
            // Create worktree using git command
            var result = RunGitCommand(mainRepoPath, "worktree", "add", worktreePath, "-B", branchName, commitRef);
            
            if (result.ExitCode != 0)
            {
                return new WorktreeResult
                {
                    Success = false,
                    Error = result.StandardError
                };
            }
            
            return new WorktreeResult
            {
                Success = true,
                WorktreePath = worktreePath,
                Branch = branchName
            };
        }
        catch (Exception ex)
        {
            return new WorktreeResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<bool> RemoveWorktreeAsync(string mainRepoPath, string worktreePath)
    {
        await Task.Yield();
        
        try
        {
            var result = RunGitCommand(mainRepoPath, "worktree", "remove", worktreePath, "--force");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> WorktreeExistsAsync(string mainRepoPath, string worktreePath)
    {
        await Task.Yield();
        
        try
        {
            var result = RunGitCommand(mainRepoPath, "worktree", "list", "--porcelain");
            return result.StandardOutput.Contains(worktreePath);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetCurrentBranchAsync(string repoPath)
    {
        await Task.Yield();
        
        try
        {
            var result = RunGitCommand(repoPath, "rev-parse", "--abbrev-ref", "HEAD");
            return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> GetDefaultBranchAsync(string repoPath)
    {
        await Task.Yield();
        
        try
        {
            // Check for origin/HEAD symref
            var headRef = RunGitCommand(repoPath, "symbolic-ref", "refs/remotes/origin/HEAD");
            var match = Regex.Match(headRef.StandardOutput, @"refs/remotes/origin/(.+)");
            if (match.Success)
                return match.Groups[1].Value;
            
            // Check local branches
            var branchesResult = RunGitCommand(repoPath, "branch", "--list");
            var localBranches = branchesResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim().Replace("* ", ""))
                .Where(b => !string.IsNullOrEmpty(b))
                .ToList();
            
            foreach (var candidate in DefaultBranchNames)
            {
                if (localBranches.Contains(candidate))
                    return candidate;
            }
            
            // Return first local branch if no default found
            return localBranches.FirstOrDefault() ?? "main";
        }
        catch
        {
            return "main";
        }
    }

    public async Task<IEnumerable<GitFileChange>> GetFileChangesAsync(string repoPath)
    {
        await Task.Yield();
        
        var changes = new List<GitFileChange>();
        
        try
        {
            // Get status with short format
            var statusResult = RunGitCommand(repoPath, "status", "--porcelain", "-z");
            var statusLines = statusResult.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in statusLines)
            {
                if (line.Length < 4) continue;
                
                var statusCode = line.Substring(0, 2);
                var filePath = line.Substring(3);
                
                var change = new GitFileChange
                {
                    FilePath = filePath,
                    Status = MapStatusCode(statusCode)
                };
                
                changes.Add(change);
            }
            
            // Get diff stats for modified files
            var diffResult = RunGitCommand(repoPath, "diff", "--numstat", "--pretty=format:");
            var diffLines = diffResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var diffLine in diffLines)
            {
                var parts = diffLine.Split('\t');
                if (parts.Length == 3 && int.TryParse(parts[0], out var additions) && int.TryParse(parts[1], out var deletions))
                {
                    var filePath = parts[2];
                    var existingChange = changes.FirstOrDefault(c => c.FilePath == filePath);
                    if (existingChange != null)
                    {
                        // Create new GitFileChange with updated stats since properties are init-only
                        var index = changes.IndexOf(existingChange);
                        changes[index] = new GitFileChange
                        {
                            FilePath = existingChange.FilePath,
                            Status = existingChange.Status,
                            Additions = additions,
                            Deletions = deletions
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting file changes: {ex.Message}");
        }
        
        return changes;
    }

    public async Task<string> GetDiffAsync(string repoPath, string? baseBranch = null)
    {
        await Task.Yield();
        
        try
        {
            // Get uncommitted changes
            var diffResult = RunGitCommand(repoPath, "diff");
            
            // If clean, compare with base branch
            if (string.IsNullOrWhiteSpace(diffResult.StandardOutput) && !string.IsNullOrEmpty(baseBranch))
            {
                var branchRef = baseBranch.StartsWith("origin/") ? baseBranch : $"origin/{baseBranch}";
                diffResult = RunGitCommand(repoPath, "diff", branchRef);
            }
            
            return diffResult.StandardOutput;
        }
        catch (Exception ex)
        {
            return $"Error getting diff: {ex.Message}";
        }
    }

    public async Task<CommitResult> CommitChangesAsync(string repoPath, string message)
    {
        await Task.Yield();
        
        try
        {
            // Stage all changes
            RunGitCommand(repoPath, "add", ".");
            
            // Create commit
            var result = RunGitCommand(repoPath, "commit", "-m", message);
            
            if (result.ExitCode != 0)
            {
                return new CommitResult
                {
                    Success = false,
                    Error = result.StandardError
                };
            }
            
            // Get commit hash
            var hashResult = RunGitCommand(repoPath, "rev-parse", "HEAD");
            
            return new CommitResult
            {
                Success = true,
                CommitHash = hashResult.StandardOutput.Trim()
            };
        }
        catch (Exception ex)
        {
            return new CommitResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<GitStatus> GetStatusAsync(string repoPath)
    {
        await Task.Yield();
        
        try
        {
            var statusResult = RunGitCommand(repoPath, "status", "--porcelain");
            var hasUncommittedChanges = !string.IsNullOrWhiteSpace(statusResult.StandardOutput);
            
            var branchResult = RunGitCommand(repoPath, "rev-parse", "--abbrev-ref", "HEAD");
            var currentBranch = branchResult.StandardOutput.Trim();
            
            // Check for unpushed commits
            var remoteResult = RunGitCommand(repoPath, "rev-list", "@{u}..HEAD");
            var hasUnpushedCommits = !string.IsNullOrWhiteSpace(remoteResult.StandardOutput);
            
            return new GitStatus
            {
                HasUncommittedChanges = hasUncommittedChanges,
                HasUnpushedCommits = hasUnpushedCommits,
                CurrentBranch = currentBranch
            };
        }
        catch (Exception ex)
        {
            return new GitStatus
            {
                HasUncommittedChanges = false,
                HasUnpushedCommits = false,
                CurrentBranch = null
            };
        }
    }

    public async Task<GitRemoteInfo?> DetectRemoteInfoAsync(string repoPath)
    {
        await Task.Yield();
        
        try
        {
            var remoteResult = RunGitCommand(repoPath, "remote", "get-url", "origin");
            if (remoteResult.ExitCode != 0)
                return null;
            
            var url = remoteResult.StandardOutput.Trim();
            
            // Parse GitHub URL
            var githubMatch = Regex.Match(url, @"github\.com[:/]([^/]+)/([^/]+?)(?:\.git)?$");
            if (githubMatch.Success)
            {
                return new GitRemoteInfo
                {
                    Owner = githubMatch.Groups[1].Value,
                    Provider = "github",
                    Repo = githubMatch.Groups[2].Value.Replace(".git", "")
                };
            }
            
            // Parse other Git URLs
            var match = Regex.Match(url, @"[:/]([^/]+)/([^/]+?)(?:\.git)?$");
            if (match.Success)
            {
                return new GitRemoteInfo
                {
                    Owner = match.Groups[1].Value,
                    Provider = "unknown",
                    Repo = match.Groups[2].Value.Replace(".git", "")
                };
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    private static ChangeStatus MapStatusCode(string statusCode)
    {
        // XY format where X is staged, Y is working dir
        var staged = statusCode[0];
        var working = statusCode[1];
        
        if (staged == 'A' || working == 'A') return ChangeStatus.Added;
        if (staged == 'M' || working == 'M') return ChangeStatus.Modified;
        if (staged == 'D' || working == 'D') return ChangeStatus.Deleted;
        if (staged == 'R' || working == 'R') return ChangeStatus.Renamed;
        if (staged == '?' || working == '?') return ChangeStatus.Untracked;
        
        return ChangeStatus.Modified;
    }
    
    private static CommandResult RunGitCommand(string workingDir, params string[] args)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = string.Join(" ", args),
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(processInfo);
        var output = process?.StandardOutput.ReadToEnd() ?? "";
        var error = process?.StandardError.ReadToEnd() ?? "";
        process?.WaitForExit();
        
        return new CommandResult
        {
            ExitCode = process?.ExitCode ?? -1,
            StandardOutput = output,
            StandardError = error
        };
    }
    
    private class CommandResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = "";
        public string StandardError { get; set; } = "";
    }
}
using Dapper;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace FlowClaude.Infrastructure.Database;

/// <summary>
/// SQLite repository implementation for workspace data
/// </summary>
public class SqliteWorkspaceRepository : IWorkspaceRepository
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public SqliteWorkspaceRepository(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        var createWorkspaces = @"
            CREATE TABLE IF NOT EXISTS Workspaces (
                Id TEXT PRIMARY KEY,
                ProjectId TEXT NOT NULL,
                Name TEXT NOT NULL,
                BranchName TEXT,
                WorktreePath TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsPinned INTEGER DEFAULT 0,
                IsArchived INTEGER DEFAULT 0,
                Stats_FileCount INTEGER DEFAULT 0,
                Stats_Additions INTEGER DEFAULT 0,
                Stats_Deletions INTEGER DEFAULT 0,
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id)
            )";
        _connection.Execute(createWorkspaces);
    }

    public Task<Workspace?> GetByIdAsync(Guid id)
    {
        var workspace = _connection.QueryFirstOrDefault<WorkspaceDb>(
            "SELECT * FROM Workspaces WHERE Id = @Id", new { Id = id.ToString() });
        return Task.FromResult(workspace?.ToEntity());
    }

    public Task<IEnumerable<Workspace>> GetByProjectIdAsync(Guid projectId)
    {
        var workspaces = _connection.Query<WorkspaceDb>(
            "SELECT * FROM Workspaces WHERE ProjectId = @ProjectId AND IsArchived = 0 ORDER BY UpdatedAt DESC",
            new { ProjectId = projectId.ToString() });
        return Task.FromResult(workspaces.Select(w => w.ToEntity()));
    }

    public Task<IEnumerable<Workspace>> GetPinnedAsync()
    {
        var workspaces = _connection.Query<WorkspaceDb>(
            "SELECT * FROM Workspaces WHERE IsPinned = 1 AND IsArchived = 0 ORDER BY UpdatedAt DESC");
        return Task.FromResult(workspaces.Select(w => w.ToEntity()));
    }

    public Task<IEnumerable<Workspace>> GetArchivedAsync()
    {
        var workspaces = _connection.Query<WorkspaceDb>(
            "SELECT * FROM Workspaces WHERE IsArchived = 1 ORDER BY UpdatedAt DESC");
        return Task.FromResult(workspaces.Select(w => w.ToEntity()));
    }

    public Task<Workspace> CreateAsync(Workspace workspace)
    {
        var db = WorkspaceDb.FromEntity(workspace);
        _connection.Execute(@"
            INSERT INTO Workspaces (Id, ProjectId, Name, BranchName, WorktreePath, CreatedAt, UpdatedAt, IsPinned, IsArchived)
            VALUES (@Id, @ProjectId, @Name, @BranchName, @WorktreePath, @CreatedAt, @UpdatedAt, @IsPinned, @IsArchived)",
            db);
        return Task.FromResult(workspace);
    }

    public Task<Workspace> UpdateAsync(Workspace workspace)
    {
        workspace.UpdatedAt = DateTime.UtcNow;
        var db = WorkspaceDb.FromEntity(workspace);
        _connection.Execute(@"
            UPDATE Workspaces SET Name = @Name, BranchName = @BranchName, WorktreePath = @WorktreePath,
                UpdatedAt = @UpdatedAt, IsPinned = @IsPinned, IsArchived = @IsArchived,
                Stats_FileCount = @Stats_FileCount, Stats_Additions = @Stats_Additions, Stats_Deletions = @Stats_Deletions
            WHERE Id = @Id", db);
        return Task.FromResult(workspace);
    }

    public Task DeleteAsync(Guid id)
    {
        _connection.Execute("DELETE FROM Workspaces WHERE Id = @Id", new { Id = id.ToString() });
        return Task.CompletedTask;
    }

    public Task ArchiveAsync(Guid id)
    {
        _connection.Execute("UPDATE Workspaces SET IsArchived = 1, UpdatedAt = @Now WHERE Id = @Id",
            new { Id = id.ToString(), Now = DateTime.UtcNow.ToString("o") });
        return Task.CompletedTask;
    }

    public Task UnarchiveAsync(Guid id)
    {
        _connection.Execute("UPDATE Workspaces SET IsArchived = 0, UpdatedAt = @Now WHERE Id = @Id",
            new { Id = id.ToString(), Now = DateTime.UtcNow.ToString("o") });
        return Task.CompletedTask;
    }

    public Task TogglePinAsync(Guid id)
    {
        _connection.Execute("UPDATE Workspaces SET IsPinned = NOT IsPinned, UpdatedAt = @Now WHERE Id = @Id",
            new { Id = id.ToString(), Now = DateTime.UtcNow.ToString("o") });
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }

    private class WorkspaceDb
    {
        public string Id { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public string Name { get; set; } = "";
        public string? BranchName { get; set; }
        public string? WorktreePath { get; set; }
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
        public int IsPinned { get; set; }
        public int IsArchived { get; set; }
        public int Stats_FileCount { get; set; }
        public int Stats_Additions { get; set; }
        public int Stats_Deletions { get; set; }

        public Workspace ToEntity() => new()
        {
            Id = Guid.Parse(Id),
            ProjectId = Guid.Parse(ProjectId),
            Name = Name,
            BranchName = BranchName,
            WorktreePath = WorktreePath,
            CreatedAt = DateTime.Parse(CreatedAt),
            UpdatedAt = DateTime.Parse(UpdatedAt),
            IsPinned = IsPinned != 0,
            IsArchived = IsArchived != 0,
            Stats = new WorkspaceStats
            {
                FileCount = Stats_FileCount,
                Additions = Stats_Additions,
                Deletions = Stats_Deletions
            }
        };

        public static WorkspaceDb FromEntity(Workspace workspace) => new()
        {
            Id = workspace.Id.ToString(),
            ProjectId = workspace.ProjectId.ToString(),
            Name = workspace.Name,
            BranchName = workspace.BranchName,
            WorktreePath = workspace.WorktreePath,
            CreatedAt = workspace.CreatedAt.ToString("o"),
            UpdatedAt = workspace.UpdatedAt.ToString("o"),
            IsPinned = workspace.IsPinned ? 1 : 0,
            IsArchived = workspace.IsArchived ? 1 : 0,
            Stats_FileCount = workspace.Stats?.FileCount ?? 0,
            Stats_Additions = workspace.Stats?.Additions ?? 0,
            Stats_Deletions = workspace.Stats?.Deletions ?? 0
        };
    }
}

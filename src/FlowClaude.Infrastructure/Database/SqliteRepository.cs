using Dapper;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace FlowClaude.Infrastructure.Database;

/// <summary>
/// SQLite repository implementation for local-first data storage
/// </summary>
public class SqliteRepository : 
    IProjectRepository, 
    IWorkspaceRepository, 
    IChatRepository,
    IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public SqliteRepository(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        // Create tables
        var createProjects = @"
            CREATE TABLE IF NOT EXISTS Projects (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Path TEXT NOT NULL UNIQUE,
                GitOwner TEXT,
                GitProvider TEXT,
                GitRepo TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )";
            
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
                Stats_FileCount INTEGER,
                Stats_Additions INTEGER,
                Stats_Deletions INTEGER,
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id)
            )";
            
        var createChats = @"
            CREATE TABLE IF NOT EXISTS Chats (
                Id TEXT PRIMARY KEY,
                WorkspaceId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Mode INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                HasUnseenChanges INTEGER DEFAULT 0,
                HasPendingPlan INTEGER DEFAULT 0,
                FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id)
            )";
            
        var createMessages = @"
            CREATE TABLE IF NOT EXISTS Messages (
                Id TEXT PRIMARY KEY,
                ChatId TEXT NOT NULL,
                Role INTEGER NOT NULL,
                Content TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (ChatId) REFERENCES Chats(Id)
            )";
            
        var createToolCalls = @"
            CREATE TABLE IF NOT EXISTS ToolCalls (
                Id TEXT PRIMARY KEY,
                MessageId TEXT NOT NULL,
                ToolName TEXT NOT NULL,
                Input TEXT,
                Output TEXT,
                Status INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (MessageId) REFERENCES Messages(Id)
            )";
            
        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_workspaces_project ON Workspaces(ProjectId);
            CREATE INDEX IF NOT EXISTS idx_chats_workspace ON Chats(WorkspaceId);
            CREATE INDEX IF NOT EXISTS idx_messages_chat ON Messages(ChatId);
            CREATE INDEX IF NOT EXISTS idx_toolcalls_message ON ToolCalls(MessageId)";
        
        _connection.Execute(createProjects);
        _connection.Execute(createWorkspaces);
        _connection.Execute(createChats);
        _connection.Execute(createMessages);
        _connection.Execute(createToolCalls);
        _connection.Execute(createIndexes);
    }

    #region IProjectRepository
    
    Task<Project?> IProjectRepository.GetByIdAsync(Guid id)
    {
        var project = _connection.QueryFirstOrDefault<ProjectDb>(
            "SELECT * FROM Projects WHERE Id = @Id", new { Id = id.ToString() });
        return Task.FromResult(project?.ToEntity());
    }

    public Task<Project?> GetByPathAsync(string path)
    {
        var project = _connection.QueryFirstOrDefault<ProjectDb>(
            "SELECT * FROM Projects WHERE Path = @Path", new { Path = path });
        return Task.FromResult(project?.ToEntity());
    }

    public Task<IEnumerable<Project>> GetAllAsync()
    {
        var projects = _connection.Query<ProjectDb>("SELECT * FROM Projects ORDER BY UpdatedAt DESC");
        return Task.FromResult(projects.Select(p => p.ToEntity()));
    }

    public Task<Project> CreateAsync(Project project)
    {
        var db = ProjectDb.FromEntity(project);
        _connection.Execute(@"
            INSERT INTO Projects (Id, Name, Path, GitOwner, GitProvider, GitRepo, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @Path, @GitOwner, @GitProvider, @GitRepo, @CreatedAt, @UpdatedAt)",
            db);
        return Task.FromResult(project);
    }

    public Task<Project> UpdateAsync(Project project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        var db = ProjectDb.FromEntity(project);
        _connection.Execute(@"
            UPDATE Projects SET Name = @Name, Path = @Path, GitOwner = @GitOwner, 
                GitProvider = @GitProvider, GitRepo = @GitRepo, UpdatedAt = @UpdatedAt
            WHERE Id = @Id", db);
        return Task.FromResult(project);
    }

    Task IProjectRepository.DeleteAsync(Guid id)
    {
        _connection.Execute("DELETE FROM Projects WHERE Id = @Id", new { Id = id.ToString() });
        return Task.CompletedTask;
    }

    #endregion

    #region IWorkspaceRepository

    Task<Workspace?> IWorkspaceRepository.GetByIdAsync(Guid id)
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

    Task IWorkspaceRepository.DeleteAsync(Guid id)
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

    #endregion

    #region IChatRepository

    Task<Chat?> IChatRepository.GetByIdAsync(Guid id)
    {
        var chat = _connection.QueryFirstOrDefault<ChatDb>(
            "SELECT * FROM Chats WHERE Id = @Id", new { Id = id.ToString() });
        return Task.FromResult(chat?.ToEntity());
    }

    public Task<IEnumerable<Chat>> GetByWorkspaceIdAsync(Guid workspaceId)
    {
        var chats = _connection.Query<ChatDb>(
            "SELECT * FROM Chats WHERE WorkspaceId = @WorkspaceId ORDER BY UpdatedAt DESC",
            new { WorkspaceId = workspaceId.ToString() });
        return Task.FromResult(chats.Select(c => c.ToEntity()));
    }

    public Task<Chat> CreateAsync(Chat chat)
    {
        var db = ChatDb.FromEntity(chat);
        _connection.Execute(@"
            INSERT INTO Chats (Id, WorkspaceId, Title, Mode, CreatedAt, UpdatedAt, HasUnseenChanges, HasPendingPlan)
            VALUES (@Id, @WorkspaceId, @Title, @Mode, @CreatedAt, @UpdatedAt, @HasUnseenChanges, @HasPendingPlan)",
            db);
        return Task.FromResult(chat);
    }

    public Task<Chat> UpdateAsync(Chat chat)
    {
        chat.UpdatedAt = DateTime.UtcNow;
        var db = ChatDb.FromEntity(chat);
        _connection.Execute(@"
            UPDATE Chats SET Title = @Title, Mode = @Mode, UpdatedAt = @UpdatedAt,
                HasUnseenChanges = @HasUnseenChanges, HasPendingPlan = @HasPendingPlan
            WHERE Id = @Id", db);
        return Task.FromResult(chat);
    }

    public Task AddMessageAsync(Guid chatId, ChatMessage message)
    {
        var db = MessageDb.FromEntity(message);
        _connection.Execute(@"
            INSERT INTO Messages (Id, ChatId, Role, Content, CreatedAt)
            VALUES (@Id, @ChatId, @Role, @Content, @CreatedAt)",
            db);
        return Task.CompletedTask;
    }

    Task IChatRepository.DeleteAsync(Guid id)
    {
        _connection.Execute("DELETE FROM Chats WHERE Id = @Id", new { Id = id.ToString() });
        return Task.CompletedTask;
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _connection.Dispose();
        _disposed = true;
    }

    // Database model classes
    private class ProjectDb
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? GitOwner { get; set; }
        public string? GitProvider { get; set; }
        public string? GitRepo { get; set; }
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";

        public Project ToEntity() => new()
        {
            Id = Guid.Parse(Id),
            Name = Name,
            Path = Path,
            GitOwner = GitOwner,
            GitProvider = GitProvider,
            GitRepo = GitRepo,
            CreatedAt = DateTime.Parse(CreatedAt),
            UpdatedAt = DateTime.Parse(UpdatedAt)
        };

        public static ProjectDb FromEntity(Project p) => new()
        {
            Id = p.Id.ToString(),
            Name = p.Name,
            Path = p.Path,
            GitOwner = p.GitOwner,
            GitProvider = p.GitProvider,
            GitRepo = p.GitRepo,
            CreatedAt = p.CreatedAt.ToString("o"),
            UpdatedAt = p.UpdatedAt.ToString("o")
        };
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
        public int? Stats_FileCount { get; set; }
        public int? Stats_Additions { get; set; }
        public int? Stats_Deletions { get; set; }

        public Workspace ToEntity() => new()
        {
            Id = Guid.Parse(Id),
            ProjectId = Guid.Parse(ProjectId),
            Name = Name,
            BranchName = BranchName,
            WorktreePath = WorktreePath,
            CreatedAt = DateTime.Parse(CreatedAt),
            UpdatedAt = DateTime.Parse(UpdatedAt),
            IsPinned = IsPinned == 1,
            IsArchived = IsArchived == 1,
            Stats = Stats_FileCount.HasValue ? new WorkspaceStats
            {
                FileCount = Stats_FileCount.Value,
                Additions = Stats_Additions ?? 0,
                Deletions = Stats_Deletions ?? 0
            } : null
        };

        public static WorkspaceDb FromEntity(Workspace w) => new()
        {
            Id = w.Id.ToString(),
            ProjectId = w.ProjectId.ToString(),
            Name = w.Name,
            BranchName = w.BranchName,
            WorktreePath = w.WorktreePath,
            CreatedAt = w.CreatedAt.ToString("o"),
            UpdatedAt = w.UpdatedAt.ToString("o"),
            IsPinned = w.IsPinned ? 1 : 0,
            IsArchived = w.IsArchived ? 1 : 0,
            Stats_FileCount = w.Stats?.FileCount,
            Stats_Additions = w.Stats?.Additions,
            Stats_Deletions = w.Stats?.Deletions
        };
    }

    private class ChatDb
    {
        public string Id { get; set; } = "";
        public string WorkspaceId { get; set; } = "";
        public string Title { get; set; } = "";
        public int Mode { get; set; }
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
        public int HasUnseenChanges { get; set; }
        public int HasPendingPlan { get; set; }

        public Chat ToEntity() => new()
        {
            Id = Guid.Parse(Id),
            WorkspaceId = Guid.Parse(WorkspaceId),
            Title = Title,
            Mode = (ChatMode)Mode,
            CreatedAt = DateTime.Parse(CreatedAt),
            UpdatedAt = DateTime.Parse(UpdatedAt),
            HasUnseenChanges = HasUnseenChanges == 1,
            HasPendingPlan = HasPendingPlan == 1
        };

        public static ChatDb FromEntity(Chat c) => new()
        {
            Id = c.Id.ToString(),
            WorkspaceId = c.WorkspaceId.ToString(),
            Title = c.Title,
            Mode = (int)c.Mode,
            CreatedAt = c.CreatedAt.ToString("o"),
            UpdatedAt = c.UpdatedAt.ToString("o"),
            HasUnseenChanges = c.HasUnseenChanges ? 1 : 0,
            HasPendingPlan = c.HasPendingPlan ? 1 : 0
        };
    }

    private class MessageDb
    {
        public string Id { get; set; } = "";
        public string ChatId { get; set; } = "";
        public int Role { get; set; }
        public string Content { get; set; } = "";
        public string CreatedAt { get; set; } = "";

        public ChatMessage ToEntity() => new()
        {
            Id = Guid.Parse(Id),
            ChatId = Guid.Parse(ChatId),
            Role = (MessageRole)Role,
            Content = Content,
            CreatedAt = DateTime.Parse(CreatedAt)
        };

        public static MessageDb FromEntity(ChatMessage m) => new()
        {
            Id = m.Id.ToString(),
            ChatId = m.ChatId.ToString(),
            Role = (int)m.Role,
            Content = m.Content,
            CreatedAt = m.CreatedAt.ToString("o")
        };
    }
}
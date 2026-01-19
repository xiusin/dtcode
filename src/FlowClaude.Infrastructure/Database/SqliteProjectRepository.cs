using Dapper;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace FlowClaude.Infrastructure.Database;

/// <summary>
/// SQLite repository implementation for project data
/// </summary>
public class SqliteProjectRepository : IProjectRepository
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public SqliteProjectRepository(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
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
        _connection.Execute(createProjects);
    }

    public Task<Project?> GetByIdAsync(Guid id)
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

    public Task DeleteAsync(Guid id)
    {
        _connection.Execute("DELETE FROM Projects WHERE Id = @Id", new { Id = id.ToString() });
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

        public static ProjectDb FromEntity(Project project) => new()
        {
            Id = project.Id.ToString(),
            Name = project.Name,
            Path = project.Path,
            GitOwner = project.GitOwner,
            GitProvider = project.GitProvider,
            GitRepo = project.GitRepo,
            CreatedAt = project.CreatedAt.ToString("o"),
            UpdatedAt = project.UpdatedAt.ToString("o")
        };
    }
}

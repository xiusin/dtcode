using Dapper;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace FlowClaude.Infrastructure.Database;

/// <summary>
/// SQLite repository implementation for chat data
/// </summary>
public class SqliteChatRepository : IChatRepository
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public SqliteChatRepository(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        var createChats = @"
            CREATE TABLE IF NOT EXISTS Chats (
                Id TEXT PRIMARY KEY,
                WorkspaceId TEXT NOT NULL,
                Name TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id)
            )";
            
        var createMessages = @"
            CREATE TABLE IF NOT EXISTS ChatMessages (
                Id TEXT PRIMARY KEY,
                ChatId TEXT NOT NULL,
                Role TEXT NOT NULL,
                Content TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Tokens INTEGER DEFAULT 0,
                FOREIGN KEY (ChatId) REFERENCES Chats(Id)
            )";
        _connection.Execute(createChats);
        _connection.Execute(createMessages);
    }

    public Task<Chat?> GetByIdAsync(Guid id)
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
            INSERT INTO Chats (Id, WorkspaceId, Title, CreatedAt, UpdatedAt)
            VALUES (@Id, @WorkspaceId, @Title, @CreatedAt, @UpdatedAt)",
            db);
        return Task.FromResult(chat);
    }

    public Task<Chat> UpdateAsync(Chat chat)
    {
        chat.UpdatedAt = DateTime.UtcNow;
        var db = ChatDb.FromEntity(chat);
        _connection.Execute(@"
            UPDATE Chats SET Title = @Title, UpdatedAt = @UpdatedAt
            WHERE Id = @Id", db);
        return Task.FromResult(chat);
    }

    public Task AddMessageAsync(Guid chatId, ChatMessage message)
    {
        var msgDb = new MessageDb
        {
            Id = message.Id.ToString(),
            ChatId = chatId.ToString(),
            Role = message.Role.ToString().ToLower(),
            Content = message.Content,
            Timestamp = message.CreatedAt.ToString("o")
        };
        _connection.Execute(@"
            INSERT INTO ChatMessages (Id, ChatId, Role, Content, Timestamp)
            VALUES (@Id, @ChatId, @Role, @Content, @Timestamp)",
            msgDb);
            
        _connection.Execute("UPDATE Chats SET UpdatedAt = @Now WHERE Id = @Id",
            new { Id = chatId.ToString(), Now = DateTime.UtcNow.ToString("o") });
            
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _connection.Execute("DELETE FROM ChatMessages WHERE ChatId = @Id", new { Id = id.ToString() });
        _connection.Execute("DELETE FROM Chats WHERE Id = @Id", new { Id = id.ToString() });
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

    private class ChatDb
    {
        public string Id { get; set; } = "";
        public string WorkspaceId { get; set; } = "";
        public string Title { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";

        public Chat ToEntity() => new()
        {
            Id = Guid.Parse(Id),
            WorkspaceId = Guid.Parse(WorkspaceId),
            Title = Title,
            CreatedAt = DateTime.Parse(CreatedAt),
            UpdatedAt = DateTime.Parse(UpdatedAt),
            Messages = new List<ChatMessage>()
        };

        public static ChatDb FromEntity(Chat chat) => new()
        {
            Id = chat.Id.ToString(),
            WorkspaceId = chat.WorkspaceId.ToString(),
            Title = chat.Title,
            CreatedAt = chat.CreatedAt.ToString("o"),
            UpdatedAt = chat.UpdatedAt.ToString("o")
        };
    }

    private class MessageDb
    {
        public string Id { get; set; } = "";
        public string ChatId { get; set; } = "";
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}

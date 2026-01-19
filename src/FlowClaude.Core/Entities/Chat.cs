namespace FlowClaude.Core.Entities;

/// <summary>
/// Represents a chat session with the AI agent
/// </summary>
public class Chat
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid WorkspaceId { get; init; }
    public required string Title { get; init; }
    public ChatMode Mode { get; set; } = ChatMode.Agent;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool HasUnseenChanges { get; set; }
    public bool HasPendingPlan { get; set; }
    
    public ICollection<ChatMessage> Messages { get; init; } = new List<ChatMessage>();
}

public enum ChatMode
{
    Plan,   // Read-only analysis, requires approval to execute
    Agent   // Full execution permissions
}

public class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ChatId { get; init; }
    public MessageRole Role { get; init; }
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    public ICollection<ToolCall> ToolCalls { get; init; } = new List<ToolCall>();
}

public enum MessageRole
{
    User,
    Assistant,
    System
}

public class ToolCall
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid MessageId { get; init; }
    public required string ToolName { get; init; }
    public string? Input { get; init; }
    public string? Output { get; init; }
    public ToolCallStatus Status { get; set; } = ToolCallStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public enum ToolCallStatus
{
    Pending,
    Running,
    Completed,
    Error
}

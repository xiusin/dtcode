using FlowClaude.Core.Entities;

namespace FlowClaude.Core.Interfaces;

/// <summary>
/// Service interface for Claude AI agent operations
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Send a message to the agent and get a response
    /// </summary>
    IAsyncEnumerable<AgentEvent> SendMessageAsync(AgentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a plan that was approved by the user
    /// </summary>
    IAsyncEnumerable<AgentEvent> ExecutePlanAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a running agent execution
    /// </summary>
    Task CancelExecutionAsync(string executionId);
}

public class AgentRequest
{
    public required string Message { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required ChatMode Mode { get; init; }
    public IEnumerable<string>? AttachmentPaths { get; init; }
}

public abstract class AgentEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public class AgentMessageEvent : AgentEvent
{
    public required string Content { get; set; }
    public MessageRole Role { get; init; }
}

public class AgentToolCallEvent : AgentEvent
{
    public required string ToolName { get; init; }
    public string? Input { get; init; }
    public ToolCallStatus Status { get; set; }
    public string? Output { get; init; }
}

public class AgentThinkingEvent : AgentEvent
{
    public required string Reasoning { get; init; }
}

public class AgentErrorEvent : AgentEvent
{
    public required string Error { get; init; }
}

public class AgentCompletedEvent : AgentEvent
{
    public bool Success { get; init; }
    public string? Summary { get; init; }
}

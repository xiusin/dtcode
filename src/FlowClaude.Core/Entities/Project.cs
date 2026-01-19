namespace FlowClaude.Core.Entities;

/// <summary>
/// Represents a project linked to a local git repository
/// </summary>
public class Project
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Path { get; init; }
    public string? GitOwner { get; init; }
    public string? GitProvider { get; init; }
    public string? GitRepo { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

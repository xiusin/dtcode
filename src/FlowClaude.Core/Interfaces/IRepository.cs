using FlowClaude.Core.Entities;

namespace FlowClaude.Core.Interfaces;

/// <summary>
/// Repository interface for project management
/// </summary>
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project?> GetByPathAsync(string path);
    Task<IEnumerable<Project>> GetAllAsync();
    Task<Project> CreateAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
}

/// <summary>
/// Repository interface for workspace management
/// </summary>
public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(Guid id);
    Task<IEnumerable<Workspace>> GetByProjectIdAsync(Guid projectId);
    Task<IEnumerable<Workspace>> GetPinnedAsync();
    Task<IEnumerable<Workspace>> GetArchivedAsync();
    Task<Workspace> CreateAsync(Workspace workspace);
    Task<Workspace> UpdateAsync(Workspace workspace);
    Task DeleteAsync(Guid id);
    Task ArchiveAsync(Guid id);
    Task UnarchiveAsync(Guid id);
    Task TogglePinAsync(Guid id);
}

/// <summary>
/// Repository interface for chat management
/// </summary>
public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(Guid id);
    Task<IEnumerable<Chat>> GetByWorkspaceIdAsync(Guid workspaceId);
    Task<Chat> CreateAsync(Chat chat);
    Task<Chat> UpdateAsync(Chat chat);
    Task AddMessageAsync(Guid chatId, ChatMessage message);
    Task DeleteAsync(Guid id);
}

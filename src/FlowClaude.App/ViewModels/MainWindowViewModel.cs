using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FlowClaude.App.ViewModels;

/// <summary>
/// Main ViewModel for the application, managing overall state
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSidebarOpen = true;
    
    [ObservableProperty]
    private double _sidebarWidth = 260;
    
    [ObservableProperty]
    private Project? _selectedProject;
    
    [ObservableProperty]
    private Workspace? _selectedWorkspace;
    
    [ObservableProperty]
    private ViewMode _currentViewMode = ViewMode.Chat;
    
    [ObservableProperty]
    private bool _isDarkTheme = true;
    
    [ObservableProperty]
    private bool _isOnboarding = false;
    
    [ObservableProperty]
    private bool _isSettingsOpen = false;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string _inputText = "";
    
    [ObservableProperty]
    private bool _showRightPanel = false;
    
    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<Workspace> PinnedWorkspaces { get; } = new();
    public ObservableCollection<Workspace> RecentWorkspaces { get; } = new();
    public ObservableCollection<Workspace> ArchivedWorkspaces { get; } = new();
    public ObservableCollection<string> Suggestions { get; } = new()
    {
        "Help me understand this codebase",
        "Create a new feature",
        "Fix a bug",
        "Review my code",
        "Explain this function"
    };
    
    public ChatViewModel ChatViewModel { get; }
    public TerminalViewModel TerminalViewModel { get; }
    public ChangesViewModel ChangesViewModel { get; }
    
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IGitService _gitService;
    private readonly ITerminalService _terminalService;
    
    public ICommand ToggleSidebarCommand { get; }
    public ICommand SelectProjectCommand { get; }
    public ICommand SelectWorkspaceCommand { get; }
    public ICommand CreateWorkspaceCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand CloseSettingsCommand { get; }
    public ICommand ArchiveWorkspaceCommand { get; }
    public ICommand DeleteWorkspaceCommand { get; }
    public ICommand NavigateToViewCommand { get; }
    public ICommand SendCommand { get; }
    
    public MainWindowViewModel(
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository,
        IGitService gitService,
        ITerminalService terminalService,
        ChatViewModel chatViewModel,
        TerminalViewModel terminalViewModel,
        ChangesViewModel changesViewModel)
    {
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
        _gitService = gitService;
        _terminalService = terminalService;
        ChatViewModel = chatViewModel;
        TerminalViewModel = terminalViewModel;
        ChangesViewModel = changesViewModel;
        
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        SelectProjectCommand = new RelayCommand<Project>(SelectProject);
        SelectWorkspaceCommand = new RelayCommand<Workspace>(SelectWorkspace);
        CreateWorkspaceCommand = new AsyncRelayCommand(CreateWorkspaceAsync);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        OpenSettingsCommand = new RelayCommand(() => IsSettingsOpen = true);
        CloseSettingsCommand = new RelayCommand(() => IsSettingsOpen = false);
        ArchiveWorkspaceCommand = new RelayCommand<Workspace>(ArchiveWorkspace);
        DeleteWorkspaceCommand = new RelayCommand<Workspace>(DeleteWorkspace);
        NavigateToViewCommand = new RelayCommand<ViewMode>(vm => CurrentViewMode = vm);
        SendCommand = new RelayCommand(SendMessage);
        
        LoadProjectsAsync();
    }
    
    private async void LoadProjectsAsync()
    {
        IsLoading = true;
        try
        {
            var projects = await _projectRepository.GetAllAsync();
            Projects.Clear();
            foreach (var p in projects)
                Projects.Add(p);
            
            if (Projects.Count > 0 && SelectedProject == null)
                SelectProject(Projects.First());
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }
    
    private void SelectProject(Project? project)
    {
        SelectedProject = project;
        if (project != null)
        {
            LoadWorkspacesAsync(project.Id);
        }
    }
    
    private async void LoadWorkspacesAsync(Guid projectId)
    {
        IsLoading = true;
        try
        {
            var pinned = await _workspaceRepository.GetPinnedAsync();
            var recent = await _workspaceRepository.GetByProjectIdAsync(projectId);
            
            PinnedWorkspaces.Clear();
            RecentWorkspaces.Clear();
            
            foreach (var w in pinned.Where(w => w.ProjectId == projectId))
                PinnedWorkspaces.Add(w);
            
            foreach (var w in recent.Where(w => !PinnedWorkspaces.Contains(w)))
                RecentWorkspaces.Add(w);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void SelectWorkspace(Workspace? workspace)
    {
        SelectedWorkspace = workspace;
        if (workspace != null)
        {
            CurrentViewMode = ViewMode.Chat;
        }
    }
    
    private async Task CreateWorkspaceAsync()
    {
        if (SelectedProject == null) return;
        
        IsLoading = true;
        try
        {
            // Generate a unique branch name
            var branchName = $"{GenerateBranchName()}";
            var worktreesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".flowclaude",
                "worktrees",
                SelectedProject.Id.ToString());
            var worktreePath = Path.Combine(worktreesDir, Guid.NewGuid().ToString());
            
            // Create git worktree
            var result = await _gitService.CreateWorktreeAsync(
                SelectedProject.Path,
                branchName,
                worktreePath);
            
            if (!result.Success)
            {
                // Show error
                return;
            }
            
            var workspace = new Workspace
            {
                ProjectId = SelectedProject.Id,
                Name = "New Workspace",
                BranchName = result.Branch,
                WorktreePath = result.WorktreePath
            };
            
            workspace = await _workspaceRepository.CreateAsync(workspace);
            RecentWorkspaces.Insert(0, workspace);
            SelectWorkspace(workspace);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void ArchiveWorkspace(Workspace? workspace)
    {
        if (workspace == null) return;
        
        _workspaceRepository.ArchiveAsync(workspace.Id);
        RecentWorkspaces.Remove(workspace);
        ArchivedWorkspaces.Add(workspace);
    }
    
    private void DeleteWorkspace(Workspace? workspace)
    {
        if (workspace == null) return;
        
        _workspaceRepository.DeleteAsync(workspace.Id);
        RecentWorkspaces.Remove(workspace);
    }
    
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }
    
    private static string GenerateBranchName()
    {
        var adjectives = new[] { "swift", "bright", "calm", "happy", "kind", "eager", "jolly", "lively" };
        var animals = new[] { "fox", "lion", "bear", "wolf", "hawk", "eagle", "otter", "badger" };
        var random = new Random();
        return $"{adjectives[random.Next(adjectives.Length)]}-{animals[random.Next(animals.Length)]}-{random.Next(1000, 9999)}";
    }
    
    private void SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(InputText))
        {
            // Send message logic
            InputText = string.Empty;
        }
    }
}

public enum ViewMode
{
    Chat,
    Terminal,
    Changes,
    Settings
}

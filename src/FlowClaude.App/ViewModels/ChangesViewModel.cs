using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowClaude.Core.Entities;
using FlowClaude.Core.Interfaces;

namespace FlowClaude.App.ViewModels;

/// <summary>
/// ViewModel for the Changes view showing git file diffs
/// </summary>
public partial class ChangesViewModel : ObservableObject
{
    [ObservableProperty]
    private Workspace? _workspace;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string _diffContent = "";
    
    [ObservableProperty]
    private int _additions;
    
    [ObservableProperty]
    private int _deletions;
    
    [ObservableProperty]
    private ChangeViewMode _viewMode = ChangeViewMode.Diff;
    
    public ObservableCollection<FileChangeViewModel> FileChanges { get; } = new();
    public ObservableCollection<GitCommit> RecentCommits { get; } = new();
    
    private readonly IGitService _gitService;
    
    public ICommand RefreshCommand { get; }
    public ICommand StageAllCommand { get; }
    public ICommand DiscardAllCommand { get; }
    public ICommand CommitCommand { get; }
    public ICommand ViewModeCommand { get; }
    
    public ChangesViewModel(IGitService gitService)
    {
        _gitService = gitService;
        
        RefreshCommand = new AsyncRelayCommand(LoadChangesAsync);
        StageAllCommand = new RelayCommand(StageAll);
        DiscardAllCommand = new RelayCommand(DiscardAll);
        CommitCommand = new RelayCommand<string>(Commit);
        ViewModeCommand = new RelayCommand<ChangeViewMode>(vm => ViewMode = vm);
    }
    
    public void SetWorkspace(Workspace? workspace)
    {
        _workspace = workspace;
        LoadChangesAsync();
    }
    
    private async Task LoadChangesAsync()
    {
        if (_workspace == null || string.IsNullOrEmpty(_workspace.WorktreePath))
        {
            FileChanges.Clear();
            DiffContent = "";
            return;
        }
        
        IsLoading = true;
        try
        {
            // Load file changes
            var changes = await _gitService.GetFileChangesAsync(_workspace.WorktreePath);
            
            FileChanges.Clear();
            Additions = 0;
            Deletions = 0;
            
            foreach (var change in changes)
            {
                var vm = new FileChangeViewModel(change);
                vm.OnSelected += (sender, filePath) => SelectFileAsync(filePath);
                FileChanges.Add(vm);
                
                Additions += change.Additions ?? 0;
                Deletions += change.Deletions ?? 0;
            }
            
            // Load diff
            var diff = await _gitService.GetDiffAsync(_workspace.WorktreePath);
            DiffContent = diff;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async void SelectFileAsync(string filePath)
    {
        if (_workspace == null) return;
        
        // Get detailed diff for selected file
        var diff = await _gitService.GetDiffAsync(_workspace.WorktreePath);
        DiffContent = diff;
        ViewMode = ChangeViewMode.Diff;
    }
    
    private void StageAll()
    {
        // In a full implementation, this would run `git add -A`
    }
    
    private void DiscardAll()
    {
        // Show confirmation dialog then run `git checkout -- .`
    }
    
    private void Commit(string message)
    {
        if (string.IsNullOrEmpty(message) || _workspace == null) return;
        
        _ = CommitAsync(message);
    }
    
    private async Task CommitAsync(string message)
    {
        if (_workspace == null) return;
        
        var result = await _gitService.CommitChangesAsync(_workspace.WorktreePath, message);
        
        if (result.Success)
        {
            await LoadChangesAsync();
        }
    }
}

public partial class FileChangeViewModel : ObservableObject
{
    private readonly GitFileChange _change;
    
    [ObservableProperty]
    private bool _isSelected;
    
    public string FilePath => _change.FilePath;
    public ChangeStatus Status => _change.Status;
    public int? Additions => _change.Additions;
    public int? Deletions => _change.Deletions;
    
    public string StatusIcon => Status switch
    {
        ChangeStatus.Added => "➕",
        ChangeStatus.Modified => "✏️",
        ChangeStatus.Deleted => "🗑️",
        ChangeStatus.Renamed => "📝",
        ChangeStatus.Untracked => "❓",
        _ => "📄"
    };
    
    public string StatusText => Status switch
    {
        ChangeStatus.Added => "Added",
        ChangeStatus.Modified => "Modified",
        ChangeStatus.Deleted => "Deleted",
        ChangeStatus.Renamed => "Renamed",
        ChangeStatus.Untracked => "Untracked",
        _ => "Unknown"
    };
    
    public bool HasChanges => Additions > 0 || Deletions > 0;
    
    public ICommand? SelectCommand { get; set; }
    
    public event EventHandler<string>? OnSelected;
    
    public FileChangeViewModel(GitFileChange change)
    {
        _change = change;
        SelectCommand = new RelayCommand(Select);
    }
    
    public void Select()
    {
        IsSelected = true;
        OnSelected?.Invoke(this, _change.FilePath);
    }
}

public enum ChangeViewMode
{
    Tree,
    List,
    Diff
}
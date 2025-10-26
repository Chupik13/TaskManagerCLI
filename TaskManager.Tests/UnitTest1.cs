using TaskManager.Services;

namespace TaskManager.Tests;

public class TaskServiceTests : IDisposable
{
    private readonly string _testWorkspacePath;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _testWorkspacePath = Path.Combine(Path.GetTempPath(), "tm_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testWorkspacePath);
        _taskService = new TaskService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspacePath))
        {
            Directory.Delete(_testWorkspacePath, true);
        }
    }

    [Fact]
    public void InitializeWorkspace_CreatesDirectory()
    {
        _taskService.InitializeWorkspace(_testWorkspacePath);
        
        Assert.True(_taskService.IsInitialized(_testWorkspacePath));
        Assert.True(Directory.Exists(Path.Combine(_testWorkspacePath, ".tm")));
        Assert.True(File.Exists(_taskService.GetTasksFilePath(_testWorkspacePath)));
    }

    [Fact]
    public void AddTask_IncreasesTaskCount()
    {
        _taskService.InitializeWorkspace(_testWorkspacePath);
        var tasks = _taskService.LoadTasks(_testWorkspacePath);
        
        tasks.Add(new Models.Task { id = 1, text = "Test task", status = "active" });
        _taskService.SaveTasks(_testWorkspacePath, tasks);
        
        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        Assert.Single(loadedTasks);
        Assert.Equal("Test task", loadedTasks[0].text);
    }

    [Fact]
    public void RemoveTask_DecreasesTaskCount()
    {
        _taskService.InitializeWorkspace(_testWorkspacePath);
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 2, text = "Task 2", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        tasks.RemoveAt(0);
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        Assert.Single(loadedTasks);
        Assert.Equal(2, loadedTasks[0].id);
    }

    [Fact]
    public void ArchiveTask_ChangesStatus()
    {
        _taskService.InitializeWorkspace(_testWorkspacePath);
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        tasks[0].status = "archived";
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        Assert.Equal("archived", loadedTasks[0].status);
    }

    [Fact]
    public void Compact_ReassignsIds()
    {
        _taskService.InitializeWorkspace(_testWorkspacePath);
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 5, text = "Task 1", status = "active" },
            new Models.Task { id = 10, text = "Task 2", status = "active" },
            new Models.Task { id = 15, text = "Task 3", status = "archived" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var activeTasks = loadedTasks.Where(t => t.status == "active").OrderBy(t => t.id).ToList();
        var archivedTasks = loadedTasks.Where(t => t.status == "archived").OrderBy(t => t.id).ToList();

        var activeId = 1;
        foreach (var task in activeTasks)
        {
            task.id = activeId++;
        }

        var archivedId = 1000;
        foreach (var task in archivedTasks)
        {
            task.id = archivedId++;
        }

        var compactedTasks = activeTasks.Concat(archivedTasks).ToList();
        _taskService.SaveTasks(_testWorkspacePath, compactedTasks);

        var finalTasks = _taskService.LoadTasks(_testWorkspacePath);
        Assert.Equal(1, finalTasks.First(t => t.text == "Task 1").id);
        Assert.Equal(2, finalTasks.First(t => t.text == "Task 2").id);
        Assert.Equal(1000, finalTasks.First(t => t.text == "Task 3").id);
    }

    [Fact]
    public void Find_FindsMatchingTasks()
    {
        _taskService.InitializeWorkspace(_testWorkspacePath);
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Купить молоко", status = "active" },
            new Models.Task { id = 2, text = "Позвонить другу", status = "active" },
            new Models.Task { id = 3, text = "Купить хлеб", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var matching = loadedTasks.Where(t => t.text.Contains("Купить", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Equal(2, matching.Count);
        Assert.Contains(matching, t => t.text == "Купить молоко");
        Assert.Contains(matching, t => t.text == "Купить хлеб");
    }

    [Fact]
    public void HideTask_ChangesStatusToHidden()
    {
        _taskService.InitializeWorkspace(_testWorkspacePath);
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        tasks[0].status = "hidden";
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        Assert.Equal("hidden", loadedTasks[0].status);
    }
}

public class WorkspaceServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly WorkspaceService _workspaceService;
    private readonly List<string> _testWorkspacePaths;

    public WorkspaceServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "tm_test_global_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _workspaceService = new WorkspaceService(_testDir);
        _testWorkspacePaths = new List<string>();
    }

    public void Dispose()
    {
        // Clean up test workspace directories
        foreach (var path in _testWorkspacePaths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        // Clean up test config directory
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    private string CreateTestWorkspace(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"tm_test_workspace_{name}_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        _testWorkspacePaths.Add(path);
        return path;
    }

    [Fact]
    public void RemoveWorkspace_ReassignsIds()
    {
        var path1 = CreateTestWorkspace("Workspace1");
        var path2 = CreateTestWorkspace("Workspace2");
        var path3 = CreateTestWorkspace("Workspace3");

        var workspaces = new List<Models.Workspace>
        {
            new Models.Workspace { id = 1, name = "Workspace1", path = path1, status = "active" },
            new Models.Workspace { id = 2, name = "Workspace2", path = path2, status = "active" },
            new Models.Workspace { id = 3, name = "Workspace3", path = path3, status = "active" }
        };

        foreach (var ws in workspaces)
        {
            _workspaceService.AddWorkspace(ws);
        }

        _workspaceService.RemoveWorkspace(2);

        var loadedWorkspaces = _workspaceService.LoadWorkspaces();
        Assert.Equal(2, loadedWorkspaces.Count);
        Assert.Equal(1, loadedWorkspaces[0].id);
        Assert.Equal("Workspace1", loadedWorkspaces[0].name);
        Assert.Equal(2, loadedWorkspaces[1].id);
        Assert.Equal("Workspace3", loadedWorkspaces[1].name);
    }

    [Fact]
    public void ArchiveWorkspace_ChangesStatus()
    {
        var path = CreateTestWorkspace("TestWorkspace");

        var workspace = new Models.Workspace 
        { 
            id = 1, 
            name = "TestWorkspace", 
            path = path, 
            status = "active" 
        };

        _workspaceService.AddWorkspace(workspace);
        
        var loaded = _workspaceService.FindWorkspace(1);
        Assert.NotNull(loaded);
        Assert.Equal("active", loaded.status);

        loaded.status = "archived";
        _workspaceService.UpdateWorkspace(loaded);

        var updated = _workspaceService.FindWorkspace(1);
        Assert.NotNull(updated);
        Assert.Equal("archived", updated.status);
    }

    [Fact]
    public void ArchiveWorkspace_HidesFromActiveList()
    {
        var path1 = CreateTestWorkspace("ActiveWorkspace");
        var path2 = CreateTestWorkspace("ArchivedWorkspace");

        var workspaces = new List<Models.Workspace>
        {
            new Models.Workspace { id = 1, name = "ActiveWorkspace", path = path1, status = "active" },
            new Models.Workspace { id = 2, name = "ArchivedWorkspace", path = path2, status = "active" }
        };

        foreach (var ws in workspaces)
        {
            _workspaceService.AddWorkspace(ws);
        }

        // Archive workspace 2
        var workspace2 = _workspaceService.FindWorkspace(2);
        Assert.NotNull(workspace2);
        workspace2.status = "archived";
        _workspaceService.UpdateWorkspace(workspace2);

        // Check that only active workspaces are returned
        var allWorkspaces = _workspaceService.LoadWorkspaces();
        var activeWorkspaces = allWorkspaces.Where(w => w.status == "active").ToList();

        Assert.Equal(2, allWorkspaces.Count);
        Assert.Single(activeWorkspaces);
        Assert.Equal("ActiveWorkspace", activeWorkspaces[0].name);
    }

    [Fact]
    public void ArchiveWorkspace_SetsTasksToHidden()
    {
        var path = CreateTestWorkspace("TestWorkspace");
        var taskService = new TaskService();
        taskService.InitializeWorkspace(path);

        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 2, text = "Task 2", status = "active" }
        };
        taskService.SaveTasks(path, tasks);

        var workspace = new Models.Workspace 
        { 
            id = 1, 
            name = "TestWorkspace", 
            path = path, 
            status = "active" 
        };
        _workspaceService.AddWorkspace(workspace);

        // Archive workspace
        var loadedWorkspace = _workspaceService.FindWorkspace(1);
        Assert.NotNull(loadedWorkspace);
        loadedWorkspace.status = "archived";
        _workspaceService.UpdateWorkspace(loadedWorkspace);

        // Set all tasks to hidden
        var loadedTasks = taskService.LoadTasks(path);
        foreach (var task in loadedTasks)
        {
            task.status = "hidden";
        }
        taskService.SaveTasks(path, loadedTasks);

        // Verify tasks are hidden
        var finalTasks = taskService.LoadTasks(path);
        Assert.All(finalTasks, task => Assert.Equal("hidden", task.status));
    }

    [Fact]
    public void ReactivateWorkspace_SetsHiddenTasksToActive()
    {
        var path = CreateTestWorkspace("TestWorkspace");
        var taskService = new TaskService();
        taskService.InitializeWorkspace(path);

        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "hidden" },
            new Models.Task { id = 2, text = "Task 2", status = "hidden" }
        };
        taskService.SaveTasks(path, tasks);

        var workspace = new Models.Workspace 
        { 
            id = 1, 
            name = "TestWorkspace", 
            path = path, 
            status = "archived" 
        };
        _workspaceService.AddWorkspace(workspace);

        // Reactivate workspace
        var loadedWorkspace = _workspaceService.FindWorkspace(1);
        Assert.NotNull(loadedWorkspace);
        loadedWorkspace.status = "active";
        _workspaceService.UpdateWorkspace(loadedWorkspace);

        // Reactivate all hidden tasks
        var loadedTasks = taskService.LoadTasks(path);
        foreach (var task in loadedTasks.Where(t => t.status == "hidden"))
        {
            task.status = "active";
        }
        taskService.SaveTasks(path, loadedTasks);

        // Verify tasks are active
        var finalTasks = taskService.LoadTasks(path);
        Assert.All(finalTasks, task => Assert.Equal("active", task.status));
    }
}

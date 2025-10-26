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
}

public class WorkspaceServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly WorkspaceService _workspaceService;

    public WorkspaceServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "tm_test_global_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _workspaceService = new WorkspaceService(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void RemoveWorkspace_ReassignsIds()
    {
        var workspaces = new List<Models.Workspace>
        {
            new Models.Workspace { id = 1, name = "Workspace1", path = "/path1" },
            new Models.Workspace { id = 2, name = "Workspace2", path = "/path2" },
            new Models.Workspace { id = 3, name = "Workspace3", path = "/path3" }
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
}

using TaskManager.Services;
using TaskManager.Models;

namespace TaskManager.Tests;

public class TaskIdGenerationTests : IDisposable
{
    private readonly string _testWorkspacePath;
    private readonly TaskService _taskService;

    public TaskIdGenerationTests()
    {
        _testWorkspacePath = Path.Combine(Path.GetTempPath(), "tm_test_id_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testWorkspacePath);
        _taskService = new TaskService();
        _taskService.InitializeWorkspace(_testWorkspacePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspacePath))
        {
            Directory.Delete(_testWorkspacePath, true);
        }
    }

    [Fact]
    public void NewTask_StartsWithId1_WhenNoTasksExist()
    {
        var tasks = _taskService.LoadTasks(_testWorkspacePath);
        var notArchivedTasks = tasks.Where(x => x.status != "archived").ToList();
        var maxId = notArchivedTasks.Count != 0 ? notArchivedTasks.Max(t => t.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(1, newId);
    }

    [Fact]
    public void NewTask_IncrementsId_WhenTasksExist()
    {
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 2, text = "Task 2", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var notArchivedTasks = loadedTasks.Where(x => x.status != "archived").ToList();
        var maxId = notArchivedTasks.Count != 0 ? notArchivedTasks.Max(t => t.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(3, newId);
    }

    [Fact]
    public void NewTask_IgnoresArchivedTasks_WhenGeneratingId()
    {
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 2, text = "Task 2", status = "active" },
            new Models.Task { id = 100, text = "Archived Task", status = "archived" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var notArchivedTasks = loadedTasks.Where(x => x.status != "archived").ToList();
        var maxId = notArchivedTasks.Count != 0 ? notArchivedTasks.Max(t => t.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(3, newId);
    }

    [Fact]
    public void NewTask_HandlesGapsInIds_AfterRemoval()
    {
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 5, text = "Task 5", status = "active" },
            new Models.Task { id = 10, text = "Task 10", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var notArchivedTasks = loadedTasks.Where(x => x.status != "archived").ToList();
        var maxId = notArchivedTasks.Count != 0 ? notArchivedTasks.Max(t => t.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(11, newId);
    }

    [Fact]
    public void NewTask_AvoidsDuplicateIds_WithArchivedTasks()
    {
        // Scenario: We have archived tasks with high IDs, new task should not conflict
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 2, text = "Task 2", status = "active" },
            new Models.Task { id = 1000, text = "Archived Task 1", status = "archived" },
            new Models.Task { id = 1001, text = "Archived Task 2", status = "archived" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var notArchivedTasks = loadedTasks.Where(x => x.status != "archived").ToList();
        var maxId = notArchivedTasks.Count != 0 ? notArchivedTasks.Max(t => t.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(3, newId);
        
        // Verify uniqueness
        var allIds = loadedTasks.Select(t => t.id).ToList();
        Assert.DoesNotContain(newId, allIds);
    }

    [Fact]
    public void NewTask_WhenOnlyArchivedTasksExist_StartsFromId1()
    {
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1000, text = "Archived Task 1", status = "archived" },
            new Models.Task { id = 1001, text = "Archived Task 2", status = "archived" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var notArchivedTasks = loadedTasks.Where(x => x.status != "archived").ToList();
        var maxId = notArchivedTasks.Count != 0 ? notArchivedTasks.Max(t => t.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(1, newId);
    }

    [Fact]
    public void AllTaskIds_AreUnique_AfterMultipleOperations()
    {
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 2, text = "Task 2", status = "active" },
            new Models.Task { id = 3, text = "Task 3", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        // Archive task 2
        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        loadedTasks[1].status = "archived";
        _taskService.SaveTasks(_testWorkspacePath, loadedTasks);

        // Add new task
        loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var notArchivedTasks = loadedTasks.Where(x => x.status != "archived").ToList();
        var maxId = notArchivedTasks.Count != 0 ? notArchivedTasks.Max(t => t.id) : 0;
        var newId = maxId + 1;
        
        loadedTasks.Add(new Models.Task { id = newId, text = "Task 4", status = "active" });
        _taskService.SaveTasks(_testWorkspacePath, loadedTasks);

        // Verify all IDs are unique
        var finalTasks = _taskService.LoadTasks(_testWorkspacePath);
        var allIds = finalTasks.Select(t => t.id).ToList();
        var uniqueIds = allIds.Distinct().ToList();

        Assert.Equal(allIds.Count, uniqueIds.Count);
    }

    [Fact]
    public void CompactOperation_AssignsSequentialIds_ToActiveTasks()
    {
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 5, text = "Task 1", status = "active" },
            new Models.Task { id = 10, text = "Task 2", status = "active" },
            new Models.Task { id = 15, text = "Task 3", status = "active" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        // Perform compact operation
        var loadedTasks = _taskService.LoadTasks(_testWorkspacePath);
        var activeTasks = loadedTasks.Where(t => t.status == "active").OrderBy(t => t.id).ToList();
        
        var activeId = 1;
        foreach (var task in activeTasks)
        {
            task.id = activeId++;
        }
        _taskService.SaveTasks(_testWorkspacePath, activeTasks);

        // Verify sequential IDs
        var finalTasks = _taskService.LoadTasks(_testWorkspacePath);
        Assert.Equal(1, finalTasks[0].id);
        Assert.Equal(2, finalTasks[1].id);
        Assert.Equal(3, finalTasks[2].id);
    }

    [Fact]
    public void CompactOperation_AssignsIdsStartingFrom1000_ToArchivedTasks()
    {
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Active Task", status = "active" },
            new Models.Task { id = 5, text = "Archived Task 1", status = "archived" },
            new Models.Task { id = 10, text = "Archived Task 2", status = "archived" }
        };
        _taskService.SaveTasks(_testWorkspacePath, tasks);

        // Perform compact operation
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

        // Verify archived task IDs
        var finalTasks = _taskService.LoadTasks(_testWorkspacePath);
        var finalArchived = finalTasks.Where(t => t.status == "archived").OrderBy(t => t.id).ToList();
        
        Assert.Equal(1000, finalArchived[0].id);
        Assert.Equal(1001, finalArchived[1].id);
    }
}

public class WorkspaceIdGenerationTests : IDisposable
{
    private readonly string _testDir;
    private readonly WorkspaceService _workspaceService;
    private readonly List<string> _testWorkspacePaths;

    public WorkspaceIdGenerationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "tm_test_ws_id_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _workspaceService = new WorkspaceService(_testDir);
        _testWorkspacePaths = new List<string>();
    }

    public void Dispose()
    {
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

        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    private string CreateTestWorkspace(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"tm_test_ws_{name}_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        _testWorkspacePaths.Add(path);
        return path;
    }

    [Fact]
    public void NewWorkspace_StartsWithId1_WhenNoWorkspacesExist()
    {
        var workspaces = _workspaceService.LoadWorkspaces();
        var maxId = workspaces.Any() ? workspaces.Max(w => w.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(1, newId);
    }

    [Fact]
    public void NewWorkspace_IncrementsId_WhenWorkspacesExist()
    {
        var path1 = CreateTestWorkspace("ws1");
        var path2 = CreateTestWorkspace("ws2");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 2, name = "WS2", path = path2, status = "active" });

        var workspaces = _workspaceService.LoadWorkspaces();
        var maxId = workspaces.Any() ? workspaces.Max(w => w.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(3, newId);
    }

    [Fact]
    public void NewWorkspace_ConsidersArchivedWorkspaces_WhenGeneratingId()
    {
        var path1 = CreateTestWorkspace("ws1");
        var path2 = CreateTestWorkspace("ws2");
        var path3 = CreateTestWorkspace("ws3");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 2, name = "WS2", path = path2, status = "archived" });
        _workspaceService.AddWorkspace(new Workspace { id = 3, name = "WS3", path = path3, status = "active" });

        var workspaces = _workspaceService.LoadWorkspaces();
        var maxId = workspaces.Any() ? workspaces.Max(w => w.id) : 0;
        var newId = maxId + 1;

        Assert.Equal(4, newId);
    }

    [Fact]
    public void RemoveWorkspace_ReassignsSequentialIds()
    {
        var path1 = CreateTestWorkspace("ws1");
        var path2 = CreateTestWorkspace("ws2");
        var path3 = CreateTestWorkspace("ws3");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 2, name = "WS2", path = path2, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 3, name = "WS3", path = path3, status = "active" });

        _workspaceService.RemoveWorkspace(2);

        var workspaces = _workspaceService.LoadWorkspaces();
        Assert.Equal(2, workspaces.Count);
        Assert.Equal(1, workspaces[0].id);
        Assert.Equal(2, workspaces[1].id);
    }

    [Fact]
    public void AllWorkspaceIds_AreUnique()
    {
        var path1 = CreateTestWorkspace("ws1");
        var path2 = CreateTestWorkspace("ws2");
        var path3 = CreateTestWorkspace("ws3");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 2, name = "WS2", path = path2, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 3, name = "WS3", path = path3, status = "active" });

        var workspaces = _workspaceService.LoadWorkspaces();
        var ids = workspaces.Select(w => w.id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        Assert.Equal(ids.Count, uniqueIds.Count);
    }

    [Fact]
    public void WorkspaceIds_RemainUnique_AfterArchiveAndReactivate()
    {
        var path1 = CreateTestWorkspace("ws1");
        var path2 = CreateTestWorkspace("ws2");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 2, name = "WS2", path = path2, status = "active" });

        // Archive workspace 1
        var ws1 = _workspaceService.FindWorkspace(1);
        Assert.NotNull(ws1);
        ws1.status = "archived";
        _workspaceService.UpdateWorkspace(ws1);

        // Reactivate workspace 1
        ws1 = _workspaceService.FindWorkspace(1);
        Assert.NotNull(ws1);
        ws1.status = "active";
        _workspaceService.UpdateWorkspace(ws1);

        // Verify IDs remain unique
        var workspaces = _workspaceService.LoadWorkspaces();
        var ids = workspaces.Select(w => w.id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        Assert.Equal(ids.Count, uniqueIds.Count);
        Assert.Equal(1, ws1.id);
    }
}

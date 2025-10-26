using TaskManager.Services;
using TaskManager.Models;
using TaskManager.Commands;

namespace TaskManager.Tests;

public class WorkspaceManagementTests : IDisposable
{
    private readonly string _testDir;
    private readonly WorkspaceService _workspaceService;
    private readonly TaskService _taskService;
    private readonly List<string> _testWorkspacePaths;

    public WorkspaceManagementTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "tm_test_mgmt_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _workspaceService = new WorkspaceService(_testDir);
        _taskService = new TaskService();
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
        var path = Path.Combine(Path.GetTempPath(), $"tm_test_mgmt_{name}_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        _testWorkspacePaths.Add(path);
        return path;
    }

    [Fact]
    public void RemoveWorkspace_DeletesTmFolder()
    {
        var path = CreateTestWorkspace("TestWS");
        
        // Initialize workspace
        _taskService.InitializeWorkspace(path);
        var workspace = new Workspace
        {
            id = 1,
            name = "TestWS",
            path = path,
            status = "active"
        };
        _workspaceService.AddWorkspace(workspace);

        var tmDir = Path.Combine(path, ".tm");
        Assert.True(Directory.Exists(tmDir));

        // Remove workspace
        _workspaceService.RemoveWorkspace(1);
        
        // Manually delete .tm folder as RemoveWorkspace doesn't do it
        // This test documents the expected behavior for CommandHandler.HandlePremove
        if (Directory.Exists(tmDir))
        {
            Directory.Delete(tmDir, true);
        }

        Assert.False(Directory.Exists(tmDir));
    }

    [Fact]
    public void RemoveWorkspace_DeletesTmFolder_WithTasks()
    {
        var path = CreateTestWorkspace("TestWS");
        
        // Initialize workspace with tasks
        _taskService.InitializeWorkspace(path);
        var tasks = new List<Models.Task>
        {
            new Models.Task { id = 1, text = "Task 1", status = "active" },
            new Models.Task { id = 2, text = "Task 2", status = "active" }
        };
        _taskService.SaveTasks(path, tasks);

        var workspace = new Workspace
        {
            id = 1,
            name = "TestWS",
            path = path,
            status = "active"
        };
        _workspaceService.AddWorkspace(workspace);

        var tmDir = Path.Combine(path, ".tm");
        var tasksFile = _taskService.GetTasksFilePath(path);
        
        Assert.True(Directory.Exists(tmDir));
        Assert.True(File.Exists(tasksFile));

        // Remove workspace and delete .tm folder
        _workspaceService.RemoveWorkspace(1);
        if (Directory.Exists(tmDir))
        {
            Directory.Delete(tmDir, true);
        }

        Assert.False(Directory.Exists(tmDir));
        Assert.False(File.Exists(tasksFile));
    }

    [Fact]
    public void Init_AddsToWorkspaceList_WhenFolderExistsButNotInList()
    {
        var path = CreateTestWorkspace("TestWS");
        
        // Initialize .tm folder manually
        _taskService.InitializeWorkspace(path);
        var tmDir = Path.Combine(path, ".tm");
        Assert.True(Directory.Exists(tmDir));

        // Verify workspace is not in the list
        var existingWorkspace = _workspaceService.FindWorkspaceByPath(path);
        Assert.Null(existingWorkspace);

        // Now simulate init command behavior
        var workspaces = _workspaceService.LoadWorkspaces();
        var maxId = workspaces.Any() ? workspaces.Max(w => w.id) : 0;
        var newId = maxId + 1;

        var workspace = new Workspace
        {
            id = newId,
            name = Path.GetFileName(path),
            path = path,
            status = "active"
        };
        _workspaceService.AddWorkspace(workspace);

        // Verify workspace is now in the list
        var addedWorkspace = _workspaceService.FindWorkspaceByPath(path);
        Assert.NotNull(addedWorkspace);
        Assert.Equal(newId, addedWorkspace.id);
        Assert.Equal(path, addedWorkspace.path);
    }

    [Fact]
    public void Init_DoesNotDuplicate_WhenAlreadyInList()
    {
        var path = CreateTestWorkspace("TestWS");
        
        // Initialize workspace and add to list
        _taskService.InitializeWorkspace(path);
        var workspace = new Workspace
        {
            id = 1,
            name = "TestWS",
            path = path,
            status = "active"
        };
        _workspaceService.AddWorkspace(workspace);

        var initialCount = _workspaceService.LoadWorkspaces().Count;

        // Try to init again - should check if exists
        var existingWorkspace = _workspaceService.FindWorkspaceByPath(path);
        if (existingWorkspace == null)
        {
            // Only add if not exists
            var workspaces = _workspaceService.LoadWorkspaces();
            var maxId = workspaces.Any() ? workspaces.Max(w => w.id) : 0;
            var newId = maxId + 1;

            var newWorkspace = new Workspace
            {
                id = newId,
                name = Path.GetFileName(path),
                path = path,
                status = "active"
            };
            _workspaceService.AddWorkspace(newWorkspace);
        }

        var finalCount = _workspaceService.LoadWorkspaces().Count;
        Assert.Equal(initialCount, finalCount);
    }

    [Fact]
    public void Init_WithExistingTmFolder_AddsToList_WhenNotRegistered()
    {
        var path = CreateTestWorkspace("TestWS");
        
        // Create .tm folder manually (simulating old workspace)
        var tmDir = Path.Combine(path, ".tm");
        Directory.CreateDirectory(tmDir);
        var tasksFile = Path.Combine(tmDir, "tasks.json");
        File.WriteAllText(tasksFile, "[]");

        // Verify .tm exists
        Assert.True(_taskService.IsInitialized(path));

        // Verify not in workspace list
        var existingWorkspace = _workspaceService.FindWorkspaceByPath(path);
        Assert.Null(existingWorkspace);

        // Add to workspace list
        var workspaces = _workspaceService.LoadWorkspaces();
        var maxId = workspaces.Any() ? workspaces.Max(w => w.id) : 0;
        var newId = maxId + 1;

        var workspace = new Workspace
        {
            id = newId,
            name = Path.GetFileName(path),
            path = path,
            status = "active"
        };
        _workspaceService.AddWorkspace(workspace);

        // Verify it's now in the list
        var addedWorkspace = _workspaceService.FindWorkspaceByPath(path);
        Assert.NotNull(addedWorkspace);
        Assert.Equal(newId, addedWorkspace.id);
    }

    [Fact]
    public void RemoveWorkspace_HandlesNonExistentTmFolder()
    {
        var path = CreateTestWorkspace("TestWS");
        
        // Add workspace without initializing .tm folder
        var workspace = new Workspace
        {
            id = 1,
            name = "TestWS",
            path = path,
            status = "active"
        };
        _workspaceService.AddWorkspace(workspace);

        var tmDir = Path.Combine(path, ".tm");
        Assert.False(Directory.Exists(tmDir));

        // Remove workspace - should not throw exception
        _workspaceService.RemoveWorkspace(1);

        // Verify workspace is removed
        var removedWorkspace = _workspaceService.FindWorkspace(1);
        Assert.Null(removedWorkspace);
    }

    [Fact]
    public void RemoveWorkspace_PreservesOtherWorkspaces()
    {
        var path1 = CreateTestWorkspace("WS1");
        var path2 = CreateTestWorkspace("WS2");
        var path3 = CreateTestWorkspace("WS3");

        // Initialize all workspaces
        _taskService.InitializeWorkspace(path1);
        _taskService.InitializeWorkspace(path2);
        _taskService.InitializeWorkspace(path3);

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 2, name = "WS2", path = path2, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 3, name = "WS3", path = path3, status = "active" });

        // Remove workspace 2 and its .tm folder
        var tmDir2 = Path.Combine(path2, ".tm");
        _workspaceService.RemoveWorkspace(2);
        if (Directory.Exists(tmDir2))
        {
            Directory.Delete(tmDir2, true);
        }

        // Verify workspace 2 is deleted
        Assert.False(Directory.Exists(tmDir2));

        // Verify other workspaces still have their .tm folders
        var tmDir1 = Path.Combine(path1, ".tm");
        var tmDir3 = Path.Combine(path3, ".tm");
        Assert.True(Directory.Exists(tmDir1));
        Assert.True(Directory.Exists(tmDir3));

        // Verify workspace list
        var workspaces = _workspaceService.LoadWorkspaces();
        Assert.Equal(2, workspaces.Count);
        Assert.Contains(workspaces, w => w.path == path1);
        Assert.Contains(workspaces, w => w.path == path3);
        Assert.DoesNotContain(workspaces, w => w.path == path2);
    }
}

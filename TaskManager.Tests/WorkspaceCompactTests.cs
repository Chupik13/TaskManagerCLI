using TaskManager.Services;
using TaskManager.Models;

namespace TaskManager.Tests;

public class WorkspaceCompactTests : IDisposable
{
    private readonly string _testDir;
    private readonly WorkspaceService _workspaceService;
    private readonly List<string> _testWorkspacePaths;

    public WorkspaceCompactTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "tm_test_pcompact_" + Guid.NewGuid().ToString());
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
        var path = Path.Combine(Path.GetTempPath(), $"tm_test_pcompact_{name}_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        _testWorkspacePaths.Add(path);
        return path;
    }

    [Fact]
    public void Pcompact_ReassignsSequentialIds_ToActiveWorkspaces()
    {
        var path1 = CreateTestWorkspace("WS1");
        var path2 = CreateTestWorkspace("WS2");
        var path3 = CreateTestWorkspace("WS3");

        // Add workspaces with gaps in IDs
        _workspaceService.AddWorkspace(new Workspace { id = 5, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 10, name = "WS2", path = path2, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 20, name = "WS3", path = path3, status = "active" });

        // Perform compact
        var workspaces = _workspaceService.LoadWorkspaces();
        var activeWorkspaces = workspaces.Where(w => w.status == "active").OrderBy(w => w.id).ToList();

        for (int i = 0; i < activeWorkspaces.Count; i++)
        {
            activeWorkspaces[i].id = i + 1;
        }

        _workspaceService.SaveWorkspaces(activeWorkspaces);

        // Verify sequential IDs
        var compactedWorkspaces = _workspaceService.LoadWorkspaces();
        Assert.Equal(3, compactedWorkspaces.Count);
        Assert.Equal(1, compactedWorkspaces[0].id);
        Assert.Equal(2, compactedWorkspaces[1].id);
        Assert.Equal(3, compactedWorkspaces[2].id);
    }

    [Fact]
    public void Pcompact_AssignsIdsFrom1000_ToArchivedWorkspaces()
    {
        var path1 = CreateTestWorkspace("WS1");
        var path2 = CreateTestWorkspace("WS2");
        var path3 = CreateTestWorkspace("WS3");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 5, name = "WS2", path = path2, status = "archived" });
        _workspaceService.AddWorkspace(new Workspace { id = 10, name = "WS3", path = path3, status = "archived" });

        // Perform compact
        var workspaces = _workspaceService.LoadWorkspaces();
        var activeWorkspaces = workspaces.Where(w => w.status == "active").OrderBy(w => w.id).ToList();
        var archivedWorkspaces = workspaces.Where(w => w.status == "archived").OrderBy(w => w.id).ToList();

        for (int i = 0; i < activeWorkspaces.Count; i++)
        {
            activeWorkspaces[i].id = i + 1;
        }

        for (int i = 0; i < archivedWorkspaces.Count; i++)
        {
            archivedWorkspaces[i].id = 1000 + i;
        }

        var compacted = activeWorkspaces.Concat(archivedWorkspaces).ToList();
        _workspaceService.SaveWorkspaces(compacted);

        // Verify IDs
        var final = _workspaceService.LoadWorkspaces();
        var active = final.Where(w => w.status == "active").ToList();
        var archived = final.Where(w => w.status == "archived").OrderBy(w => w.id).ToList();

        Assert.Single(active);
        Assert.Equal(1, active[0].id);
        Assert.Equal(2, archived.Count);
        Assert.Equal(1000, archived[0].id);
        Assert.Equal(1001, archived[1].id);
    }

    [Fact]
    public void Pcompact_HandlesOnlyActiveWorkspaces()
    {
        var path1 = CreateTestWorkspace("WS1");
        var path2 = CreateTestWorkspace("WS2");

        _workspaceService.AddWorkspace(new Workspace { id = 5, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 15, name = "WS2", path = path2, status = "active" });

        // Perform compact
        var workspaces = _workspaceService.LoadWorkspaces();
        var activeWorkspaces = workspaces.Where(w => w.status == "active").OrderBy(w => w.id).ToList();

        for (int i = 0; i < activeWorkspaces.Count; i++)
        {
            activeWorkspaces[i].id = i + 1;
        }

        _workspaceService.SaveWorkspaces(activeWorkspaces);

        // Verify
        var final = _workspaceService.LoadWorkspaces();
        Assert.Equal(2, final.Count);
        Assert.Equal(1, final[0].id);
        Assert.Equal(2, final[1].id);
    }

    [Fact]
    public void Pcompact_HandlesOnlyArchivedWorkspaces()
    {
        var path1 = CreateTestWorkspace("WS1");
        var path2 = CreateTestWorkspace("WS2");

        _workspaceService.AddWorkspace(new Workspace { id = 5, name = "WS1", path = path1, status = "archived" });
        _workspaceService.AddWorkspace(new Workspace { id = 15, name = "WS2", path = path2, status = "archived" });

        // Perform compact
        var workspaces = _workspaceService.LoadWorkspaces();
        var archivedWorkspaces = workspaces.Where(w => w.status == "archived").OrderBy(w => w.id).ToList();

        for (int i = 0; i < archivedWorkspaces.Count; i++)
        {
            archivedWorkspaces[i].id = 1000 + i;
        }

        _workspaceService.SaveWorkspaces(archivedWorkspaces);

        // Verify
        var final = _workspaceService.LoadWorkspaces();
        Assert.Equal(2, final.Count);
        Assert.Equal(1000, final[0].id);
        Assert.Equal(1001, final[1].id);
    }

    [Fact]
    public void Pcompact_HandlesEmptyWorkspaceList()
    {
        var workspaces = _workspaceService.LoadWorkspaces();
        Assert.Empty(workspaces);

        // Perform compact on empty list - should not throw
        var activeWorkspaces = workspaces.Where(w => w.status == "active").OrderBy(w => w.id).ToList();
        var archivedWorkspaces = workspaces.Where(w => w.status == "archived").OrderBy(w => w.id).ToList();

        for (int i = 0; i < activeWorkspaces.Count; i++)
        {
            activeWorkspaces[i].id = i + 1;
        }

        for (int i = 0; i < archivedWorkspaces.Count; i++)
        {
            archivedWorkspaces[i].id = 1000 + i;
        }

        var compacted = activeWorkspaces.Concat(archivedWorkspaces).ToList();
        _workspaceService.SaveWorkspaces(compacted);

        // Verify still empty
        var final = _workspaceService.LoadWorkspaces();
        Assert.Empty(final);
    }

    [Fact]
    public void Pcompact_PreservesWorkspaceOrder()
    {
        var path1 = CreateTestWorkspace("WS_A");
        var path2 = CreateTestWorkspace("WS_B");
        var path3 = CreateTestWorkspace("WS_C");

        _workspaceService.AddWorkspace(new Workspace { id = 10, name = "WS_A", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 5, name = "WS_B", path = path2, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 20, name = "WS_C", path = path3, status = "active" });

        // Perform compact - order by id, then reassign
        var workspaces = _workspaceService.LoadWorkspaces();
        var activeWorkspaces = workspaces.Where(w => w.status == "active").OrderBy(w => w.id).ToList();

        for (int i = 0; i < activeWorkspaces.Count; i++)
        {
            activeWorkspaces[i].id = i + 1;
        }

        _workspaceService.SaveWorkspaces(activeWorkspaces);

        // Verify order is preserved (originally sorted by ID)
        var final = _workspaceService.LoadWorkspaces();
        Assert.Equal("WS_B", final[0].name); // Was ID 5, now ID 1
        Assert.Equal("WS_A", final[1].name); // Was ID 10, now ID 2
        Assert.Equal("WS_C", final[2].name); // Was ID 20, now ID 3
    }

    [Fact]
    public void ReactivateArchivedWorkspace_ReassignsValidId()
    {
        var path1 = CreateTestWorkspace("WS1");
        var path2 = CreateTestWorkspace("WS2");
        var path3 = CreateTestWorkspace("WS3");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 2, name = "WS2", path = path2, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 1000, name = "WS3", path = path3, status = "archived" });

        var workspaces = _workspaceService.LoadWorkspaces();
        var archivedWorkspace = workspaces.First(w => w.status == "archived");

        // Simulate reactivation with ID reassignment
        // First update status to active
        archivedWorkspace.status = "active";
        _workspaceService.UpdateWorkspace(archivedWorkspace);

        // Then reassign IDs through compact
        var allWorkspaces = _workspaceService.LoadWorkspaces();
        var active = allWorkspaces.Where(w => w.status == "active").OrderBy(w => w.id).ToList();
        var archived = allWorkspaces.Where(w => w.status == "archived").OrderBy(w => w.id).ToList();

        for (int i = 0; i < active.Count; i++)
        {
            active[i].id = i + 1;
        }

        for (int i = 0; i < archived.Count; i++)
        {
            archived[i].id = 1000 + i;
        }

        var compacted = active.Concat(archived).ToList();
        _workspaceService.SaveWorkspaces(compacted);

        // Verify
        var final = _workspaceService.LoadWorkspaces();
        var reactivated = final.First(w => w.name == "WS3");
        
        Assert.Equal("active", reactivated.status);
        Assert.Equal(3, reactivated.id);
    }

    [Fact]
    public void ReactivateArchivedWorkspace_AvoidsDuplicateIds()
    {
        var path1 = CreateTestWorkspace("WS1");
        var path2 = CreateTestWorkspace("WS2");

        _workspaceService.AddWorkspace(new Workspace { id = 1, name = "WS1", path = path1, status = "active" });
        _workspaceService.AddWorkspace(new Workspace { id = 1000, name = "WS2", path = path2, status = "archived" });

        var workspaces = _workspaceService.LoadWorkspaces();
        var archivedWorkspace = workspaces.First(w => w.status == "archived");

        // Reactivate
        archivedWorkspace.status = "active";
        _workspaceService.UpdateWorkspace(archivedWorkspace);

        // Compact
        var allWorkspaces = _workspaceService.LoadWorkspaces();
        var active = allWorkspaces.Where(w => w.status == "active").OrderBy(w => w.id).ToList();

        for (int i = 0; i < active.Count; i++)
        {
            active[i].id = i + 1;
        }

        _workspaceService.SaveWorkspaces(active);

        // Verify no duplicate IDs
        var final = _workspaceService.LoadWorkspaces();
        var ids = final.Select(w => w.id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        Assert.Equal(ids.Count, uniqueIds.Count);
        Assert.All(final, w => Assert.Equal("active", w.status));
    }
}

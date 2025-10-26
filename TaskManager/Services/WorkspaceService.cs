using System.Text.Json;
using TaskManager.Models;

namespace TaskManager.Services;

public class WorkspaceService
{
    private readonly string _globalConfigPath;

    public WorkspaceService(string? testConfigPath = null)
    {
        if (!string.IsNullOrEmpty(testConfigPath))
        {
            _globalConfigPath = Path.Combine(testConfigPath, "workspaces.json");
            if (!Directory.Exists(testConfigPath))
            {
                Directory.CreateDirectory(testConfigPath);
            }
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var globalDir = Path.Combine(userProfile, ".tm");
            _globalConfigPath = Path.Combine(globalDir, "workspaces.json");

            if (!Directory.Exists(globalDir))
            {
                Directory.CreateDirectory(globalDir);
            }
        }
    }

    public List<Workspace> LoadWorkspaces()
    {
        if (!File.Exists(_globalConfigPath))
        {
            return new List<Workspace>();
        }

        var json = File.ReadAllText(_globalConfigPath);
        return JsonSerializer.Deserialize<List<Workspace>>(json) ?? new List<Workspace>();
    }

    public void SaveWorkspaces(List<Workspace> workspaces)
    {
        var json = JsonSerializer.Serialize(workspaces, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_globalConfigPath, json);
    }

    public Workspace? FindWorkspace(int id)
    {
        var workspaces = LoadWorkspaces();
        return workspaces.FirstOrDefault(w => w.id == id);
    }

    public Workspace? FindWorkspaceByPath(string path)
    {
        var workspaces = LoadWorkspaces();
        var normalizedPath = Path.GetFullPath(path);
        return workspaces.FirstOrDefault(w => Path.GetFullPath(w.path) == normalizedPath);
    }

    public void AddWorkspace(Workspace workspace)
    {
        var workspaces = LoadWorkspaces();
        workspaces.Add(workspace);
        SaveWorkspaces(workspaces);
    }

    public void RemoveWorkspace(int id)
    {
        var workspaces = LoadWorkspaces();
        var workspace = workspaces.FirstOrDefault(w => w.id == id);
        
        if (workspace == null)
        {
            throw new InvalidOperationException("Рабочее пространство не найдено.");
        }

        workspaces.Remove(workspace);
        
        // Reassign IDs to maintain order starting from 1
        for (int i = 0; i < workspaces.Count; i++)
        {
            workspaces[i].id = i + 1;
        }
        
        SaveWorkspaces(workspaces);
    }

    public void UpdateWorkspace(Workspace workspace)
    {
        var workspaces = LoadWorkspaces();
        var existing = workspaces.FirstOrDefault(w => w.id == workspace.id);
        
        if (existing != null)
        {
            existing.status = workspace.status;
            existing.name = workspace.name;
            existing.path = workspace.path;
            SaveWorkspaces(workspaces);
        }
    }
}

using System.Text.Json;
using TaskManager.Models;

namespace TaskManager.Services;

public class WorkspaceService
{
    private readonly string _globalConfigPath;

    public WorkspaceService()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalDir = Path.Combine(userProfile, ".tm");
        _globalConfigPath = Path.Combine(globalDir, "workspaces.json");

        if (!Directory.Exists(globalDir))
        {
            Directory.CreateDirectory(globalDir);
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
}

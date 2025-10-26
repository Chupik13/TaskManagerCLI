using System.Text.Json;

namespace TaskManager.Services;

public class TaskService
{
    public string GetTasksFilePath(string workspacePath)
    {
        return Path.Combine(workspacePath, ".tm", "tasks.json");
    }

    public List<Models.Task> LoadTasks(string workspacePath)
    {
        var tasksFile = GetTasksFilePath(workspacePath);
        if (!File.Exists(tasksFile))
        {
            return new List<Models.Task>();
        }

        var json = File.ReadAllText(tasksFile);
        return JsonSerializer.Deserialize<List<Models.Task>>(json) ?? new List<Models.Task>();
    }

    public void SaveTasks(string workspacePath, List<Models.Task> tasks)
    {
        var tasksFile = GetTasksFilePath(workspacePath);
        var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(tasksFile, json);
    }

    public bool IsInitialized(string workspacePath)
    {
        var tmDir = Path.Combine(workspacePath, ".tm");
        var tasksFile = GetTasksFilePath(workspacePath);
        return Directory.Exists(tmDir) && File.Exists(tasksFile);
    }

    public void InitializeWorkspace(string workspacePath)
    {
        var tmDir = Path.Combine(workspacePath, ".tm");
        
        if (!Directory.Exists(tmDir))
        {
            var dirInfo = Directory.CreateDirectory(tmDir);
            // Попытка сделать директорию скрытой (работает на Windows)
            try
            {
                dirInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            catch
            {
                // Игнорируем ошибки для кросс-платформенности
            }
        }

        var tasksFile = GetTasksFilePath(workspacePath);
        if (!File.Exists(tasksFile))
        {
            SaveTasks(workspacePath, new List<Models.Task>());
        }
    }
}

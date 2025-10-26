using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Commands;

public class CommandHandler
{
    private readonly WorkspaceService _workspaceService;
    private readonly TaskService _taskService;

    public CommandHandler()
    {
        _workspaceService = new WorkspaceService();
        _taskService = new TaskService();
    }

    public void HandleInit()
    {
        var currentDir = Directory.GetCurrentDirectory();

        if (_taskService.IsInitialized(currentDir))
        {
            Console.WriteLine("Пространство уже инициализировано.");
            return;
        }

        _taskService.InitializeWorkspace(currentDir);

        var workspaces = _workspaceService.LoadWorkspaces();
        var maxId = workspaces.Any() ? workspaces.Max(w => w.id) : 0;
        var newId = maxId + 1;

        var workspace = new Workspace
        {
            id = newId,
            name = Path.GetFileName(currentDir),
            path = currentDir
        };

        _workspaceService.AddWorkspace(workspace);

        Console.WriteLine($"Рабочее пространство инициализировано с ID {newId}.");
    }

    public void HandleAdd(string text, int? workspaceId)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("Текст заметки не может быть пустым.");
            return;
        }

        var workspacePath = GetWorkspacePath(workspaceId);
        if (workspacePath == null) return;

        var tasks = _taskService.LoadTasks(workspacePath);
        var maxId = tasks.Any() ? tasks.Max(t => t.id) : 0;
        var newId = maxId + 1;

        var task = new Models.Task
        {
            id = newId,
            text = text,
            status = "active"
        };

        tasks.Add(task);
        _taskService.SaveTasks(workspacePath, tasks);

        Console.WriteLine($"Заметка добавлена с ID {newId}.");
    }

    public void HandleList(int? workspaceId)
    {
        var workspacePath = GetWorkspacePath(workspaceId);
        if (workspacePath == null) return;

        var tasks = _taskService.LoadTasks(workspacePath);
        var activeTasks = tasks.Where(t => t.status == "active").ToList();

        if (!activeTasks.Any())
        {
            Console.WriteLine("Нет активных заметок.");
            return;
        }

        foreach (var task in activeTasks)
        {
            Console.WriteLine($"{task.id}: {task.text}");
        }
    }

    public void HandleRemove(int taskId, int? workspaceId)
    {
        var workspacePath = GetWorkspacePath(workspaceId);
        if (workspacePath == null) return;

        var tasks = _taskService.LoadTasks(workspacePath);
        var task = tasks.FirstOrDefault(t => t.id == taskId);

        if (task == null)
        {
            Console.WriteLine("Заметка не найдена.");
            return;
        }

        tasks.Remove(task);
        _taskService.SaveTasks(workspacePath, tasks);

        Console.WriteLine($"Заметка {taskId} удалена.");
    }

    public void HandleArchive(int taskId, int? workspaceId)
    {
        var workspacePath = GetWorkspacePath(workspaceId);
        if (workspacePath == null) return;

        var tasks = _taskService.LoadTasks(workspacePath);
        var task = tasks.FirstOrDefault(t => t.id == taskId);

        if (task == null || task.status == "archived")
        {
            Console.WriteLine("Заметка не найдена или уже архивирована.");
            return;
        }

        task.status = "archived";
        _taskService.SaveTasks(workspacePath, tasks);

        Console.WriteLine($"Заметка {taskId} архивирована.");
    }

    public void HandleCompact(int? workspaceId)
    {
        var workspacePath = GetWorkspacePath(workspaceId);
        if (workspacePath == null) return;

        var tasks = _taskService.LoadTasks(workspacePath);
        var activeTasks = tasks.Where(t => t.status == "active").OrderBy(t => t.id).ToList();
        var archivedTasks = tasks.Where(t => t.status == "archived").OrderBy(t => t.id).ToList();

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
        _taskService.SaveTasks(workspacePath, compactedTasks);

        Console.WriteLine($"Компактизация завершена. Активные: {activeTasks.Count}, Архивированные: {archivedTasks.Count}.");
    }

    public void HandlePlist()
    {
        var workspaces = _workspaceService.LoadWorkspaces();

        if (!workspaces.Any())
        {
            Console.WriteLine("Нет рабочих пространств.");
            return;
        }

        foreach (var workspace in workspaces)
        {
            Console.WriteLine($"{workspace.id}: {workspace.name} ({workspace.path})");
        }
    }

    public void HandlePremove(int workspaceId)
    {
        try
        {
            _workspaceService.RemoveWorkspace(workspaceId);
            Console.WriteLine($"Рабочее пространство {workspaceId} удалено. ID переприсвоены.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void HandleFind(string searchText, int? workspaceId, bool isGlobal)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Console.WriteLine("Текст для поиска не указан.");
            return;
        }

        var workspaces = new List<Workspace>();

        if (isGlobal)
        {
            workspaces = _workspaceService.LoadWorkspaces();
            if (!workspaces.Any())
            {
                Console.WriteLine("Нет рабочих пространств.");
                return;
            }
        }
        else
        {
            var workspacePath = GetWorkspacePath(workspaceId);
            if (workspacePath == null) return;

            var workspace = new Workspace { path = workspacePath };
            workspaces.Add(workspace);
        }

        bool foundAny = false;

        foreach (var workspace in workspaces)
        {
            var tasks = _taskService.LoadTasks(workspace.path);
            var activeTasks = tasks.Where(t => t.status == "active").ToList();

            foreach (var task in activeTasks)
            {
                var index = task.text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    foundAny = true;
                    var context = GetContext(task.text, index, searchText.Length, 50);
                    Console.WriteLine($"ID: {task.id}, {context}");
                }
            }
        }

        if (!foundAny)
        {
            Console.WriteLine("Вхождений не найдено.");
        }
    }

    private string GetContext(string text, int index, int matchLength, int contextLength)
    {
        var leftStart = Math.Max(0, index - contextLength);
        var rightEnd = Math.Min(text.Length, index + matchLength + contextLength);

        var leftPart = text.Substring(leftStart, index - leftStart);
        var match = text.Substring(index, matchLength);
        var rightPart = text.Substring(index + matchLength, rightEnd - index - matchLength);

        var leftPrefix = leftStart > 0 ? "..." : "";
        var rightSuffix = rightEnd < text.Length ? "..." : "";

        return $"{leftPrefix}{leftPart}{match}{rightPart}{rightSuffix}";
    }

    private string? GetWorkspacePath(int? workspaceId)
    {
        if (workspaceId.HasValue)
        {
            var workspace = _workspaceService.FindWorkspace(workspaceId.Value);
            if (workspace == null)
            {
                Console.WriteLine("Пространство не найдено.");
                return null;
            }
            return workspace.path;
        }
        else
        {
            var currentDir = Directory.GetCurrentDirectory();
            if (!_taskService.IsInitialized(currentDir))
            {
                Console.WriteLine("Рабочее пространство не инициализировано. Используйте 'tm init' или '-p [id]'.");
                return null;
            }
            return currentDir;
        }
    }
}

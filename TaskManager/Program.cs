using TaskManager.Commands;

var handler = new CommandHandler();

if (args.Length == 0)
{
    Console.WriteLine("Использование: tm <команда> [аргументы]");
    Console.WriteLine("Команды: init, add, list, remove, archive, compact, plist, premove, find");
    return;
}

var command = args[0].ToLower();
var remainingArgs = args.Skip(1).ToArray();

try
{
    switch (command)
    {
        case "init":
            handler.HandleInit();
            break;

        case "add":
            {
                var (text, workspaceId) = ParseTextAndWorkspace(remainingArgs);
                handler.HandleAdd(text, workspaceId);
            }
            break;

        case "list":
            {
                var (workspaceId, isGlobal) = ParseWorkspaceIdAndGlobal(remainingArgs);
                handler.HandleList(workspaceId, isGlobal);
            }
            break;

        case "remove":
            {
                if (remainingArgs.Length == 0)
                {
                    Console.WriteLine("Укажите ID заметки для удаления.");
                    return;
                }

                var (taskIdStr, workspaceId) = ParseIdAndWorkspace(remainingArgs);
                if (!int.TryParse(taskIdStr, out var taskId))
                {
                    Console.WriteLine("ID заметки должен быть числом.");
                    return;
                }

                handler.HandleRemove(taskId, workspaceId);
            }
            break;

        case "archive":
            {
                if (remainingArgs.Length == 0)
                {
                    Console.WriteLine("Укажите ID заметки для архивации.");
                    return;
                }

                var (taskIdStr, workspaceId) = ParseIdAndWorkspace(remainingArgs);
                if (!int.TryParse(taskIdStr, out var taskId))
                {
                    Console.WriteLine("ID заметки должен быть числом.");
                    return;
                }

                handler.HandleArchive(taskId, workspaceId);
            }
            break;

        case "compact":
            {
                var workspaceId = ParseWorkspaceId(remainingArgs);
                handler.HandleCompact(workspaceId);
            }
            break;

        case "plist":
            handler.HandlePlist();
            break;

        case "premove":
            {
                if (remainingArgs.Length == 0)
                {
                    Console.WriteLine("Укажите ID рабочего пространства для удаления.");
                    return;
                }

                if (!int.TryParse(remainingArgs[0], out var workspaceId))
                {
                    Console.WriteLine("ID рабочего пространства должен быть числом.");
                    return;
                }

                handler.HandlePremove(workspaceId);
            }
            break;

        case "find":
            {
                var (searchText, workspaceId, isGlobal) = ParseFindArgs(remainingArgs);
                handler.HandleFind(searchText, workspaceId, isGlobal);
            }
            break;

        default:
            Console.WriteLine($"Неизвестная команда: {command}");
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}

static (string text, int? workspaceId) ParseTextAndWorkspace(string[] args)
{
    int? workspaceId = null;
    var textParts = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "-p" && i + 1 < args.Length)
        {
            if (int.TryParse(args[i + 1], out var id))
            {
                workspaceId = id;
                i++; // Пропускаем следующий аргумент
            }
        }
        else
        {
            textParts.Add(args[i]);
        }
    }

    return (string.Join(" ", textParts), workspaceId);
}

static int? ParseWorkspaceId(string[] args)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "-p" && int.TryParse(args[i + 1], out var id))
        {
            return id;
        }
    }
    return null;
}

static (int? workspaceId, bool isGlobal) ParseWorkspaceIdAndGlobal(string[] args)
{
    int? workspaceId = null;
    bool isGlobal = false;

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out var id))
        {
            workspaceId = id;
        }
        else if (args[i] == "-g")
        {
            isGlobal = true;
        }
    }

    return (workspaceId, isGlobal);
}

static (string idStr, int? workspaceId) ParseIdAndWorkspace(string[] args)
{
    int? workspaceId = null;
    string idStr = args[0];

    for (int i = 1; i < args.Length - 1; i++)
    {
        if (args[i] == "-p" && int.TryParse(args[i + 1], out var id))
        {
            workspaceId = id;
            break;
        }
    }

    return (idStr, workspaceId);
}

static (string searchText, int? workspaceId, bool isGlobal) ParseFindArgs(string[] args)
{
    int? workspaceId = null;
    bool isGlobal = false;
    var textParts = new List<string>();

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "-p" && i + 1 < args.Length)
        {
            if (int.TryParse(args[i + 1], out var id))
            {
                workspaceId = id;
                i++; // Пропускаем следующий аргумент
            }
        }
        else if (args[i] == "-g")
        {
            isGlobal = true;
        }
        else
        {
            textParts.Add(args[i]);
        }
    }

    return (string.Join(" ", textParts), workspaceId, isGlobal);
}

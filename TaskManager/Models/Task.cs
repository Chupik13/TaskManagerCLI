namespace TaskManager.Models;

public class Task
{
    public int id { get; set; }
    public string text { get; set; } = string.Empty;
    public string status { get; set; } = "active";
}

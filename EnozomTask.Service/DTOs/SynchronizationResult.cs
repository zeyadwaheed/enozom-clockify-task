namespace EnozomTask.Service.DTOs;

public class SynchronizationResult
{
    // Counts include inserted and updated local records.
    public int UsersProcessed { get; set; }
    public int ProjectsProcessed { get; set; }
    public int TasksProcessed { get; set; }
    public int TimeEntriesProcessed { get; set; }
    public List<string> Issues { get; } = [];
    public bool IsComplete => Issues.Count == 0;
}

namespace EnozomTask.Data.Entities;

public class ClockifyUser
{
    public int Id { get; set; }
    public required string ClockifyId { get; set; }
    public required string Name { get; set; }

    public ICollection<ProjectTask> AssignedTasks { get; set; }
        = new List<ProjectTask>();

}

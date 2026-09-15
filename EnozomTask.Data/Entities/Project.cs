namespace EnozomTask.Data.Entities;

public class Project
{
    public int Id { get; set; }
    public required string ClockifyId { get; set; }
    public required string Name { get; set; }

    public ICollection<ProjectTask> Tasks { get; set; }
        = new List<ProjectTask>();

}

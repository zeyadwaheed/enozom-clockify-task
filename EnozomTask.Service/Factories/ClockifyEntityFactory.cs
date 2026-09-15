using System.Xml;
using EnozomTask.Data.Entities;
using EnozomTask.Service.Exceptions;
using EnozomTask.Service.Integrations.Clockify.DTOs;

namespace EnozomTask.Service.Factories;

public class ClockifyEntityFactory : IClockifyEntityFactory
{
    public ClockifyUser CreateUser(ClockifyUserDto source)
    {
        return new ClockifyUser { ClockifyId = source.Id, Name = source.Name };
    }

    public Project CreateProject(ClockifyProjectDto source)
    {
        return new Project { ClockifyId = source.Id, Name = source.Name };
    }

    public ProjectTask CreateTask(ClockifyTaskDto source, Project project, ClockifyUser user)
    {
        return new ProjectTask
        {
            ClockifyId = source.Id,
            Name = source.Name,
            OriginalEstimateHours = GetEstimateHours(source),
            Project = project,
            ProjectId = project.Id,
            AssignedUser = user,
            AssignedUserId = user.Id
        };
    }

    public TimeEntry CreateTimeEntry(ClockifyTimeEntryDto source, ProjectTask task)
    {
        // The service validates the interval and relationships before mapping.
        var interval = source.TimeInterval!;
        return new TimeEntry
        {
            ClockifyId = source.Id,
            ProjectTask = task,
            ProjectTaskId = task.Id,
            StartUtc = interval.Start.UtcDateTime,
            EndUtc = interval.End?.UtcDateTime,
            Description = source.Description
        };
    }

    public decimal GetEstimateHours(ClockifyTaskDto source)
    {
        if (string.IsNullOrWhiteSpace(source.Estimate))
            return 0;

        try
        {
            var duration = XmlConvert.ToTimeSpan(source.Estimate);
            return decimal.Round((decimal)duration.Ticks / TimeSpan.TicksPerHour, 4);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException)
        {
            throw new ValidationException($"Task {source.Id} has an invalid ISO-8601 estimate.");
        }
    }

}

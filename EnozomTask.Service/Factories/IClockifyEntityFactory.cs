using EnozomTask.Data.Entities;
using EnozomTask.Service.Integrations.Clockify.DTOs;

namespace EnozomTask.Service.Factories;

public interface IClockifyEntityFactory
{
    ClockifyUser CreateUser(ClockifyUserDto source);
    Project CreateProject(ClockifyProjectDto source);
    ProjectTask CreateTask(ClockifyTaskDto source, Project project, ClockifyUser user);
    TimeEntry CreateTimeEntry(ClockifyTimeEntryDto source, ProjectTask task);
    decimal GetEstimateHours(ClockifyTaskDto source);
}

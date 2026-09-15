using EnozomTask.Data.Entities;
using EnozomTask.Service.Exceptions;
using EnozomTask.Service.Integrations.Clockify.DTOs;

namespace EnozomTask.Service.Validation;

// Checks remote records before they are mapped and saved. No extra strategy or DI abstraction is needed.
internal static class ClockifyRecordValidation
{
    public static void ValidateUser(ClockifyUserDto user) => ValidateIdentity(user.Id, user.Name, 200);

    public static void ValidateProject(ClockifyProjectDto project) => ValidateIdentity(project.Id, project.Name, 500);

    public static void ValidateTask(
        ClockifyTaskDto task, Project project, ClockifyUser user, decimal estimateHours)
    {
        ValidateIdentity(task.Id, task.Name, 500);
        var assignees = task.GetAssigneeIds();
        if (task.ProjectId != project.ClockifyId || assignees.Count != 1 || assignees[0] != user.ClockifyId)
            throw new ValidationException($"Task {task.Id} must belong to its project and exactly one known assignee.");

        if (estimateHours < 0 || estimateHours > 99999999.9999m)
            throw new ValidationException($"Task {task.Id} has an out-of-range estimate.");
    }

    public static void ValidateTimeEntry(ClockifyTimeEntryDto entry, ProjectTask task)
    {
        if (string.IsNullOrWhiteSpace(entry.Id) || entry.Id.Length > 100 ||
            entry.TaskId != task.ClockifyId || entry.ProjectId != task.Project.ClockifyId ||
            entry.UserId != task.AssignedUser.ClockifyId)
            throw new ValidationException($"Time entry {entry.Id} must match its task's project and assigned user.");

        var interval = entry.TimeInterval;
        if (interval is null || interval.Start == default || interval.Start.UtcDateTime.Year < 1000 ||
            interval.End is { } end && (end <= interval.Start || end.UtcDateTime.Year < 1000))
            throw new ValidationException($"Time entry {entry.Id} has an invalid time interval.");
    }

    private static void ValidateIdentity(string id, string name, int maximumNameLength)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100 ||
            string.IsNullOrWhiteSpace(name) || name.Length > maximumNameLength)
            throw new ValidationException("A Clockify record has an invalid ID or name for the local model.");
    }
}

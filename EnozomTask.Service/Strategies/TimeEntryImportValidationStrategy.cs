using EnozomTask.Service.DTOs;
using EnozomTask.Service.Exceptions;

namespace EnozomTask.Service.Strategies;

public class TimeEntryImportValidationStrategy : IImportValidationStrategy
{
    public void Validate(ImportDatasetRequest request)
    {
        if (request is null || request.Tasks is null || request.TimeEntries is null ||
            request.Tasks.Any(task => task is null))
            throw new ValidationException("Provide valid tasks and timeEntries arrays.");

        var keys = new HashSet<(string User, string Project, string Task, DateTimeOffset Start, DateTimeOffset End)>();
        foreach (var entry in request.TimeEntries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.UserClockifyId) ||
                string.IsNullOrWhiteSpace(entry.ProjectName) || string.IsNullOrWhiteSpace(entry.TaskName))
                throw new ValidationException("Every time entry requires a user, project and task.");

            if (entry.Start == default || entry.End == default || entry.End <= entry.Start)
                throw new ValidationException("Time-entry end must be later than start.");

            // MySQL datetime supports years 1000 through 9999.
            if (entry.Start.UtcDateTime.Year < 1000 || entry.End.UtcDateTime.Year < 1000)
                throw new ValidationException("Time entries must fall within MySQL's supported date range.");

            if (entry.Description?.Length > 3000)
                throw new ValidationException("Time-entry descriptions cannot exceed 3000 characters.");

            var task = request.Tasks.FirstOrDefault(task =>
                task.ProjectName.Trim() == entry.ProjectName.Trim() &&
                task.TaskName.Trim() == entry.TaskName.Trim());

            if (task is null)
                throw new ValidationException("Every time entry must reference a task in the import dataset.");

            if (task.AssignedUserClockifyId != entry.UserClockifyId)
                throw new ValidationException("Imported time must belong to the task's assigned user.");

            if (!keys.Add((entry.UserClockifyId, entry.ProjectName.Trim(),
                entry.TaskName.Trim(), entry.Start, entry.End)))
                throw new ValidationException("The dataset contains a duplicate time entry.");
        }
    }
}

using EnozomTask.Service.DTOs;
using EnozomTask.Service.Exceptions;

namespace EnozomTask.Service.Strategies;

public class TaskImportValidationStrategy : IImportValidationStrategy
{
    public void Validate(ImportDatasetRequest request)
    {
        if (request is null || request.Tasks is null || request.TimeEntries is null)
            throw new ValidationException("Provide tasks and timeEntries arrays.");

        if (request.Tasks.Count == 0)
            throw new ValidationException("Provide at least one task.");

        var keys = new HashSet<(string Project, string Task)>();
        foreach (var task in request.Tasks)
        {
            if (task is null)
                throw new ValidationException("A task cannot be null.");

            if (string.IsNullOrWhiteSpace(task.ProjectName) ||
                task.ProjectName.Trim().Length is < 2 or > 250)
                throw new ValidationException("Project names must contain 2 to 250 characters.");

            if (string.IsNullOrWhiteSpace(task.TaskName) || task.TaskName.Trim().Length > 500)
                throw new ValidationException("Task names must contain 1 to 500 characters.");

            if (string.IsNullOrWhiteSpace(task.AssignedUserClockifyId) ||
                task.AssignedUserClockifyId.Length > 100)
                throw new ValidationException("Every task needs a valid Clockify user ID.");

            if (task.OriginalEstimateHours < 0 || task.OriginalEstimateHours > 99999999.9999m ||
                decimal.Round(task.OriginalEstimateHours, 4) != task.OriginalEstimateHours)
                throw new ValidationException("Estimates must be nonnegative, fit decimal(12,4), and have at most four decimal places.");

            if (!keys.Add((task.ProjectName.Trim(), task.TaskName.Trim())))
                throw new ValidationException("Each project/task combination must appear once, with one assignee.");
        }
    }
}

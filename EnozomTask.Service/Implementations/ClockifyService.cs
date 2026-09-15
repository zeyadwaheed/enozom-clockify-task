using EnozomTask.Data.Entities;
using EnozomTask.Repository.Interfaces;
using EnozomTask.Service.DTOs;
using EnozomTask.Service.Exceptions;
using EnozomTask.Service.Factories;
using EnozomTask.Service.Integrations.Clockify.DTOs;
using EnozomTask.Service.Interfaces;
using EnozomTask.Service.Strategies;
using EnozomTask.Service.Validation;

namespace EnozomTask.Service.Implementations;

public class ClockifyService : IClockifyService
{
    private readonly IClockifyClient _client;
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;
    private readonly IProjectTaskRepository _tasks;
    private readonly ITimeEntryRepository _entries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClockifyEntityFactory _factory;
    private readonly IEnumerable<IImportValidationStrategy> _validators;

    public ClockifyService(
        IClockifyClient client,
        IUserRepository users,
        IProjectRepository projects,
        IProjectTaskRepository tasks,
        ITimeEntryRepository entries,
        IUnitOfWork unitOfWork,
        IClockifyEntityFactory factory,
        IEnumerable<IImportValidationStrategy> validators)
    {
        _client = client;
        _users = users;
        _projects = projects;
        _tasks = tasks;
        _entries = entries;
        _unitOfWork = unitOfWork;
        _factory = factory;
        _validators = validators;
    }

    // Retrieve the list of users in the configured Clockify workspace.
    public Task<IReadOnlyList<ClockifyUserDto>> GetWorkspaceUsersAsync(CancellationToken cancellationToken = default) =>
        _client.GetUsersAsync(cancellationToken);

    // Retrieve data from Clockify and persist it locally, returning a summary of the operation.
    public async Task<SynchronizationResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new SynchronizationResult();
        var remoteUsers = await _client.GetUsersAsync(cancellationToken);

        var users = await SynchronizeUsersAsync(remoteUsers, result, cancellationToken);
        var tasks = await SynchronizeProjectsAndTasksAsync(users, result, cancellationToken);
        await SynchronizeTimeEntriesAsync(users.Keys, tasks, result, cancellationToken);

        return result;
    }

    // it has 2 phases: first, it validates the input and checks for existing remote records; second, it creates any missing remote records and saves them locally.
    public async Task<SynchronizationResult> ImportAsync(
        ImportDatasetRequest request, CancellationToken cancellationToken = default)
    {
        // Phase 1 : validate the input and check for existing remote records.
        // Validate the import request.
        foreach (var validator in _validators)
            validator.Validate(request);

        cancellationToken.ThrowIfCancellationRequested();
        // get the remote users, projects, tasks, and time entries that correspond to the import request.
        var selectedUsers = await FindImportUsersAsync(request, cancellationToken);
        var (projectsByName, tasksByProject) = await LoadExistingProjectsAndTasksAsync(request, cancellationToken);
        var entriesByUser = await LoadExistingTimeEntriesAsync(request, cancellationToken);

        // Phase 2 : create any missing remote records and save them locally.
        var result = new SynchronizationResult();
        var users = await ImportUsersAsync(selectedUsers, result, cancellationToken);
        var projects = await ImportProjectsAsync(tasksByProject.Keys, projectsByName, result, cancellationToken);
        var tasks = await ImportTasksAsync(request, tasksByProject, users, projects, result, cancellationToken);
        await ImportTimeEntriesAsync(request, entriesByUser, tasks, result, cancellationToken);

        return result;
    }

    private async Task<Dictionary<string, ClockifyUser>> SynchronizeUsersAsync(
        IReadOnlyList<ClockifyUserDto> remoteUsers,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        var users = new Dictionary<string, ClockifyUser>(StringComparer.Ordinal);
        foreach (var remoteUser in remoteUsers)
        {
            try
            {
                users[remoteUser.Id] = await SaveUserAsync(remoteUser, cancellationToken);
                result.UsersProcessed++;
            }
            catch (ValidationException exception)
            {
                result.Issues.Add($"User {remoteUser.Id}: {exception.Message}");
            }
        }
        return users;
    }

    private async Task<Dictionary<string, ProjectTask>> SynchronizeProjectsAndTasksAsync(
        IReadOnlyDictionary<string, ClockifyUser> users,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        var tasks = new Dictionary<string, ProjectTask>(StringComparer.Ordinal);
        foreach (var remoteProject in await _client.GetProjectsAsync(cancellationToken))
        {
            Project project;
            try
            {
                project = await SaveProjectAsync(remoteProject, cancellationToken);
                result.ProjectsProcessed++;
            }
            catch (ValidationException exception)
            {
                result.Issues.Add($"Project {remoteProject.Id}: {exception.Message}");
                continue;
            }

            await SynchronizeProjectTasksAsync(project, users, tasks, result, cancellationToken);
        }
        return tasks;
    }

    private async Task SynchronizeProjectTasksAsync(
        Project project,
        IReadOnlyDictionary<string, ClockifyUser> users,
        Dictionary<string, ProjectTask> tasks,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        foreach (var remoteTask in await _client.GetTasksAsync(project.ClockifyId, cancellationToken))
        {
            var assignees = remoteTask.GetAssigneeIds();
            if (assignees.Count != 1 || !users.TryGetValue(assignees[0], out var assignee))
            {
                result.Issues.Add($"Task {remoteTask.Id}: requires exactly one known assignee; not synchronized.");
                continue;
            }

            try
            {
                tasks[remoteTask.Id] = await SaveTaskAsync(remoteTask, project, assignee, cancellationToken);
                result.TasksProcessed++;
            }
            catch (ValidationException exception)
            {
                result.Issues.Add($"Task {remoteTask.Id}: {exception.Message}");
            }
        }
    }

    private async Task SynchronizeTimeEntriesAsync(
        IEnumerable<string> userClockifyIds,
        IReadOnlyDictionary<string, ProjectTask> tasks,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        foreach (var userClockifyId in userClockifyIds)
            await SynchronizeUserTimeEntriesAsync(userClockifyId, tasks, result, cancellationToken);
    }

    private async Task SynchronizeUserTimeEntriesAsync(
        string userClockifyId,
        IReadOnlyDictionary<string, ProjectTask> tasks,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        foreach (var remoteEntry in await _client.GetTimeEntriesAsync(userClockifyId, cancellationToken))
        {
            if (remoteEntry.TaskId is null || !tasks.TryGetValue(remoteEntry.TaskId, out var task))
            {
                result.Issues.Add($"Time entry {remoteEntry.Id}: requires a synchronized task; not synchronized.");
                continue;
            }

            try
            {
                await SaveEntryAsync(remoteEntry, task, cancellationToken);
                result.TimeEntriesProcessed++;
            }
            catch (ValidationException exception)
            {
                result.Issues.Add($"Time entry {remoteEntry.Id}: {exception.Message}");
            }
        }
    }

    // Import preparation: these methods read and validate; they do not create remote records.
    private async Task<Dictionary<string, ClockifyUserDto>> FindImportUsersAsync(
        ImportDatasetRequest request, CancellationToken cancellationToken)
    {
        var remoteUsers = await _client.GetUsersAsync(cancellationToken);
        var userIds = request.Tasks.Select(task => task.AssignedUserClockifyId)
            .Concat(request.TimeEntries.Select(entry => entry.UserClockifyId))
            .Distinct(StringComparer.Ordinal).ToList();

        var selectedUsers = new Dictionary<string, ClockifyUserDto>(StringComparer.Ordinal);
        foreach (var userId in userIds)
        {
            var user = remoteUsers.FirstOrDefault(user => user.Id == userId)
                ?? throw new ValidationException($"User {userId} must already exist in the configured Clockify workspace.");
            ClockifyRecordValidation.ValidateUser(user);
            selectedUsers[userId] = user;
        }
        return selectedUsers;
    }

    private async Task<(
        Dictionary<string, ClockifyProjectDto> ProjectsByName,
        Dictionary<string, List<ClockifyTaskDto>> TasksByProject)> LoadExistingProjectsAndTasksAsync(
        ImportDatasetRequest request, CancellationToken cancellationToken)
    {
        var remoteProjects = await _client.GetProjectsAsync(cancellationToken);
        var projectsByName = new Dictionary<string, ClockifyProjectDto>(StringComparer.Ordinal);
        var tasksByProject = new Dictionary<string, List<ClockifyTaskDto>>(StringComparer.Ordinal);

        foreach (var projectName in request.Tasks.Select(task => task.ProjectName.Trim()).Distinct(StringComparer.Ordinal))
        {
            var matches = remoteProjects.Where(project => project.Name == projectName).ToList();
            if (matches.Count > 1)
                throw new ValidationException($"Project name '{projectName}' is ambiguous in Clockify.");
            if (matches.Count == 1)
            {
                ClockifyRecordValidation.ValidateProject(matches[0]);
                projectsByName[projectName] = matches[0];
                tasksByProject[projectName] =
                    (await _client.GetTasksAsync(matches[0].Id, cancellationToken)).ToList();
            }
            else
            {
                tasksByProject[projectName] = [];
            }
        }
        foreach (var input in request.Tasks)
        {
            var matches = tasksByProject[input.ProjectName.Trim()]
                .Where(task => task.Name == input.TaskName.Trim()).ToList();
            if (matches.Count > 1)
                throw new ValidationException($"Task '{input.TaskName}' is ambiguous within its project.");
            if (matches.Count == 1)
                ValidateExistingTask(matches[0], input);
        }
        return (projectsByName, tasksByProject);
    }

    private async Task<Dictionary<string, List<ClockifyTimeEntryDto>>> LoadExistingTimeEntriesAsync(
        ImportDatasetRequest request, CancellationToken cancellationToken)
    {
        var entriesByUser = new Dictionary<string, List<ClockifyTimeEntryDto>>(StringComparer.Ordinal);
        foreach (var userId in request.TimeEntries.Select(entry => entry.UserClockifyId).Distinct(StringComparer.Ordinal))
            entriesByUser[userId] = (await _client.GetTimeEntriesAsync(userId, cancellationToken)).ToList();
        return entriesByUser;
    }

    // Import execution: reuse or create remote records, then save their local counterparts.
    private async Task<Dictionary<string, ClockifyUser>> ImportUsersAsync(
        IReadOnlyDictionary<string, ClockifyUserDto> selectedUsers,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        var users = new Dictionary<string, ClockifyUser>(StringComparer.Ordinal);
        foreach (var remoteUser in selectedUsers.Values)
        {
            users[remoteUser.Id] = await SaveUserAsync(remoteUser, cancellationToken);
            result.UsersProcessed++;
        }
        return users;
    }

    private async Task<Dictionary<string, Project>> ImportProjectsAsync(
        IEnumerable<string> projectNames,
        IReadOnlyDictionary<string, ClockifyProjectDto> projectsByName,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        var projects = new Dictionary<string, Project>(StringComparer.Ordinal);
        foreach (var projectName in projectNames)
        {
            if (!projectsByName.TryGetValue(projectName, out var remoteProject))
            {
                remoteProject = await _client.CreateProjectAsync(projectName, cancellationToken);
            }
            projects[projectName] = await SaveProjectAsync(remoteProject, cancellationToken);
            result.ProjectsProcessed++;
        }
        return projects;
    }

    private async Task<Dictionary<(string Project, string Task), ProjectTask>> ImportTasksAsync(
        ImportDatasetRequest request,
        IReadOnlyDictionary<string, List<ClockifyTaskDto>> tasksByProject,
        IReadOnlyDictionary<string, ClockifyUser> users,
        IReadOnlyDictionary<string, Project> projects,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        var tasks = new Dictionary<(string Project, string Task), ProjectTask>();
        foreach (var input in request.Tasks)
        {
            var projectName = input.ProjectName.Trim();
            var taskName = input.TaskName.Trim();
            var remoteTask = tasksByProject[projectName].SingleOrDefault(task => task.Name == taskName);
            if (remoteTask is null)
            {
                remoteTask = await _client.CreateTaskAsync(projects[projectName].ClockifyId, input, cancellationToken);
                tasksByProject[projectName].Add(remoteTask);
            }

            // Verify that Clockify actually accepted the requested assignee and estimate.
            ValidateExistingTask(remoteTask, input);
            tasks[(projectName, taskName)] = await SaveTaskAsync(
                remoteTask, projects[projectName], users[input.AssignedUserClockifyId], cancellationToken);
            result.TasksProcessed++;
        }
        return tasks;
    }

    private async Task ImportTimeEntriesAsync(
        ImportDatasetRequest request,
        IReadOnlyDictionary<string, List<ClockifyTimeEntryDto>> entriesByUser,
        IReadOnlyDictionary<(string Project, string Task), ProjectTask> tasks,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        foreach (var input in request.TimeEntries)
        {
            var task = tasks[(input.ProjectName.Trim(), input.TaskName.Trim())];
            var remoteEntry = FindMatchingTimeEntry(entriesByUser[input.UserClockifyId], task, input);

            if (remoteEntry is null)
            {
                remoteEntry = await _client.CreateTimeEntryAsync(
                    task.Project.ClockifyId, task.ClockifyId, input, cancellationToken);
                entriesByUser[input.UserClockifyId].Add(remoteEntry);
            }

            await SaveEntryAsync(remoteEntry, task, cancellationToken);
            result.TimeEntriesProcessed++;
        }
    }

    private static ClockifyTimeEntryDto? FindMatchingTimeEntry(
        IReadOnlyList<ClockifyTimeEntryDto> existingEntries,
        ProjectTask task,
        ImportTimeEntryDto input)
    {
        var matches = existingEntries.Where(entry =>
            entry.TaskId == task.ClockifyId &&
            entry.ProjectId == task.Project.ClockifyId &&
            entry.UserId == input.UserClockifyId &&
            entry.TimeInterval?.Start == input.Start &&
            entry.TimeInterval?.End == input.End).ToList();

        if (matches.Count > 1)
            throw new ValidationException("Multiple Clockify entries match the same user/task/time interval; resolve them before retrying.");

        var remoteEntry = matches.SingleOrDefault();
        if (remoteEntry is not null && (remoteEntry.Description ?? "") != (input.Description ?? ""))
            throw new ValidationException("An existing time entry has the same interval but a different description.");
        return remoteEntry;
    }

    private void ValidateExistingTask(ClockifyTaskDto remoteTask, ImportTaskDto input)
    {
        var assignees = remoteTask.GetAssigneeIds();
        if (assignees.Count != 1 || assignees[0] != input.AssignedUserClockifyId ||
            _factory.GetEstimateHours(remoteTask) != input.OriginalEstimateHours)
            throw new ValidationException(
                $"Task '{input.TaskName}' has a different assignee or estimate in Clockify. Resolve the conflict before importing.");
    }

    private async Task<ClockifyUser> SaveUserAsync(ClockifyUserDto source, CancellationToken cancellationToken)
    {
        ClockifyRecordValidation.ValidateUser(source);
        var incoming = _factory.CreateUser(source);
        var user = await _users.GetByClockifyIdAsync(source.Id, cancellationToken);
        if (user is null)
        {
            user = incoming;
            await _users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.Name = incoming.Name;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task<Project> SaveProjectAsync(ClockifyProjectDto source, CancellationToken cancellationToken)
    {
        ClockifyRecordValidation.ValidateProject(source);
        var incoming = _factory.CreateProject(source);
        var project = await _projects.GetByClockifyIdAsync(source.Id, cancellationToken);
        if (project is null)
        {
            project = incoming;
            await _projects.AddAsync(project, cancellationToken);
        }
        else
        {
            project.Name = incoming.Name;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return project;
    }

    private async Task<ProjectTask> SaveTaskAsync(
        ClockifyTaskDto source, Project project, ClockifyUser user, CancellationToken cancellationToken)
    {
        ClockifyRecordValidation.ValidateTask(source, project, user, _factory.GetEstimateHours(source));
        var incoming = _factory.CreateTask(source, project, user);
        var task = await _tasks.GetByClockifyIdAsync(source.Id, cancellationToken);
        if (task is null)
        {
            task = incoming;
            await _tasks.AddAsync(task, cancellationToken);
        }
        else
        {
            task.Name = incoming.Name;
            task.OriginalEstimateHours = incoming.OriginalEstimateHours;
            task.Project = project;
            task.ProjectId = project.Id;
            task.AssignedUser = user;
            task.AssignedUserId = user.Id;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task;
    }

    private async Task SaveEntryAsync(
        ClockifyTimeEntryDto source, ProjectTask task, CancellationToken cancellationToken)
    {
        ClockifyRecordValidation.ValidateTimeEntry(source, task);
        var incoming = _factory.CreateTimeEntry(source, task);
        var entry = await _entries.GetByClockifyIdAsync(source.Id, cancellationToken);
        if (entry is null)
        {
            await _entries.AddAsync(incoming, cancellationToken);
        }
        else
        {
            entry.ProjectTask = task;
            entry.ProjectTaskId = task.Id;
            entry.StartUtc = incoming.StartUtc;
            entry.EndUtc = incoming.EndUtc;
            entry.Description = incoming.Description;
        }

        // Save each confirmed remote record. A local transaction cannot roll back Clockify.
        // If a later operation fails, retrying reconciles by remote IDs and existing remote data.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

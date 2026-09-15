using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
using EnozomTask.Service.DTOs;
using EnozomTask.Service.Exceptions;
using EnozomTask.Service.Integrations.Clockify.DTOs;
using EnozomTask.Service.Interfaces;
using EnozomTask.Service.Options;
using Microsoft.Extensions.Options;

namespace EnozomTask.Service.Integrations.Clockify;

public class ClockifyClient : IClockifyClient
{
    private const int PageSize = 50;
    private readonly HttpClient _httpClient;
    private readonly ClockifyOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // insert the options(apiKey, workspaceId) parameter into the constructor
    public ClockifyClient(HttpClient httpClient, IOptions<ClockifyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<IReadOnlyList<ClockifyUserDto>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        GetPagesAsync<ClockifyUserDto>("users", cancellationToken);

    public async Task<IReadOnlyList<ClockifyProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var active = await GetPagesAsync<ClockifyProjectDto>("projects?archived=false", cancellationToken);
        var archived = await GetPagesAsync<ClockifyProjectDto>("projects?archived=true", cancellationToken);
        return active.Concat(archived).DistinctBy(project => project.Id).ToList();
    }

    public async Task<IReadOnlyList<ClockifyTaskDto>> GetTasksAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var path = $"projects/{Segment(projectId)}/tasks";
        var active = await GetPagesAsync<ClockifyTaskDto>($"{path}?is-active=true", cancellationToken);
        var inactive = await GetPagesAsync<ClockifyTaskDto>($"{path}?is-active=false", cancellationToken);
        return active.Concat(inactive).DistinctBy(task => task.Id).ToList();
    }

    public Task<IReadOnlyList<ClockifyTimeEntryDto>> GetTimeEntriesAsync(string userId, CancellationToken cancellationToken = default) =>
        GetPagesAsync<ClockifyTimeEntryDto>($"user/{Segment(userId)}/time-entries", cancellationToken);

    public Task<ClockifyProjectDto> CreateProjectAsync(string name,
    CancellationToken cancellationToken = default)=>

        SendAsync<ClockifyProjectDto>(HttpMethod.Post, "projects",
        new { name,
            isPublic = true,
            color = "#03A9F4",
            billable = false },
            cancellationToken
            );

    public Task<ClockifyTaskDto> CreateTaskAsync(string projectId, ImportTaskDto task, CancellationToken cancellationToken = default) =>
        SendAsync<ClockifyTaskDto>(HttpMethod.Post, $"projects/{Segment(projectId)}/tasks",
            new
            {
                name = task.TaskName.Trim(),
                assigneeIds = new[] { task.AssignedUserClockifyId },
                estimate = XmlConvert.ToString(TimeSpan.FromTicks(
                    (long)(task.OriginalEstimateHours * TimeSpan.TicksPerHour))),
                status = "ACTIVE"
            }, cancellationToken);

    public Task<ClockifyTimeEntryDto> CreateTimeEntryAsync(
        string projectId, string taskId, ImportTimeEntryDto entry, CancellationToken cancellationToken = default) =>
        SendAsync<ClockifyTimeEntryDto>(HttpMethod.Post, $"user/{Segment(entry.UserClockifyId)}/time-entries",
            new
            {
                projectId,
                taskId,
                start = entry.Start.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                end = entry.End.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                description = entry.Description ?? "",
                billable = false,
                type = "REGULAR"
            }, cancellationToken);

    // return generic list to centralize the paging logic for all GET requests
    private async Task<IReadOnlyList<T>> GetPagesAsync<T>(string path, CancellationToken cancellationToken)
    {
        var records = new List<T>();
        for (var page = 1; ; page++)
        {
            var separator = path.Contains('?') ? '&' : '?';
            var batch = await SendAsync<List<T>>(HttpMethod.Get,
                $"{path}{separator}page={page}&page-size={PageSize}", null, cancellationToken);
            records.AddRange(batch);
            if (batch.Count < PageSize)
                return records;
        }
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.WorkspaceId))
            throw new ValidationException("Configure Clockify:ApiKey and Clockify:WorkspaceId before using the integration.");

        var uri = new Uri($"https://api.clockify.me/api/v1/workspaces/{Segment(_options.WorkspaceId)}/{path}");
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Api-Key", _options.ApiKey);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var explanation = (int)response.StatusCode switch
                {
                    401 => "Check the API key.",
                    403 => "Check workspace permissions and subscription features.",
                    404 => "Check the workspace and record IDs.",
                    429 => "The rate limit was reached. Wait before retrying.",
                    _ => "Check the request and workspace settings."
                };
                throw new ClockifyApiException(
                    $"Clockify returned HTTP {(int)response.StatusCode}. {explanation}",
                    (int)response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                ?? throw new ClockifyApiException("Clockify returned an empty response.");
        }
        catch (HttpRequestException)
        {
            throw new ClockifyApiException("Unable to reach Clockify. A write may have completed remotely; retry the import to reconcile records.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ClockifyApiException("Clockify timed out. A write may have completed remotely; retry the import to reconcile records.");
        }
        catch (JsonException)
        {
            throw new ClockifyApiException("Clockify returned an unexpected response format.");
        }
    }

    private static string Segment(string value) => Uri.EscapeDataString(value);
}

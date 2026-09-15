namespace EnozomTask.API.Contracts;

public record ApiErrorResponse(
    string Error,
    string TraceId,
    int? RemoteStatusCode = null,
    IDictionary<string, string[]>? ValidationErrors = null);

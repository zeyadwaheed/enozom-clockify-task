using EnozomTask.API.Contracts;
using EnozomTask.Service.Exceptions;

namespace EnozomTask.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller disconnected; do not turn cancellation into an application failure.
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
                throw;

            var statusCode = exception switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                NotFoundException => StatusCodes.Status404NotFound,
                ClockifyApiException => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status500InternalServerError
            };

            var message = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message;

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled request failure. TraceId: {TraceId}", context.TraceIdentifier);
            else if (exception is ClockifyApiException remoteError)
                _logger.LogWarning("Clockify request failed with remote status {StatusCode}. TraceId: {TraceId}",
                    remoteError.RemoteStatusCode, context.TraceIdentifier);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
                message,
                context.TraceIdentifier,
                (exception as ClockifyApiException)?.RemoteStatusCode),
                cancellationToken: context.RequestAborted);
        }
    }
}

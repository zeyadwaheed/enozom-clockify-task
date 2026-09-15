using EnozomTask.API.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EnozomTask.API.Configuration;

public static class ApiConfiguration
{
    public static void ConfigureModelValidation(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(field => field.Value?.Errors.Count > 0)
                .ToDictionary(
                    field => field.Key,
                    field => field.Value!.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid."
                            : error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new ApiErrorResponse(
                "Request validation failed.",
                context.HttpContext.TraceIdentifier,
                ValidationErrors: errors));
        };
    }
}

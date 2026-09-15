using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnozomTask.API.Swagger;

public class CsvResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath != "api/reports/time/csv")
            return;

        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Downloadable CSV of completed tracked time.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["text/csv"] = new()
                {
                    Schema = new OpenApiSchema { Type = "string", Format = "binary" }
                }
            },
            Headers = new Dictionary<string, OpenApiHeader>
            {
                ["X-Running-Entries-Excluded"] = new()
                {
                    Description = "Number of running timers excluded from the report.",
                    Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
                }
            }
        };
    }
}

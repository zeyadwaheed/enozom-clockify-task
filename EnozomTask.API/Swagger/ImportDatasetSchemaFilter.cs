using EnozomTask.Service.DTOs;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnozomTask.API.Swagger;

public class ImportDatasetSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(ImportDatasetRequest))
            return;

        schema.Example = new OpenApiObject
        {
            ["tasks"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["projectName"] = new OpenApiString("Website Redesign"),
                    ["taskName"] = new OpenApiString("Homepage Mockup"),
                    ["assignedUserClockifyId"] = new OpenApiString("REPLACE_WITH_WORKSPACE_USER_ID"),
                    ["originalEstimateHours"] = new OpenApiInteger(7)
                }
            },
            ["timeEntries"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["userClockifyId"] = new OpenApiString("REPLACE_WITH_WORKSPACE_USER_ID"),
                    ["projectName"] = new OpenApiString("Website Redesign"),
                    ["taskName"] = new OpenApiString("Homepage Mockup"),
                    ["start"] = new OpenApiString("2025-07-20T09:15:00Z"),
                    ["end"] = new OpenApiString("2025-07-20T11:45:00Z")
                }
            }
        };
    }
}

using System.Text.Json.Serialization;
using EnozomTask.API.Configuration;
using EnozomTask.API.Middleware;
using EnozomTask.API.Swagger;
using EnozomTask.Data;
using EnozomTask.Repository.Implementations;
using EnozomTask.Repository.Interfaces;
using EnozomTask.Service.Factories;
using EnozomTask.Service.Implementations;
using EnozomTask.Service.Integrations.Clockify;
using EnozomTask.Service.Interfaces;
using EnozomTask.Service.Options;
using EnozomTask.Service.Strategies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(ApiConfiguration.ConfigureModelValidation);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Enozom Clockify API",
        Version = "v1",
        Description = "Import Clockify datasets, synchronize local records, and export tracked-time reports."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "EnozomTask.API.xml"));
    options.SchemaFilter<ImportDatasetSchemaFilter>();
    options.OperationFilter<CsvResponseOperationFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.Configure<ClockifyOptions>(builder.Configuration.GetSection("Clockify"));
builder.Services.AddHttpClient<IClockifyClient, ClockifyClient>(client =>
    client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddSingleton<IClockifyEntityFactory, ClockifyEntityFactory>();
builder.Services.AddSingleton<IImportValidationStrategy, TaskImportValidationStrategy>();
builder.Services.AddSingleton<IImportValidationStrategy, TimeEntryImportValidationStrategy>();
builder.Services.AddScoped<IClockifyService, ClockifyService>();
builder.Services.AddScoped<ITimeReportService, TimeReportService>();


var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

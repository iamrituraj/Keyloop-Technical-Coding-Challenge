using Keyloop.Scheduler.Api.Middleware;
using Keyloop.Scheduler.Core;
using Keyloop.Scheduler.Infrastructure;
using Keyloop.Scheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Keyloop Unified Service Scheduler API",
        Version = "v1",
        Description = "Enterprise multi-resource constraint scheduling engine for automotive retail and workshop management."
    });
});

// Dependency Injection & Persistence
var dbPath = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=scheduler.db;Cache=Shared";

builder.Services.AddSchedulerInfrastructure(dbPath);
builder.Services.AddSchedulerCore();

// CORS for Frontend Access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate and Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    await DatabaseSeeder.InitializeAndSeedAsync(context);
}

// Middleware Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Keyloop Scheduler API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();

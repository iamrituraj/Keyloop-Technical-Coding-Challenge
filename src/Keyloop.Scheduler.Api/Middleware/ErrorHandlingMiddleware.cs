using System.Net;
using System.Text.Json;
using Keyloop.Scheduler.Domain.Exceptions;

namespace Keyloop.Scheduler.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        string correlationId;
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            correlationId = headerVal.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        context.Response.Headers["X-Correlation-ID"] = correlationId;
        _logger.LogError(exception, "[{CorrelationId}] Request execution failed: {Message}", correlationId, exception.Message);

        var code = HttpStatusCode.InternalServerError;
        var title = "An unexpected error occurred.";
        List<string>? suggestedAlternatives = null;

        switch (exception)
        {
            case ResourceNotFoundException:
                code = HttpStatusCode.NotFound;
                title = "Resource Not Found";
                break;
            case ResourceConflictException conflictEx:
                code = HttpStatusCode.Conflict;
                title = "Scheduling Conflict / Double Booking Detected";
                if (conflictEx.SuggestedAlternatives.Any())
                {
                    suggestedAlternatives = conflictEx.SuggestedAlternatives;
                }
                break;
            case BusinessRuleValidationException:
                code = HttpStatusCode.BadRequest;
                title = "Validation Failed";
                break;
            case InvalidOperationException when exception.Message.Contains("Concurrency conflict"):
                code = HttpStatusCode.Conflict;
                title = "Concurrent Booking Collision";
                break;
        }

        var response = new
        {
            type = $"https://httpstatuses.com/{(int)code}",
            title,
            status = (int)code,
            detail = exception.Message,
            suggestedAlternatives,
            correlationId,
            timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

using System.Net;
using System.Text.Json;
using EmployeeManagement.Application.Common.Exceptions;

namespace EmployeeManagement.API.Middleware;

/// <summary>
/// Global exception handling middleware. Catches unhandled exceptions, logs them,
/// and returns a consistent problem-details style JSON response instead of leaking
/// stack traces to API consumers.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, exception);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ApiException apiException => apiException.StatusCode,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var detail = _environment.IsDevelopment() ? exception.ToString() : null;
        if (exception is ApiException)
        {
            detail = _environment.IsDevelopment() ? exception.Message : null;
        }

        var response = new
        {
            status = context.Response.StatusCode,
            title = exception is ApiException ? exception.Message : "An unexpected error occurred while processing the request.",
            detail,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

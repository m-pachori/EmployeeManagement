using System.Net;
using System.Text.Json;
using EmployeeManagement.Application.Common.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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

        var isDuplicateKeyViolation = exception is DbUpdateException dbUpdateException && IsUniqueConstraintViolation(dbUpdateException);

        context.Response.StatusCode = exception switch
        {
            ApiException apiException => apiException.StatusCode,
            _ when isDuplicateKeyViolation => (int)HttpStatusCode.Conflict,
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

        var title = exception switch
        {
            ApiException apiException => apiException.Message,
            _ when isDuplicateKeyViolation => "A record with the same unique value already exists.",
            _ => "An unexpected error occurred while processing the request."
        };

        var response = new
        {
            status = context.Response.StatusCode,
            title,
            detail,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    // Detects a SQL Server unique index/constraint violation (2601/2627) surfaced through
    // EF Core's DbUpdateException, so concurrent duplicate-name/code races (TOCTOU between
    // an application-level uniqueness check and SaveChanges) return a clean 409 Conflict
    // instead of an opaque 500.
    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number == 2601 || sqlException.Number == 2627);
    }
}

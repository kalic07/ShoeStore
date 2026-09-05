using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Exceptions;

namespace Shared.Infrastructure.Middleware;

/// <summary>
/// Centralized exception handling for every service. Converts both expected
/// (<see cref="AppException"/>) and unexpected exceptions into RFC 7807
/// ProblemDetails responses, and makes sure nothing unhandled ever leaks a
/// stack trace to a client outside Development.
/// </summary>
public sealed class ExceptionHandlingMiddleware
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
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = MapException(exception);

        // Expected AppExceptions are routine (not-found, conflicts, validation) - log as
        // Warning. Anything else is a bug and gets logged as Error with full stack trace.
        if (exception is AppException)
        {
            _logger.LogWarning(exception, "Handled application exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception is AppException || _environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Please try again later.",
            Instance = context.Request.Path,
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ValidationAppException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }

    private static (HttpStatusCode StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
        ConflictException => (HttpStatusCode.Conflict, "Conflict"),
        ValidationAppException => (HttpStatusCode.BadRequest, "Validation failed"),
        ForbiddenException => (HttpStatusCode.Forbidden, "Forbidden"),
        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred"),
    };
}

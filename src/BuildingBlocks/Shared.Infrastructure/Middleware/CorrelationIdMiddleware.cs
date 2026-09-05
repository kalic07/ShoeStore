using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Infrastructure.Middleware;

/// <summary>
/// Propagates (or creates) an "X-Correlation-Id" header across service
/// boundaries and pushes it into the Serilog log context, so a single
/// customer request can be traced across the API Gateway, Order.Api,
/// Payment.Api, etc. in aggregated logs.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

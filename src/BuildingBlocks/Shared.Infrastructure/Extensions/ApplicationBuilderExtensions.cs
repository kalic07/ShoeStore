using Microsoft.AspNetCore.Builder;
using Shared.Infrastructure.Middleware;

namespace Shared.Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Wires up the standard middleware pipeline shared by every service:
    /// correlation-id propagation first, then centralized exception handling
    /// wrapping everything downstream.
    /// </summary>
    public static IApplicationBuilder UseSharedRequestPipeline(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }
}

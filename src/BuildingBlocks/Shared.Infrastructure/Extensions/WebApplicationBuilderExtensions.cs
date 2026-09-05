using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Shared.Infrastructure.Extensions;

public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures structured (JSON-friendly) console logging via Serilog,
    /// enriched with the service name and environment so logs from every
    /// microservice can be aggregated and filtered centrally (e.g. in
    /// Seq/ELK/Grafana Loki) by "Service" = "order-api", etc.
    /// </summary>
    public static WebApplicationBuilder AddSharedSerilogLogging(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console();
        });

        return builder;
    }
}

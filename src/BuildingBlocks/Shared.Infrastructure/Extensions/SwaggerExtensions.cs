using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Shared.Infrastructure.Extensions;

public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger/OpenAPI with a JWT "Bearer" security scheme pre-wired,
    /// so every service's /swagger UI lets you paste a token and call
    /// protected endpoints directly.
    /// </summary>
    public static IServiceCollection AddSharedSwagger(this IServiceCollection services, string title, string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter: Bearer {your JWT token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() },
            });
        });

        return services;
    }
}

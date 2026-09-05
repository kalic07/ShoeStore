using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging(serviceName: "api-gateway");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("StorefrontPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSharedRequestPipeline();

app.UseCors("StorefrontPolicy");

app.MapHealthChecks("/health");

// Single entry point the React SPA talks to: it forwards
//   /api/auth/**      -> Identity.Api
//   /api/products/**  -> Catalog.Api
//   /api/orders/**    -> Order.Api
//   /api/payments/**  -> Payment.Api
// so the frontend never needs to know each microservice's own host/port, and
// new services/routes can be added here without redeploying the SPA.
app.MapReverseProxy();

app.Run();

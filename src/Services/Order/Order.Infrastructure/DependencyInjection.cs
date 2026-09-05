using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Common;
using Order.Infrastructure.ExternalServices;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.Persistence;
using Polly;
using Polly.Extensions.Http;
using Shared.Infrastructure.Messaging;

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("OrderDb")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.Configure<CatalogClientOptions>(configuration.GetSection(CatalogClientOptions.SectionName));
        services.AddTransient<BearerTokenPropagationHandler>();

        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CatalogClientOptions>>().Value;
                client.BaseAddress = new Uri(options.CatalogApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<BearerTokenPropagationHandler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddSharedMassTransit(configuration, busConfigurator =>
        {
            busConfigurator.AddConsumer<PaymentSucceededConsumer>();
            busConfigurator.AddConsumer<PaymentFailedConsumer>();

            // EF Core transactional outbox: outgoing Publish() calls made against
            // OrderDbContext are staged in the Outbox tables and delivered by this
            // bus-outbox delivery service, guaranteeing at-least-once delivery
            // even across process restarts/broker outages.
            busConfigurator.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });
        });

        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        var rabbitConnectionString = $"amqp://{rabbitMqOptions.Username}:{rabbitMqOptions.Password}@{rabbitMqOptions.Host}:5672";

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("OrderDb")!, name: "order-db")
            .AddRabbitMQ(rabbitConnectionString: rabbitConnectionString, name: "rabbitmq", tags: new[] { "ready" });

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));
}

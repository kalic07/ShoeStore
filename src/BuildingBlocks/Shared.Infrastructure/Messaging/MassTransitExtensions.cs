using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Messaging;

public static class MassTransitExtensions
{
    /// <summary>
    /// Registers MassTransit with RabbitMQ as the transport, using the
    /// shared "RabbitMq" configuration section. The <paramref name="configureBus"/>
    /// callback lets each service register its own consumers/sagas before
    /// the bus is finalized, while the exchange/queue naming convention and
    /// retry policy stay consistent everywhere.
    /// </summary>
    public static IServiceCollection AddSharedMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator> configureBus)
    {
        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("Missing 'RabbitMq' configuration section.");

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            configureBus(busConfigurator);

            busConfigurator.UsingRabbitMq((context, rabbitConfigurator) =>
            {
                rabbitConfigurator.Host(rabbitMqOptions.Host, "/", hostConfigurator =>
                {
                    hostConfigurator.Username(rabbitMqOptions.Username);
                    hostConfigurator.Password(rabbitMqOptions.Password);
                });

                // Retry transient consumer failures with backoff before the
                // message is moved to its _error queue.
                rabbitConfigurator.UseMessageRetry(retryConfigurator =>
                    retryConfigurator.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));

                rabbitConfigurator.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

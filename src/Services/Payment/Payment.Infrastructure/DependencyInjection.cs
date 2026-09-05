using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Common;
using Payment.Infrastructure.Gateway;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Shared.Infrastructure.Messaging;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PaymentDb")));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();

        services.AddSharedMassTransit(configuration, busConfigurator =>
        {
            busConfigurator.AddConsumer<OrderCreatedConsumer>();

            busConfigurator.AddEntityFrameworkOutbox<PaymentDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });
        });

        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        var rabbitConnectionString = $"amqp://{rabbitMqOptions.Username}:{rabbitMqOptions.Password}@{rabbitMqOptions.Host}:5672";

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("PaymentDb")!, name: "payment-db")
            .AddRabbitMQ(rabbitConnectionString: rabbitConnectionString, name: "rabbitmq", tags: new[] { "ready" });

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Payment.Application;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging(serviceName: "payment-api");

builder.Services.AddControllers();
builder.Services.AddSharedSwagger("ShoeStore Payment API");
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddPaymentApplication();
builder.Services.AddPaymentInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSharedRequestPipeline();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

    // See Identity.Api/Program.cs for why EnsureCreatedAsync() is used here
    // instead of Database.MigrateAsync() + generated migrations in this scaffold.
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program { }

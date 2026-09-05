using Microsoft.EntityFrameworkCore;
using Order.Api.Services;
using Order.Application;
using Order.Application.Common;
using Order.Infrastructure;
using Order.Infrastructure.Persistence;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging(serviceName: "order-api");

builder.Services.AddControllers();
builder.Services.AddSharedSwagger("ShoeStore Order API");
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddOrderApplication();
builder.Services.AddOrderInfrastructure(builder.Configuration);

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
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

    // See Identity.Api/Program.cs for why EnsureCreatedAsync() is used here
    // instead of Database.MigrateAsync() + generated migrations in this scaffold.
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program { }

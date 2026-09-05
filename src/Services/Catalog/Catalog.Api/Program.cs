using Catalog.Api.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging(serviceName: "catalog-api");

builder.Services.AddControllers();
builder.Services.AddSharedSwagger("ShoeStore Catalog API");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CatalogDb")));

builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("CatalogDb")!, name: "catalog-db");

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
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    // See Identity.Api/Program.cs for why EnsureCreatedAsync() is used in this
    // scaffold instead of Database.MigrateAsync() + generated migrations.
    await dbContext.Database.EnsureCreatedAsync();
    await CatalogSeeder.SeedAsync(dbContext);
}

app.Run();

public partial class Program { }

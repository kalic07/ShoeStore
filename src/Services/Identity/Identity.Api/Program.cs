using Identity.Api.Data;
using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging(serviceName: "identity-api");

builder.Services.AddControllers();
builder.Services.AddSharedSwagger("ShoeStore Identity API");

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Best-practice password/account policy (tune to your compliance needs).
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("IdentityDb")!, name: "identity-db");

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

await SeedRolesAsync(app);

app.Run();

static async Task SeedRolesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

    // NOTE: this scaffold uses EnsureCreatedAsync() so `docker-compose up` works
    // immediately with zero extra steps (no EF tool / SDK install required to
    // generate migrations). Before shipping to production, switch to real EF Core
    // migrations for controlled, reviewable schema changes:
    //   dotnet ef migrations add InitialCreate -p Identity.Api
    // and replace this call with `await dbContext.Database.MigrateAsync();`.
    await dbContext.Database.EnsureCreatedAsync();

    foreach (var roleName in new[] { "Customer", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }

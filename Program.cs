

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. DATABASE CONFIGURATION
// ==========================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==========================================
// 2. ASP.NET CORE IDENTITY
// ==========================================

builder.Services.AddDefaultIdentity<IdentityUser>(
    options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ==========================================
// 3. MVC AND RAZOR PAGES
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ==========================================
// 4. SWAGGER INTEGRATION
// ==========================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "RaceDay API documentation for PROG6212 Part 2. " +
            "The system supports authentication, user profiles, " +
            "race events, categories, participant enrolments, " +
            "race results and role-based access control."
    });
});

// ==========================================
// 5. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 6. INTEGRATION TESTING CONFIGURATION
// ==========================================

// Integration tests use a separate database setup.
// Skip normal migrations and seeding in test mode.

var isIntegrationTest =
    app.Configuration.GetValue<bool>(
        "RaceDay:IntegrationTesting");

// ==========================================
// 7. DATABASE MIGRATIONS AND ROLE SEEDING
// ==========================================

if (!isIntegrationTest)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var db =
            services.GetRequiredService<ApplicationDbContext>();

        // Apply pending SQL Server migrations.
        await db.Database.MigrateAsync();

        // Create application roles if missing.
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = new[]
        {
            "Admin",
            "Organiser",
            "Participant"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult =
                    await roleManager.CreateAsync(
                        new IdentityRole(role));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}': " +
                        string.Join(", ",
                            roleResult.Errors.Select(
                                e => e.Description)));
                }
            }
        }

        // ======================================
        // 8. ADMIN ACCOUNT SEEDING
        // ======================================

        var userManager =
            services.GetRequiredService<UserManager<IdentityUser>>();

        string email = "admin@admin.com";
        string password = "Admin@123";

        var user =
            await userManager.FindByEmailAsync(email);

        // Create Admin account if it doesn't exist.
        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(
                    user,
                    password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to create Admin account: " +
                    string.Join(", ",
                        createResult.Errors.Select(
                            e => e.Description)));
            }
        }

        // Assign Admin role if missing.
        if (!await userManager.IsInRoleAsync(
            user, "Admin"))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    user, "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign Admin role: " +
                    string.Join(", ",
                        roleResult.Errors.Select(
                            e => e.Description)));
            }
        }
    }
}

// ==========================================
// 9. HTTP REQUEST PIPELINE
// ==========================================

if (app.Environment.IsDevelopment())
{
    if (!isIntegrationTest)
    {
        app.UseMigrationsEndPoint();
    }

    // Swagger documentation.
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "RaceDay API v1");

        options.DocumentTitle =
            "RaceDay API - Swagger Documentation";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ==========================================
// 10. MIDDLEWARE CONFIGURATION
// ==========================================

app.UseHttpsRedirection();

app.UseRouting();

// Authentication must come before authorization.
app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 11. STATIC FILES AND ROUTING
// ==========================================

// Serve static assets.
app.MapStaticAssets();

// MVC controllers and Razor views.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Identity Razor Pages.
app.MapRazorPages()
    .WithStaticAssets();

// Attribute-routed API controllers.
app.MapControllers();

// ==========================================
// 12. START RACEDAY
// ==========================================

app.Run();

// Allows WebApplicationFactory<Program>
// to access the application entry point.
public partial class Program
{
}

using APPR_ST10278170_POE_PART_2.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Configuration
// ---------------------------
var configuration = builder.Configuration;
var allowAutoMigrate = configuration.GetValue<bool>("AllowAutoMigrate", false);

// ---------------------------
// Logging
// ---------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ---------------------------
// Database (SQL Server / Azure SQL)
// ---------------------------
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------------------------
// Identity (users + roles) - hardened defaults
// ---------------------------
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // For production, consider RequireConfirmedAccount = true
    options.SignIn.RequireConfirmedAccount = false;

    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Cookie security (adjust for dev if not using HTTPS)
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.Cookie.HttpOnly = true;
    opts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    opts.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// ---------------------------
// Authorization policies
// ---------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("VolunteerOnly", p => p.RequireRole("Volunteer"));
});

// ---------------------------
// MVC / Razor
// ---------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ---------------------------
// Environment-specific middleware
// ---------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------
// Routes
// ---------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ---------------------------
// Migration + Role seeding (safe, logged, environment-aware)
// ---------------------------
// This helper runs synchronously during startup but avoids duplicate local names and handles errors.
void EnsureDatabaseAndSeedRoles(IServiceProvider services, IHostEnvironment env, ILogger logger)
{
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        if (env.IsDevelopment() || allowAutoMigrate)
        {
            logger.LogInformation("Applying EF Core migrations.");
            dbContext.Database.Migrate();
        }
        else
        {
            logger.LogInformation("Skipping automatic migrations (not Development and AllowAutoMigrate=false).");
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { "Admin", "Volunteer" };

        foreach (var role in roles)
        {
            var exists = roleManager.RoleExistsAsync(role).GetAwaiter().GetResult();
            if (!exists)
            {
                logger.LogInformation("Creating role {Role}", role);
                var result = roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        // Do not rethrow by default to allow the app to start for diagnostic purposes.
        // If you want fail-fast behavior, rethrow here.
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var env = services.GetRequiredService<IHostEnvironment>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    EnsureDatabaseAndSeedRoles(services, env, logger);
}

// ---------------------------
// Run
// ---------------------------
app.Run();

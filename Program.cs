// File: Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;
using WindMonitoringSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Connection String ──────────────────────────────────────────────────────────
// Use environment variable CONNECTIONSTRINGS__DEFAULTCONNECTION in production;
// falls back to appsettings.json for development.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ── Database & Identity ────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Render typically provides 'DATABASE_URL' for its internal Postgres
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    // Check if we should use Postgres
    var usePostgres = !string.IsNullOrEmpty(databaseUrl) || 
                      connectionString.StartsWith("postgres://") || 
                      connectionString.StartsWith("postgresql://") || 
                      connectionString.Contains("Host=") || 
                      !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));

    if (usePostgres)
    {
        // Use the environment variable URL if available, otherwise use the connection string
        var activeString = !string.IsNullOrEmpty(databaseUrl) ? databaseUrl : connectionString;

        // If it's a postgres:// URL, we must convert it for Npgsql
        if (activeString.StartsWith("postgres://") || activeString.StartsWith("postgresql://"))
        {
            var databaseUri = new Uri(activeString);
            var userInfo = databaseUri.UserInfo.Split(':');
            activeString = $"Host={databaseUri.Host};Port={databaseUri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={databaseUri.LocalPath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
        }
        
        options.UseNpgsql(activeString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit           = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequiredLength         = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // for Identity Razor Pages

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Wind Monitoring API", Version = "v1" });
});

// ── App Services ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISensorSimulator, SensorSimulator>();
builder.Services.AddSingleton<ApiLogger>();
builder.Services.AddHostedService<BackgroundReadingService>();

builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Seed Database on Startup ──────────────────────────────────────────────────
await DbInitializer.SeedAsync(app.Services);

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wind Monitoring API v1"));
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Identity pages (Login, Register, etc.)
app.MapHub<WindMonitoringSystem.Hubs.NotificationHub>("/notificationHub");

app.Run();

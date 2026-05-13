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
    // Render provides various environment variables for the database
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
                   ?? Environment.GetEnvironmentVariable("INTERNAL_DATABASE_URL")
                   ?? Environment.GetEnvironmentVariable("EXTERNAL_DATABASE_URL");
    
    bool isRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));
    
    // Determine the final connection string to use
    string activeString = !string.IsNullOrEmpty(databaseUrl) ? databaseUrl : connectionString;

    // Decide whether to use Postgres
    bool usePostgres = isRender || 
                       activeString.StartsWith("postgres://") || 
                       activeString.StartsWith("postgresql://") || 
                       activeString.Contains("Host=");

    if (usePostgres)
    {
        // If we are forced to use Postgres but the string looks like SQL Server, 
        // it means the environment variables aren't set correctly.
        if (activeString.Contains("Trusted_Connection") || activeString.Contains("mssqllocaldb"))
        {
             // If we're on Render but have no Postgres string, this will fail anyway.
             // We'll try to use it but we really need the user to set the env var.
        }

        // Convert postgres:// URL to standard connection string if necessary
        if (activeString.StartsWith("postgres://") || activeString.StartsWith("postgresql://"))
        {
            try 
            {
                var databaseUri = new Uri(activeString);
                var userInfo = databaseUri.UserInfo.Split(':');
                activeString = $"Host={databaseUri.Host};Port={databaseUri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={databaseUri.LocalPath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
            }
            catch { /* fallback to original if URI parsing fails */ }
        }
        
        // Final safety: Remove SQL Server specific keywords that crash Npgsql
        activeString = activeString.Replace("Trusted_Connection=True;", "")
                                   .Replace("Trusted_Connection=true;", "")
                                   .Replace("MultipleActiveResultSets=true;", "")
                                   .Replace("MultipleActiveResultSets=True;", "");
        
        options.UseNpgsql(activeString);
    }
    else
    {
        options.UseSqlServer(activeString);
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

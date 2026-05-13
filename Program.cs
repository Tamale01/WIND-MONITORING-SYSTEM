// File: Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;
using WindMonitoringSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Connection String ──────────────────────────────────────────────────────────
// builder.Configuration.GetConnectionString("DefaultConnection") logic moved inside AddDbContext.


// ── Database & Identity ────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // 1. Try to get connection string from DATABASE_URL (Render Postgres)
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
             ?? Environment.GetEnvironmentVariable("INTERNAL_DATABASE_URL")
             ?? Environment.GetEnvironmentVariable("EXTERNAL_DATABASE_URL");

    // 2. Fallback to appsettings / environment ConnectionStrings:DefaultConnection
    var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");

    bool isRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));

    if (!string.IsNullOrEmpty(dbUrl))
    {
        Console.WriteLine("DB Config: Using DATABASE_URL environment variable.");
        // 1. Clean the string
        dbUrl = dbUrl.Trim().Trim('"').Trim('\'');
        string connStr = dbUrl;

        // 2. If it's a URL, ensure SSL settings are present
        if (dbUrl.Contains("://") && !dbUrl.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
        {
            dbUrl += (dbUrl.Contains("?") ? "&" : "?") + "sslmode=require&TrustServerCertificate=true";
            connStr = dbUrl;
        }

        // 3. Debug logging (masking password)
        var masked = connStr;
        if (masked.Contains("://") && masked.Contains(":") && masked.Contains("@"))
        {
             var start = masked.IndexOf("://") + 3;
             var end = masked.LastIndexOf("@");
             var userInfo = masked.Substring(start, end - start);
             if (userInfo.Contains(":"))
             {
                 var user = userInfo.Split(':')[0];
                 masked = masked.Replace(userInfo, $"{user}:********");
             }
        }
        Console.WriteLine($"DB Config: Connection string being used: {masked}");
        
        options.UseNpgsql(connStr);
    }
    else if (isRender)
    {
        // We are on Render but DATABASE_URL is missing. 
        // Do not fall back to SQL Server as it will crash.
        throw new InvalidOperationException("CRITICAL: Running on Render but 'DATABASE_URL' is not set. " + 
            "Please add 'DATABASE_URL' to your Render Environment Variables with your PostgreSQL connection string.");
    }
    else if (!string.IsNullOrEmpty(defaultConn))
    {
        // Local development or manual config
        if (defaultConn.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
            defaultConn.Contains("Username=", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("DB Config: Using Postgres connection string from DefaultConnection.");
            options.UseNpgsql(defaultConn);
        }
        else
        {
            Console.WriteLine("DB Config: Using SQL Server connection string from DefaultConnection.");
            options.UseSqlServer(defaultConn);
        }
    }
    else
    {
        throw new InvalidOperationException("Database connection string not found. Please set DATABASE_URL or ConnectionStrings:DefaultConnection.");
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

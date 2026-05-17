// File: Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;
using WindMonitoringSystem.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ── Connection String ──────────────────────────────────────────────────────────
// builder.Configuration.GetConnectionString("DefaultConnection") logic moved inside AddDbContext.


// ── Database & Identity ────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // 1. Try to get connection string from DATABASE_URL (Render Postgres)
    // Priority: INTERNAL_DATABASE_URL > DATABASE_URL > EXTERNAL_DATABASE_URL
    var dbUrl = Environment.GetEnvironmentVariable("INTERNAL_DATABASE_URL")
             ?? Environment.GetEnvironmentVariable("DATABASE_URL") 
             ?? Environment.GetEnvironmentVariable("EXTERNAL_DATABASE_URL");

    // 2. Fallback to appsettings / environment ConnectionStrings:DefaultConnection
    var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");

    bool isRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));

    if (!string.IsNullOrEmpty(dbUrl)) 
    {
        dbUrl = dbUrl.Trim().Trim('"').Trim('\'');
        string connStr = dbUrl;

        // Render provides a URI like postgresql://user:pass@host/db
        // Npgsql supports this, but manual parsing is safer for injecting SSL requirements.
        if (dbUrl.Contains("://"))
        {
            try 
            {
                var uri = new Uri(dbUrl);
                var userInfo = (uri.UserInfo ?? "").Split(':');
                var user = Uri.UnescapeDataString(userInfo[0]);
                var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');
                
                // Remove query parameters from database name if present in path
                if (database.Contains("?")) database = database.Split('?')[0];

                var npgsqlBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = host,
                    Port = port,
                    Username = user,
                    Password = pass,
                    Database = database,
                    SslMode = SslMode.Require,
                    TrustServerCertificate = true, // Often required for Render's managed Postgres
                    Pooling = true,
                    // KeepAlive is helpful for long-running background tasks on some platforms
                    KeepAlive = 30 
                };
                connStr = npgsqlBuilder.ToString();
                
                Console.WriteLine($"DB Config: Successfully parsed DATABASE_URL into Npgsql format (Host={host}, Database={database}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Config: WARNING - Manual parse failed ({ex.Message}). Using raw string.");
                // We fallback to the raw string, but we should still try to append SSL if it's a URI
                if (!dbUrl.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
                {
                    connStr = dbUrl + (dbUrl.Contains("?") ? "&" : "?") + "sslmode=require&TrustServerCertificate=true";
                }
            }
        }
        else
        {
            Console.WriteLine("DB Config: Using DATABASE_URL as a standard connection string.");
        }
        
        options.UseNpgsql(connStr);
    }
    else if (isRender)
    {
        throw new InvalidOperationException("CRITICAL: Running on Render but 'DATABASE_URL' is not set.");
    }
    else if (!string.IsNullOrEmpty(defaultConn))
    {
        if (defaultConn.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
            defaultConn.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
            defaultConn.Contains("postgresql://", StringComparison.OrdinalIgnoreCase))
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

// Configure Forwarded Headers for Render (behind load balancer)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Seed Database on Startup ──────────────────────────────────────────────────
await DbInitializer.SeedAsync(app.Services);

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseForwardedHeaders();

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

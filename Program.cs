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
    options.UseSqlServer(connectionString));

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

app.Run();

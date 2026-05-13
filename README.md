# Wind Monitoring System

A real-time wind speed monitoring web application built with **ASP.NET Core 8 MVC**, **Entity Framework Core (Code-First)**, **SQL Server LocalDB**, and **ASP.NET Core Identity**.

---

## Features

- 🌬️ **Live Dashboard** — wind speed auto-refreshes every 5 seconds with an animated SVG gauge
- 📊 **Historical Charts** — Chart.js line charts with 6h / 24h / 7-day range pickers and statistics
- 🔐 **Authentication** — register/login via ASP.NET Core Identity
- 🔌 **REST API** — `GET /api/wind/latest`, `GET /api/wind/history`, `POST /api/wind` (API key or auth)
- 🤖 **Background Simulator** — generates a new reading every 10 seconds automatically
- 🛠️ **Admin Panel** — generate 100 test readings or clear all simulated data
- 📝 **API Logging** — all API calls logged to `logs/api_log.txt`
- 📖 **Swagger UI** — available at `/swagger` in development

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`

---

## Setup Steps

```bash
# 1. Restore NuGet packages
dotnet restore

# 2. Apply database migrations (creates the LocalDB database)
dotnet ef migrations add InitialCreate
dotnet ef database update

# 3. Run the application
dotnet run
```

Then open: **https://localhost:5001** (or the URL shown in the terminal).

---

## Default Admin Account

| Field    | Value                    |
|----------|--------------------------|
| Email    | admin@windmonitor.com    |
| Password | Admin@123                |
| Role     | Admin                    |

The admin user and 50 sample readings are seeded automatically on first run.

---

## API Reference

| Method | Endpoint                          | Auth Required        | Description                        |
|--------|-----------------------------------|----------------------|------------------------------------|
| GET    | `/api/wind/status`                | None (public)        | System health check                |
| GET    | `/api/wind/latest`                | Login                | Most recent wind reading           |
| GET    | `/api/wind/history?hours=24`      | Login                | Readings for last N hours (max 168)|
| POST   | `/api/wind`                       | Login **or** API Key | Submit real sensor reading         |

### API Key Usage (for real hardware)
```http
POST /api/wind
X-Api-Key: wms-secret-api-key-2024
Content-Type: application/json

{ "windSpeed": 12.5, "sensorId": "SENSOR-A1" }
```

---

## Project Structure

```
WindMonitoringSystem/
├── Controllers/
│   ├── HomeController.cs          # Public home page
│   ├── DashboardController.cs     # Auth-required live dashboard
│   ├── ChartsController.cs        # Auth-required charts page
│   ├── AdminController.cs         # Admin-only data management
│   └── WindApiController.cs       # REST API endpoints
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext + Identity
│   └── DbInitializer.cs           # Seed data + admin user
├── Models/
│   └── WindReading.cs             # Main entity
├── Services/
│   ├── ISensorSimulator.cs        # Sensor interface
│   ├── SensorSimulator.cs         # Simulated random readings
│   ├── BackgroundReadingService.cs # IHostedService (every 10s)
│   └── ApiLogger.cs               # File-based API logger
├── Views/
│   ├── Shared/_Layout.cshtml      # Bootstrap 5 dark layout
│   ├── Home/Index.cshtml          # Public landing page
│   ├── Dashboard/Index.cshtml     # Live dashboard
│   ├── Charts/Index.cshtml        # Chart.js history
│   └── Admin/Index.cshtml         # Admin panel
├── wwwroot/
│   ├── js/dashboard.js            # Live polling logic
│   └── js/charts.js               # Chart.js integration
├── logs/
│   └── api_log.txt                # Auto-created at runtime
├── Program.cs                     # App configuration
└── appsettings.json               # Connection string + API key
```

---

## Configuration

### Connection String
Edit `appsettings.json` for development:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WindMonitoringDb;Trusted_Connection=True;"
}
```

For production, set the environment variable:
```
CONNECTIONSTRINGS__DEFAULTCONNECTION=<your-production-connection-string>
```

### API Key
Change the default API key in `appsettings.json`:
```json
"ApiKey": "your-secure-api-key-here"
```

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["WindMonitoringSystem.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build "WindMonitoringSystem.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WindMonitoringSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
# Render uses the PORT environment variable
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "WindMonitoringSystem.dll"]

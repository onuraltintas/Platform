# Use the official ASP.NET Core runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Services/SpeedReading.ContentService/SpeedReading.ContentService.csproj", "Services/SpeedReading.ContentService/"]
COPY ["Shared/Security/EgitimPlatform.Shared.Security.csproj", "Shared/Security/"]
COPY ["Shared/Errors/EgitimPlatform.Shared.Errors.csproj", "Shared/Errors/"]
COPY ["Shared/Logging/EgitimPlatform.Shared.Logging.csproj", "Shared/Logging/"]
COPY ["Shared/Configuration/EgitimPlatform.Shared.Configuration.csproj", "Shared/Configuration/"]
COPY ["Shared/Observability/EgitimPlatform.Shared.Observability.csproj", "Shared/Observability/"]
COPY ["Shared/Auditing/EgitimPlatform.Shared.Auditing.csproj", "Shared/Auditing/"]

RUN dotnet restore "Services/SpeedReading.ContentService/SpeedReading.ContentService.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/Services/SpeedReading.ContentService"
RUN dotnet build "SpeedReading.ContentService.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SpeedReading.ContentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "SpeedReading.ContentService.dll"]


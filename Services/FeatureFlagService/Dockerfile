# Use the official ASP.NET Core runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_URLS=http://+:80

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Services/FeatureFlagService/EgitimPlatform.Services.FeatureFlagService.csproj", "Services/FeatureFlagService/"]
COPY ["Shared/Security/EgitimPlatform.Shared.Security.csproj", "Shared/Security/"]
COPY ["Shared/Errors/EgitimPlatform.Shared.Errors.csproj", "Shared/Errors/"]
COPY ["Shared/Logging/EgitimPlatform.Shared.Logging.csproj", "Shared/Logging/"]
COPY ["Shared/Configuration/EgitimPlatform.Shared.Configuration.csproj", "Shared/Configuration/"]
COPY ["Shared/Messaging/EgitimPlatform.Shared.Messaging.csproj", "Shared/Messaging/"]

RUN dotnet restore "Services/FeatureFlagService/EgitimPlatform.Services.FeatureFlagService.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/Services/FeatureFlagService"
RUN dotnet build "EgitimPlatform.Services.FeatureFlagService.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "EgitimPlatform.Services.FeatureFlagService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80;https://+:443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "EgitimPlatform.Services.FeatureFlagService.dll"]
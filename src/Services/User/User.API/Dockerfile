# User API Dockerfile - Optimized for fast builds
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project files first for better layer caching
COPY ["Directory.Packages.props", "."]
COPY ["src/Services/User/User.Core/User.Core.csproj", "src/Services/User/User.Core/"]
COPY ["src/Services/User/User.Application/User.Application.csproj", "src/Services/User/User.Application/"]
COPY ["src/Services/User/User.Infrastructure/User.Infrastructure.csproj", "src/Services/User/User.Infrastructure/"]
COPY ["src/Services/User/User.API/User.API.csproj", "src/Services/User/User.API/"]

# Copy all required shared project files
COPY ["src/Shared/Enterprise.Shared.Common/Enterprise.Shared.Common.csproj", "src/Shared/Enterprise.Shared.Common/"]
COPY ["src/Shared/Enterprise.Shared.ErrorHandling/Enterprise.Shared.ErrorHandling.csproj", "src/Shared/Enterprise.Shared.ErrorHandling/"]
COPY ["src/Shared/Enterprise.Shared.Caching/Enterprise.Shared.Caching.csproj", "src/Shared/Enterprise.Shared.Caching/"]
COPY ["src/Shared/Enterprise.Shared.Storage/Enterprise.Shared.Storage.csproj", "src/Shared/Enterprise.Shared.Storage/"]
COPY ["src/Shared/Enterprise.Shared.Events/Enterprise.Shared.Events.csproj", "src/Shared/Enterprise.Shared.Events/"]
COPY ["src/Shared/Enterprise.Shared.Email/Enterprise.Shared.Email.csproj", "src/Shared/Enterprise.Shared.Email/"]
COPY ["src/Shared/Enterprise.Shared.Security/Enterprise.Shared.Security.csproj", "src/Shared/Enterprise.Shared.Security/"]
COPY ["src/Shared/Enterprise.Shared.Configuration/Enterprise.Shared.Configuration.csproj", "src/Shared/Enterprise.Shared.Configuration/"]
COPY ["src/Shared/Enterprise.Shared.Validation/Enterprise.Shared.Validation.csproj", "src/Shared/Enterprise.Shared.Validation/"]
COPY ["src/Shared/Enterprise.Shared.Logging/Enterprise.Shared.Logging.csproj", "src/Shared/Enterprise.Shared.Logging/"]
COPY ["src/Shared/Enterprise.Shared.Observability/Enterprise.Shared.Observability.csproj", "src/Shared/Enterprise.Shared.Observability/"]
COPY ["src/Shared/Enterprise.Shared.Privacy/Enterprise.Shared.Privacy.csproj", "src/Shared/Enterprise.Shared.Privacy/"]
COPY ["src/Shared/Enterprise.Shared.Resilience/Enterprise.Shared.Resilience.csproj", "src/Shared/Enterprise.Shared.Resilience/"]
COPY ["src/Shared/Enterprise.Shared.Permissions/Enterprise.Shared.Permissions.csproj", "src/Shared/Enterprise.Shared.Permissions/"]
COPY ["src/Shared/Enterprise.Shared.Authorization/Enterprise.Shared.Authorization.csproj", "src/Shared/Enterprise.Shared.Authorization/"]
COPY ["src/Shared/Enterprise.Shared.Auditing/Enterprise.Shared.Auditing.csproj", "src/Shared/Enterprise.Shared.Auditing/"]

# Restore dependencies
RUN dotnet restore "src/Services/User/User.API/User.API.csproj" --no-cache

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/src/Services/User/User.API"
RUN dotnet build "User.API.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "User.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for environment files
RUN mkdir -p /app/config

# Install curl for health checks (minimized)
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* && apt-get clean

# Health check with reduced frequency for faster startup
HEALTHCHECK --interval=60s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "User.API.dll"]
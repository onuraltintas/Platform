# Identity API Dockerfile - Optimized for fast builds
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project files first for better layer caching
COPY ["Directory.Packages.props", "."]
COPY ["src/Services/Identity/Identity.Core/Identity.Core.csproj", "src/Services/Identity/Identity.Core/"]
COPY ["src/Services/Identity/Identity.Application/Identity.Application.csproj", "src/Services/Identity/Identity.Application/"]
COPY ["src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/Services/Identity/Identity.Infrastructure/"]
COPY ["src/Services/Identity/Identity.API/Identity.API.csproj", "src/Services/Identity/Identity.API/"]

# Copy only required shared project files
COPY ["src/Shared/Enterprise.Shared.Common/Enterprise.Shared.Common.csproj", "src/Shared/Enterprise.Shared.Common/"]
COPY ["src/Shared/Enterprise.Shared.Security/Enterprise.Shared.Security.csproj", "src/Shared/Enterprise.Shared.Security/"]
COPY ["src/Shared/Enterprise.Shared.Auditing/Enterprise.Shared.Auditing.csproj", "src/Shared/Enterprise.Shared.Auditing/"]
COPY ["src/Shared/Enterprise.Shared.Authorization/Enterprise.Shared.Authorization.csproj", "src/Shared/Enterprise.Shared.Authorization/"]
COPY ["src/Shared/Enterprise.Shared.Permissions/Enterprise.Shared.Permissions.csproj", "src/Shared/Enterprise.Shared.Permissions/"]
COPY ["src/Shared/Enterprise.Shared.ErrorHandling/Enterprise.Shared.ErrorHandling.csproj", "src/Shared/Enterprise.Shared.ErrorHandling/"]
COPY ["src/Shared/Enterprise.Shared.Logging/Enterprise.Shared.Logging.csproj", "src/Shared/Enterprise.Shared.Logging/"]

# Restore dependencies
RUN dotnet restore "src/Services/Identity/Identity.API/Identity.API.csproj" --no-cache

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/src/Services/Identity/Identity.API"
RUN dotnet build "Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "Identity.API.dll"]
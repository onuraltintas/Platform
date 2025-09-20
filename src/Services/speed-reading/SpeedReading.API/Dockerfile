# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["src/Services/speed-reading/SpeedReading.API/SpeedReading.API.csproj", "src/Services/speed-reading/SpeedReading.API/"]
COPY ["src/Services/speed-reading/SpeedReading.Application/SpeedReading.Application.csproj", "src/Services/speed-reading/SpeedReading.Application/"]
COPY ["src/Services/speed-reading/SpeedReading.Infrastructure/SpeedReading.Infrastructure.csproj", "src/Services/speed-reading/SpeedReading.Infrastructure/"]
COPY ["src/Services/speed-reading/SpeedReading.Domain/SpeedReading.Domain.csproj", "src/Services/speed-reading/SpeedReading.Domain/"]
COPY ["src/Shared/Enterprise.Shared.Common/Enterprise.Shared.Common.csproj", "src/Shared/Enterprise.Shared.Common/"]
COPY ["src/Shared/Enterprise.Shared.Security/Enterprise.Shared.Security.csproj", "src/Shared/Enterprise.Shared.Security/"]
COPY ["src/Shared/Enterprise.Shared.ErrorHandling/Enterprise.Shared.ErrorHandling.csproj", "src/Shared/Enterprise.Shared.ErrorHandling/"]
COPY ["src/Shared/Enterprise.Shared.Logging/Enterprise.Shared.Logging.csproj", "src/Shared/Enterprise.Shared.Logging/"]
COPY ["src/Shared/Enterprise.Shared.Caching/Enterprise.Shared.Caching.csproj", "src/Shared/Enterprise.Shared.Caching/"]
COPY ["src/Shared/Enterprise.Shared.Validation/Enterprise.Shared.Validation.csproj", "src/Shared/Enterprise.Shared.Validation/"]

RUN dotnet restore "src/Services/speed-reading/SpeedReading.API/SpeedReading.API.csproj"

# Copy source code
COPY . .
WORKDIR "/src/src/Services/speed-reading/SpeedReading.API"

# Build the application
RUN dotnet build "SpeedReading.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SpeedReading.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Create logs directory and set permissions
RUN mkdir -p logs && chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "SpeedReading.API.dll"]
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/services/speed-reading-service/SpeedReading.API/SpeedReading.API.csproj", "services/speed-reading-service/SpeedReading.API/"]
COPY ["src/services/speed-reading-service/SpeedReading.Application/SpeedReading.Application.csproj", "services/speed-reading-service/SpeedReading.Application/"]
COPY ["src/services/speed-reading-service/SpeedReading.Domain/SpeedReading.Domain.csproj", "services/speed-reading-service/SpeedReading.Domain/"]
COPY ["src/services/speed-reading-service/SpeedReading.Infrastructure/SpeedReading.Infrastructure.csproj", "services/speed-reading-service/SpeedReading.Infrastructure/"]

# Copy shared libraries
COPY ["src/shared/Common/Common.csproj", "shared/Common/"]
COPY ["src/shared/Database/Database.csproj", "shared/Database/"]
COPY ["src/shared/EventBus/EventBus.csproj", "shared/EventBus/"]
COPY ["src/shared/Logging/Logging.csproj", "shared/Logging/"]

# Restore dependencies
RUN dotnet restore "services/speed-reading-service/SpeedReading.API/SpeedReading.API.csproj"

# Copy source code
COPY src/services/speed-reading-service/ services/speed-reading-service/
COPY src/shared/ shared/

# Build the application
WORKDIR "/src/services/speed-reading-service/SpeedReading.API"
RUN dotnet build "SpeedReading.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SpeedReading.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 80

# Entry point
ENTRYPOINT ["dotnet", "SpeedReading.API.dll"]
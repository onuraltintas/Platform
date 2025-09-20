# Docker Infrastructure Setup

## Genel Bakış
Bu dizin, Enterprise Platform'un Docker tabanlı geliştirme ve test ortamları için gerekli tüm konfigürasyonları içerir. Mikroservislerin yanı sıra destekleyici altyapı bileşenleri (database, cache, monitoring) için Docker Compose konfigürasyonları sağlanır.

## Dizin Yapısı
```
docker/
├── docker-compose.dev.yml           # Development ortamı
├── docker-compose.test.yml          # Test ortamı  
├── docker-compose.prod.yml          # Production ortamı
├── databases/                       # Database konfigürasyonları
│   ├── sqlserver/
│   │   ├── Dockerfile
│   │   ├── init-scripts/
│   │   └── data/
│   └── mongo/
├── cache-storage/                   # Cache ve storage
│   ├── redis/
│   │   ├── redis.conf
│   │   └── cluster/
│   └── minio/
│       ├── Dockerfile
│       └── data/
├── monitoring/                      # Monitoring stack
│   ├── prometheus/
│   ├── grafana/
│   ├── jaeger/
│   └── seq/
└── devops/                         # DevOps araçları
    ├── gitea/
    ├── sonarqube/
    └── jenkins/
```

## Hızlı Başlangıç

### Development Environment
```bash
# 1. Repository root dizine geçin
cd /path/to/PlatformV1

# 2. Development infrastructure'ı başlatın
cd infrastructure/docker
docker-compose -f docker-compose.dev.yml up -d

# 3. Services'in hazır olduğunu kontrol edin
docker-compose -f docker-compose.dev.yml ps

# 4. Database'leri initialize edin
./scripts/init-databases.sh

# 5. Test data yükleyin (opsiyonel)
./scripts/seed-test-data.sh
```

### Production Environment
```bash
# 1. Environment değişkenlerini ayarlayın
cp .env.production.example .env.production
# .env.production dosyasını düzenleyin

# 2. Production infrastructure'ı başlatın
docker-compose -f docker-compose.prod.yml up -d

# 3. Health check'leri çalıştırın
./scripts/health-check.sh
```

## Docker Compose Konfigürasyonları

### Development (docker-compose.dev.yml)
```yaml
version: '3.8'

services:
  # Databases
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "StrongPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
      - ./databases/sqlserver/init-scripts:/docker-entrypoint-initdb.d
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "StrongPassword123!" -Q "SELECT 1"
      interval: 30s
      timeout: 10s
      retries: 3

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
      - ./cache-storage/redis/redis.conf:/usr/local/etc/redis/redis.conf
    command: redis-server /usr/local/etc/redis/redis.conf
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Message Queue
  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    environment:
      RABBITMQ_DEFAULT_USER: enterprise
      RABBITMQ_DEFAULT_PASS: enterprise123
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Object Storage
  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin123
    ports:
      - "9000:9000"   # API port
      - "9001:9001"   # Console port
    volumes:
      - minio_data:/data
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Monitoring
  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:80"
    volumes:
      - seq_data:/data
    restart: unless-stopped

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      GF_SECURITY_ADMIN_PASSWORD: admin123
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/dashboards:/var/lib/grafana/dashboards
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # Jaeger UI
      - "14268:14268"  # HTTP collector
    environment:
      COLLECTOR_OTLP_ENABLED: true

volumes:
  sqlserver_data:
  redis_data:
  rabbitmq_data:
  minio_data:
  seq_data:
  prometheus_data:
  grafana_data:

networks:
  default:
    name: enterprise_network
    driver: bridge
```

### Production (docker-compose.prod.yml)
```yaml
version: '3.8'

services:
  # API Gateway
  api-gateway:
    build:
      context: ../../src/Gateways/ApiGateway
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetapp.pfx
    volumes:
      - ./certs:/app/certs:ro
      - ./logs:/app/logs
    depends_on:
      - identity-service
      - user-service
    restart: unless-stopped
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '1.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M

  # Identity Service
  identity-service:
    build:
      context: ../../src/Services/IdentityService
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${IDENTITY_DB_CONNECTION}
      - JwtSettings__SecretKey=${JWT_SECRET}
      - Redis__ConnectionString=${REDIS_CONNECTION}
    volumes:
      - ./logs:/app/logs
    depends_on:
      - sqlserver
      - redis
    restart: unless-stopped
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '0.5'
          memory: 512M

  # User Service  
  user-service:
    build:
      context: ../../src/Services/UserService
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${USER_DB_CONNECTION}
      - Redis__ConnectionString=${REDIS_CONNECTION}
      - RabbitMQ__ConnectionString=${RABBITMQ_CONNECTION}
    volumes:
      - ./logs:/app/logs
    depends_on:
      - sqlserver
      - redis
      - rabbitmq
    restart: unless-stopped
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '0.5'
          memory: 512M

  # Infrastructure Services (same as dev but with resource limits)
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: ${SQL_SA_PASSWORD}
      ACCEPT_EULA: "Y"
    volumes:
      - sqlserver_data:/var/opt/mssql
      - ./backups:/var/backups
    restart: unless-stopped
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 4G
        reservations:
          cpus: '1.0'
          memory: 2G

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
      - ./cache-storage/redis/redis.conf:/usr/local/etc/redis/redis.conf
    command: redis-server /usr/local/etc/redis/redis.conf
    restart: unless-stopped
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 2G

volumes:
  sqlserver_data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /var/lib/enterprise/sqlserver

  redis_data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /var/lib/enterprise/redis

networks:
  default:
    driver: overlay
    attachable: true
```

## Environment Konfigürasyonu

### .env.development
```env
# Database
SQL_SA_PASSWORD=StrongPassword123!
IDENTITY_DB_CONNECTION=Server=sqlserver;Database=EnterpriseIdentity;User=sa;Password=StrongPassword123!;TrustServerCertificate=true;
USER_DB_CONNECTION=Server=sqlserver;Database=EnterpriseUser;User=sa;Password=StrongPassword123!;TrustServerCertificate=true;

# Cache & Queue
REDIS_CONNECTION=redis:6379
RABBITMQ_CONNECTION=amqp://enterprise:enterprise123@rabbitmq:5672/

# Security
JWT_SECRET=YourSuperSecretKeyForJWTTokenGenerationAndValidation123!

# Storage
MINIO_ENDPOINT=minio:9000
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin123

# Monitoring
SEQ_URL=http://seq:80
JAEGER_ENDPOINT=http://jaeger:14268
PROMETHEUS_URL=http://prometheus:9090
```

### .env.production
```env
# Database (Production credentials)
SQL_SA_PASSWORD=${SQL_SA_PASSWORD_FROM_VAULT}
IDENTITY_DB_CONNECTION=Server=sqlserver;Database=EnterpriseIdentity;User=sa;Password=${SQL_SA_PASSWORD};TrustServerCertificate=false;

# Security (From secure vault)
JWT_SECRET=${JWT_SECRET_FROM_VAULT}
CERT_PASSWORD=${CERT_PASSWORD_FROM_VAULT}

# Redis Cluster
REDIS_CONNECTION=redis-cluster:6379

# Monitoring
SEQ_URL=http://seq-cluster:80
```

## Dockerfile Örnekleri

### .NET Service Dockerfile
```dockerfile
# src/Services/IdentityService/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/IdentityService/IdentityService.csproj", "src/Services/IdentityService/"]
COPY ["src/Shared/", "src/Shared/"]
RUN dotnet restore "src/Services/IdentityService/IdentityService.csproj"
COPY . .
WORKDIR "/src/src/Services/IdentityService"
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Add healthcheck
RUN apt-get update && apt-get install -y curl
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "IdentityService.dll"]
```

### Custom SQL Server Dockerfile
```dockerfile
# databases/sqlserver/Dockerfile
FROM mcr.microsoft.com/mssql/server:2022-latest

# Switch to root user for installation
USER root

# Install sqlcmd
RUN apt-get update && apt-get install -y curl

# Copy initialization scripts
COPY init-scripts/ /docker-entrypoint-initdb.d/
COPY wait-for-it.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/wait-for-it.sh

# Switch back to mssql user
USER mssql

# Expose port
EXPOSE 1433
```

## Health Checks ve Monitoring

### Health Check Script
```bash
#!/bin/bash
# scripts/health-check.sh

echo "=== Enterprise Platform Health Check ==="

# Check SQL Server
echo "Checking SQL Server..."
docker exec docker_sqlserver_1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "StrongPassword123!" -Q "SELECT 1" > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ SQL Server is healthy"
else
    echo "❌ SQL Server is unhealthy"
fi

# Check Redis
echo "Checking Redis..."
docker exec docker_redis_1 redis-cli ping > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Redis is healthy"
else
    echo "❌ Redis is unhealthy"
fi

# Check RabbitMQ
echo "Checking RabbitMQ..."
docker exec docker_rabbitmq_1 rabbitmq-diagnostics ping > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ RabbitMQ is healthy"
else
    echo "❌ RabbitMQ is unhealthy"
fi

# Check MinIO
echo "Checking MinIO..."
curl -f http://localhost:9000/minio/health/live > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ MinIO is healthy"
else
    echo "❌ MinIO is unhealthy"
fi

# Check Application Services
echo "Checking Identity Service..."
curl -f http://localhost:8081/health > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Identity Service is healthy"
else
    echo "❌ Identity Service is unhealthy"
fi

echo "Checking User Service..."
curl -f http://localhost:8082/health > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ User Service is healthy"
else
    echo "❌ User Service is unhealthy"
fi

echo "=== Health Check Complete ==="
```

## Database Initialization Scripts

### SQL Server Init Script
```sql
-- databases/sqlserver/init-scripts/01-create-databases.sql
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EnterpriseIdentity')
BEGIN
    CREATE DATABASE [EnterpriseIdentity];
END
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EnterpriseUser') 
BEGIN
    CREATE DATABASE [EnterpriseUser];
END
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EnterpriseAudit')
BEGIN
    CREATE DATABASE [EnterpriseAudit];
END
GO

-- Create application user
USE [master]
GO
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'enterprise_app')
BEGIN
    CREATE LOGIN [enterprise_app] WITH PASSWORD = 'AppPassword123!', 
        DEFAULT_DATABASE = [EnterpriseIdentity],
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = OFF;
END
GO

-- Grant permissions to databases
USE [EnterpriseIdentity]
GO
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'enterprise_app')
BEGIN
    CREATE USER [enterprise_app] FOR LOGIN [enterprise_app];
    ALTER ROLE [db_datareader] ADD MEMBER [enterprise_app];
    ALTER ROLE [db_datawriter] ADD MEMBER [enterprise_app];
    ALTER ROLE [db_ddladmin] ADD MEMBER [enterprise_app];
END
GO

USE [EnterpriseUser]
GO
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'enterprise_app')
BEGIN
    CREATE USER [enterprise_app] FOR LOGIN [enterprise_app];
    ALTER ROLE [db_datareader] ADD MEMBER [enterprise_app];
    ALTER ROLE [db_datawriter] ADD MEMBER [enterprise_app];
    ALTER ROLE [db_ddladmin] ADD MEMBER [enterprise_app];
END
GO
```

## Utility Scripts

### Start/Stop Scripts
```bash
#!/bin/bash
# scripts/start-dev.sh
echo "Starting Enterprise Platform Development Environment..."

# Pull latest images
docker-compose -f docker-compose.dev.yml pull

# Start infrastructure
docker-compose -f docker-compose.dev.yml up -d

# Wait for services to be ready
echo "Waiting for services to start..."
sleep 30

# Run health check
./health-check.sh

echo "Development environment is ready!"
echo "Access points:"
echo "- API Gateway: http://localhost:8080"
echo "- Seq Logs: http://localhost:5341"
echo "- Grafana: http://localhost:3000 (admin/admin123)"
echo "- Prometheus: http://localhost:9090"
echo "- RabbitMQ Management: http://localhost:15672 (enterprise/enterprise123)"
echo "- MinIO Console: http://localhost:9001 (minioadmin/minioadmin123)"
```

```bash
#!/bin/bash
# scripts/stop-dev.sh
echo "Stopping Enterprise Platform Development Environment..."

docker-compose -f docker-compose.dev.yml down

echo "Environment stopped."
```

### Backup Script
```bash
#!/bin/bash
# scripts/backup-dev.sh
BACKUP_DIR="./backups/$(date +%Y%m%d_%H%M%S)"
mkdir -p $BACKUP_DIR

echo "Creating backup in $BACKUP_DIR..."

# Backup SQL Server databases
docker exec docker_sqlserver_1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "StrongPassword123!" -Q "BACKUP DATABASE [EnterpriseIdentity] TO DISK = '/var/backups/identity.bak'"
docker exec docker_sqlserver_1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "StrongPassword123!" -Q "BACKUP DATABASE [EnterpriseUser] TO DISK = '/var/backups/user.bak'"

# Copy backup files
docker cp docker_sqlserver_1:/var/backups/identity.bak $BACKUP_DIR/
docker cp docker_sqlserver_1:/var/backups/user.bak $BACKUP_DIR/

# Backup Redis data
docker exec docker_redis_1 redis-cli BGSAVE
docker cp docker_redis_1:/data/dump.rdb $BACKUP_DIR/

# Backup MinIO data
docker exec docker_minio_1 mc mirror --overwrite /data $BACKUP_DIR/minio/

echo "Backup completed: $BACKUP_DIR"
```

## Troubleshooting

### Yaygın Sorunlar

#### Port Conflicts
```bash
# Port kullanımını kontrol et
netstat -tulpn | grep :1433
lsof -i :1433

# Çakışan servisleri durdur
sudo systemctl stop postgresql
sudo systemctl stop mysql
```

#### Container Start Issues
```bash
# Container loglarını kontrol et
docker-compose -f docker-compose.dev.yml logs sqlserver
docker-compose -f docker-compose.dev.yml logs redis

# Container resource kullanımını kontrol et
docker stats

# Disk space kontrolü
df -h
docker system df
```

#### Network Issues
```bash
# Docker network kontrolü
docker network ls
docker network inspect enterprise_network

# DNS resolution test
docker exec docker_api-gateway_1 nslookup identity-service
```

#### Database Connection Issues
```bash
# SQL Server connection test
docker exec docker_sqlserver_1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "StrongPassword123!" -Q "SELECT @@VERSION"

# Redis connection test  
docker exec docker_redis_1 redis-cli ping

# RabbitMQ connection test
docker exec docker_rabbitmq_1 rabbitmq-diagnostics ping
```

## Performance Tuning

### Resource Limits
```yaml
# docker-compose.prod.yml içinde
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 4G
    reservations:
      cpus: '1.0' 
      memory: 2G
```

### Volume Optimization
```yaml
# SSD mount points kullanın
volumes:
  sqlserver_data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /ssd/enterprise/sqlserver
```

### Network Performance
```yaml
# Custom network settings
networks:
  enterprise_network:
    driver: bridge
    driver_opts:
      com.docker.network.driver.mtu: 1500
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

Bu Docker konfigürasyonu ile development'tan production'a kadar tüm environment'lar için tutarlı bir altyapı sağlanır.
# 🏗️ Enterprise Mikroservis Mimarisi - Teknik Dokümantasyon

## 📋 İçindekiler

1. [Genel Mimari](#genel-mimari)
2. [Teknoloji Stack](#teknoloji-stack)
3. [Mikroservis Yapısı](#mikroservis-yapısı)
4. [Shared Components](#shared-components)
5. [Database Tasarımı](#database-tasarımı)
6. [Monitoring ve Logging](#monitoring-ve-logging)
7. [DevOps Pipeline](#devops-pipeline)
8. [Deployment Guide](#deployment-guide)
9. [Development Workflow](#development-workflow)
10. [Çalıştırma Rehberi ve Ortam Standartları](#çalıştırma-rehberi-ve-ortam-standartları)

---

## 🏛️ Genel Mimari

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        INTERNET                                 │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                 LOAD BALANCER                                   │
│                  (IIS/Nginx)                                    │
│              SSL Termination                                    │
│             • Port 443 (HTTPS)                                 │
│             • Port 80 → 443 Redirect                           │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                  API GATEWAY                                    │
│                    (YARP)                                       │
│              • Authentication                                   │
│              • Authorization                                    │
│              • Rate Limiting                                    │
│              • Load Balancing                                   │
│              • Request Routing                                  │
│              • Health Checks                                    │
│                Port 5000                                        │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│  IDENTITY   │ │    USER     │ │   OTHER     │
│  SERVICE    │ │  SERVICE    │ │  SERVICES   │
│ Port 5001   │ │ Port 5002   │ │ Port 5003+  │
└─────────────┘ └─────────────┘ └─────────────┘
        │             │             │
        ▼             ▼             ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│IDENTITY DB  │ │  USER DB    │ │  OTHER DBs  │
│SQL Server   │ │SQL Server   │ │SQL Server   │
│Port 1433    │ │Port 1434    │ │Port 1435+   │
└─────────────┘ └─────────────┘ └─────────────┘
        │             │             │
        └─────────────┼─────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                   SHARED SERVICES                               │
└─────────────────────────────────────────────────────────────────┘
```

### Shared Services Layer

```
┌─────────────────────────────────────────────────────────────────┐
│                    SHARED SERVICES LAYER                        │
├─────────────────────────────────────────────────────────────────┤
│  🗄️ CACHE & STORAGE           📊 MONITORING & OBSERVABILITY     │
│  • Redis Cache (Port 6379)    • Prometheus (Port 9090)         │
│  • MinIO Storage (Port 9000)  • Grafana (Port 3000)            │
│                               • Seq Logging (Port 5341)        │
│                               • Jaeger Tracing (Port 16686)    │
│                                                                 │
│  🚌 MESSAGE QUEUE             🔧 DEVOPS & CI/CD                 │
│  • RabbitMQ (Port 5672)       • Gitea (Port 3001)              │
│  • Management UI (Port 15672) • SonarQube (Port 9000)          │
│                               • Harbor Registry (Port 8080)    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 💻 Teknoloji Stack

### ✅ Microsoft Ekosistemi (Ücretsiz)
- **.NET 8** - Runtime ve SDK
- **ASP.NET Core Identity** - Authentication framework
- **Entity Framework Core** - ORM
- **SQL Server Express/Developer** - Database
- **YARP** - Reverse proxy
- **SignalR** - Real-time communication

### ✅ Açık Kaynak Araçları (Ücretsiz)
- **Redis** - Distributed cache
- **RabbitMQ** - Message queue
- **MinIO** - S3-compatible object storage
- **Prometheus** - Metrics collection
- **Grafana** - Visualization
- **Seq** - Structured logging
- **Jaeger** - Distributed tracing
- **Gitea** - Git server
- **SonarQube Community** - Code quality
- **Harbor** - Container registry

---

## 🔧 Mikroservis Yapısı

### Identity Service (Port 5001)

**Sorumluluklar:**
- Authentication & Authorization
- JWT token management
- OAuth 2.0 flows (Google, Microsoft)
- Multi-factor authentication
- Session management
- Password reset workflows

**API Endpoints:**
```
POST /api/v1/auth/login
POST /api/v1/auth/logout
POST /api/v1/auth/refresh
POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password
GET  /api/v1/oauth/google/login
POST /api/v1/oauth/google/callback
POST /api/v1/mfa/enable
POST /api/v1/mfa/verify
GET  /api/v1/sessions
DELETE /api/v1/sessions/{id}
```

### User Service (Port 5002)

**Sorumluluklar:**
- User profile management
- Email verification
- GDPR compliance operations
- User activity tracking
- Soft/Hard delete operations

**API Endpoints:**
```
GET    /api/v1/users/profile
PUT    /api/v1/users/profile
POST   /api/v1/users/profile/picture
POST   /api/v1/account/register
POST   /api/v1/account/verify-email/{token}
PUT    /api/v1/account/email
PUT    /api/v1/account/password
PUT    /api/v1/account/username
DELETE /api/v1/users/account
GET    /api/v1/users/preferences
PUT    /api/v1/users/preferences
GET    /api/v1/users/activities
POST   /api/v1/gdpr/export
POST   /api/v1/gdpr/delete
GET    /api/v1/gdpr/requests
```

---

## 📦 Shared Components

### Klasör Yapısı

```
📁 Shared/
├── 📁 Enterprise.Shared.Email/
├── 📁 Enterprise.Shared.Auditing/
├── 📁 Enterprise.Shared.ErrorHandling/
├── 📁 Enterprise.Shared.Caching/
├── 📁 Enterprise.Shared.Logging/
├── 📁 Enterprise.Shared.Security/
├── 📁 Enterprise.Shared.Validation/
├── 📁 Enterprise.Shared.Events/
├── 📁 Enterprise.Shared.Common/
├── 📁 Enterprise.Shared.Storage/
├── 📁 Enterprise.Shared.Notifications/
└── 📁 Enterprise.Shared.Configuration/
```

### Email Service Implementation

```csharp
// IEmailService.cs
public interface IEmailService
{
    Task<EmailResult> SendAsync(EmailMessage message);
    Task<EmailResult> SendTemplateAsync(string templateName, string to, object model);
    Task<EmailResult> SendBulkAsync(List<EmailMessage> messages);
}

// Kullanım
await _emailService.SendTemplateAsync("welcome-email", user.Email, new { Name = user.FirstName });
```

### Auditing Service Implementation

```csharp
// IAuditService.cs
public interface IAuditService
{
    Task LogEventAsync(AuditEvent auditEvent);
    Task LogSecurityEventAsync(SecurityAuditEvent securityEvent);
    Task<List<AuditEvent>> SearchEventsAsync(AuditSearchCriteria criteria);
}

// Kullanım
await _auditService.LogEventAsync(new AuditEvent
{
    Action = "USER_LOGIN",
    Resource = "Identity",
    Result = "SUCCESS",
    UserId = userId
});
```

### Error Handling Implementation

```csharp
// Global Exception Middleware
public class GlobalExceptionMiddleware
{
    // Tüm hataları yakalar ve standart format döner
    // Audit log kaydeder
    // Structured logging yapar
}

// Custom Exceptions
public class BusinessException : Exception
public class ValidationException : Exception
public class NotFoundException : Exception
```

---

## 🗄️ Database Tasarımı

### Identity Database (Port 1433)

```sql
-- Users Table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(256) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(MAX),
    IsEmailConfirmed BIT DEFAULT 0,
    TwoFactorEnabled BIT DEFAULT 0,
    LockoutEnd DATETIMEOFFSET NULL,
    FailedLoginAttempts INT DEFAULT 0,
    CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    IsDeleted BIT DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL
);

-- ExternalLogins Table
CREATE TABLE ExternalLogins (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Users(Id),
    Provider NVARCHAR(50) NOT NULL, -- 'Google', 'Microsoft'
    ProviderKey NVARCHAR(256) NOT NULL,
    CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- RefreshTokens Table
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Users(Id),
    Token NVARCHAR(MAX) NOT NULL,
    ExpiresAt DATETIMEOFFSET NOT NULL,
    IsRevoked BIT DEFAULT 0,
    CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- UserSessions Table
CREATE TABLE UserSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Users(Id),
    SessionId NVARCHAR(256) UNIQUE NOT NULL,
    DeviceInfo NVARCHAR(MAX),
    IpAddress NVARCHAR(45),
    RememberMe BIT DEFAULT 0,
    ExpiresAt DATETIMEOFFSET NOT NULL,
    CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- MfaTokens Table
CREATE TABLE MfaTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Users(Id),
    Token NVARCHAR(10) NOT NULL,
    TokenType NVARCHAR(20) NOT NULL, -- 'TOTP', 'SMS', 'Email'
    ExpiresAt DATETIMEOFFSET NOT NULL,
    IsUsed BIT DEFAULT 0,
    CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);
```

### User Database (Port 1434)

```sql
-- UserProfiles Table
CREATE TABLE UserProfiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY, -- Same as Identity.Users.Id
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    DateOfBirth DATE,
    ProfilePictureUrl NVARCHAR(500),
    Bio NVARCHAR(1000),
    TimeZone NVARCHAR(50),
    Language NVARCHAR(10) DEFAULT 'en-US',
    IsDeleted BIT DEFAULT 0,
    DeletedAt DATETIMEOFFSET NULL,
    CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- UserActivities Table
CREATE TABLE UserActivities (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES UserProfiles(Id),
    ActivityType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    Metadata NVARCHAR(MAX), -- JSON
    Timestamp DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    INDEX IX_UserActivities_UserId_Timestamp (UserId, Timestamp)
);

-- EmailVerifications Table
CREATE TABLE EmailVerifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES UserProfiles(Id),
    Email NVARCHAR(256) NOT NULL,
    Token NVARCHAR(256) NOT NULL,
    ExpiresAt DATETIMEOFFSET NOT NULL,
    IsVerified BIT DEFAULT 0,
    VerifiedAt DATETIMEOFFSET NULL,
    CreatedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
);

-- GdprRequests Table
CREATE TABLE GdprRequests (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES UserProfiles(Id),
    RequestType NVARCHAR(20) NOT NULL, -- 'EXPORT', 'DELETE'
    Status NVARCHAR(20) DEFAULT 'PENDING',
    RequestedAt DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET(),
    ProcessedAt DATETIMEOFFSET NULL,
    ExportUrl NVARCHAR(500) NULL
);
```

---

## 📊 Monitoring ve Logging

### Docker Compose Configuration

```yaml
# docker-compose.monitoring.yml
version: '3.8'
services:
  prometheus:
    image: prom/prometheus:latest
    ports: ["9090:9090"]
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    restart: unless-stopped

  grafana:
    image: grafana/grafana-oss:latest
    ports: ["3000:3000"]
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin123
    volumes:
      - grafana-data:/var/lib/grafana
    restart: unless-stopped

  seq:
    image: datalust/seq:latest
    ports: ["5341:80"]
    environment:
      - ACCEPT_EULA=Y
      - SEQ_FIRSTRUN_ADMINPASSWORDHASH=QWRtaW4xMjM=
    volumes:
      - seq-data:/data
    restart: unless-stopped

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports: 
      - "16686:16686"  # Jaeger UI
      - "14268:14268"  # HTTP collector
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    restart: unless-stopped
```

### Grafana Dashboards

**System Overview Dashboard:**
- CPU, Memory, Disk Usage
- Network I/O
- Service Health Status

**Application Performance Dashboard:**
- Request Rate & Latency
- Error Rate & 4xx/5xx Responses
- Database Query Performance

**Business Metrics Dashboard:**
- User Registrations
- Login Success/Failure Rates
- Active User Sessions
- API Usage by Endpoint

**Security Monitoring Dashboard:**
- Failed Login Attempts
- Account Lockouts
- Suspicious Activity Alerts
- MFA Usage Statistics

---

## 🚀 DevOps Pipeline

### CI/CD with Gitea Actions

```yaml
# .gitea/workflows/ci-cd.yml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET 8
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release
      
    - name: SonarQube Analysis
      run: |
        dotnet sonarscanner begin /k:"enterprise-microservices"
        dotnet build
        dotnet sonarscanner end
        
    - name: Build Docker Image
      run: |
        docker build -t identity-service:${{ github.sha }} ./IdentityService
        docker build -t user-service:${{ github.sha }} ./UserService
        
    - name: Push to Harbor
      run: |
        docker push harbor.local/enterprise/identity-service:${{ github.sha }}
        docker push harbor.local/enterprise/user-service:${{ github.sha }}

  deploy:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - name: Deploy to Production
      run: |
        docker-compose -f docker-compose.prod.yml pull
        docker-compose -f docker-compose.prod.yml up -d
```

---

## 📋 Deployment Guide

### Gerekli Sunucu Spesifikasyonları

**Production Environment:**

**Web Server (Load Balancer):**
- OS: Windows Server 2022 / Ubuntu 22.04
- RAM: 8GB
- CPU: 4 Cores
- Storage: 100GB SSD
- Software: IIS / Nginx

**Application Servers (2x):**
- OS: Windows Server 2022 / Ubuntu 22.04
- RAM: 16GB
- CPU: 8 Cores
- Storage: 200GB SSD
- Software: .NET 8 Runtime, Docker

**Database Server:**
- OS: Windows Server 2022
- RAM: 32GB
- CPU: 8 Cores
- Storage: 500GB SSD + 1TB HDD
- Software: SQL Server 2022 Developer

**Monitoring Server:**
- OS: Ubuntu 22.04
- RAM: 16GB
- CPU: 4 Cores
- Storage: 500GB SSD
- Software: Docker, Prometheus, Grafana, Seq, Jaeger

### Deployment Scripts

```powershell
# deploy.ps1
Write-Host "Starting enterprise microservices deployment..."

# Start infrastructure
docker-compose -f docker-compose.databases.yml up -d
docker-compose -f docker-compose.monitoring.yml up -d
docker-compose -f docker-compose.cache-storage.yml up -d
docker-compose -f docker-compose.devops.yml up -d

# Build and deploy services
dotnet publish IdentityService/IdentityService.csproj -c Release -o ./publish/identity
dotnet publish UserService/UserService.csproj -c Release -o ./publish/user

# Run database migrations
dotnet ef database update --project IdentityService
dotnet ef database update --project UserService

Write-Host "Deployment completed successfully!"
Write-Host "Access Points:"
Write-Host "- API Gateway: http://localhost"
Write-Host "- Grafana: http://localhost:3000 (admin/admin123)"
Write-Host "- Seq Logs: http://localhost:5341"
Write-Host "- Jaeger: http://localhost:16686"
Write-Host "- RabbitMQ: http://localhost:15672 (admin/admin123)"
Write-Host "- MinIO: http://localhost:9001 (minioadmin/minioadmin123)"
Write-Host "- Gitea: http://localhost:3001"
Write-Host "- SonarQube: http://localhost:9000 (admin/admin)"
```

### Configuration Files

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "Database": "Server=localhost,1433;Database=IdentityDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;",
    "Redis": "localhost:6379",
    "RabbitMQ": "amqp://admin:admin123@localhost:5672",
    "Seq": "http://localhost:5341"
  },
  "Jwt": {
    "Key": "your-super-secret-key-that-is-at-least-256-bits-long",
    "Issuer": "https://your-identity-service.com",
    "Audience": "https://your-api.com",
    "ExpireMinutes": 15
  },
  "Google": {
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret"
  }
}
```

---

## 👨‍💻 Development Workflow

### Proje Yapısı

```
Enterprise-Microservices/
├── 📁 src/
│   ├── 📁 Services/
│   │   ├── 📁 IdentityService/
│   │   └── 📁 UserService/
│   ├── 📁 Gateways/
│   │   └── 📁 ApiGateway/
│   └── 📁 Shared/
│       ├── 📁 Enterprise.Shared.Email/
│       ├── 📁 Enterprise.Shared.Auditing/
│       └── 📁 ... (other shared libraries)
├── 📁 infrastructure/
│   ├── 📁 docker/
│   ├── 📁 kubernetes/
│   └── 📁 monitoring/
├── 📁 tests/
│   ├── 📁 Unit/
│   ├── 📁 Integration/
│   └── 📁 E2E/
├── 📁 docs/
└── 📁 scripts/
```

### Geliştirme Adımları

1. **Shared Library Geliştirme:**
   - Enterprise.Shared.* projelerini oluştur
   - NuGet package olarak yayınla (local)
   - Tüm servislerde referans al

2. **Database Setup:**
   - SQL Server Docker container'ları başlat
   - EF Core migrations oluştur ve çalıştır
   - Seed data ekle

3. **Identity Service:**
   - ASP.NET Core Identity entegrasyonu
   - JWT token implementasyonu
   - Google OAuth entegrasyonu
   - MFA implementasyonu

4. **User Service:**
   - User profile CRUD operations
   - Email verification workflow
   - GDPR compliance features
   - Event-driven communication

5. **API Gateway:**
   - YARP configuration
   - Authentication middleware
   - Rate limiting
   - Health checks

6. **Monitoring Setup:**
   - Prometheus metrics
   - Grafana dashboards
   - Seq structured logging
   - Jaeger distributed tracing

### Testing Strategy

```csharp
// Unit Tests
[Test]
public async Task Login_ValidCredentials_ReturnsJwtToken()
{
    // Arrange
    var authService = new AuthenticationService(_mockUserManager, _mockTokenService);
    
    // Act
    var result = await authService.LoginAsync("test@example.com", "password");
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.IsNotNull(result.Token);
}

// Integration Tests
[Test]
public async Task POST_Login_ValidCredentials_Returns200WithToken()
{
    // Arrange
    var client = _factory.CreateClient();
    var loginRequest = new { Email = "test@example.com", Password = "password" };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var token = await response.Content.ReadAsStringAsync();
    Assert.IsNotNull(token);
}
```

### Git Workflow

```bash
# Feature development
git checkout -b feature/user-registration
git add .
git commit -m "feat: implement user registration with email verification"
git push origin feature/user-registration

# Pull request to develop
# Code review
# Merge to develop

# Release preparation
git checkout -b release/v1.0.0
git push origin release/v1.0.0

# Merge to main
git checkout main
git merge release/v1.0.0
git tag v1.0.0
git push origin main --tags
```

---

## 🎯 Sonraki Adımlar

### Phase 1: Foundation (Hafta 1-2)
- [ ] Shared libraries oluştur
- [ ] Database container'ları kur
- [ ] Basic Identity Service
- [ ] Basic User Service
- [ ] API Gateway configuration

### Phase 2: Core Features (Hafta 3-4)
- [ ] Google OAuth entegrasyonu
- [ ] Email verification
- [ ] MFA implementation
- [ ] Basic monitoring

### Phase 3: Advanced Features (Hafta 5-6)
- [ ] GDPR compliance
- [ ] Advanced auditing
- [ ] Performance optimization
- [ ] Security hardening

### Phase 4: Production Ready (Hafta 7-8)
- [ ] Load testing
- [ ] Security penetration testing
- [ ] Documentation completion
- [ ] Production deployment

---

## 📞 İletişim ve Destek

**Teknik Dokümantasyon:** Bu dokümanda

**Kod Repository:** Gitea (http://localhost:3001)

**Monitoring:** Grafana (http://localhost:3000)

**Logs:** Seq (http://localhost:5341)

**Issue Tracking:** Gitea Issues

---

*Bu dokümantasyon, enterprise seviyesinde tamamen ücretsiz, self-hosted mikroservis mimarisinin teknik detaylarını içermektedir. Tüm bileşenler açık kaynak araçlar ve Microsoft'un ücretsiz teknolojileri kullanılarak tasarlanmıştır.*

---

## 🚀 Çalıştırma Rehberi ve Ortam Standartları

### .env Dosyaları (Merkezi)
- `config/env/shared.env`: Ortak değişkenler (JWT_*, DB vs.)
- `config/env/secrets.env`: Gizli değişkenler (git-ignore)
- `config/env/dev.env`:
  - `ASPNETCORE_ENVIRONMENT=Development`
  - `CORS_ORIGINS=http://localhost:4200`
  - `REQUIRE_HTTPS=false`
  - `APPLY_DATABASE_MIGRATIONS=true`
  - `ADMIN_EMAIL=admin@local.dev`
  - `ADMIN_PASSWORD=Admin123!`
- `config/env/prod.env`:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `CORS_ORIGINS=https://admin.platformv1.com`
  - `REQUIRE_HTTPS=true`
  - `APPLY_DATABASE_MIGRATIONS=false`

### Docker Compose Override’ları
- `docker-compose.yml`: Ortak tanım
- `docker-compose.dev.yml`: Tüm servislerde `env_file` zinciri (shared → dev → secrets) ve `ASPNETCORE_ENVIRONMENT=Development`
- `docker-compose.prod.yml`: Tüm servislerde `env_file` zinciri (shared → prod → secrets) ve `ASPNETCORE_ENVIRONMENT=Production`

Çalıştırma:
```bash
# Development
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build

# Production
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

### API Gateway Standartları
- Dış istekler: `/api/v1/...` (tüm trafik sadece gateway üzerinden)
- YARP rotaları örnekleri:
  - Identity: `/api/v1/auth`, `/api/v1/account`, `/api/v1/users`, `/api/v1/roles`, `/api/v1/permissions`, `/api/v1/categories`
  - User: `/api/v1/user-profiles`, `/api/v1/user-preferences`, `/api/v1/gdpr`, `/api/v1/email-verification`
  - SpeedReading: `/api/v1/user-reading-profiles`, `/api/v1/exercises`, `/api/v1/reading-texts`, `/api/v1/analytics`
- CORS: Policy ile, `CORS_ORIGINS` env’den
- HTTPS: `REQUIRE_HTTPS` env (Dev=false, Prod=true)
- Swagger: Dev=root (/), Prod=`/docs`

### Frontend
- Dev: `environment.ts → apiUrl = 'http://localhost:5001/api'`
- Prod: `environment.prod.ts → apiUrl = 'https://api.platformv1.com/api'`
- Tüm istekler `${environment.apiUrl}/v1/...` (yalnız gateway)

### Smoke Test (Dev)
```bash
# Auth
curl -i -X POST http://localhost:5001/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"<email>","password":"<pwd>"}'

# Identity kaynakları
curl -I http://localhost:5001/api/v1/users
curl -I http://localhost:5001/api/v1/roles
curl -I http://localhost:5001/api/v1/permissions
curl -I http://localhost:5001/api/v1/categories

# User
curl -I http://localhost:5001/api/v1/user-profiles/me
curl -I http://localhost:5001/api/v1/user-preferences/me
curl -I http://localhost:5001/api/v1/gdpr/export/json

# SpeedReading
curl -I http://localhost:5001/api/v1/exercises
curl -I http://localhost:5001/api/v1/reading-texts
```
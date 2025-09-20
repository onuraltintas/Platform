# User Service API

A comprehensive, production-ready user management microservice with GDPR compliance, advanced features, and enterprise-grade capabilities.

## 🚀 Features

### Core Functionality
- **User Profile Management**: Complete CRUD operations for user profiles
- **User Preferences**: Notification settings, privacy controls, and personalization
- **GDPR Compliance**: Data export (JSON/CSV/XML), right to erasure, consent management
- **Email Verification**: Token-based email verification system
- **Event Integration**: MassTransit/RabbitMQ for event-driven architecture

### Production Features
- **JWT Authentication & Authorization**: Secure API endpoints with role-based access
- **Rate Limiting**: Configurable rate limits for different endpoint categories
- **Response Compression**: Optimized data transfer
- **Health Checks**: Comprehensive health monitoring endpoints
- **Swagger Documentation**: Complete API documentation with examples
- **Structured Logging**: Serilog with request tracking and correlation IDs
- **Security Headers**: HSTS, XSS protection, content type validation
- **CORS Configuration**: Configurable cross-origin resource sharing
- **Global Exception Handling**: Consistent error responses

## 🏗️ Architecture

The service follows **Clean Architecture** principles:

```
User.API/           # API Layer (Controllers, Middleware, Configuration)
User.Application/   # Application Layer (Services, Use Cases, Mapping)
User.Infrastructure/# Infrastructure Layer (Data Access, External Services)
User.Core/          # Domain Layer (Entities, DTOs, Interfaces)
User.Tests/         # Test Layer (Unit, Integration, Helpers)
```

## 🔧 Quick Start

### Prerequisites
- .NET 8.0 SDK
- SQL Server (or SQL Server Express)
- RabbitMQ (optional, for event publishing)

### Environment Variables
```bash
USER_DB_CONNECTION_STRING="Server=localhost;Database=UserServiceDb;Trusted_Connection=true;TrustServerCertificate=true;"
JWT_SECRET_KEY="your-super-secret-jwt-key-minimum-256-bits"
JWT_ISSUER="https://your-auth-server.com"
JWT_AUDIENCE="https://your-api.com"
RABBITMQ_CONNECTION_STRING="amqp://guest:guest@localhost:5672/"
ALLOWED_ORIGINS="http://localhost:3000,https://localhost:3001"
```

### Run the Service
```bash
cd User.API
dotnet run
```

The service will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`
- Health Checks: `https://localhost:5001/health`

## 📋 API Endpoints

### User Profile Management
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/v1/userprofile/me` | Get current user's profile | ✅ |
| POST | `/api/v1/userprofile/me` | Create user profile | ✅ |
| PUT | `/api/v1/userprofile/me` | Update user profile | ✅ |
| DELETE | `/api/v1/userprofile/me` | Delete user profile | ✅ |
| GET | `/api/v1/userprofile/{userId}` | Get user profile (Admin only) | ✅ (Admin) |
| PUT | `/api/v1/userprofile/{userId}` | Update user profile (Admin only) | ✅ (Admin) |
| DELETE | `/api/v1/userprofile/{userId}` | Delete user profile (Admin only) | ✅ (Admin) |

### User Preferences
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/v1/userpreferences/me` | Get current user's preferences | ✅ |
| PUT | `/api/v1/userpreferences/me` | Update user preferences | ✅ |
| GET | `/api/v1/userpreferences/{userId}` | Get user preferences (Admin only) | ✅ (Admin) |
| PUT | `/api/v1/userpreferences/{userId}` | Update user preferences (Admin only) | ✅ (Admin) |

### GDPR Compliance
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/v1/gdpr/export/json` | Export user data as JSON | ✅ |
| GET | `/api/v1/gdpr/export/csv` | Export user data as CSV | ✅ |
| GET | `/api/v1/gdpr/export/xml` | Export user data as XML | ✅ |
| POST | `/api/v1/gdpr/delete-account` | Request account deletion | ✅ |
| POST | `/api/v1/gdpr/cancel-deletion` | Cancel account deletion | ✅ |
| GET | `/api/v1/gdpr/consent` | Get consent status | ✅ |
| POST | `/api/v1/gdpr/consent` | Update consent preferences | ✅ |

### Email Verification
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/v1/emailverification/send` | Send verification email | ❌ |
| POST | `/api/v1/emailverification/verify` | Verify email with token | ❌ |
| GET | `/api/v1/emailverification/status` | Check verification status | ✅ |
| POST | `/api/v1/emailverification/resend` | Resend verification email | ✅ |
| POST | `/api/v1/emailverification/update-email` | Update email address | ✅ |

### Health & Monitoring
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/health` | Comprehensive health check | ❌ |
| GET | `/health/ready` | Readiness probe | ❌ |
| GET | `/health/live` | Liveness probe | ❌ |

## 🔐 Authentication

The service uses **JWT Bearer tokens** for authentication. Include the token in the Authorization header:

```bash
Authorization: Bearer <your-jwt-token>
```

### JWT Token Claims
The service expects the following claims in JWT tokens:
- `sub` or `userId` or `NameIdentifier`: Unique user identifier
- `role`: User role for authorization (e.g., "Admin")

### Example Request
```bash
curl -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \\
     https://localhost:5001/api/v1/userprofile/me
```

## 📊 Rate Limiting

The service implements different rate limiting policies:

| Policy | Endpoints | Limit | Window |
|--------|-----------|--------|--------|
| Global | All endpoints | 100 requests | 1 minute |
| API | Most API endpoints | 50 requests | 1 minute |
| GDPR | GDPR export endpoints | 5 requests | 1 hour |
| Email | Email verification endpoints | 10 requests | 1 hour |

## 🗄️ Database Schema

### UserProfiles Table
```sql
CREATE TABLE UserProfiles (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId nvarchar(450) UNIQUE NOT NULL,
    FirstName nvarchar(100) NOT NULL,
    LastName nvarchar(100) NOT NULL,
    PhoneNumber nvarchar(20),
    DateOfBirth datetime2,
    Bio nvarchar(500),
    ProfilePictureUrl nvarchar(255),
    TimeZone int NOT NULL,
    Language int NOT NULL,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2 NOT NULL
);
```

### UserPreferences Table
```sql
CREATE TABLE UserPreferences (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId nvarchar(450) UNIQUE NOT NULL,
    EmailNotifications bit NOT NULL DEFAULT 1,
    SmsNotifications bit NOT NULL DEFAULT 0,
    PushNotifications bit NOT NULL DEFAULT 1,
    ProfileVisibility nvarchar(20) NOT NULL DEFAULT 'Public',
    Theme nvarchar(20) NOT NULL DEFAULT 'Light',
    DataProcessingConsent bit NOT NULL DEFAULT 0,
    MarketingEmailsConsent bit NOT NULL DEFAULT 0,
    ConsentGivenAt datetime2,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2 NOT NULL
);
```

## 🔧 Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=UserServiceDb;Trusted_Connection=true;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "https://your-auth-server.com",
    "Audience": "https://your-api.com",
    "ExpirationHours": 24
  },
  "RabbitMq": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {"Name": "Console"},
      {"Name": "File", "Args": {"path": "logs/user-service-.txt"}}
    ]
  }
}
```

## 🧪 Testing

The service includes comprehensive test coverage:

### Run Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Unit"
```

### Test Structure
- **Unit Tests**: Service layer, repository layer, and controller tests
- **Integration Tests**: Full API endpoint testing
- **Test Helpers**: Common test utilities and data builders

## 🚀 Deployment

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["User.API/User.API.csproj", "User.API/"]
RUN dotnet restore "User.API/User.API.csproj"
COPY . .
WORKDIR "/src/User.API"
RUN dotnet build "User.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "User.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "User.API.dll"]
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: user-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: user-service
  template:
    metadata:
      labels:
        app: user-service
    spec:
      containers:
      - name: user-service
        image: your-registry/user-service:latest
        ports:
        - containerPort: 8080
        env:
        - name: USER_DB_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: user-service-secrets
              key: db-connection
        - name: JWT_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: user-service-secrets
              key: jwt-secret
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
```

## 📈 Monitoring & Observability

### Logs
The service uses **Serilog** for structured logging with:
- Request/response logging
- Correlation IDs for request tracking
- Performance metrics
- Error tracking with stack traces

### Health Checks
- **Database connectivity**: Verifies SQL Server connection
- **Self-check**: Basic service health
- **Dependency checks**: External service availability

### Metrics
The service exposes metrics for:
- Request throughput and latency
- Authentication success/failure rates
- Cache hit/miss ratios
- Database query performance

## 🔒 Security

### Security Features
- **JWT Authentication**: Secure token-based authentication
- **Authorization**: Role-based access control
- **Rate Limiting**: Protection against abuse
- **Security Headers**: HSTS, XSS protection, content-type validation
- **Input Validation**: Comprehensive request validation
- **CORS**: Configurable cross-origin policies
- **HTTPS Enforcement**: Redirect HTTP to HTTPS

### GDPR Compliance
- **Data Portability**: Export user data in multiple formats
- **Right to Erasure**: Secure account deletion with grace period
- **Consent Management**: Granular consent tracking
- **Data Minimization**: Only collect necessary data
- **Audit Logging**: Track all data access and modifications

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:
- Create an issue in the GitHub repository
- Check the API documentation at `/swagger`
- Review the health endpoints at `/health`

## 📚 Additional Resources

- [API Documentation (Swagger)](https://localhost:5001/swagger)
- [Health Checks](https://localhost:5001/health)
- [Enterprise Shared Libraries](../../../Shared/)
- [Architecture Decision Records](docs/adr/)
- [Deployment Guide](docs/deployment.md)
- [Security Guide](docs/security.md)
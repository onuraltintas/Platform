# Enterprise Platform Infrastructure

## Genel Bakış
Bu dizin, Enterprise Platform'un altyapı bileşenlerini ve deployment konfigürasyonlarını içerir. Docker, Kubernetes, monitoring araçları ve development environment setup'ı için gerekli tüm dosyalar burada yer alır.

## Altyapı Mimarisi

### Ana Bileşenler
```
infrastructure/
├── docker/                 # Docker konfigürasyonları
│   ├── databases/           # SQL Server, Redis vb.
│   ├── cache-storage/       # Redis, MinIO
│   ├── monitoring/          # Prometheus, Grafana, Seq
│   └── devops/             # Gitea, SonarQube
├── kubernetes/             # K8s manifests
│   ├── namespaces/
│   ├── services/
│   ├── deployments/
│   └── ingress/
├── monitoring/             # Monitoring konfigürasyonları
│   ├── prometheus/
│   ├── grafana/
│   ├── jaeger/
│   └── seq/
└── scripts/                # Deployment ve utility scriptleri
```

## Hızlı Başlangıç

### Development Environment
```bash
# 1. Repository'yi clone edin
git clone <repository-url>
cd PlatformV1

# 2. Development infrastructure'ı başlatın
cd infrastructure/docker
docker-compose -f docker-compose.dev.yml up -d

# 3. Database'leri initialize edin
./scripts/setup-databases.sh

# 4. Servisleri başlatın
cd ../../
dotnet run --project src/Services/IdentityService
dotnet run --project src/Services/UserService
dotnet run --project src/Gateways/ApiGateway
```

### Production Environment
```bash
# 1. Kubernetes cluster'ını hazırlayın
kubectl apply -f infrastructure/kubernetes/namespaces/

# 2. Secrets ve ConfigMaps'i oluşturun
kubectl apply -f infrastructure/kubernetes/config/

# 3. Infrastructure services'i deploy edin
kubectl apply -f infrastructure/kubernetes/infrastructure/

# 4. Application services'i deploy edin
kubectl apply -f infrastructure/kubernetes/services/

# 5. Ingress'i configure edin
kubectl apply -f infrastructure/kubernetes/ingress/
```

## Altyapı Bileşenleri

### 1. Veritabanları
- **SQL Server**: Ana uygulama veritabanı
- **Redis**: Cache ve session store
- **MongoDB**: Log ve event store (opsiyonel)

### 2. Message Queue
- **RabbitMQ**: Mikroservis komunikasyonu
- **MassTransit**: .NET integration layer

### 3. Storage
- **MinIO**: Object storage (S3 compatible)
- **NFS**: Persistent volume storage

### 4. Monitoring Stack
- **Prometheus**: Metrics collection
- **Grafana**: Visualization ve dashboards
- **Jaeger**: Distributed tracing
- **Seq**: Structured logging

### 5. DevOps Araçları
- **Gitea**: Git server
- **SonarQube**: Code quality analysis
- **Jenkins**: CI/CD pipeline (opsiyonel)

### 6. API Gateway
- **YARP**: Reverse proxy ve load balancing
- **Rate Limiting**: AspNetCoreRateLimit
- **Authentication**: JWT Bearer

### 7. Service Discovery
- **Kubernetes DNS**: Service discovery
- **ConfigMaps**: Configuration management
- **Secrets**: Sensitive data management

## Environment Ayarları

### Development
```yaml
# .env.development
ASPNETCORE_ENVIRONMENT=Development
DATABASE_SERVER=localhost
REDIS_CONNECTION=localhost:6379
RABBITMQ_HOST=localhost
MINIO_ENDPOINT=localhost:9000
SEQ_URL=http://localhost:5341
JAEGER_ENDPOINT=http://localhost:14268
```

### Staging
```yaml
# .env.staging
ASPNETCORE_ENVIRONMENT=Staging
DATABASE_SERVER=sql-server-staging
REDIS_CONNECTION=redis-staging:6379
RABBITMQ_HOST=rabbitmq-staging
MINIO_ENDPOINT=minio-staging:9000
SEQ_URL=http://seq-staging:5341
JAEGER_ENDPOINT=http://jaeger-staging:14268
```

### Production
```yaml
# .env.production (stored in Kubernetes Secrets)
ASPNETCORE_ENVIRONMENT=Production
DATABASE_SERVER=sql-server-prod
REDIS_CONNECTION=redis-prod:6379
RABBITMQ_HOST=rabbitmq-prod
MINIO_ENDPOINT=minio-prod:9000
SEQ_URL=http://seq-prod:5341
JAEGER_ENDPOINT=http://jaeger-prod:14268
```

## Network Topolojisi

### Development (Docker Compose)
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Gateway   │────│  Identity Svc   │────│   User Service  │
│     :8080       │    │     :8081       │    │     :8082       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
         ┌─────────────────┬─────┴─────┬─────────────────┐
         │                 │           │                 │
┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│ SQL Server  │  │    Redis    │  │  RabbitMQ   │  │   MinIO     │
│   :1433     │  │    :6379    │  │   :5672     │  │   :9000     │
└─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘
```

### Production (Kubernetes)
```
                    ┌─────────────────┐
                    │   Ingress       │
                    │  (nginx-ingress)│
                    └─────────────────┘
                             │
                    ┌─────────────────┐
                    │   API Gateway   │
                    │   (3 replicas)  │
                    └─────────────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
    │Identity Svc │  │ User Service│  │Other Services│
    │(2 replicas) │  │(2 replicas) │  │   ...       │
    └─────────────┘  └─────────────┘  └─────────────┘
              │              │              │
              └──────────────┼──────────────┘
                             │
         ┌─────────────────┬─┴───┬─────────────────┐
         │                 │     │                 │
┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│SQL Server   │  │Redis Cluster│  │ RabbitMQ    │  │MinIO Cluster│
│(StatefulSet)│  │(StatefulSet)│  │ (StatefulSet│  │(StatefulSet)│
└─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘
```

## Security Considerations

### Network Security
- **Service Mesh**: Istio için hazır (opsiyonel)
- **Network Policies**: Pod-to-pod communication restrictions
- **TLS**: Inter-service communication encryption
- **Secrets Management**: Kubernetes Secrets + Sealed Secrets

### Authentication & Authorization
- **JWT Tokens**: Stateless authentication
- **RBAC**: Role-based access control
- **OAuth 2.0**: External provider integration
- **API Keys**: Service-to-service authentication

### Data Security
- **Encryption at Rest**: Database ve storage encryption
- **Encryption in Transit**: TLS everywhere
- **Backup Encryption**: Encrypted backups
- **Secret Rotation**: Automated secret rotation

## Monitoring ve Observability

### Metrics (Prometheus)
```yaml
# Toplanan metrikler
- Application metrics (business KPIs)
- Infrastructure metrics (CPU, memory, disk)
- Network metrics (latency, throughput)
- Error rates ve success rates
- Custom business metrics
```

### Logging (Seq + Serilog)
```yaml
# Log kategorileri
- Application logs (structured logging)
- Access logs (requests/responses)
- Audit logs (security events)
- Error logs (exceptions)
- Performance logs (slow queries)
```

### Tracing (Jaeger)
```yaml
# Trace bilgileri
- Request flow tracking
- Service dependencies
- Performance bottlenecks
- Error propagation
- Database query tracing
```

### Alerting (Grafana + AlertManager)
```yaml
# Alert kuralları
- High error rates (>5%)
- Slow response times (>2s)
- High resource usage (>80%)
- Service down alerts
- Database connection issues
```

## Backup ve Disaster Recovery

### Backup Strategy
```bash
# Database backups
- Full backup: Daily
- Differential backup: Every 6 hours  
- Transaction log backup: Every 15 minutes
- Retention: 30 days

# Application data backups
- MinIO objects: Daily snapshot
- Configuration: Git-based versioning
- Secrets: Encrypted backup to secure storage
```

### Recovery Procedures
```bash
# Database recovery
1. Stop application services
2. Restore database from backup
3. Apply transaction logs if needed
4. Verify data integrity
5. Start services

# Application recovery
1. Deploy from known good version
2. Restore configuration from Git
3. Verify service connectivity
4. Run health checks
```

## Performance Tuning

### Application Level
- **Connection Pooling**: Database ve Redis bağlantıları
- **Caching**: Multi-level caching strategy
- **Async Operations**: Non-blocking I/O
- **Resource Management**: Memory ve CPU optimization

### Infrastructure Level
- **Load Balancing**: Request distribution
- **Auto Scaling**: Horizontal pod autoscaler
- **Resource Limits**: CPU ve memory limits
- **Storage**: SSD storage for databases

## Troubleshooting Guide

### Yaygın Sorunlar ve Çözümleri

#### Service Discovery Issues
```bash
# Kubernetes DNS çözümlenme problemi
kubectl run -it --rm debug --image=busybox --restart=Never -- nslookup identity-service

# Service endpoint kontrolü
kubectl get endpoints identity-service
```

#### Database Connection Issues
```bash
# Connection string kontrolü
kubectl get secret database-secret -o yaml

# Database pod durumu
kubectl logs sql-server-0 -f

# Network connectivity testi
kubectl run -it --rm debug --image=busybox --restart=Never -- telnet sql-server 1433
```

#### Memory/CPU Issues
```bash
# Resource usage kontrolü
kubectl top pods
kubectl top nodes

# Pod resource limits
kubectl describe pod identity-service-xxx

# Metrics detayı
kubectl port-forward svc/prometheus 9090:9090
# Prometheus UI'dan memory_usage sorgusu
```

#### Storage Issues
```bash
# PVC durumu kontrolü
kubectl get pvc

# Disk space kontrolü
kubectl exec -it sql-server-0 -- df -h

# MinIO cluster durumu
kubectl exec -it minio-0 -- mc admin info local
```

## Development Workflow

### Local Development
```bash
# 1. Tüm infrastructure'ı başlat
docker-compose -f docker-compose.dev.yml up -d

# 2. Migration'ları çalıştır
dotnet ef database update --project IdentityService

# 3. Test data'sını yükle
dotnet run --project DataSeeder

# 4. Servisleri debug mode'da başlat
# Visual Studio veya VS Code kullanarak
```

### CI/CD Pipeline
```yaml
# GitHub Actions workflow
stages:
  - build: # .NET build ve test
  - quality: # SonarQube analysis
  - security: # Security scan
  - package: # Docker image build
  - deploy-dev: # Development environment
  - integration-tests: # API tests
  - deploy-staging: # Staging environment
  - acceptance-tests: # E2E tests
  - deploy-prod: # Production deployment
```

## Kapasite Planlama

### Resource Requirements

#### Minimum (Development)
```yaml
CPU: 4 cores
RAM: 8GB
Storage: 100GB SSD
Network: 100Mbps
```

#### Recommended (Staging)
```yaml
CPU: 8 cores
RAM: 16GB
Storage: 500GB SSD
Network: 1Gbps
```

#### Production (High Availability)
```yaml
Masters: 3x (4 CPU, 8GB RAM)
Workers: 5x (8 CPU, 16GB RAM)
Storage: 2TB SSD (replicated)
Network: 10Gbps
Load Balancer: External
```

## İletişim ve Destek

### Operasyon Ekibi
- **DevOps Lead**: Platform maintenance
- **Database Admin**: Database operations  
- **Security Officer**: Security compliance
- **Monitoring Specialist**: Observability

### Escalation Matrix
1. **Level 1**: Development team (response: 1 hour)
2. **Level 2**: DevOps team (response: 30 minutes)
3. **Level 3**: Architecture team (response: 15 minutes)
4. **Level 4**: Management escalation

### Documentation Links
- [Docker Setup Guide](docker/README.md)
- [Kubernetes Deployment Guide](kubernetes/README.md)
- [Monitoring Setup](monitoring/README.md)
- [Security Guidelines](../docs/security/README.md)
- [Troubleshooting Guide](../docs/troubleshooting/README.md)

---

**Not**: Bu altyapı konfigürasyonu production-ready olarak tasarlanmıştır. Development environment için basitleştirilmiş versiyonları mevcuttur.
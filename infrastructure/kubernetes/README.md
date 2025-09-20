# Kubernetes Infrastructure Guide

## Genel Bakış
Bu dizin, Enterprise Platform'un Kubernetes üzerinde production-ready deployment'ı için gerekli tüm manifests'leri ve konfigürasyonları içerir. High availability, scalability, security ve observability için optimize edilmiş Kubernetes resources'ları sağlanır.

## Dizin Yapısı
```
kubernetes/
├── namespaces/                      # Namespace definitions
│   ├── enterprise-dev.yaml
│   ├── enterprise-staging.yaml
│   └── enterprise-prod.yaml
├── config/                          # ConfigMaps ve Secrets
│   ├── configmaps/
│   ├── secrets/
│   └── sealed-secrets/
├── infrastructure/                  # Infrastructure services
│   ├── databases/
│   ├── cache/
│   ├── messaging/
│   ├── storage/
│   └── monitoring/
├── services/                        # Application services
│   ├── identity-service/
│   ├── user-service/
│   └── api-gateway/
├── ingress/                         # Ingress resources
│   ├── nginx-ingress/
│   └── cert-manager/
├── operators/                       # Custom operators
│   ├── redis-operator/
│   └── prometheus-operator/
└── helm/                           # Helm charts
    ├── enterprise-platform/
    └── monitoring-stack/
```

## Cluster Requirements

### Minimum Cluster Specs
```yaml
# Development Cluster
Nodes: 3 (1 master, 2 workers)
CPU: 8 cores total (2-2-4)
Memory: 16GB total (4-6-6)
Storage: 500GB SSD
Network: 1Gbps
Kubernetes: v1.28+
```

### Production Cluster Specs
```yaml
# Production Cluster  
Masters: 3 nodes (4 CPU, 8GB RAM each)
Workers: 6+ nodes (8 CPU, 16GB RAM each)
Storage: 5TB+ NVMe SSD (replicated)
Network: 10Gbps with redundancy
Load Balancer: External (cloud or on-prem)
Kubernetes: v1.28+ (managed)
```

## Hızlı Başlangıç

### Prerequisites
```bash
# kubectl kurulumu
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x kubectl && sudo mv kubectl /usr/local/bin/

# helm kurulumu
curl https://get.helm.sh/helm-v3.13.0-linux-amd64.tar.gz | tar xz
sudo mv linux-amd64/helm /usr/local/bin/

# kustomize kurulumu
curl -s "https://raw.githubusercontent.com/kubernetes-sigs/kustomize/master/hack/install_kustomize.sh" | bash
sudo mv kustomize /usr/local/bin/
```

### Development Environment Setup
```bash
# 1. Namespace oluştur
kubectl apply -f namespaces/enterprise-dev.yaml

# 2. Secrets ve ConfigMaps
kubectl apply -f config/secrets/ -n enterprise-dev
kubectl apply -f config/configmaps/ -n enterprise-dev

# 3. Infrastructure services
kubectl apply -f infrastructure/ -n enterprise-dev

# 4. Application services
kubectl apply -f services/ -n enterprise-dev

# 5. Ingress rules
kubectl apply -f ingress/ -n enterprise-dev

# 6. Deployment durumunu kontrol et
kubectl get all -n enterprise-dev
```

### Production Deployment
```bash
# Helm ile production deployment
cd helm/enterprise-platform
helm install enterprise-platform . \
  --namespace enterprise-prod \
  --create-namespace \
  --values values-production.yaml

# Deployment status kontrolü
helm status enterprise-platform -n enterprise-prod
kubectl get all -n enterprise-prod
```

## Namespace Konfigürasyonları

### enterprise-dev.yaml
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: enterprise-dev
  labels:
    name: enterprise-dev
    environment: development
    project: enterprise-platform
---
apiVersion: v1
kind: ResourceQuota
metadata:
  name: enterprise-dev-quota
  namespace: enterprise-dev
spec:
  hard:
    requests.cpu: "4"
    requests.memory: 8Gi
    limits.cpu: "8"
    limits.memory: 16Gi
    persistentvolumeclaims: "10"
    services: "20"
    pods: "30"
---
apiVersion: v1
kind: LimitRange
metadata:
  name: enterprise-dev-limits
  namespace: enterprise-dev
spec:
  limits:
  - default:
      cpu: 500m
      memory: 512Mi
    defaultRequest:
      cpu: 100m
      memory: 128Mi
    type: Container
```

### enterprise-prod.yaml
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: enterprise-prod
  labels:
    name: enterprise-prod
    environment: production
    project: enterprise-platform
---
apiVersion: v1
kind: ResourceQuota
metadata:
  name: enterprise-prod-quota
  namespace: enterprise-prod
spec:
  hard:
    requests.cpu: "20"
    requests.memory: 40Gi
    limits.cpu: "40"
    limits.memory: 80Gi
    persistentvolumeclaims: "20"
    services: "50"
    pods: "100"
---
apiVersion: v1
kind: LimitRange
metadata:
  name: enterprise-prod-limits
  namespace: enterprise-prod
spec:
  limits:
  - default:
      cpu: 1000m
      memory: 1Gi
    defaultRequest:
      cpu: 200m
      memory: 256Mi
    type: Container
```

## Application Services

### Identity Service Deployment
```yaml
# services/identity-service/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: identity-service
  namespace: enterprise-prod
  labels:
    app: identity-service
    version: v1.0.0
spec:
  replicas: 3
  selector:
    matchLabels:
      app: identity-service
  template:
    metadata:
      labels:
        app: identity-service
        version: v1.0.0
    spec:
      containers:
      - name: identity-service
        image: enterprise/identity-service:1.0.0
        ports:
        - containerPort: 80
        - containerPort: 443
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: database-secrets
              key: identity-connection-string
        - name: JwtSettings__SecretKey
          valueFrom:
            secretKeyRef:
              name: jwt-secrets
              key: secret-key
        - name: Redis__ConnectionString
          valueFrom:
            configMapKeyRef:
              name: redis-config
              key: connection-string
        resources:
          requests:
            cpu: 200m
            memory: 256Mi
          limits:
            cpu: 1000m
            memory: 1Gi
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
        volumeMounts:
        - name: logs
          mountPath: /app/logs
        - name: certificates
          mountPath: /app/certs
          readOnly: true
      volumes:
      - name: logs
        persistentVolumeClaim:
          claimName: identity-logs-pvc
      - name: certificates
        secret:
          secretName: tls-certificates
      imagePullSecrets:
      - name: registry-credentials
---
apiVersion: v1
kind: Service
metadata:
  name: identity-service
  namespace: enterprise-prod
  labels:
    app: identity-service
spec:
  selector:
    app: identity-service
  ports:
  - name: http
    port: 80
    targetPort: 80
  - name: https
    port: 443
    targetPort: 443
  type: ClusterIP
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: identity-logs-pvc
  namespace: enterprise-prod
spec:
  accessModes:
    - ReadWriteMany
  resources:
    requests:
      storage: 10Gi
  storageClassName: fast-ssd
```

### API Gateway Deployment
```yaml
# services/api-gateway/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
  namespace: enterprise-prod
  labels:
    app: api-gateway
    version: v1.0.0
spec:
  replicas: 2
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
        version: v1.0.0
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "9090"
        prometheus.io/path: "/metrics"
    spec:
      containers:
      - name: api-gateway
        image: enterprise/api-gateway:1.0.0
        ports:
        - containerPort: 80
        - containerPort: 443
        - containerPort: 9090  # Metrics
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ReverseProxy__Clusters__IdentityCluster__Destinations__destination1__Address
          value: "http://identity-service:80"
        - name: ReverseProxy__Clusters__UserCluster__Destinations__destination1__Address
          value: "http://user-service:80"
        resources:
          requests:
            cpu: 300m
            memory: 512Mi
          limits:
            cpu: 2000m
            memory: 2Gi
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: api-gateway
  namespace: enterprise-prod
  labels:
    app: api-gateway
  annotations:
    service.beta.kubernetes.io/aws-load-balancer-type: nlb
    service.beta.kubernetes.io/aws-load-balancer-cross-zone-load-balancing-enabled: "true"
spec:
  selector:
    app: api-gateway
  ports:
  - name: http
    port: 80
    targetPort: 80
  - name: https
    port: 443
    targetPort: 443
  type: LoadBalancer
```

## Infrastructure Services

### SQL Server StatefulSet
```yaml
# infrastructure/databases/sqlserver.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: sqlserver
  namespace: enterprise-prod
  labels:
    app: sqlserver
spec:
  serviceName: sqlserver-headless
  replicas: 1
  selector:
    matchLabels:
      app: sqlserver
  template:
    metadata:
      labels:
        app: sqlserver
    spec:
      securityContext:
        fsGroup: 10001
      containers:
      - name: sqlserver
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports:
        - containerPort: 1433
        env:
        - name: SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: database-secrets
              key: sa-password
        - name: ACCEPT_EULA
          value: "Y"
        - name: MSSQL_PID
          value: "Developer"
        resources:
          requests:
            cpu: 1000m
            memory: 2Gi
          limits:
            cpu: 4000m
            memory: 8Gi
        volumeMounts:
        - name: sqlserver-data
          mountPath: /var/opt/mssql
        - name: sqlserver-backups
          mountPath: /var/backups
        livenessProbe:
          exec:
            command:
            - /bin/sh
            - -c
            - /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT 1"
          initialDelaySeconds: 60
          periodSeconds: 30
        readinessProbe:
          exec:
            command:
            - /bin/sh
            - -c
            - /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT 1"
          initialDelaySeconds: 30
          periodSeconds: 10
  volumeClaimTemplates:
  - metadata:
      name: sqlserver-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      storageClassName: fast-ssd
      resources:
        requests:
          storage: 100Gi
  - metadata:
      name: sqlserver-backups
    spec:
      accessModes: [ "ReadWriteOnce" ]
      storageClassName: backup-storage
      resources:
        requests:
          storage: 500Gi
---
apiVersion: v1
kind: Service
metadata:
  name: sqlserver
  namespace: enterprise-prod
  labels:
    app: sqlserver
spec:
  selector:
    app: sqlserver
  ports:
  - port: 1433
    targetPort: 1433
  type: ClusterIP
---
apiVersion: v1
kind: Service
metadata:
  name: sqlserver-headless
  namespace: enterprise-prod
  labels:
    app: sqlserver
spec:
  selector:
    app: sqlserver
  ports:
  - port: 1433
    targetPort: 1433
  clusterIP: None
```

### Redis Cluster
```yaml
# infrastructure/cache/redis-cluster.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: redis
  namespace: enterprise-prod
  labels:
    app: redis
spec:
  serviceName: redis-headless
  replicas: 6
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        - containerPort: 16379
        command:
        - redis-server
        - /etc/redis/redis.conf
        - --cluster-enabled
        - "yes"
        - --cluster-config-file
        - /data/nodes.conf
        - --cluster-node-timeout
        - "5000"
        - --appendonly
        - "yes"
        resources:
          requests:
            cpu: 200m
            memory: 512Mi
          limits:
            cpu: 1000m
            memory: 2Gi
        volumeMounts:
        - name: redis-data
          mountPath: /data
        - name: redis-config
          mountPath: /etc/redis
        livenessProbe:
          exec:
            command:
            - redis-cli
            - ping
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          exec:
            command:
            - redis-cli
            - ping
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: redis-config
        configMap:
          name: redis-config
  volumeClaimTemplates:
  - metadata:
      name: redis-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      storageClassName: fast-ssd
      resources:
        requests:
          storage: 20Gi
---
apiVersion: v1
kind: Service
metadata:
  name: redis
  namespace: enterprise-prod
  labels:
    app: redis
spec:
  selector:
    app: redis
  ports:
  - name: redis
    port: 6379
    targetPort: 6379
  - name: redis-cluster
    port: 16379
    targetPort: 16379
  type: ClusterIP
---
apiVersion: v1
kind: Service
metadata:
  name: redis-headless
  namespace: enterprise-prod
  labels:
    app: redis
spec:
  selector:
    app: redis
  ports:
  - name: redis
    port: 6379
    targetPort: 6379
  - name: redis-cluster
    port: 16379
    targetPort: 16379
  clusterIP: None
```

## Configuration Management

### ConfigMaps
```yaml
# config/configmaps/application-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: application-config
  namespace: enterprise-prod
data:
  appsettings.Production.json: |
    {
      "Serilog": {
        "MinimumLevel": {
          "Default": "Information"
        },
        "WriteTo": [
          {
            "Name": "Seq",
            "Args": {
              "serverUrl": "http://seq:80"
            }
          }
        ]
      },
      "HealthChecks": {
        "UI": {
          "EvaluationTimeInSeconds": 30,
          "MinimumSecondsBetweenFailureNotifications": 300
        }
      }
    }
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: redis-config
  namespace: enterprise-prod
data:
  redis.conf: |
    bind 0.0.0.0
    protected-mode no
    port 6379
    tcp-backlog 511
    timeout 0
    tcp-keepalive 300
    daemonize no
    supervised no
    pidfile /var/run/redis_6379.pid
    loglevel notice
    logfile ""
    databases 16
    always-show-logo yes
    save 900 1
    save 300 10
    save 60 10000
    stop-writes-on-bgsave-error yes
    rdbcompression yes
    rdbchecksum yes
    dbfilename dump.rdb
    dir /data
    maxmemory 1gb
    maxmemory-policy allkeys-lru
```

### Secrets (Sealed Secrets)
```yaml
# config/sealed-secrets/database-secrets.yaml
apiVersion: bitnami.com/v1alpha1
kind: SealedSecret
metadata:
  name: database-secrets
  namespace: enterprise-prod
spec:
  encryptedData:
    sa-password: AgBy3i4OJSWK+PiTySYZZA9rO5QtQYwnS...
    identity-connection-string: AgAGV3GtBc8kpHQPTp...
    user-connection-string: AgBNEKqC0T8YBTQvI...
  template:
    metadata:
      name: database-secrets
      namespace: enterprise-prod
    type: Opaque
---
apiVersion: bitnami.com/v1alpha1
kind: SealedSecret
metadata:
  name: jwt-secrets
  namespace: enterprise-prod
spec:
  encryptedData:
    secret-key: AgBsKNI7d2vKXAEP2JGhN8H4CzUZgY...
  template:
    metadata:
      name: jwt-secrets
      namespace: enterprise-prod
    type: Opaque
```

## Ingress Configuration

### NGINX Ingress
```yaml
# ingress/nginx-ingress/enterprise-ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: enterprise-ingress
  namespace: enterprise-prod
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/use-regex: "true"
    nginx.ingress.kubernetes.io/rewrite-target: /$2
    nginx.ingress.kubernetes.io/rate-limit: "100"
    nginx.ingress.kubernetes.io/rate-limit-window: "1m"
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/cors-allow-origin: "https://app.enterprise.com"
    nginx.ingress.kubernetes.io/cors-allow-methods: "GET, POST, PUT, DELETE, OPTIONS"
    nginx.ingress.kubernetes.io/cors-allow-headers: "DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range,Authorization"
spec:
  tls:
  - hosts:
    - api.enterprise.com
    secretName: enterprise-tls
  rules:
  - host: api.enterprise.com
    http:
      paths:
      - path: /api/v1/identity(/|$)(.*)
        pathType: Prefix
        backend:
          service:
            name: identity-service
            port:
              number: 80
      - path: /api/v1/users(/|$)(.*)
        pathType: Prefix
        backend:
          service:
            name: user-service
            port:
              number: 80
      - path: /(.*)
        pathType: Prefix
        backend:
          service:
            name: api-gateway
            port:
              number: 80
```

### Cert Manager
```yaml
# ingress/cert-manager/cluster-issuer.yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: devops@enterprise.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
```

## Monitoring Stack

### Prometheus
```yaml
# infrastructure/monitoring/prometheus.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: prometheus
  namespace: enterprise-prod
  labels:
    app: prometheus
spec:
  replicas: 1
  selector:
    matchLabels:
      app: prometheus
  template:
    metadata:
      labels:
        app: prometheus
    spec:
      serviceAccountName: prometheus
      containers:
      - name: prometheus
        image: prom/prometheus:latest
        ports:
        - containerPort: 9090
        args:
        - '--config.file=/etc/prometheus/prometheus.yml'
        - '--storage.tsdb.path=/prometheus'
        - '--web.console.libraries=/etc/prometheus/console_libraries'
        - '--web.console.templates=/etc/prometheus/consoles'
        - '--web.enable-lifecycle'
        - '--storage.tsdb.retention.time=30d'
        resources:
          requests:
            cpu: 500m
            memory: 1Gi
          limits:
            cpu: 2000m
            memory: 4Gi
        volumeMounts:
        - name: prometheus-config
          mountPath: /etc/prometheus
        - name: prometheus-data
          mountPath: /prometheus
      volumes:
      - name: prometheus-config
        configMap:
          name: prometheus-config
      - name: prometheus-data
        persistentVolumeClaim:
          claimName: prometheus-pvc
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: prometheus
  namespace: enterprise-prod
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: prometheus
rules:
- apiGroups: [""]
  resources: ["nodes", "services", "endpoints", "pods"]
  verbs: ["get", "list", "watch"]
- apiGroups: ["extensions"]
  resources: ["ingresses"]
  verbs: ["get", "list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: prometheus
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: prometheus
subjects:
- kind: ServiceAccount
  name: prometheus
  namespace: enterprise-prod
```

## HPA (Horizontal Pod Autoscaler)

### Identity Service HPA
```yaml
# services/identity-service/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: identity-service-hpa
  namespace: enterprise-prod
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: identity-service
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
```

## Deployment Scripts

### Deploy Script
```bash
#!/bin/bash
# scripts/deploy-k8s.sh

set -e

NAMESPACE=${1:-enterprise-prod}
ENVIRONMENT=${2:-production}

echo "Deploying Enterprise Platform to $NAMESPACE ($ENVIRONMENT)"

# Create namespace
kubectl apply -f namespaces/enterprise-${ENVIRONMENT}.yaml

# Apply secrets (requires sealed-secrets controller)
kubectl apply -f config/sealed-secrets/ -n $NAMESPACE

# Apply configmaps
kubectl apply -f config/configmaps/ -n $NAMESPACE

# Deploy infrastructure
kubectl apply -f infrastructure/databases/ -n $NAMESPACE
kubectl apply -f infrastructure/cache/ -n $NAMESPACE
kubectl apply -f infrastructure/messaging/ -n $NAMESPACE
kubectl apply -f infrastructure/storage/ -n $NAMESPACE

echo "Waiting for infrastructure to be ready..."
kubectl wait --for=condition=ready pod -l app=sqlserver -n $NAMESPACE --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n $NAMESPACE --timeout=300s

# Deploy application services
kubectl apply -f services/ -n $NAMESPACE

echo "Waiting for services to be ready..."
kubectl wait --for=condition=ready pod -l app=identity-service -n $NAMESPACE --timeout=300s
kubectl wait --for=condition=ready pod -l app=user-service -n $NAMESPACE --timeout=300s
kubectl wait --for=condition=ready pod -l app=api-gateway -n $NAMESPACE --timeout=300s

# Deploy monitoring
kubectl apply -f infrastructure/monitoring/ -n $NAMESPACE

# Deploy ingress
kubectl apply -f ingress/ -n $NAMESPACE

echo "Deployment completed successfully!"
echo "API Gateway: https://api.enterprise.com"
echo "Grafana: https://grafana.enterprise.com"
echo "Prometheus: https://prometheus.enterprise.com"

# Health check
kubectl get all -n $NAMESPACE
```

### Rolling Update Script
```bash
#!/bin/bash
# scripts/rolling-update.sh

SERVICE=$1
VERSION=$2
NAMESPACE=${3:-enterprise-prod}

if [ -z "$SERVICE" ] || [ -z "$VERSION" ]; then
    echo "Usage: $0 <service-name> <version> [namespace]"
    exit 1
fi

echo "Performing rolling update for $SERVICE to version $VERSION in namespace $NAMESPACE"

# Update deployment with new image
kubectl set image deployment/$SERVICE $SERVICE=enterprise/$SERVICE:$VERSION -n $NAMESPACE

# Monitor rollout
kubectl rollout status deployment/$SERVICE -n $NAMESPACE --timeout=600s

echo "Rolling update completed successfully!"
```

## Troubleshooting

### Debug Scripts
```bash
#!/bin/bash
# scripts/debug-pod.sh

POD_NAME=$1
NAMESPACE=${2:-enterprise-prod}

if [ -z "$POD_NAME" ]; then
    echo "Usage: $0 <pod-name> [namespace]"
    exit 1
fi

echo "=== Pod Description ==="
kubectl describe pod $POD_NAME -n $NAMESPACE

echo "=== Pod Logs ==="
kubectl logs $POD_NAME -n $NAMESPACE --tail=100

echo "=== Pod Events ==="
kubectl get events --field-selector involvedObject.name=$POD_NAME -n $NAMESPACE
```

### Health Check Script
```bash
#!/bin/bash
# scripts/health-check-k8s.sh

NAMESPACE=${1:-enterprise-prod}

echo "=== Health Check for $NAMESPACE ==="

# Check pod status
echo "Pod Status:"
kubectl get pods -n $NAMESPACE

# Check service endpoints
echo -e "\nService Endpoints:"
kubectl get endpoints -n $NAMESPACE

# Check ingress status
echo -e "\nIngress Status:"
kubectl get ingress -n $NAMESPACE

# Check HPA status
echo -e "\nHPA Status:"
kubectl get hpa -n $NAMESPACE

# Check resource usage
echo -e "\nResource Usage:"
kubectl top pods -n $NAMESPACE
```

Bu Kubernetes konfigürasyonu ile production-ready, scalable ve maintainable bir Enterprise Platform deployment'ı sağlanır.
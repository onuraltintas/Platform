# Monitoring Infrastructure Guide

## Genel Bakış
Bu dizin, Enterprise Platform için kapsamlı monitoring ve observability stack'ini içerir. Prometheus, Grafana, Jaeger ve Seq ile metrics, logs ve distributed tracing için complete visibility sağlanır.

## Monitoring Stack Bileşenleri

### 1. Metrics Collection (Prometheus)
- Application metrics (business KPIs)
- Infrastructure metrics (CPU, memory, disk, network)
- Custom metrics (response times, error rates)
- Service discovery ve auto-scraping

### 2. Visualization (Grafana)
- Pre-configured dashboards
- Alerting rules
- Multi-datasource support
- Custom panels ve visualizations

### 3. Distributed Tracing (Jaeger)
- Request flow tracking
- Performance bottleneck identification
- Service dependency mapping
- Error propagation analysis

### 4. Structured Logging (Seq)
- Centralized log aggregation
- Query ve filtering capabilities
- Log correlation
- Real-time log streaming

## Dizin Yapısı
```
monitoring/
├── prometheus/
│   ├── prometheus.yml           # Ana konfigürasyon
│   ├── rules/                   # Alert rules
│   │   ├── application.yml
│   │   ├── infrastructure.yml
│   │   └── business.yml
│   └── targets/                 # Static targets
├── grafana/
│   ├── provisioning/
│   │   ├── datasources/
│   │   ├── dashboards/
│   │   └── notifiers/
│   ├── dashboards/
│   │   ├── application/
│   │   ├── infrastructure/
│   │   └── business/
│   └── grafana.ini
├── jaeger/
│   ├── jaeger-all-in-one.yml
│   ├── jaeger-production.yml
│   └── sampling-strategies.json
└── seq/
    ├── seq.json
    └── log-retention-policy.json
```

## Prometheus Konfigürasyonu

### prometheus.yml
```yaml
# prometheus/prometheus.yml
global:
  scrape_interval: 15s
  scrape_timeout: 10s
  evaluation_interval: 15s
  external_labels:
    cluster: 'enterprise-cluster'
    replica: 'prometheus-1'

rule_files:
  - "/etc/prometheus/rules/*.yml"

scrape_configs:
  # Kubernetes API Server
  - job_name: 'kubernetes-apiservers'
    kubernetes_sd_configs:
    - role: endpoints
      namespaces:
        names:
        - default
    scheme: https
    tls_config:
      ca_file: /var/run/secrets/kubernetes.io/serviceaccount/ca.crt
      insecure_skip_verify: true
    bearer_token_file: /var/run/secrets/kubernetes.io/serviceaccount/token
    relabel_configs:
    - source_labels: [__meta_kubernetes_namespace, __meta_kubernetes_service_name, __meta_kubernetes_endpoint_port_name]
      action: keep
      regex: default;kubernetes;https

  # Kubernetes Nodes
  - job_name: 'kubernetes-nodes'
    kubernetes_sd_configs:
    - role: node
    scheme: https
    tls_config:
      ca_file: /var/run/secrets/kubernetes.io/serviceaccount/ca.crt
      insecure_skip_verify: true
    bearer_token_file: /var/run/secrets/kubernetes.io/serviceaccount/token
    relabel_configs:
    - action: labelmap
      regex: __meta_kubernetes_node_label_(.+)

  # Kubernetes Pods
  - job_name: 'kubernetes-pods'
    kubernetes_sd_configs:
    - role: pod
    relabel_configs:
    - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_scrape]
      action: keep
      regex: true
    - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_path]
      action: replace
      target_label: __metrics_path__
      regex: (.+)
    - source_labels: [__address__, __meta_kubernetes_pod_annotation_prometheus_io_port]
      action: replace
      regex: ([^:]+)(?::\d+)?;(\d+)
      replacement: $1:$2
      target_label: __address__
    - action: labelmap
      regex: __meta_kubernetes_pod_label_(.+)
    - source_labels: [__meta_kubernetes_namespace]
      action: replace
      target_label: kubernetes_namespace
    - source_labels: [__meta_kubernetes_pod_name]
      action: replace
      target_label: kubernetes_pod_name

  # Application Services
  - job_name: 'identity-service'
    static_configs:
    - targets: ['identity-service:9090']
    scrape_interval: 5s
    metrics_path: /metrics
    relabel_configs:
    - source_labels: [__address__]
      target_label: service
      replacement: identity-service

  - job_name: 'user-service'
    static_configs:
    - targets: ['user-service:9090']
    scrape_interval: 5s
    metrics_path: /metrics
    relabel_configs:
    - source_labels: [__address__]
      target_label: service
      replacement: user-service

  - job_name: 'api-gateway'
    static_configs:
    - targets: ['api-gateway:9090']
    scrape_interval: 5s
    metrics_path: /metrics
    relabel_configs:
    - source_labels: [__address__]
      target_label: service
      replacement: api-gateway

  # Infrastructure Services
  - job_name: 'redis-exporter'
    static_configs:
    - targets: ['redis-exporter:9121']
    scrape_interval: 15s

  - job_name: 'sqlserver-exporter'
    static_configs:
    - targets: ['sqlserver-exporter:9399']
    scrape_interval: 30s

  - job_name: 'node-exporter'
    static_configs:
    - targets: ['node-exporter:9100']
    scrape_interval: 15s

  # RabbitMQ
  - job_name: 'rabbitmq'
    static_configs:
    - targets: ['rabbitmq:15692']
    scrape_interval: 15s

alerting:
  alertmanagers:
  - static_configs:
    - targets:
      - alertmanager:9093

# Remote write for long-term storage (optional)
# remote_write:
#   - url: "https://prometheus-remote-write.example.com/api/v1/write"
#     basic_auth:
#       username: "user"
#       password: "password"
```

### Alert Rules

#### Application Alert Rules
```yaml
# prometheus/rules/application.yml
groups:
- name: application.rules
  rules:
  - alert: HighErrorRate
    expr: |
      (
        sum(rate(http_requests_total{status=~"5.."}[5m])) by (service)
        /
        sum(rate(http_requests_total[5m])) by (service)
      ) > 0.05
    for: 5m
    labels:
      severity: warning
      category: application
    annotations:
      summary: "High error rate detected for {{ $labels.service }}"
      description: "Error rate is {{ $value | humanizePercentage }} for service {{ $labels.service }}"

  - alert: SlowResponseTime
    expr: histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (le, service)) > 2
    for: 10m
    labels:
      severity: warning
      category: performance
    annotations:
      summary: "Slow response time for {{ $labels.service }}"
      description: "95th percentile response time is {{ $value }}s for service {{ $labels.service }}"

  - alert: ServiceDown
    expr: up{job=~"identity-service|user-service|api-gateway"} == 0
    for: 1m
    labels:
      severity: critical
      category: availability
    annotations:
      summary: "Service {{ $labels.job }} is down"
      description: "{{ $labels.job }} has been down for more than 1 minute"

  - alert: HighRequestRate
    expr: sum(rate(http_requests_total[5m])) by (service) > 1000
    for: 15m
    labels:
      severity: warning
      category: capacity
    annotations:
      summary: "High request rate for {{ $labels.service }}"
      description: "Request rate is {{ $value }} req/s for service {{ $labels.service }}"

  - alert: DatabaseConnectionIssues
    expr: |
      sum(rate(database_connection_errors_total[5m])) by (service) > 0
    for: 5m
    labels:
      severity: critical
      category: database
    annotations:
      summary: "Database connection issues for {{ $labels.service }}"
      description: "Database connection errors detected for service {{ $labels.service }}"
```

#### Infrastructure Alert Rules
```yaml
# prometheus/rules/infrastructure.yml
groups:
- name: infrastructure.rules
  rules:
  - alert: HighCPUUsage
    expr: |
      (
        sum(rate(container_cpu_usage_seconds_total{container!=""}[5m])) by (pod)
        /
        sum(container_spec_cpu_quota{container!=""}/container_spec_cpu_period{container!=""}) by (pod)
      ) > 0.8
    for: 10m
    labels:
      severity: warning
      category: resources
    annotations:
      summary: "High CPU usage for pod {{ $labels.pod }}"
      description: "CPU usage is {{ $value | humanizePercentage }} for pod {{ $labels.pod }}"

  - alert: HighMemoryUsage
    expr: |
      (
        sum(container_memory_working_set_bytes{container!=""}) by (pod)
        /
        sum(container_spec_memory_limit_bytes{container!=""}) by (pod)
      ) > 0.9
    for: 10m
    labels:
      severity: warning
      category: resources
    annotations:
      summary: "High memory usage for pod {{ $labels.pod }}"
      description: "Memory usage is {{ $value | humanizePercentage }} for pod {{ $labels.pod }}"

  - alert: DiskSpaceLow
    expr: |
      (
        node_filesystem_free_bytes{mountpoint="/"}
        /
        node_filesystem_size_bytes{mountpoint="/"}
      ) < 0.1
    for: 15m
    labels:
      severity: critical
      category: storage
    annotations:
      summary: "Low disk space on {{ $labels.instance }}"
      description: "Disk space is {{ $value | humanizePercentage }} full on {{ $labels.instance }}"

  - alert: NodeDown
    expr: up{job="node-exporter"} == 0
    for: 5m
    labels:
      severity: critical
      category: availability
    annotations:
      summary: "Node {{ $labels.instance }} is down"
      description: "Node exporter for {{ $labels.instance }} has been down for more than 5 minutes"
```

#### Business Alert Rules
```yaml
# prometheus/rules/business.yml
groups:
- name: business.rules
  rules:
  - alert: LowUserRegistrations
    expr: |
      sum(increase(user_registrations_total[1h])) < 10
    for: 30m
    labels:
      severity: info
      category: business
    annotations:
      summary: "Low user registration rate"
      description: "Only {{ $value }} users registered in the last hour"

  - alert: HighLoginFailures
    expr: |
      (
        sum(increase(login_failures_total[5m]))
        /
        sum(increase(login_attempts_total[5m]))
      ) > 0.1
    for: 10m
    labels:
      severity: warning
      category: security
    annotations:
      summary: "High login failure rate"
      description: "Login failure rate is {{ $value | humanizePercentage }} in the last 5 minutes"

  - alert: PaymentProcessingIssues
    expr: |
      sum(increase(payment_failures_total[10m])) > 5
    for: 5m
    labels:
      severity: critical
      category: business
    annotations:
      summary: "Payment processing issues detected"
      description: "{{ $value }} payment failures in the last 10 minutes"
```

## Grafana Konfigürasyonu

### datasources/prometheus.yml
```yaml
# grafana/provisioning/datasources/prometheus.yml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: true
    jsonData:
      httpMethod: POST
      queryTimeout: 60s
      
  - name: Jaeger
    type: jaeger
    access: proxy
    url: http://jaeger:16686
    editable: true

  - name: Seq
    type: fluentd
    access: proxy
    url: http://seq:80
    editable: true
    basicAuth: false
```

### Dashboard Provisioning
```yaml
# grafana/provisioning/dashboards/default.yml
apiVersion: 1

providers:
  - name: 'Application Dashboards'
    folder: 'Enterprise Platform'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 30
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards/application

  - name: 'Infrastructure Dashboards'
    folder: 'Infrastructure'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 30
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards/infrastructure

  - name: 'Business Dashboards'
    folder: 'Business KPIs'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 30
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards/business
```

### Application Dashboard Example
```json
{
  "dashboard": {
    "id": null,
    "title": "Enterprise Platform - Application Overview",
    "tags": ["enterprise", "application"],
    "timezone": "browser",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "sum(rate(http_requests_total[5m])) by (service)",
            "legendFormat": "{{ service }}"
          }
        ],
        "yAxes": [
          {
            "label": "Requests/sec",
            "min": 0
          }
        ],
        "gridPos": {
          "h": 8,
          "w": 12,
          "x": 0,
          "y": 0
        }
      },
      {
        "title": "Error Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "sum(rate(http_requests_total{status=~\"5..\"}[5m])) by (service) / sum(rate(http_requests_total[5m])) by (service)",
            "legendFormat": "{{ service }}"
          }
        ],
        "yAxes": [
          {
            "label": "Error Rate",
            "max": 1,
            "min": 0
          }
        ],
        "gridPos": {
          "h": 8,
          "w": 12,
          "x": 12,
          "y": 0
        }
      },
      {
        "title": "Response Time (95th percentile)",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (le, service))",
            "legendFormat": "{{ service }}"
          }
        ],
        "yAxes": [
          {
            "label": "Seconds",
            "min": 0
          }
        ],
        "gridPos": {
          "h": 8,
          "w": 24,
          "x": 0,
          "y": 8
        }
      },
      {
        "title": "Active Users",
        "type": "stat",
        "targets": [
          {
            "expr": "active_users_total"
          }
        ],
        "gridPos": {
          "h": 4,
          "w": 6,
          "x": 0,
          "y": 16
        }
      },
      {
        "title": "Database Connections",
        "type": "stat",
        "targets": [
          {
            "expr": "sum(database_connections_active) by (service)"
          }
        ],
        "gridPos": {
          "h": 4,
          "w": 6,
          "x": 6,
          "y": 16
        }
      },
      {
        "title": "Cache Hit Rate",
        "type": "stat",
        "targets": [
          {
            "expr": "sum(cache_hits_total) / (sum(cache_hits_total) + sum(cache_misses_total))"
          }
        ],
        "gridPos": {
          "h": 4,
          "w": 6,
          "x": 12,
          "y": 16
        }
      },
      {
        "title": "Queue Length",
        "type": "stat",
        "targets": [
          {
            "expr": "sum(queue_length) by (queue_name)"
          }
        ],
        "gridPos": {
          "h": 4,
          "w": 6,
          "x": 18,
          "y": 16
        }
      }
    ],
    "time": {
      "from": "now-1h",
      "to": "now"
    },
    "refresh": "5s"
  }
}
```

## Jaeger Konfigürasyonu

### jaeger-production.yml
```yaml
# jaeger/jaeger-production.yml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: jaeger-collector
  labels:
    app: jaeger
    component: collector
spec:
  replicas: 3
  selector:
    matchLabels:
      app: jaeger
      component: collector
  template:
    metadata:
      labels:
        app: jaeger
        component: collector
    spec:
      containers:
      - name: jaeger-collector
        image: jaegertracing/jaeger-collector:latest
        ports:
        - containerPort: 14268
          protocol: TCP
        - containerPort: 14250
          protocol: TCP
        env:
        - name: SPAN_STORAGE_TYPE
          value: elasticsearch
        - name: ES_SERVER_URLS
          value: http://elasticsearch:9200
        - name: ES_NUM_SHARDS
          value: "5"
        - name: ES_NUM_REPLICAS
          value: "1"
        - name: COLLECTOR_ZIPKIN_HOST_PORT
          value: ":9411"
        resources:
          requests:
            cpu: 200m
            memory: 256Mi
          limits:
            cpu: 1000m
            memory: 1Gi
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: jaeger-query
  labels:
    app: jaeger
    component: query
spec:
  replicas: 2
  selector:
    matchLabels:
      app: jaeger
      component: query
  template:
    metadata:
      labels:
        app: jaeger
        component: query
    spec:
      containers:
      - name: jaeger-query
        image: jaegertracing/jaeger-query:latest
        ports:
        - containerPort: 16686
          protocol: TCP
        env:
        - name: SPAN_STORAGE_TYPE
          value: elasticsearch
        - name: ES_SERVER_URLS
          value: http://elasticsearch:9200
        resources:
          requests:
            cpu: 100m
            memory: 128Mi
          limits:
            cpu: 500m
            memory: 512Mi
```

### sampling-strategies.json
```json
{
  "service_strategies": [
    {
      "service": "identity-service",
      "type": "probabilistic",
      "param": 0.5,
      "operation_strategies": [
        {
          "operation": "POST /api/auth/login",
          "type": "probabilistic",
          "param": 1.0
        }
      ]
    },
    {
      "service": "user-service",
      "type": "probabilistic",
      "param": 0.3
    },
    {
      "service": "api-gateway",
      "type": "probabilistic",
      "param": 0.1
    }
  ],
  "default_strategy": {
    "type": "probabilistic",
    "param": 0.1
  },
  "max_traces_per_second": 1000
}
```

## Seq Konfigürasyonu

### seq.json
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId"
    ]
  },
  "Storage": {
    "RetentionDays": 30,
    "MaxSize": "10GB",
    "CompactAfterDays": 7
  },
  "Api": {
    "ListenUris": [
      "http://+:80"
    ]
  },
  "Authentication": {
    "Provider": "None"
  },
  "Ingestion": {
    "ApiKey": {
      "Enabled": true,
      "Keys": [
        {
          "Id": "enterprise-platform",
          "Title": "Enterprise Platform",
          "Token": "enterprise-api-key-2024",
          "AssignedPermissions": ["Ingest"]
        }
      ]
    }
  },
  "Alerting": {
    "Enabled": true,
    "Rules": [
      {
        "Id": "high-error-rate",
        "Title": "High Error Rate",
        "Description": "Triggers when error rate exceeds 5%",
        "Filter": "@Level = 'Error' and @Timestamp > Now() - 5m",
        "Threshold": 50,
        "Actions": [
          {
            "Type": "Webhook",
            "Url": "https://hooks.slack.com/services/...",
            "Method": "POST",
            "Headers": {
              "Content-Type": "application/json"
            },
            "Body": "{'text': 'High error rate detected: {Count} errors in 5 minutes'}"
          }
        ]
      }
    ]
  }
}
```

## Deployment Scripts

### Start Monitoring Stack
```bash
#!/bin/bash
# scripts/start-monitoring.sh

set -e

NAMESPACE=${1:-enterprise-monitoring}

echo "Starting monitoring stack in namespace $NAMESPACE..."

# Create namespace
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# Deploy Prometheus
echo "Deploying Prometheus..."
kubectl apply -f prometheus/ -n $NAMESPACE

# Wait for Prometheus
kubectl wait --for=condition=ready pod -l app=prometheus -n $NAMESPACE --timeout=300s

# Deploy Grafana
echo "Deploying Grafana..."
kubectl apply -f grafana/ -n $NAMESPACE

# Wait for Grafana
kubectl wait --for=condition=ready pod -l app=grafana -n $NAMESPACE --timeout=300s

# Deploy Jaeger
echo "Deploying Jaeger..."
kubectl apply -f jaeger/ -n $NAMESPACE

# Deploy Seq
echo "Deploying Seq..."
kubectl apply -f seq/ -n $NAMESPACE

echo "Monitoring stack deployed successfully!"
echo "Access URLs:"
echo "- Prometheus: http://prometheus.enterprise.local"
echo "- Grafana: http://grafana.enterprise.local (admin/admin)"
echo "- Jaeger: http://jaeger.enterprise.local"
echo "- Seq: http://seq.enterprise.local"
```

### Health Check Script
```bash
#!/bin/bash
# scripts/monitoring-health-check.sh

NAMESPACE=${1:-enterprise-monitoring}

echo "=== Monitoring Stack Health Check ==="

# Check Prometheus
echo "Checking Prometheus..."
PROMETHEUS_STATUS=$(kubectl get pods -l app=prometheus -n $NAMESPACE -o jsonpath='{.items[0].status.phase}')
if [ "$PROMETHEUS_STATUS" = "Running" ]; then
    echo "✅ Prometheus is running"
else
    echo "❌ Prometheus is not running: $PROMETHEUS_STATUS"
fi

# Check Grafana
echo "Checking Grafana..."
GRAFANA_STATUS=$(kubectl get pods -l app=grafana -n $NAMESPACE -o jsonpath='{.items[0].status.phase}')
if [ "$GRAFANA_STATUS" = "Running" ]; then
    echo "✅ Grafana is running"
else
    echo "❌ Grafana is not running: $GRAFANA_STATUS"
fi

# Check Jaeger
echo "Checking Jaeger..."
JAEGER_STATUS=$(kubectl get pods -l app=jaeger -n $NAMESPACE -o jsonpath='{.items[0].status.phase}')
if [ "$JAEGER_STATUS" = "Running" ]; then
    echo "✅ Jaeger is running"
else
    echo "❌ Jaeger is not running: $JAEGER_STATUS"
fi

# Check Seq
echo "Checking Seq..."
SEQ_STATUS=$(kubectl get pods -l app=seq -n $NAMESPACE -o jsonpath='{.items[0].status.phase}')
if [ "$SEQ_STATUS" = "Running" ]; then
    echo "✅ Seq is running"
else
    echo "❌ Seq is not running: $SEQ_STATUS"
fi

# Check resource usage
echo -e "\nResource Usage:"
kubectl top pods -n $NAMESPACE
```

## Alerting Integration

### Slack Integration
```yaml
# alertmanager/alertmanager.yml
global:
  slack_api_url: 'https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK'

route:
  group_by: ['alertname']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'web.hook'
  routes:
  - match:
      severity: critical
    receiver: 'slack-critical'
  - match:
      severity: warning
    receiver: 'slack-warning'

receivers:
- name: 'web.hook'
  webhook_configs:
  - url: 'http://127.0.0.1:5001/'

- name: 'slack-critical'
  slack_configs:
  - channel: '#alerts-critical'
    title: 'Critical Alert'
    text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
    color: 'danger'

- name: 'slack-warning'
  slack_configs:
  - channel: '#alerts-warning'
    title: 'Warning Alert'
    text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
    color: 'warning'
```

## Performance Optimization

### Prometheus Optimization
```yaml
# High-performance Prometheus config
global:
  scrape_interval: 15s
  scrape_timeout: 10s
  
# Storage optimization
storage.tsdb.retention.time: 15d
storage.tsdb.retention.size: 50GB
storage.tsdb.wal-compression: true

# Query optimization
query.max-concurrency: 20
query.timeout: 2m
```

### Grafana Optimization
```ini
# grafana.ini performance settings
[database]
max_idle_conn = 5
max_open_conn = 10
conn_max_lifetime = 14400

[session]
provider = redis
provider_config = addr=redis:6379,pool_size=4,db=grafana

[caching]
enabled = true
```

Bu monitoring stack ile Enterprise Platform'un tüm bileşenlerinde complete observability sağlanır.
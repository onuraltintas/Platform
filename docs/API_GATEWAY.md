# API Gateway

Bu doküman, EgitimPlatform API Gateway (YARP) konfigürasyonu ve ortam ayrımlarını açıklar.

## 1) Reverse Proxy
- Development: `Gateways/EgitimPlatform.Gateway/appsettings.Development.json`
  - Cluster hedefleri local HTTPS portlarına yönlenir (launchSettings.json ile uyumlu).
- Docker/Prod: `Gateways/EgitimPlatform.Gateway/appsettings.Docker.json`
  - Hedefler container DNS isimleridir: `http://identity-service/`, `http://user-service/` vb.

## 2) Ortam Ayrımı
- `ASPNETCORE_ENVIRONMENT` = `Docker` iken Gateway Kestrel sadece HTTP:80 dinler (TLS offload reverse proxy/ingress ile yapılır).
- Development’ta Kestrel 80/443 dinler ve local sertifika kullanır.

## 3) CORS
- Development: `Cors:AllowedOrigins` içinde `http(s)://localhost:4200` tanımlıdır.
- Docker: `appsettings.Docker.json` içinde `Cors:AllowedOrigins` ile üretim domain’leri whitelist yapılmalıdır (ör. `https://admin.yourdomain.com`).

## 4) Health Checks
- Development: Gateway içi HealthChecks UI, `appsettings.Development.json` altındaki hedeflerden beslenir.
- Docker: Ayrı `healthchecks-ui` container’ı kullanılır; Gateway `/health` endpoint’i ve servis health’leri UI tarafından izlenir.

## 5) Güvenlik & Politikalar
- JWT doğrulama Shared Security uzantılarıyla yapılır.
- YARP rotalarında `AuthorizationPolicy` tanımları ile Admin/Authenticated politikaları uygulanır.

## 6) Loglama
- Yapılandırılmış loglama aktif; Docker’da `gateway_logs` volume’u altında kalıcıdır.



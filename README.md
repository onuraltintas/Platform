## Platform (EgitimPlatform)

Monorepo: Angular Admin Panel + .NET 8 mikro servisler (Identity, User, Notification, FeatureFlag) + Docker Compose.

### Özellikler
- Kimlik Doğrulama: JWT Access + HttpOnly Refresh Cookie, Remember Me akışı, Google OAuth 2.0 ile giriş/kayıt (otomatik kayıt)
- Yetkilendirme: Rol/İzin tabanlı, URL düzenine göre Merkezi Politika (DB’den dinamik)
- Güvenlik: E‑posta enumerate koruması, şifre sıfırlama token URL‑encode/decode, Security Headers, CORS
- UX İyileştirmeleri: Modern çıkış butonu, login sonrası avatar görünürlüğü, konsol/toast gürültüsü azaltma
- Gözlemlenebilirlik: Basit loglama, izleme ve metrik orta katmanları

### Kurulum (Hızlı Başlangıç)
1) .env oluşturun
```bash
cp env.example .env
# .env içinde en azından e‑posta ve Google OAuth ayarlarını doldurun:
# GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, EMAIL_*
```

2) Docker ile tüm sistemi başlatın
```bash
docker compose up -d
```

3) Uygulamalar
- Admin Panel (Angular): http://localhost:4200
- IdentityService: http://localhost:5002
- SQL Server, Redis, RabbitMQ: docker-compose ile ayağa kalkar

### Geliştirme
- Frontend
  - Yol: `Client/angular/admin-panel`
  - Geliştirme sunucusu: `npm install && npm start` veya `ng serve`
- Backend
  - Servisler Docker içinde çalışır. Kod değişikliklerinde ilgili servisi yeniden build etmek için:
```bash
docker compose build identity-service && docker compose up -d identity-service
```

### Ortam Değişkenleri (özet)
- Identity/Genel: `JWT_*`, `CORS_*`, `LOG_*`
- DB: `DB_SERVER`, `DB_USER`, `DB_PASSWORD`, `DB_NAME_*`
- Google OAuth: `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_REDIRECT_URI`
- E‑posta: `EMAIL_SMTP_SERVER`, `EMAIL_USERNAME`, `EMAIL_PASSWORD`, `EMAIL_FROM_*`

Detaylar için `env.example` dosyasına bakın.

### Yetkilendirme Politikaları (Merkezi)
- Backend: `AuthorizationPolicies` tablosu (Regex ile eşleşen URL → gereken rol/izin)
- API: `GET /api/auth/authorization-policies` (Gateway: `/api/v1/auth/authorization-policies`)
- Frontend: `AuthorizationPolicyService` ve `permissionGuard` tüm korumalı rotalarda kullanılır.

### Güvenlik Notları
- Refresh token frontend’de saklanmaz; HttpOnly cookie kullanılır (Remember Me seçeneği ve Google akışında ayarlanır).
- E‑posta akışlarında “kayıtlı/kayıtsız” ayrımı kullanıcıya gösterilmez.
- Şifre sıfırlama token’ları URL‑encode edilerek üretilir, frontend URL‑decode eder.

### Sık Komutlar
```bash
# Logları izleme
docker logs -f egitimplatform-identity-service | cat

# Servisi yeniden başlatma
docker compose restart identity-service

# Veritabanı tohumlama (seed) scriptleri
docker/database/*
```

### Dizin Yapısı (özet)
```
Client/            # Angular Admin Panel
Services/
  IdentityService/ # Auth, JWT, OAuth, Yetki Politikaları
  UserService/
  NotificationService/
  FeatureFlagService/
Shared/            # Ortak kütüphaneler (Security, Logging, Email, v.s.)
Gateways/          # API Gateway (varsa)
docker/            # DB init, seed, vs.
docker-compose.yml
env.example
```

### Katkı
- PR’lar ve Issue’lar memnuniyetle karşılanır. Kod standardı ve güvenlik ilkelerine uyun.

### Lisans
- Telif sahibine aittir. Gerektiğinde lisans dosyası eklenecektir.


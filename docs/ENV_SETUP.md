# Environment Configuration Setup

Bu dokümantasyon, EgitimPlatform projesinin environment konfigürasyonunu açıklar.

## .env Dosyası Kullanımı

### 1. .env Dosyası Oluşturma

Proje kök dizininde `.env` dosyası oluşturun:

```bash
# Örnek .env dosyasını kopyalayın
cp env.example .env
```

### 2. .env Dosyası İçeriği

`.env` dosyası aşağıdaki konfigürasyonları içerir:

#### Database Configuration
```env
DB_SERVER=sqlserver
DB_PORT=1433
DB_NAME_IDENTITY=EgitimPlatform_Identity
DB_NAME_USER_SERVICE=EgitimPlatform_UserService
DB_NAME_NOTIFICATION_SERVICE=EgitimPlatform_NotificationService
DB_NAME_FEATURE_FLAG_SERVICE=EgitimPlatform_FeatureFlagService
DB_USER=sa
DB_PASSWORD=EgitimPlatform123!
DB_TRUST_SERVER_CERTIFICATE=true
```

#### Redis Configuration
```env
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_DATABASE=0
```

#### JWT Configuration
```env
JWT_SECRET_KEY=EgitimPlatformSuperSecretKey2024!
JWT_ISSUER=EgitimPlatform
JWT_AUDIENCE=EgitimPlatform
JWT_EXPIRATION_MINUTES=60
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
```

#### Email Configuration
```env
EMAIL_SMTP_SERVER=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=your-app-password
EMAIL_FROM_EMAIL=noreply@egitimplatform.com
EMAIL_FROM_NAME=Eğitim Platform
```

#### Google OAuth Configuration
```env
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GOOGLE_REDIRECT_URI=http://localhost:5002/api/auth/google/callback
GOOGLE_AUTHORIZATION_ENDPOINT=https://accounts.google.com/o/oauth2/v2/auth
GOOGLE_TOKEN_ENDPOINT=https://oauth2.googleapis.com/token
GOOGLE_USER_INFO_ENDPOINT=https://www.googleapis.com/oauth2/v2/userinfo
GOOGLE_SCOPE=openid email profile
```

#### Service Ports
```env
API_GATEWAY_PORT=5000
API_GATEWAY_HTTPS_PORT=5001
IDENTITY_SERVICE_PORT=5002
IDENTITY_SERVICE_HTTPS_PORT=5003
USER_SERVICE_PORT=5004
USER_SERVICE_HTTPS_PORT=5005
NOTIFICATION_SERVICE_PORT=5006
NOTIFICATION_SERVICE_HTTPS_PORT=5007
FEATURE_FLAG_SERVICE_PORT=5008
FEATURE_FLAG_SERVICE_HTTPS_PORT=5009
```

### 3. Google OAuth Kurulumu

#### Google Cloud Console Ayarları

1. **Google Cloud Console'a gidin**
   - https://console.cloud.google.com/
   - Yeni bir proje oluşturun veya mevcut projeyi seçin

2. **OAuth 2.0 Client ID oluşturun**
   - "APIs & Services" > "Credentials" bölümüne gidin
   - "Create Credentials" > "OAuth 2.0 Client IDs" seçin
   - Application type: "Web application" seçin
   - Authorized redirect URIs: `http://localhost:5002/api/auth/google/callback` ekleyin

3. **Client ID ve Client Secret'ı alın**
   - Oluşturulan OAuth 2.0 client'ın detaylarını görüntüleyin
   - Client ID ve Client Secret'ı kopyalayın

4. **.env dosyasını güncelleyin**
```env
GOOGLE_CLIENT_ID=your-actual-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-actual-client-secret
```

#### Google OAuth Test

Google OAuth'ın çalışıp çalışmadığını test etmek için:

```bash
# Identity Service'in çalıştığından emin olun
docker-compose ps

# Google OAuth endpoint'ini test edin
curl http://localhost:5002/api/auth/google/authorize?state=test
```

### 4. Güvenlik Notları

#### Hassas Bilgiler
- `.env` dosyası asla version control'e commit edilmemelidir
- `.gitignore` dosyasında `.env` satırının olduğundan emin olun
- Production ortamında güçlü şifreler kullanın

#### Örnek Güvenli Konfigürasyon
```env
# Production için güçlü şifreler
DB_PASSWORD=YourStrongPassword123!@#
JWT_SECRET_KEY=YourVeryLongSecretKeyAtLeast256BitsLongForSecurity
EMAIL_PASSWORD=your-app-specific-password
GOOGLE_CLIENT_SECRET=your-production-google-client-secret
```

### 5. Docker Compose ile Kullanım

Docker Compose otomatik olarak `.env` dosyasını okur ve environment variable'ları kullanır:

```bash
# .env dosyası ile çalıştırma
docker-compose up -d

# Belirli bir .env dosyası ile çalıştırma
docker-compose --env-file .env.production up -d
```

### 6. Environment-Specific Konfigürasyonlar

#### Development
```env
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
GOOGLE_REDIRECT_URI=http://localhost:5002/api/auth/google/callback
```

#### Production
```env
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Warning
GOOGLE_REDIRECT_URI=https://yourdomain.com/api/auth/google/callback
```

### 7. Validation

Environment variable'ların doğru yüklendiğini kontrol etmek için:

```bash
# Docker container içinde kontrol
docker exec egitimplatform-identity-service env | grep GOOGLE_

# Docker Compose ile kontrol
docker-compose config
```

### 8. Troubleshooting

#### Yaygın Sorunlar

1. **Environment variable bulunamadı**
   - `.env` dosyasının doğru konumda olduğundan emin olun
   - Dosya formatının doğru olduğunu kontrol edin

2. **Docker Compose environment variable'ları okumuyor**
   - Docker Compose versiyonunuzu kontrol edin
   - `.env` dosyasının syntax'ını kontrol edin

3. **Google OAuth çalışmıyor**
   - Google Cloud Console'da OAuth 2.0 client'ın doğru yapılandırıldığından emin olun
   - Redirect URI'nin doğru olduğunu kontrol edin
   - Client ID ve Client Secret'ın doğru kopyalandığından emin olun

4. **Güvenlik uyarıları**
   - Hassas bilgilerin `.env` dosyasında olduğundan emin olun
   - Production ortamında güçlü şifreler kullanın

### 9. Örnek Kullanım

```bash
# 1. .env dosyasını oluşturun
cp env.example .env

# 2. .env dosyasını düzenleyin
nano .env

# 3. Google OAuth ayarlarını yapılandırın
# GOOGLE_CLIENT_ID ve GOOGLE_CLIENT_SECRET'ı güncelleyin

# 4. Docker Compose ile çalıştırın
docker-compose up -d

# 5. Servisleri kontrol edin
docker-compose ps
```

### 10. CI/CD Entegrasyonu

CI/CD pipeline'larında environment variable'ları güvenli bir şekilde yönetmek için:

- GitHub Secrets
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault

gibi servisleri kullanabilirsiniz. 
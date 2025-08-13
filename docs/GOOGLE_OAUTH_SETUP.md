# Google OAuth Kurulum Rehberi

Bu dokümantasyon, EgitimPlatform projesi için Google OAuth entegrasyonunun nasıl kurulacağını açıklar.

## 📋 Ön Gereksinimler

- Google hesabı
- Google Cloud Console erişimi
- EgitimPlatform projesinin çalışır durumda olması

## 🚀 Kurulum Adımları

### 1. Google Cloud Console Projesi Oluşturma

1. **Google Cloud Console'a gidin**
   - https://console.cloud.google.com/
   - Google hesabınızla giriş yapın

2. **Yeni proje oluşturun**
   - Üst menüden "Select a project" > "New Project"
   - Proje adı: `EgitimPlatform`
   - "Create" butonuna tıklayın

3. **Projeyi seçin**
   - Oluşturulan projeyi seçin

### 2. OAuth Consent Screen Yapılandırması

1. **OAuth consent screen'e gidin**
   - Sol menüden "APIs & Services" > "OAuth consent screen"
   - "External" seçin (eğer Google Workspace kullanmıyorsanız)
   - "Create" butonuna tıklayın

2. **App information doldurun**
   ```
   App name: EgitimPlatform
   User support email: your-email@gmail.com
   Developer contact information: your-email@gmail.com
   ```

3. **Scopes ekleyin**
   - "Add or remove scopes" > "Save and continue"
   - Aşağıdaki scopes'ları ekleyin:
     - `openid`
     - `email`
     - `profile`

4. **Test users ekleyin (opsiyonel)**
   - Test kullanıcıları ekleyin
   - "Save and continue"

### 3. OAuth 2.0 Client ID Oluşturma

1. **Credentials sayfasına gidin**
   - Sol menüden "APIs & Services" > "Credentials"
   - "Create Credentials" > "OAuth 2.0 Client IDs"

2. **Application type seçin**
   - "Web application" seçin
   - "Create" butonuna tıklayın

3. **Client configuration**
   ```
   Name: EgitimPlatform Web Client
   ```

4. **Authorized JavaScript origins ekleyin**
   ```
   # Development ortamı
   http://localhost:4200
   https://localhost:4200
   
   # Production ortamı (gelecekte)
   https://yourdomain.com
   https://www.yourdomain.com
   ```

5. **Authorized redirect URIs ekleyin**
   ```
   # Development ortamı
   http://localhost:5002/api/auth/google/callback
   https://localhost:5003/api/auth/google/callback
   
   # Production ortamı (gelecekte)
   https://yourdomain.com/api/auth/google/callback
   https://www.yourdomain.com/api/auth/google/callback
   ```

6. **Create butonuna tıklayın**

### 4. Client ID ve Client Secret Alma

1. **Oluşturulan OAuth 2.0 client'ı görüntüleyin**
   - Credentials sayfasında oluşturulan client'ı tıklayın
   - Client ID ve Client Secret'ı kopyalayın

2. **Client Secret'ı indirin (opsiyonel)**
   - "Download JSON" butonuna tıklayın
   - JSON dosyasını güvenli bir yerde saklayın

### 5. Environment Variables Güncelleme

1. **.env dosyasını güncelleyin**
   ```bash
   # .env dosyasını açın
   nano .env
   ```

2. **Google OAuth ayarlarını ekleyin**
   ```env
   # Google OAuth Configuration
   GOOGLE_CLIENT_ID=your-actual-client-id.apps.googleusercontent.com
   GOOGLE_CLIENT_SECRET=your-actual-client-secret
   GOOGLE_REDIRECT_URI=http://localhost:5002/api/auth/google/callback
   GOOGLE_AUTHORIZATION_ENDPOINT=https://accounts.google.com/o/oauth2/v2/auth
   GOOGLE_TOKEN_ENDPOINT=https://oauth2.googleapis.com/token
   GOOGLE_USER_INFO_ENDPOINT=https://www.googleapis.com/oauth2/v2/userinfo
   GOOGLE_SCOPE=openid email profile
   ```

### 6. Docker Compose ile Test

1. **Servisleri yeniden başlatın**
   ```bash
   docker-compose down
   docker-compose up -d
   ```

2. **Servislerin çalıştığını kontrol edin**
   ```bash
   docker-compose ps
   ```

3. **Google OAuth endpoint'ini test edin**
   ```bash
   curl "http://localhost:5002/api/auth/google/authorize?state=test"
   ```

## 🔍 Test ve Doğrulama

### 1. Authorization URL Test

```bash
# Authorization URL'yi test edin
curl "http://localhost:5002/api/auth/google/authorize?state=test"
```

Beklenen çıktı:
```
https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=...&scope=...&response_type=code&state=test&access_type=offline&prompt=consent
```

### 2. Callback Endpoint Test

```bash
# Callback endpoint'ini test edin (code parametresi olmadan)
curl "http://localhost:5002/api/auth/google/callback?state=test"
```

### 3. Frontend Entegrasyonu

Angular uygulamanızda Google OAuth'ı test etmek için:

```typescript
// Google OAuth butonuna tıklandığında
const authUrl = 'http://localhost:5002/api/auth/google/authorize?state=' + Math.random();
window.location.href = authUrl;
```

## 🛠️ Troubleshooting

### Yaygın Sorunlar

1. **"redirect_uri_mismatch" hatası**
   - Google Cloud Console'da Authorized redirect URIs'de doğru URL'nin eklendiğinden emin olun
   - URL'nin tam olarak eşleştiğini kontrol edin (http vs https, port numarası)

2. **"invalid_client" hatası**
   - Client ID ve Client Secret'ın doğru kopyalandığından emin olun
   - .env dosyasında boşluk olmadığından emin olun

3. **"access_denied" hatası**
   - OAuth consent screen'de gerekli scopes'ların eklendiğinden emin olun
   - Test kullanıcılarının eklendiğinden emin olun

4. **CORS hatası**
   - Authorized JavaScript origins'de frontend URL'sinin eklendiğinden emin olun
   - CORS ayarlarının doğru yapılandırıldığından emin olun

### Debug Adımları

1. **Environment variables kontrolü**
   ```bash
   docker exec egitimplatform-identity-service env | grep GOOGLE_
   ```

2. **Log kontrolü**
   ```bash
   docker logs egitimplatform-identity-service
   ```

3. **Network kontrolü**
   ```bash
   docker exec egitimplatform-identity-service curl -I https://accounts.google.com
   ```

## 🔒 Güvenlik Notları

1. **Client Secret'ı güvenli tutun**
   - Client Secret'ı asla version control'e commit etmeyin
   - .env dosyasını .gitignore'a eklediğinizden emin olun

2. **Production ortamı**
   - Production'da HTTPS kullanın
   - Güçlü Client Secret kullanın
   - Domain whitelist'i sıkılaştırın

3. **Rate limiting**
   - Google OAuth endpoint'lerinde rate limiting uygulayın
   - Brute force saldırılarına karşı koruma ekleyin

## 📚 Ek Kaynaklar

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Google Cloud Console](https://console.cloud.google.com/)
- [OAuth 2.0 Best Practices](https://tools.ietf.org/html/rfc6819)

## 🎯 Sonraki Adımlar

1. **Frontend entegrasyonu**
   - Angular uygulamasında Google OAuth butonunu ekleyin
   - Callback handling'i implement edin

2. **Error handling**
   - OAuth hatalarını handle edin
   - User-friendly error mesajları ekleyin

3. **Testing**
   - Unit testler yazın
   - Integration testler ekleyin

4. **Monitoring**
   - OAuth başarı/başarısızlık oranlarını izleyin
   - Log analizi yapın 
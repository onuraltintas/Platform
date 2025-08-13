# Database Setup

Bu klasör, EgitimPlatform için otomatik veritabanı kurulumu ve seed data yönetimi içerir.

## Yapı

```
docker/database/
├── Dockerfile              # SQL Server container yapılandırması
├── entrypoint.sh          # Otomatik başlatma ve kurulum script'i
├── init.sql               # Ana veritabanı kurulum script'i
├── seeds/                 # Seed data script'leri
│   ├── 01_identity_seed.sql
│   ├── 02_user_service_seed.sql
│   ├── 03_notification_service_seed.sql
│   └── 04_feature_flag_service_seed.sql
└── README.md              # Bu dosya
```

## Otomatik Kurulum

Sistem başlatıldığında aşağıdaki işlemler otomatik olarak gerçekleşir:

1. **Veritabanları Oluşturulur:**
   - `EgitimPlatform_Identity`
   - `EgitimPlatform_UserService`
   - `EgitimPlatform_NotificationService`
   - `EgitimPlatform_FeatureFlagService`

2. **Kullanıcı ve İzinler:**
   - `EgitimPlatformUser` kullanıcısı oluşturulur
   - Her veritabanına gerekli izinler verilir

3. **Tablolar ve Yapılar:**
   - Her servis için gerekli tablolar oluşturulur
   - İlişkiler ve kısıtlamalar tanımlanır

4. **Seed Data:**
   - Varsayılan roller (Admin, User, Teacher, Student)
   - Admin kullanıcısı
   - Bildirim şablonları
   - Feature flag'ler
   - Örnek veriler

## Seed Data Ekleme

Yeni seed data eklemek için:

1. `seeds/` klasörüne yeni `.sql` dosyası ekleyin
2. Dosya adını `XX_description.sql` formatında verin (XX = sıra numarası)
3. Script'i `IF NOT EXISTS` kontrolleri ile yazın

## Örnek Kullanım

```bash
# Tüm sistemi başlat
docker-compose up -d

# Sadece veritabanını başlat
docker-compose up -d sqlserver

# Logları kontrol et
docker-compose logs -f sqlserver
```

## Veritabanı Bağlantısı

- **Server:** localhost:1433
- **Username:** sa
- **Password:** VForVan_40! (veya DB_PASSWORD environment variable)
- **Trust Server Certificate:** true

## Sorun Giderme

### Veritabanı Bağlantı Sorunu
```bash
# Container'ın çalışıp çalışmadığını kontrol et
docker-compose ps sqlserver

# Logları kontrol et
docker-compose logs sqlserver
```

### Seed Data Sorunu
```bash
# Container'a bağlan
docker exec -it egitimplatform-sqlserver bash

# SQL Server'a bağlan
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -C
```

### Veritabanlarını Sıfırlama
```bash
# Tüm container'ları durdur
docker-compose down

# Volume'u sil
docker volume rm egitimplatform_sqlserver_data

# Yeniden başlat
docker-compose up -d
``` 
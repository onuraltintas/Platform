# Database Setup

Bu rehber, EgitimPlatform projesinde SQL Server veritabanının Docker ile kurulumu ve yönetimini açıklar.

## 1) .env oluşturma

```bash
cp env.example .env
# Aşağıdaki anahtarları gözden geçirin
# DB_SERVER=sqlserver
# DB_PORT=1433
# DB_USER=EgitimPlatformUser
# DB_PASSWORD=<güçlü-parola>
# DB_NAME_* değerleri
```

Notlar:
- Uygulamalar `EgitimPlatformUser` ile bağlanır. SA sadece yönetim içindir.
- `docker/database/init.sql` login ve yetkileri otomatik kurar.

## 2) Docker ile başlatma

```bash
docker-compose up -d sqlserver
```

Healthcheck tamamlandıktan sonra diğer servisleri başlatabilirsiniz:

```bash
docker-compose up -d
```

## 3) EF Migrations

UserService için ilk migration komutları:

```bash
cd Services/UserService
# Gerekirse tasarım zamanı bağlantı için host override
export DB_HOST_FOR_MIGRATIONS=localhost
# DB bağlantısı .env'den otomatik alınır
# Migration oluşturma (oluşturulduysa atlayın)
dotnet ef migrations add InitialCreate -o Migrations
# Veritabanına uygulama
dotnet ef database update
```

## 4) Bağlantı dizesi

- Uygulama container'larında bağlantı dizesi `ConnectionStrings__DefaultConnection` environment değişkenleri ile geçilir (compose dosyasında tanımlı).
- Lokal EF CLI işlemlerinde `DesignTimeDbContextFactory` `.env` okur ve bağlantıyı oluşturur.

## 5) Güvenlik

- SA parolasını .env üzerinden verin; Dockerfile içinde parola tutulmaz.
- Production ortamında SA parolasını `EgitimPlatformUser` parolasından farklı ve daha güçlü tanımlayın.

## 6) Troubleshooting

- Login failed: `.env` içindeki `DB_PASSWORD` ve container içindeki SA/EgitimPlatformUser parolalarının eşleştiğini doğrulayın.
- EF CLI bağlantı sorunu: `DB_HOST_FOR_MIGRATIONS=localhost` ile hostta çalışırken `sqlserver` DNS’ini `localhost`'a yönlendirin.



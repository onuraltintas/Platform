# Enterprise.Shared.Notifications

## Proje Hakkında

Enterprise.Shared.Notifications, kurumsal platformlar için geliştirilmiş kapsamlı bir çok kanallı bildirim sistemidir. Bu sistem, kullanıcılara e-posta, SMS, push bildirimleri, uygulama içi bildirimler ve webhook'lar aracılığıyla bildirim gönderebilir.

## Temel Özellikler

### 🚀 Çok Kanallı Bildirim Desteği
- **E-posta**: HTML ve metin formatında e-posta bildirimleri
- **SMS**: Kısa mesaj bildirimleri 
- **Push**: Mobil ve web push bildirimleri
- **Uygulama İçi**: Gerçek zamanlı uygulama bildirimleri
- **Webhook**: Üçüncü parti sistem entegrasyonları

### 📋 Şablon Sistemi
- **Liquid Template Engine**: DotLiquid kullanarak dinamik şablonlar
- **Çoklu Dil Desteği**: Farklı dillerde şablon yönetimi
- **Şablon Önizleme**: Şablonları test etme özelliği
- **Varsayılan Şablonlar**: Hoş geldin, e-posta doğrulama, şifre sıfırlama şablonları

### ⚙️ Kullanıcı Tercihleri
- **Kanal Bazlı Tercihler**: Kullanıcılar hangi kanallardan bildirim almak istediklerini seçebilir
- **Bildirim Türü Tercihleri**: Her bildirim türü için ayrı kanal ayarları
- **Sessiz Saatler**: Belirli saatlerde bildirim almama
- **Rahatsız Etme Modu**: Kritik bildirimler dışında tüm bildirimleri durdurma

### 🔧 Gelişmiş Özellikler
- **Toplu Bildirim**: Birden fazla kullanıcıya aynı anda bildirim
- **Zamanlanmış Bildirimler**: Gelecek tarihte gönderilecek bildirimler
- **Öncelik Sistemi**: Kritik, yüksek, normal, düşük öncelik seviyeleri
- **Geçmiş ve İstatistikler**: Bildirim geçmişi ve detaylı istatistikler

## Kullanılan Teknolojiler

### 🏗️ .NET Ekosistemi
- **.NET 8.0**: Modern C# özellikleri ve performans
- **Microsoft.Extensions.DependencyInjection**: Bağımlılık enjeksiyonu
- **Microsoft.Extensions.Logging**: Yapılandırılabilir loglama
- **Microsoft.Extensions.Options**: Yapılandırma yönetimi
- **Microsoft.Extensions.Hosting**: Background servisler

### 📧 Bildirim Teknolojileri
- **DotLiquid**: Şablon motoru (v2.2.656)
- **FluentEmail**: E-posta gönderim kütüphanesi (v3.0.2)
- **SignalR**: Gerçek zamanlı bildirimler (v1.1.0)

### 🧪 Test ve Kalite
- **xUnit**: Unit testing framework
- **FluentAssertions**: Test assertion kütüphanesi
- **Microsoft.Extensions.Configuration**: Yapılandırma testleri

## Kurulum ve Kullanım

### 1. Proje Referansı
```xml
<ProjectReference Include="../Enterprise.Shared.Notifications/Enterprise.Shared.Notifications.csproj" />
```

### 2. Servis Kaydı
```csharp
// Development/Test ortamı için
services.AddInMemoryNotifications(configuration);

// Production ortamı için
services.AddProductionNotifications(configuration);

// Manuel yapılandırma
services.AddNotifications(configuration, options =>
{
    options.UseInMemoryProviders = false;
    options.EnableBackgroundServices = true;
    options.EnableSignalR = true;
});
```

### 3. Yapılandırma (appsettings.json)
```json
{
  "NotificationSettings": {
    "General": {
      "Enabled": true,
      "DefaultLanguage": "tr-TR",
      "DefaultTimezone": "Europe/Istanbul"
    },
    "Email": {
      "Enabled": true,
      "SmtpHost": "smtp.example.com",
      "SmtpPort": 587,
      "FromAddress": "noreply@example.com"
    },
    "Sms": {
      "Enabled": true,
      "Provider": "Twilio"
    },
    "Push": {
      "Enabled": true,
      "Provider": "Firebase"
    },
    "Delivery": {
      "BatchSize": 100,
      "RetryAttempts": 3,
      "RetryDelay": "00:05:00"
    }
  }
}
```

### 4. Temel Kullanım
```csharp
public class UserController : ControllerBase
{
    private readonly INotificationService _notificationService;
    
    public UserController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Kullanıcı kaydı işlemi...
        
        // Hoş geldin bildirimi gönder
        await _notificationService.SendAsync(new NotificationRequest
        {
            NotificationId = Guid.NewGuid(),
            UserId = newUser.Id,
            Type = NotificationType.Welcome,
            Channels = new[] { NotificationChannel.Email, NotificationChannel.Push },
            TemplateKey = "welcome",
            Data = new Dictionary<string, object>
            {
                ["company_name"] = "Şirketim",
                ["user.first_name"] = newUser.FirstName
            }
        });
        
        return Ok();
    }
}
```

## Şablon Yönetimi

### Şablon Oluşturma
```csharp
var template = new NotificationTemplate
{
    Key = "order_confirmation",
    Language = "tr-TR",
    Name = "Sipariş Onayı",
    SubjectTemplate = "Sipariş #{{ order.number }} Onaylandı",
    HtmlTemplate = "<h1>Siparişiniz onaylandı!</h1><p>{{ order.total | currency }} tutarındaki siparişiniz başarıyla alındı.</p>",
    TextTemplate = "Siparişiniz #{{ order.number }} onaylandı. Tutar: {{ order.total | currency }}",
    RequiredFields = new List<string> { "order.number", "order.total" }
};

await templateService.CreateOrUpdateTemplateAsync(template);
```

### Şablon Kullanma
```csharp
var renderedTemplate = await templateService.RenderAsync("order_confirmation", new Dictionary<string, object>
{
    ["order"] = new { number = "12345", total = 299.99 }
});
```

## Kullanıcı Tercihleri

### Tercih Yönetimi
```csharp
// Kullanıcının tercihlerini al
var preferences = await preferencesService.GetUserPreferencesAsync(userId);

// E-posta bildirimlerini kapat
await preferencesService.OptOutAsync(userId, NotificationType.Marketing, NotificationChannel.Email);

// Sessiz saatler ayarla (22:00 - 08:00)
await preferencesService.SetQuietHoursAsync(userId, new TimeSpan(22, 0, 0), new TimeSpan(8, 0, 0));

// Rahatsız etme modunu aç
await preferencesService.SetDoNotDisturbAsync(userId, true);
```

## Gelişmiş Özellikler

### Toplu Bildirim
```csharp
await notificationService.SendBulkAsync(new BulkNotificationRequest
{
    UserIds = userIds.ToArray(),
    Type = NotificationType.SystemMaintenance,
    Channels = new[] { NotificationChannel.Email, NotificationChannel.InApp },
    TemplateKey = "maintenance_notice",
    Data = new Dictionary<string, object>
    {
        ["maintenance_date"] = DateTime.Now.AddDays(7),
        ["duration"] = "2 saat"
    },
    BatchSize = 50
});
```

### Zamanlanmış Bildirim
```csharp
await notificationService.ScheduleAsync(new ScheduledNotificationRequest
{
    NotificationId = Guid.NewGuid(),
    UserId = userId,
    Type = NotificationType.Reminder,
    Channels = new[] { NotificationChannel.Push },
    ScheduledAt = DateTime.Now.AddHours(24),
    TemplateKey = "appointment_reminder",
    Data = appointmentData
});
```

## Mimari Yapı

### Katmanlar
1. **Interfaces**: Servis ve provider sözleşmeleri
2. **Models**: Veri modelleri ve yapılandırma sınıfları
3. **Services**: Ana iş mantığı servisleri
4. **Providers**: Kanal-spesifik bildirim sağlayıcıları
5. **Extensions**: DI container yapılandırması

### Temel Servisler
- **NotificationService**: Ana bildirim orkestratörü
- **TemplateService**: Şablon yönetimi ve render
- **NotificationPreferencesService**: Kullanıcı tercihleri
- **Provider'lar**: E-posta, SMS, Push, InApp, Webhook sağlayıcıları

### Veri Akışı
1. Bildirim isteği alınır (NotificationRequest)
2. Kullanıcı tercihleri kontrol edilir
3. Uygun kanallar filtrelenir
4. Şablon render edilir (eğer varsa)
5. Her kanal için bildirim gönderilir
6. Sonuçlar loglanır

## Test Edilmiş Durumu

Proje **196/196 test** ile %100 başarı oranında test edilmiştir. Test kapsamı:

- ✅ Bildirim gönderimi tüm kanallar
- ✅ Şablon yönetimi ve rendering
- ✅ Kullanıcı tercihleri yönetimi
- ✅ Toplu bildirimler
- ✅ Zamanlanmış bildirimler
- ✅ Hata senaryoları
- ✅ Performans testleri

## Production Hazırlığı

Proje production ortamında kullanıma hazırdır:

- ✅ Exception handling
- ✅ Logging yapılandırması
- ✅ Yapılandırma yönetimi
- ✅ Background services
- ✅ Health checks
- ✅ Performance optimizasyonu
- ✅ Security best practices

## Lisans

Bu proje Enterprise Platform bünyesinde geliştirilmiş olup, kurumsal kullanım için tasarlanmıştır.

## Katkıda Bulunma

Proje geliştirme sürecine katkıda bulunmak için:
1. Feature branch oluşturun
2. Değişikliklerinizi yapın
3. Unit testler ekleyin
4. Pull request açın

## İletişim

Enterprise Platform Geliştirme Ekibi
- Versiyon: 1.0.0
- .NET: 8.0
- Son Güncelleme: 2025


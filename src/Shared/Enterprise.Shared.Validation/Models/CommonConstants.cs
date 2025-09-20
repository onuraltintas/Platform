namespace Enterprise.Shared.Validation.Models;

/// <summary>
/// Common constants used throughout the validation system
/// </summary>
public static class CommonConstants
{
    /// <summary>
    /// Date format constants for Turkish locale
    /// </summary>
    public static class DateFormats
    {
        public const string IsoDateTime = "yyyy-MM-ddTHH:mm:ss.fffZ";
        public const string IsoDate = "yyyy-MM-dd";
        public const string TurkishDisplayDateTime = "dd/MM/yyyy HH:mm";
        public const string TurkishDisplayDate = "dd/MM/yyyy";
        public const string TimeOnly = "HH:mm";
        public const string TurkishLongDate = "dd MMMM yyyy";
        public const string TurkishShortDate = "dd.MM.yyyy";
    }

    /// <summary>
    /// Regex patterns for Turkish validation
    /// </summary>
    public static class RegexPatterns
    {
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const string Phone = @"^\+?[1-9]\d{1,14}$";
        public const string TurkishPhone = @"^(\+90|0)?5\d{9}$";
        public const string TurkishTCNo = @"^\d{11}$";
        public const string TurkishVKNo = @"^\d{10}$";
        public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]";
        public const string AlphaNumeric = @"^[a-zA-Z0-9]+$";
        public const string Slug = @"^[a-z0-9-]+$";
        public const string TurkishText = @"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$";
        public const string PostalCode = @"^\d{5}$";
        public const string IbanTurkey = @"^TR\d{2}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{2}$";
    }

    /// <summary>
    /// HTTP status messages in Turkish
    /// </summary>
    public static class HttpStatusMessages
    {
        public const string BadRequest = "Geçersiz İstek";
        public const string Unauthorized = "Yetkisiz Erişim";
        public const string Forbidden = "Erişim Yasak";
        public const string NotFound = "Bulunamadı";
        public const string Conflict = "Çakışma";
        public const string InternalServerError = "Sunucu Hatası";
        public const string ValidationFailed = "Doğrulama Başarısız";
    }

    /// <summary>
    /// Cache key prefixes
    /// </summary>
    public static class CacheKeys
    {
        public const string UserPrefix = "kullanici:";
        public const string SessionPrefix = "oturum:";
        public const string ConfigPrefix = "yapilandirma:";
        public const string TempPrefix = "gecici:";
        public const string ValidationPrefix = "dogrulama:";
    }

    /// <summary>
    /// Claims for Turkish context
    /// </summary>
    public static class Claims
    {
        public const string UserId = "kullanici_id";
        public const string Email = "eposta";
        public const string Role = "rol";
        public const string Permission = "yetki";
        public const string TenantId = "tenant_id";
        public const string FullName = "tam_ad";
    }

    /// <summary>
    /// Turkish validation error messages
    /// </summary>
    public static class ValidationMessages
    {
        public const string Required = "{PropertyName} alanı zorunludur.";
        public const string EmailInvalid = "Geçerli bir e-posta adresi giriniz.";
        public const string PhoneInvalid = "Geçerli bir telefon numarası giriniz.";
        public const string TCNoInvalid = "Geçerli bir T.C. kimlik numarası giriniz.";
        public const string VKNoInvalid = "Geçerli bir vergi kimlik numarası giriniz.";
        public const string PasswordWeak = "Şifre en az 8 karakter, büyük harf, küçük harf, rakam ve özel karakter içermelidir.";
        public const string MinLength = "{PropertyName} en az {MinLength} karakter olmalıdır.";
        public const string MaxLength = "{PropertyName} en fazla {MaxLength} karakter olabilir.";
        public const string MinValue = "{PropertyName} en az {MinValue} olmalıdır.";
        public const string MaxValue = "{PropertyName} en fazla {MaxValue} olabilir.";
        public const string DateRange = "Tarih {From} ile {To} arasında olmalıdır.";
        public const string FileSize = "Dosya boyutu en fazla {MaxSize} MB olabilir.";
        public const string FileType = "Desteklenen dosya türleri: {AllowedTypes}";
        public const string Duplicate = "{PropertyName} zaten kullanımda.";
        public const string NotFound = "{PropertyName} bulunamadı.";
    }

    /// <summary>
    /// File validation constants
    /// </summary>
    public static class FileValidation
    {
        public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };
        public static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".xls", ".xlsx" };
        public static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" };
        public static readonly string[] AudioExtensions = { ".mp3", ".wav", ".flac", ".aac", ".ogg" };
        
        public const int MaxImageSizeMB = 5;
        public const int MaxDocumentSizeMB = 10;
        public const int MaxVideoSizeMB = 100;
        public const int MaxAudioSizeMB = 20;
    }

    /// <summary>
    /// Turkish cities for validation
    /// </summary>
    public static class TurkishCities
    {
        public static readonly Dictionary<int, string> Cities = new()
        {
            { 1, "Adana" }, { 2, "Adıyaman" }, { 3, "Afyonkarahisar" }, { 4, "Ağrı" }, { 5, "Amasya" },
            { 6, "Ankara" }, { 7, "Antalya" }, { 8, "Artvin" }, { 9, "Aydın" }, { 10, "Balıkesir" },
            { 11, "Bilecik" }, { 12, "Bingöl" }, { 13, "Bitlis" }, { 14, "Bolu" }, { 15, "Burdur" },
            { 16, "Bursa" }, { 17, "Çanakkale" }, { 18, "Çankırı" }, { 19, "Çorum" }, { 20, "Denizli" },
            { 21, "Diyarbakır" }, { 22, "Edirne" }, { 23, "Elazığ" }, { 24, "Erzincan" }, { 25, "Erzurum" },
            { 26, "Eskişehir" }, { 27, "Gaziantep" }, { 28, "Giresun" }, { 29, "Gümüşhane" }, { 30, "Hakkâri" },
            { 31, "Hatay" }, { 32, "Isparta" }, { 33, "Mersin" }, { 34, "İstanbul" }, { 35, "İzmir" },
            { 36, "Kars" }, { 37, "Kastamonu" }, { 38, "Kayseri" }, { 39, "Kırklareli" }, { 40, "Kırşehir" },
            { 41, "Kocaeli" }, { 42, "Konya" }, { 43, "Kütahya" }, { 44, "Malatya" }, { 45, "Manisa" },
            { 46, "Kahramanmaraş" }, { 47, "Mardin" }, { 48, "Muğla" }, { 49, "Muş" }, { 50, "Nevşehir" },
            { 51, "Niğde" }, { 52, "Ordu" }, { 53, "Rize" }, { 54, "Sakarya" }, { 55, "Samsun" },
            { 56, "Siirt" }, { 57, "Sinop" }, { 58, "Sivas" }, { 59, "Tekirdağ" }, { 60, "Tokat" },
            { 61, "Trabzon" }, { 62, "Tunceli" }, { 63, "Şanlıurfa" }, { 64, "Uşak" }, { 65, "Van" },
            { 66, "Yozgat" }, { 67, "Zonguldak" }, { 68, "Aksaray" }, { 69, "Bayburt" }, { 70, "Karaman" },
            { 71, "Kırıkkale" }, { 72, "Batman" }, { 73, "Şırnak" }, { 74, "Bartın" }, { 75, "Ardahan" },
            { 76, "Iğdır" }, { 77, "Yalova" }, { 78, "Karabük" }, { 79, "Kilis" }, { 80, "Osmaniye" },
            { 81, "Düzce" }
        };
    }
}
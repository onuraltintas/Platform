using FluentValidation;
using Enterprise.Shared.Validation.Extensions;
using Enterprise.Shared.Validation.Models;

namespace Enterprise.Shared.Validation.Validators;

/// <summary>
/// Common model validators with Turkish localization
/// </summary>

/// <summary>
/// User registration model
/// </summary>
public class UserRegistrationModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordConfirmation { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? TCNumber { get; set; }
    public bool AcceptTerms { get; set; }
}

/// <summary>
/// User registration validator
/// </summary>
public class UserRegistrationValidator : BaseValidator<UserRegistrationModel>
{
    public UserRegistrationValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .Must(BeValidEmailAddress).WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .Must(BeStrongPassword).WithMessage("Şifre en az bir büyük harf, küçük harf, rakam ve özel karakter içermelidir.");

        RuleFor(x => x.PasswordConfirmation)
            .NotEmpty().WithMessage("Şifre tekrarı zorunludur.")
            .Equal(x => x.Password).WithMessage("Şifre tekrarı şifre ile eşleşmiyor.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı zorunludur.")
            .Length(2, 50).WithMessage("Ad 2-50 karakter arasında olmalıdır.")
            .Must(BeTurkishText).WithMessage("Ad sadece harf içerebilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı zorunludur.")
            .Length(2, 50).WithMessage("Soyad 2-50 karakter arasında olmalıdır.")
            .Must(BeTurkishText).WithMessage("Soyad sadece harf içerebilir.");

        RuleFor(x => x.PhoneNumber)
            .Must(BeValidTurkishPhone).When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Geçerli bir Türk telefon numarası giriniz.");

        RuleFor(x => x.BirthDate)
            .Must(date => BeValidAge(date, 18)).When(x => x.BirthDate.HasValue)
            .WithMessage("18 yaşından büyük olmalısınız.");

        RuleFor(x => x.TCNumber)
            .Must(BeValidTCNumber).When(x => !string.IsNullOrEmpty(x.TCNumber))
            .WithMessage("Geçerli bir T.C. kimlik numarası giriniz.");

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Kullanım şartlarını kabul etmelisiniz.");
    }
}

/// <summary>
/// Company information model
/// </summary>
public class CompanyModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string? TaxOffice { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? Website { get; set; }
}

/// <summary>
/// Company information validator
/// </summary>
public class CompanyValidator : BaseValidator<CompanyModel>
{
    public CompanyValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Şirket adı zorunludur.")
            .Length(2, 200).WithMessage("Şirket adı 2-200 karakter arasında olmalıdır.");

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi numarası zorunludur.")
            .Must(BeValidTurkishTaxNumber).WithMessage("Geçerli bir vergi numarası giriniz.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .Must(BeValidEmailAddress).WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası zorunludur.")
            .Must(BeValidTurkishPhone).WithMessage("Geçerli bir Türk telefon numarası giriniz.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres zorunludur.")
            .Length(10, 500).WithMessage("Adres 10-500 karakter arasında olmalıdır.");

        RuleFor(x => x.CityId)
            .GreaterThan(0).WithMessage("Şehir seçimi zorunludur.")
            .LessThanOrEqualTo(81).WithMessage("Geçerli bir şehir seçiniz.");

        RuleFor(x => x.PostalCode)
            .Matches(@"^\d{5}$").When(x => !string.IsNullOrEmpty(x.PostalCode))
            .WithMessage("Posta kodu 5 haneli olmalıdır.");

        RuleFor(x => x.Website)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Geçerli bir web site adresi giriniz.");
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result) && 
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Product model
/// </summary>
public class ProductModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public int CategoryId { get; set; }
    public string? SKU { get; set; }
    public int StockQuantity { get; set; }
    public decimal? Weight { get; set; }
    public string[]? Tags { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Product validator
/// </summary>
public class ProductValidator : BaseValidator<ProductModel>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .Length(2, 200).WithMessage("Ürün adı 2-200 karakter arasında olmalıdır.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Ürün açıklaması zorunludur.")
            .Length(10, 2000).WithMessage("Ürün açıklaması 10-2000 karakter arasında olmalıdır.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(1000000).WithMessage("Ürün fiyatı çok yüksek.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Para birimi zorunludur.")
            .Must(BeValidCurrency).WithMessage("Geçerli bir para birimi giriniz.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Kategori seçimi zorunludur.");

        RuleFor(x => x.SKU)
            .Length(3, 50).When(x => !string.IsNullOrEmpty(x.SKU))
            .WithMessage("SKU kodu 3-50 karakter arasında olmalıdır.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stok miktarı negatif olamaz.");

        RuleFor(x => x.Weight)
            .GreaterThan(0).When(x => x.Weight.HasValue)
            .WithMessage("Ağırlık 0'dan büyük olmalıdır.");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 10)
            .WithMessage("En fazla 10 etiket ekleyebilirsiniz.");
    }

    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "TRY", "USD", "EUR", "GBP" };
        return validCurrencies.Contains(currency.ToUpperInvariant());
    }
}

/// <summary>
/// File upload model
/// </summary>
public class FileUploadModel
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public byte[]? FileContent { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// File upload validator
/// </summary>
public class FileUploadValidator : BaseValidator<FileUploadModel>
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
    private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".xls", ".xlsx" };
    
    public FileUploadValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Dosya adı zorunludur.")
            .Must(BeValidFileName).WithMessage("Geçersiz dosya adı.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("Dosya boş olamaz.")
            .Must((model, size) => BeValidFileSizeForCategory(size, model.Category))
            .WithMessage("Dosya boyutu kategori için çok büyük.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Dosya tipi zorunludur.");

        RuleFor(x => x.FileContent)
            .NotNull().WithMessage("Dosya içeriği zorunludur.")
            .Must(content => content != null && content.Length > 0)
            .WithMessage("Dosya içeriği boş olamaz.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Dosya kategorisi zorunludur.")
            .Must(BeValidCategory).WithMessage("Geçersiz dosya kategorisi.");

        RuleFor(x => x.FileName)
            .Must((model, fileName) => HaveValidExtensionForCategory(fileName, model.Category))
            .WithMessage("Dosya uzantısı kategori için uygun değil.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
    }

    private static bool BeValidFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        
        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c));
    }

    private static bool BeValidFileSizeForCategory(long sizeBytes, string category)
    {
        var sizeMB = sizeBytes / (1024 * 1024);
        
        return category.ToLowerInvariant() switch
        {
            "image" => sizeMB <= CommonConstants.FileValidation.MaxImageSizeMB,
            "document" => sizeMB <= CommonConstants.FileValidation.MaxDocumentSizeMB,
            "video" => sizeMB <= CommonConstants.FileValidation.MaxVideoSizeMB,
            "audio" => sizeMB <= CommonConstants.FileValidation.MaxAudioSizeMB,
            _ => sizeMB <= 10 // Default 10MB
        };
    }

    private static bool BeValidCategory(string category)
    {
        var validCategories = new[] { "image", "document", "video", "audio", "other" };
        return validCategories.Contains(category.ToLowerInvariant());
    }

    private static bool HaveValidExtensionForCategory(string fileName, string category)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return category.ToLowerInvariant() switch
        {
            "image" => CommonConstants.FileValidation.ImageExtensions.Contains(extension),
            "document" => CommonConstants.FileValidation.DocumentExtensions.Contains(extension),
            "video" => CommonConstants.FileValidation.VideoExtensions.Contains(extension),
            "audio" => CommonConstants.FileValidation.AudioExtensions.Contains(extension),
            _ => true // Other category allows any extension
        };
    }
}

/// <summary>
/// Paged request validator
/// </summary>
public class PagedRequestValidator : BaseValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Sayfa numarası 1'den büyük olmalıdır.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır.");

        RuleFor(x => x.Search)
            .MaximumLength(500).WithMessage("Arama metni en fazla 500 karakter olabilir.");

        RuleFor(x => x.SortBy)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.SortBy))
            .WithMessage("Sıralama alanı en fazla 100 karakter olabilir.");
    }
}
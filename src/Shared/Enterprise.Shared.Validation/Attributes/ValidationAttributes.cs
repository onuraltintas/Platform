using System.ComponentModel.DataAnnotations;
using Enterprise.Shared.Validation.Extensions;

namespace Enterprise.Shared.Validation.Attributes;

/// <summary>
/// Turkish phone number validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class TurkishPhoneAttribute : ValidationAttribute
{
    public TurkishPhoneAttribute() : base("Geçerli bir Türk telefon numarası giriniz.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not string phone) return false;
        
        return phone.IsValidTurkishPhone();
    }
}

/// <summary>
/// Turkish TC identity number validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class TCNumberAttribute : ValidationAttribute
{
    public TCNumberAttribute() : base("Geçerli bir T.C. kimlik numarası giriniz.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not string tcNo) return false;
        
        return tcNo.IsValidTCNumber();
    }
}

/// <summary>
/// Turkish tax number validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class TurkishTaxNumberAttribute : ValidationAttribute
{
    public TurkishTaxNumberAttribute() : base("Geçerli bir vergi kimlik numarası giriniz.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not string vkn) return false;
        
        return vkn.IsValidTurkishTaxNumber();
    }
}

/// <summary>
/// Turkish IBAN validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class TurkishIbanAttribute : ValidationAttribute
{
    public TurkishIbanAttribute() : base("Geçerli bir Türkiye IBAN numarası giriniz.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not string iban) return false;
        
        return iban.IsValidTurkishIban();
    }
}

/// <summary>
/// Strong password validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class StrongPasswordAttribute : ValidationAttribute
{
    public int MinimumLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;

    public StrongPasswordAttribute() : base("Şifre en az {0} karakter uzunluğunda olmalı ve büyük harf, küçük harf, rakam ve özel karakter içermelidir.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not string password) return false;
        
        if (password.Length < MinimumLength) return false;
        
        if (RequireUppercase && !password.Any(char.IsUpper)) return false;
        if (RequireLowercase && !password.Any(char.IsLower)) return false;
        if (RequireDigit && !password.Any(char.IsDigit)) return false;
        if (RequireSpecialCharacter && !password.Any(c => !char.IsLetterOrDigit(c))) return false;
        
        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString!, MinimumLength);
    }
}

/// <summary>
/// Turkish text validation attribute (allows Turkish characters)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class TurkishTextAttribute : ValidationAttribute
{
    public bool AllowNumbers { get; set; } = false;
    public bool AllowSpaces { get; set; } = true;

    public TurkishTextAttribute() : base("Sadece Türkçe karakterler kullanabilirsiniz.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not string text) return false;
        
        return text.All(c => 
            char.IsLetter(c) || 
            "çÇğĞıİöÖşŞüÜ".Contains(c) ||
            (AllowSpaces && char.IsWhiteSpace(c)) ||
            (AllowNumbers && char.IsDigit(c)));
    }
}

/// <summary>
/// Minimum age validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MinimumAgeAttribute : ValidationAttribute
{
    public int MinimumAge { get; }

    public MinimumAgeAttribute(int minimumAge) : base("En az {0} yaşında olmalısınız.")
    {
        MinimumAge = minimumAge;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not DateTime birthDate) return false;
        
        return birthDate.Age() >= MinimumAge;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString!, MinimumAge);
    }
}

/// <summary>
/// Maximum age validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MaximumAgeAttribute : ValidationAttribute
{
    public int MaximumAge { get; }

    public MaximumAgeAttribute(int maximumAge) : base("En fazla {0} yaşında olabilirsiniz.")
    {
        MaximumAge = maximumAge;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not DateTime birthDate) return false;
        
        return birthDate.Age() <= MaximumAge;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString!, MaximumAge);
    }
}

/// <summary>
/// File size validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MaxFileSizeAttribute : ValidationAttribute
{
    public int MaxSizeMB { get; }

    public MaxFileSizeAttribute(int maxSizeMB) : base("Dosya boyutu en fazla {0} MB olabilir.")
    {
        MaxSizeMB = maxSizeMB;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        long sizeBytes = value switch
        {
            byte[] bytes => bytes.Length,
            long size => size,
            int size => size,
            _ => 0
        };
        
        var sizeMB = sizeBytes / (1024.0 * 1024.0);
        return sizeMB <= MaxSizeMB;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString!, MaxSizeMB);
    }
}

/// <summary>
/// Allowed file extensions validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class AllowedExtensionsAttribute : ValidationAttribute
{
    public string[] Extensions { get; }

    public AllowedExtensionsAttribute(params string[] extensions) : base("Desteklenen dosya türleri: {0}")
    {
        Extensions = extensions.Select(ext => ext.ToLowerInvariant()).ToArray();
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not string fileName) return false;
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return Extensions.Contains(extension);
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString!, string.Join(", ", Extensions));
    }
}

/// <summary>
/// Business hours validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class BusinessHoursAttribute : ValidationAttribute
{
    public TimeSpan StartTime { get; set; } = new(9, 0, 0); // 09:00
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);  // 17:00
    public bool ExcludeWeekends { get; set; } = true;

    public BusinessHoursAttribute() : base("İşlem sadece mesai saatleri içinde yapılabilir.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        DateTime dateTime = value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.DateTime,
            _ => DateTime.MinValue
        };
        
        if (dateTime == DateTime.MinValue) return false;
        
        // Convert to Turkey time
        var turkeyTime = dateTime.ToTurkeyTime();
        
        // Check weekend
        if (ExcludeWeekends && turkeyTime.IsWeekend()) return false;
        
        // Check business hours
        var currentTime = turkeyTime.TimeOfDay;
        return currentTime >= StartTime && currentTime <= EndTime;
    }
}

/// <summary>
/// Future date validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class FutureDateAttribute : ValidationAttribute
{
    public bool AllowToday { get; set; } = false;

    public FutureDateAttribute() : base("Tarih gelecekte olmalıdır.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not DateTime dateTime) return false;
        
        var now = DateTimeExtensions.GetTurkeyNow();
        
        return AllowToday ? dateTime.Date >= now.Date : dateTime.Date > now.Date;
    }
}

/// <summary>
/// Past date validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class PastDateAttribute : ValidationAttribute
{
    public bool AllowToday { get; set; } = false;

    public PastDateAttribute() : base("Tarih geçmişte olmalıdır.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is not DateTime dateTime) return false;
        
        var now = DateTimeExtensions.GetTurkeyNow();
        
        return AllowToday ? dateTime.Date <= now.Date : dateTime.Date < now.Date;
    }
}

/// <summary>
/// Turkish city validation attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class TurkishCityAttribute : ValidationAttribute
{
    public TurkishCityAttribute() : base("Geçerli bir Türkiye şehri seçiniz.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Optional field
        
        if (value is int cityId)
        {
            return Models.CommonConstants.TurkishCities.Cities.ContainsKey(cityId);
        }
        
        if (value is string cityName)
        {
            return Models.CommonConstants.TurkishCities.Cities.Values
                .Any(city => string.Equals(city, cityName, StringComparison.OrdinalIgnoreCase));
        }
        
        return false;
    }
}
namespace Enterprise.Shared.Storage.Models;

/// <summary>
/// Dosya validasyon sonucu
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Validasyon başarılı mı
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Hata mesajları
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Uyarı mesajları
    /// </summary>
    public List<string> WarningMessages { get; set; } = new();

    /// <summary>
    /// Ek bilgiler
    /// </summary>
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();

    /// <summary>
    /// Başarılı validasyon sonucu oluşturur
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Başarısız validasyon sonucu oluşturur
    /// </summary>
    public static ValidationResult Failure(params string[] errorMessages)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessages = errorMessages.ToList()
        };
    }

    /// <summary>
    /// Başarısız validasyon sonucu oluşturur
    /// </summary>
    public static ValidationResult Failure(List<string> errorMessages)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessages = errorMessages
        };
    }

    /// <summary>
    /// Uyarı ile birlikte başarılı validasyon sonucu oluşturur
    /// </summary>
    public static ValidationResult SuccessWithWarnings(params string[] warningMessages)
    {
        return new ValidationResult
        {
            IsValid = true,
            WarningMessages = warningMessages.ToList()
        };
    }

    /// <summary>
    /// Hata mesajı ekler
    /// </summary>
    public void AddError(string message)
    {
        ErrorMessages.Add(message);
        IsValid = false;
    }

    /// <summary>
    /// Uyarı mesajı ekler
    /// </summary>
    public void AddWarning(string message)
    {
        WarningMessages.Add(message);
    }

    /// <summary>
    /// Ek bilgi ekler
    /// </summary>
    public void AddInfo(string key, object value)
    {
        AdditionalInfo[key] = value;
    }

    /// <summary>
    /// Sonucun string temsilini döndürür
    /// </summary>
    public override string ToString()
    {
        if (IsValid)
        {
            return WarningMessages.Any() 
                ? $"Başarılı (Uyarılar: {string.Join(", ", WarningMessages)})"
                : "Başarılı";
        }

        return $"Başarısız (Hatalar: {string.Join(", ", ErrorMessages)})";
    }
}
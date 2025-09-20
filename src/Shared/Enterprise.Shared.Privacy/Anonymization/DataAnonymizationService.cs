using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Enterprise.Shared.Privacy.Interfaces;
using Enterprise.Shared.Privacy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Privacy.Anonymization;

public class DataAnonymizationService : IDataAnonymizationService
{
    private readonly PrivacySettings _settings;
    private readonly ILogger<DataAnonymizationService> _logger;
    private readonly Dictionary<DataCategory, string> _maskingPatterns;

    public DataAnonymizationService(
        IOptions<PrivacySettings> settings,
        ILogger<DataAnonymizationService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _maskingPatterns = InitializeMaskingPatterns();
    }

    public async Task<string> AnonymizeAsync(string data, AnonymizationLevel level, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        return level switch
        {
            AnonymizationLevel.None => data,
            AnonymizationLevel.Masked => await MaskDataAsync(data, DataCategory.Personal, cancellationToken),
            AnonymizationLevel.Hashed => await HashDataAsync(data, cancellationToken),
            AnonymizationLevel.Encrypted => await EncryptDataAsync(data, cancellationToken),
            AnonymizationLevel.Pseudonymized => await PseudonymizeAsync(data, "default", cancellationToken),
            AnonymizationLevel.Anonymized => GenerateAnonymizedValue(data),
            AnonymizationLevel.Deleted => string.Empty,
            _ => data
        };
    }

    public async Task<string> HashDataAsync(string data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        try
        {
            var salt = Encoding.UTF8.GetBytes(_settings.Anonymization.HashingSalt);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            
            using var pbkdf2 = new Rfc2898DeriveBytes(dataBytes, salt, _settings.Anonymization.HashingIterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            
            var result = Convert.ToBase64String(hash);
            
            _logger.LogDebug("Data hashed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash data");
            throw;
        }
    }

    public async Task<string> EncryptDataAsync(string data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        try
        {
            var key = Convert.FromBase64String(_settings.Anonymization.EncryptionKey);
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Prepend IV to encrypted data
            await msEncrypt.WriteAsync(aes.IV, cancellationToken);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                await swEncrypt.WriteAsync(data);
            }

            var result = Convert.ToBase64String(msEncrypt.ToArray());
            
            _logger.LogDebug("Data encrypted successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw;
        }
    }

    public async Task<string> DecryptDataAsync(string encryptedData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return encryptedData;

        try
        {
            var key = Convert.FromBase64String(_settings.Anonymization.EncryptionKey);
            var cipherBytes = Convert.FromBase64String(encryptedData);
            
            using var aes = Aes.Create();
            aes.Key = key;
            
            // Extract IV from the beginning
            var iv = new byte[16];
            Array.Copy(cipherBytes, 0, iv, 0, 16);
            aes.IV = iv;
            
            var cipherData = new byte[cipherBytes.Length - 16];
            Array.Copy(cipherBytes, 16, cipherData, 0, cipherData.Length);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            var result = await srDecrypt.ReadToEndAsync();
            
            _logger.LogDebug("Data decrypted successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw;
        }
    }

    public async Task<string> MaskDataAsync(string data, DataCategory category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        try
        {
            var pattern = _maskingPatterns.GetValueOrDefault(category, "*");
            
            return category switch
            {
                DataCategory.Personal when IsEmail(data) => MaskEmail(data),
                DataCategory.Personal when IsPhoneNumber(data) => MaskPhoneNumber(data),
                DataCategory.Financial => MaskCreditCard(data),
                DataCategory.Personal => MaskGeneral(data, pattern),
                _ => MaskGeneral(data, pattern)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mask data for category {Category}", category);
            throw;
        }
    }

    public async Task<string> PseudonymizeAsync(string data, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        try
        {
            var combined = $"{userId}:{data}:{_settings.Anonymization.HashingSalt}";
            var hash = await HashDataAsync(combined, cancellationToken);
            
            // Create a consistent pseudonym based on hash
            var pseudonym = $"USER_{hash[..8].ToUpper()}";
            
            _logger.LogDebug("Data pseudonymized for user {UserId}", userId);
            return pseudonym;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pseudonymize data for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PersonalDataRecord> AnonymizePersonalDataRecordAsync(PersonalDataRecord record, 
        AnonymizationLevel level, CancellationToken cancellationToken = default)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        try
        {
            var anonymizedRecord = new PersonalDataRecord
            {
                Id = record.Id,
                UserId = record.UserId,
                Category = record.Category,
                DataType = record.DataType,
                OriginalValue = record.OriginalValue,
                ProcessedValue = await AnonymizeAsync(record.OriginalValue, level, cancellationToken),
                AnonymizationLevel = level,
                CreatedAt = record.CreatedAt,
                LastAccessedAt = DateTime.UtcNow,
                RetentionExpiresAt = record.RetentionExpiresAt,
                Metadata = new Dictionary<string, string>(record.Metadata)
                {
                    ["AnonymizedAt"] = DateTime.UtcNow.ToString("O"),
                    ["AnonymizationLevel"] = level.ToString()
                },
                Source = record.Source,
                ProcessingStatus = record.ProcessingStatus
            };

            _logger.LogInformation("Personal data record {RecordId} anonymized with level {Level}", 
                record.Id, level);

            return anonymizedRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to anonymize personal data record {RecordId}", record.Id);
            throw;
        }
    }

    public async Task<PersonalDataRecord[]> BulkAnonymizeAsync(PersonalDataRecord[] records, 
        AnonymizationLevel level, CancellationToken cancellationToken = default)
    {
        if (records == null || records.Length == 0)
            return Array.Empty<PersonalDataRecord>();

        try
        {
            var tasks = records.Select(record => 
                AnonymizePersonalDataRecordAsync(record, level, cancellationToken));
            
            var results = await Task.WhenAll(tasks);
            
            _logger.LogInformation("Bulk anonymized {Count} records with level {Level}", 
                records.Length, level);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk anonymize {Count} records", records.Length);
            throw;
        }
    }

    public bool IsDataAnonymized(string data)
    {
        if (string.IsNullOrEmpty(data))
            return false;

        // Check for common anonymization patterns
        return data.StartsWith("USER_") || 
               data.Contains("*") || 
               data.StartsWith("ANON_") ||
               IsBase64Hash(data);
    }

    public bool CanReverseAnonymization(AnonymizationLevel level)
    {
        return level switch
        {
            AnonymizationLevel.None => true,
            AnonymizationLevel.Encrypted => true,
            AnonymizationLevel.Masked => false,
            AnonymizationLevel.Hashed => false,
            AnonymizationLevel.Pseudonymized => false,
            AnonymizationLevel.Anonymized => false,
            AnonymizationLevel.Deleted => false,
            _ => false
        };
    }

    public async Task<Dictionary<string, object>> GetAnonymizationStatisticsAsync(CancellationToken cancellationToken = default)
    {
        // This would typically query a database for real statistics
        // For now, returning mock data structure
        return new Dictionary<string, object>
        {
            ["TotalAnonymizedRecords"] = 0,
            ["AnonymizationsByLevel"] = new Dictionary<string, int>(),
            ["AnonymizationsByCategory"] = new Dictionary<string, int>(),
            ["LastAnonymizationDate"] = DateTime.UtcNow,
            ["PendingAnonymizations"] = 0
        };
    }

    private Dictionary<DataCategory, string> InitializeMaskingPatterns()
    {
        return new Dictionary<DataCategory, string>
        {
            [DataCategory.Personal] = "*",
            [DataCategory.Sensitive] = "X",
            [DataCategory.Financial] = "#",
            [DataCategory.Health] = "H",
            [DataCategory.Biometric] = "B",
            [DataCategory.Location] = "L",
            [DataCategory.Behavioral] = "~",
            [DataCategory.Technical] = "T",
            [DataCategory.Communication] = "@",
            [DataCategory.Preference] = "P"
        };
    }

    private static string GenerateAnonymizedValue(string data)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        var hash = data.GetHashCode();
        return $"ANON_{Math.Abs(hash):X8}";
    }

    private static bool IsEmail(string data)
    {
        return !string.IsNullOrEmpty(data) && data.Contains('@') && data.Contains('.');
    }

    private static bool IsPhoneNumber(string data)
    {
        return !string.IsNullOrEmpty(data) && data.All(c => char.IsDigit(c) || "+-() ".Contains(c));
    }

    private static bool IsBase64Hash(string data)
    {
        if (string.IsNullOrEmpty(data))
            return false;

        try
        {
            var buffer = Convert.FromBase64String(data);
            return buffer.Length >= 16; // Typical hash length
        }
        catch
        {
            return false;
        }
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return email;

        var parts = email.Split('@');
        var localPart = parts[0];
        var domain = parts[1];

        var maskedLocal = localPart.Length <= 2 
            ? new string('*', localPart.Length)
            : localPart[0] + new string('*', localPart.Length - 2) + localPart[^1];

        return $"{maskedLocal}@{domain}";
    }

    private static string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return phone;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return new string('*', phone.Length);

        var masked = new string('*', digits.Length - 4) + digits[^4..];
        return phone.Replace(digits, masked);
    }

    private static string MaskCreditCard(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
            return cardNumber;

        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return new string('*', cardNumber.Length);

        var masked = new string('*', digits.Length - 4) + digits[^4..];
        return cardNumber.Replace(digits, masked);
    }

    private static string MaskGeneral(string data, string maskChar)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        if (data.Length <= 2)
            return new string(maskChar[0], data.Length);

        return data[0] + new string(maskChar[0], data.Length - 2) + data[^1];
    }
}
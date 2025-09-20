using System.IO;
using Microsoft.AspNetCore.DataProtection;

namespace Enterprise.Shared.Security.Services;

/// <summary>
/// Service for encrypting and decrypting data using AES
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly SecuritySettings _settings;
    private readonly IDataProtector? _dataProtector;
    private readonly byte[] _defaultKey;
    private readonly byte[] _defaultIv;

    public EncryptionService(
        ILogger<EncryptionService> logger,
        IOptions<SecuritySettings> settings,
        IDataProtectionProvider? dataProtectionProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        // Initialize default key and IV from settings or generate new ones
        _defaultKey = string.IsNullOrEmpty(_settings.EncryptionKey)
            ? GenerateRandomBytes(32)
            : Convert.FromBase64String(_settings.EncryptionKey);

        _defaultIv = string.IsNullOrEmpty(_settings.EncryptionIV)
            ? GenerateRandomBytes(16)
            : Convert.FromBase64String(_settings.EncryptionIV);

        // Use DataProtection API if available
        _dataProtector = dataProtectionProvider?.CreateProtector("Enterprise.Security.Encryption");

        _logger.LogDebug("Encryption service initialized");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        try
        {
            // Use DataProtection API if available for better security
            if (_dataProtector != null && _settings.UseDataProtectionApi)
            {
                return _dataProtector.Protect(plainText);
            }

            using var aes = Aes.Create();
            aes.Key = _defaultKey;
            aes.IV = _defaultIv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV and cipher text for storage
            var result = new byte[_defaultIv.Length + cipherBytes.Length];
            Buffer.BlockCopy(_defaultIv, 0, result, 0, _defaultIv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, _defaultIv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw new SecurityException("Encryption failed", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));

        try
        {
            // Use DataProtection API if available
            if (_dataProtector != null && _settings.UseDataProtectionApi)
            {
                return _dataProtector.Unprotect(cipherText);
            }

            var fullCipher = Convert.FromBase64String(cipherText);

            // Extract IV and cipher text
            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using var aes = Aes.Create();
            aes.Key = _defaultKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw new SecurityException("Decryption failed", ex);
        }
    }

    public string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var keyBytes = Convert.FromBase64String(key);
            var iv = GenerateRandomBytes(16);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV and cipher text
            var result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data with custom key");
            throw new SecurityException("Encryption failed", ex);
        }
    }

    public string Decrypt(string cipherText, string key)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var keyBytes = Convert.FromBase64String(key);
            var fullCipher = Convert.FromBase64String(cipherText);

            // Extract IV and cipher text
            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data with custom key");
            throw new SecurityException("Decryption failed", ex);
        }
    }

    public string GenerateKey()
    {
        var key = GenerateRandomBytes(32); // 256-bit key
        return Convert.ToBase64String(key);
    }

    public string GenerateIV()
    {
        var iv = GenerateRandomBytes(16); // 128-bit IV
        return Convert.ToBase64String(iv);
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }
}
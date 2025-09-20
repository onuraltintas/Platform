using System.Security.Cryptography;
using System.Text;

namespace Enterprise.Shared.Validation.Extensions;

/// <summary>
/// Cryptographic utility helper for secure operations
/// </summary>
public static class CryptoHelper
{
    /// <summary>
    /// Generates random string with specified length and character set
    /// </summary>
    public static string GenerateRandomString(int length, bool includeSpecialChars = false)
    {
        if (length <= 0) throw new ArgumentException("Uzunluk sıfırdan büyük olmalıdır.", nameof(length));

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        var characterSet = includeSpecialChars ? chars + specialChars : chars;
        
        return new string(Enumerable.Repeat(characterSet, length)
            .Select(s => s[Random.Shared.Next(s.Length)])
            .ToArray());
    }

    /// <summary>
    /// Generates cryptographically secure token
    /// </summary>
    public static string GenerateSecureToken(int length = 32)
    {
        if (length <= 0) throw new ArgumentException("Token uzunluğu sıfırdan büyük olmalıdır.", nameof(length));

        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    /// <summary>
    /// Generates URL-safe base64 token
    /// </summary>
    public static string GenerateUrlSafeToken(int byteLength = 32)
    {
        if (byteLength <= 0) throw new ArgumentException("Byte uzunluğu sıfırdan büyük olmalıdır.", nameof(byteLength));

        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Hashes string using SHA256
    /// </summary>
    public static string HashString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Hashes string using SHA256 and returns hex string
    /// </summary>
    public static string HashStringHex(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies hash against original input
    /// </summary>
    public static bool VerifyHash(string input, string hash)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash)) return false;
        
        var inputHash = HashString(input);
        return string.Equals(inputHash, hash, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies hex hash against original input
    /// </summary>
    public static bool VerifyHashHex(string input, string hash)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash)) return false;
        
        var inputHash = HashStringHex(input);
        return string.Equals(inputHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates new GUID
    /// </summary>
    public static Guid GenerateGuid()
    {
        return Guid.NewGuid();
    }

    /// <summary>
    /// Generates short unique ID (base62 encoded)
    /// </summary>
    public static string GenerateShortId(int length = 8)
    {
        if (length <= 0) throw new ArgumentException("Uzunluk sıfırdan büyük olmalıdır.", nameof(length));

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)])
            .ToArray());
    }

    /// <summary>
    /// Creates HMAC-SHA256 hash with key
    /// </summary>
    public static string CreateHmac(string message, string key)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(key))
            throw new ArgumentException("Mesaj ve anahtar boş olamaz.");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies HMAC signature
    /// </summary>
    public static bool VerifyHmac(string message, string key, string signature)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(signature))
            return false;

        var expectedSignature = CreateHmac(message, key);
        return string.Equals(expectedSignature, signature, StringComparison.Ordinal);
    }

    /// <summary>
    /// Encrypts string using AES
    /// </summary>
    public static string EncryptAes(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(key))
            throw new ArgumentException("Metin ve anahtar boş olamaz.");

        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts AES encrypted string
    /// </summary>
    public static string DecryptAes(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(key))
            throw new ArgumentException("Şifreli metin ve anahtar boş olamaz.");

        var cipherBytes = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));

        var iv = new byte[16];
        Array.Copy(cipherBytes, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Generates password hash using PBKDF2
    /// </summary>
    public static string HashPassword(string password, out string salt)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Şifre boş olamaz.", nameof(password));

        // Generate salt
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        salt = Convert.ToBase64String(saltBytes);

        // Hash password
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(32);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies password against hash
    /// </summary>
    public static bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            return false;

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(32);
            var computedHash = Convert.ToBase64String(hashBytes);
            
            return string.Equals(hash, computedHash, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates time-based OTP (TOTP)
    /// </summary>
    public static string GenerateTotp(string secret, DateTime? timestamp = null, int digits = 6, int period = 30)
    {
        var time = timestamp ?? DateTime.UtcNow;
        var unixTime = ((DateTimeOffset)time).ToUnixTimeSeconds();
        var timeStep = unixTime / period;

        var secretBytes = Convert.FromBase64String(secret);
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(timeBytes);

        var offset = hash[hash.Length - 1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) |
                    ((hash[offset + 1] & 0xFF) << 16) |
                    ((hash[offset + 2] & 0xFF) << 8) |
                    (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, digits);
        return otp.ToString().PadLeft(digits, '0');
    }
}
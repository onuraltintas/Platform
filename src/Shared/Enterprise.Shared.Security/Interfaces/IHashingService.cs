namespace Enterprise.Shared.Security.Interfaces;

/// <summary>
/// Service for hashing and verifying data
/// </summary>
public interface IHashingService
{
    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    /// <param name="password">The password to hash</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    /// <param name="password">The password to verify</param>
    /// <param name="hash">The hash to verify against</param>
    /// <returns>True if password matches hash</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Computes SHA256 hash of the input
    /// </summary>
    /// <param name="input">The input to hash</param>
    /// <returns>SHA256 hash as hex string</returns>
    string ComputeSha256(string input);

    /// <summary>
    /// Computes SHA512 hash of the input
    /// </summary>
    /// <param name="input">The input to hash</param>
    /// <returns>SHA512 hash as hex string</returns>
    string ComputeSha512(string input);

    /// <summary>
    /// Computes HMAC-SHA256 hash
    /// </summary>
    /// <param name="input">The input to hash</param>
    /// <param name="key">The HMAC key</param>
    /// <returns>HMAC-SHA256 hash as base64 string</returns>
    string ComputeHmacSha256(string input, string key);

    /// <summary>
    /// Generates a cryptographically secure random salt
    /// </summary>
    /// <param name="size">Size of the salt in bytes</param>
    /// <returns>Base64 encoded salt</returns>
    string GenerateSalt(int size = 32);
}
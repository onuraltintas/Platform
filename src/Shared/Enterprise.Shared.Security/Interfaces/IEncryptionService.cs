namespace Enterprise.Shared.Security.Interfaces;

/// <summary>
/// Service for encrypting and decrypting data
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Encrypted text as base64 string</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts the specified cipher text
    /// </summary>
    /// <param name="cipherText">The base64 encrypted text</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Encrypts data using a specific key
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <param name="key">The encryption key</param>
    /// <returns>Encrypted text as base64 string</returns>
    string Encrypt(string plainText, string key);

    /// <summary>
    /// Decrypts data using a specific key
    /// </summary>
    /// <param name="cipherText">The base64 encrypted text</param>
    /// <param name="key">The decryption key</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string cipherText, string key);

    /// <summary>
    /// Generates a new encryption key
    /// </summary>
    /// <returns>Base64 encoded encryption key</returns>
    string GenerateKey();

    /// <summary>
    /// Generates a new initialization vector
    /// </summary>
    /// <returns>Base64 encoded IV</returns>
    string GenerateIV();
}
using BCrypt.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EgitimPlatform.Shared.Security.Services;

public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;
    
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
            
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }
    
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;
            
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
    
    public bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;
            
        // Minimum 8 characters
        if (password.Length < 8)
            return false;
            
        // Must contain at least one uppercase letter
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;
            
        // Must contain at least one lowercase letter
        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;
            
        // Must contain at least one digit
        if (!Regex.IsMatch(password, @"[0-9]"))
            return false;
            
        // Must contain at least one special character
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            return false;
            
        return true;
    }
    
    public string GenerateRandomPassword(int length = 12)
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        var password = new StringBuilder();
        using var rng = RandomNumberGenerator.Create();
        
        // Ensure at least one character from each category
        password.Append(GetRandomCharacter(uppercase, rng));
        password.Append(GetRandomCharacter(lowercase, rng));
        password.Append(GetRandomCharacter(digits, rng));
        password.Append(GetRandomCharacter(special, rng));
        
        // Fill the rest randomly
        var allChars = uppercase + lowercase + digits + special;
        for (int i = 4; i < length; i++)
        {
            password.Append(GetRandomCharacter(allChars, rng));
        }
        
        // Shuffle the password
        return ShuffleString(password.ToString(), rng);
    }
    
    public string GeneratePasswordResetToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
    
    private char GetRandomCharacter(string chars, RandomNumberGenerator rng)
    {
        var randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        var randomInt = BitConverter.ToUInt32(randomBytes, 0);
        return chars[(int)(randomInt % chars.Length)];
    }
    
    private string ShuffleString(string input, RandomNumberGenerator rng)
    {
        var chars = input.ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomInt = BitConverter.ToUInt32(randomBytes, 0);
            var j = (int)(randomInt % (i + 1));
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }
}
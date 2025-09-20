using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Enterprise.Shared.Validation.Extensions;

/// <summary>
/// String extension methods for validation and utility operations
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if string is null or empty
    /// </summary>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Checks if string is null, empty or whitespace
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if string has a meaningful value
    /// </summary>
    public static bool HasValue(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Converts PascalCase to kebab-case
    /// </summary>
    public static string ToKebabCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return Regex.Replace(value, "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
    }

    /// <summary>
    /// Converts string to camelCase
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts string to PascalCase
    /// </summary>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts string to URL-friendly slug
    /// </summary>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Convert Turkish characters
        value = value.Replace("ç", "c").Replace("Ç", "C")
                    .Replace("ğ", "g").Replace("Ğ", "G")
                    .Replace("ı", "i").Replace("İ", "I")
                    .Replace("ö", "o").Replace("Ö", "O")
                    .Replace("ş", "s").Replace("Ş", "S")
                    .Replace("ü", "u").Replace("Ü", "U");

        value = value.ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"-+", "-");
        return value.Trim('-');
    }

    /// <summary>
    /// Truncates string to specified length with suffix
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= maxLength) return value;

        return value[..(maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Masks email address for privacy
    /// </summary>
    public static string MaskEmail(this string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@')) return email;

        var parts = email.Split('@');
        if (parts[0].Length <= 2) return email;

        var masked = parts[0][0] + new string('*', parts[0].Length - 2) + parts[0][^1];
        return masked + "@" + parts[1];
    }

    /// <summary>
    /// Masks phone number for privacy
    /// </summary>
    public static string MaskPhone(this string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4) return phone;

        var visibleDigits = Math.Min(4, phone.Length);
        var maskedLength = phone.Length - visibleDigits;
        
        return new string('*', maskedLength) + phone[^visibleDigits..];
    }

    /// <summary>
    /// Validates email address format
    /// </summary>
    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrEmpty(email)) return false;

        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates Turkish phone number format
    /// </summary>
    public static bool IsValidTurkishPhone(this string phone)
    {
        if (string.IsNullOrEmpty(phone)) return false;
        
        // Remove spaces and dashes
        phone = Regex.Replace(phone, @"[\s\-]", "");
        
        // Check Turkish mobile format
        return Regex.IsMatch(phone, @"^(\+90|0)?5\d{9}$");
    }

    /// <summary>
    /// Validates Turkish TC Identity Number
    /// </summary>
    public static bool IsValidTCNumber(this string tcNo)
    {
        if (string.IsNullOrEmpty(tcNo) || tcNo.Length != 11 || !tcNo.All(char.IsDigit))
            return false;

        if (tcNo[0] == '0') return false;

        var digits = tcNo.Select(c => int.Parse(c.ToString())).ToArray();
        
        // Algorithm for Turkish TC validation
        var sum1 = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sum2 = digits[1] + digits[3] + digits[5] + digits[7];
        
        var check1 = ((sum1 * 7) - sum2) % 10;
        var check2 = (sum1 + sum2 + check1) % 10;

        return digits[9] == check1 && digits[10] == check2;
    }

    /// <summary>
    /// Validates Turkish Tax Number (VKN)
    /// </summary>
    public static bool IsValidTurkishTaxNumber(this string vkn)
    {
        if (string.IsNullOrEmpty(vkn) || vkn.Length != 10 || !vkn.All(char.IsDigit))
            return false;

        var digits = vkn.Select(c => int.Parse(c.ToString())).ToArray();
        var sum = 0;

        for (int i = 0; i < 9; i++)
        {
            var temp = (digits[i] + (10 - i - 1)) % 10;
            sum += temp < 2 ? temp : (temp * temp) % 9;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return digits[9] == checkDigit;
    }

    /// <summary>
    /// Removes special characters from string
    /// </summary>
    public static string RemoveSpecialCharacters(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        
        return Regex.Replace(value, @"[^a-zA-ZçÇğĞıİöÖşŞüÜ0-9\s]", "");
    }

    /// <summary>
    /// Removes Turkish diacritics from string
    /// </summary>
    public static string RemoveTurkishDiacritics(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return value.Replace("ç", "c").Replace("Ç", "C")
                   .Replace("ğ", "g").Replace("Ğ", "G")
                   .Replace("ı", "i").Replace("İ", "I")
                   .Replace("ö", "o").Replace("Ö", "O")
                   .Replace("ş", "s").Replace("Ş", "S")
                   .Replace("ü", "u").Replace("Ü", "U");
    }

    /// <summary>
    /// Validates Turkish IBAN format
    /// </summary>
    public static bool IsValidTurkishIban(this string iban)
    {
        if (string.IsNullOrEmpty(iban)) return false;

        // Remove spaces and convert to uppercase
        iban = Regex.Replace(iban.ToUpperInvariant(), @"\s", "");

        // Check format
        if (!Regex.IsMatch(iban, @"^TR\d{24}$")) return false;

        // IBAN validation algorithm
        var rearranged = iban[4..] + iban[..4];
        var numericString = "";

        foreach (char c in rearranged)
        {
            if (char.IsLetter(c))
                numericString += ((int)c - 55).ToString();
            else
                numericString += c;
        }

        // Calculate mod 97
        var remainder = 0;
        foreach (char digit in numericString)
        {
            remainder = (remainder * 10 + (digit - '0')) % 97;
        }

        return remainder == 1;
    }

    /// <summary>
    /// Capitalizes first letter of each word (Turkish-aware)
    /// </summary>
    public static string ToTitleCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();

        foreach (var word in words)
        {
            if (word.Length == 0) continue;
            
            var firstChar = word[0];
            var capitalizedFirst = char.IsLetter(firstChar) ? char.ToUpperInvariant(firstChar) : firstChar;
            var restOfWord = word.Length > 1 ? word[1..].ToLowerInvariant() : "";
            
            result.Add(capitalizedFirst + restOfWord);
        }

        return string.Join(" ", result);
    }
}
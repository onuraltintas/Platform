namespace SpeedReading.Domain.ValueObjects;

public class ReadingPreferences
{
    public int? TargetReadingSpeed { get; private set; }
    public string[]? PreferredTextTypes { get; private set; }
    public string? ReadingGoals { get; private set; }
    public string PreferredLanguage { get; private set; }
    public int FontSize { get; private set; }
    public float LineSpacing { get; private set; }
    public string BackgroundColor { get; private set; }
    public string TextColor { get; private set; }

    private ReadingPreferences() { }

    public ReadingPreferences(
        int? targetReadingSpeed = null,
        string[]? preferredTextTypes = null,
        string? readingGoals = null,
        string preferredLanguage = "tr-TR",
        int fontSize = 14,
        float lineSpacing = 1.5f,
        string backgroundColor = "#FFFFFF",
        string textColor = "#000000")
    {
        TargetReadingSpeed = targetReadingSpeed;
        PreferredTextTypes = preferredTextTypes;
        ReadingGoals = readingGoals;
        PreferredLanguage = preferredLanguage;
        FontSize = fontSize;
        LineSpacing = lineSpacing;
        BackgroundColor = backgroundColor;
        TextColor = textColor;
    }

    public ReadingPreferences UpdateTargetSpeed(int targetSpeed)
    {
        return new ReadingPreferences(
            targetSpeed,
            PreferredTextTypes,
            ReadingGoals,
            PreferredLanguage,
            FontSize,
            LineSpacing,
            BackgroundColor,
            TextColor);
    }

    public ReadingPreferences UpdateDisplaySettings(int fontSize, float lineSpacing, string backgroundColor, string textColor)
    {
        return new ReadingPreferences(
            TargetReadingSpeed,
            PreferredTextTypes,
            ReadingGoals,
            PreferredLanguage,
            fontSize,
            lineSpacing,
            backgroundColor,
            textColor);
    }

    // Equality implementation removed - was: protected override IEnumerable<object?> GetEqualityComponents()
}
namespace SpeedReading.Domain.ValueObjects;

public class TextMetadata
{
    public string Author { get; private set; }
    public string? Publisher { get; private set; }
    public DateTime? PublishDate { get; private set; }
    public string? Source { get; private set; }
    public string? Language { get; private set; }
    public string[]? Tags { get; private set; }
    public string? Copyright { get; private set; }
    public string? Isbn { get; private set; }

    private TextMetadata() 
    {
        Author = string.Empty;
        Language = "tr-TR";
    }

    public TextMetadata(
        string author,
        string? publisher = null,
        DateTime? publishDate = null,
        string? source = null,
        string? language = "tr-TR",
        string[]? tags = null,
        string? copyright = null,
        string? isbn = null)
    {
        Author = author ?? "Anonim";
        Publisher = publisher;
        PublishDate = publishDate;
        Source = source;
        Language = language ?? "tr-TR";
        Tags = tags;
        Copyright = copyright;
        Isbn = isbn;
    }

    // Equality implementation removed - was: protected override IEnumerable<object?> GetEqualityComponents()
}

public class TextStatistics
{
    public int WordCount { get; private set; }
    public int CharacterCount { get; private set; }
    public int SentenceCount { get; private set; }
    public int ParagraphCount { get; private set; }
    public double AverageWordLength { get; private set; }
    public double AverageSentenceLength { get; private set; }
    public int UniqueWordCount { get; private set; }
    public double LexicalDiversity { get; private set; } // Unique/Total words
    public int EstimatedReadingTime { get; private set; } // in seconds

    private TextStatistics() { }

    public TextStatistics(
        int wordCount,
        int characterCount,
        int sentenceCount,
        int paragraphCount,
        double averageWordLength,
        double averageSentenceLength,
        int uniqueWordCount,
        int estimatedReadingTime = 0)
    {
        WordCount = wordCount;
        CharacterCount = characterCount;
        SentenceCount = sentenceCount;
        ParagraphCount = paragraphCount;
        AverageWordLength = averageWordLength;
        AverageSentenceLength = averageSentenceLength;
        UniqueWordCount = uniqueWordCount;
        LexicalDiversity = wordCount > 0 ? (double)uniqueWordCount / wordCount : 0;
        EstimatedReadingTime = estimatedReadingTime > 0 
            ? estimatedReadingTime 
            : CalculateEstimatedReadingTime(wordCount);
    }

    private static int CalculateEstimatedReadingTime(int wordCount)
    {
        // Average reading speed: 200 WPM
        const int averageWpm = 200;
        return (int)Math.Ceiling((double)wordCount / averageWpm * 60);
    }

    public static TextStatistics Calculate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TextStatistics(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var wordCount = words.Length;
        var characterCount = text.Length;
        var sentenceCount = sentences.Length;
        var paragraphCount = paragraphs.Length;
        
        var avgWordLength = words.Any() ? words.Average(w => w.Length) : 0;
        var avgSentenceLength = sentenceCount > 0 ? (double)wordCount / sentenceCount : 0;
        
        var uniqueWords = words.Select(w => w.ToLowerInvariant()).Distinct().Count();
        
        return new TextStatistics(
            wordCount,
            characterCount,
            sentenceCount,
            paragraphCount,
            avgWordLength,
            avgSentenceLength,
            uniqueWords
        );
    }

    // Equality implementation removed - was: protected override IEnumerable<object?> GetEqualityComponents()
}
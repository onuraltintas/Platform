using System.Text;
using System.Text.RegularExpressions;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.ValueObjects;

namespace SpeedReading.Application.Services;

public class TurkishTextAnalyzer : ITextAnalysisService, ITurkishLanguageService
{
    private static readonly string[] CommonTurkishWords = 
    {
        "ve", "bir", "bu", "da", "de", "için", "ile", "olan", "olarak", "daha",
        "var", "çok", "en", "gibi", "sonra", "kadar", "her", "ne", "ya", "ki",
        "ama", "veya", "ancak", "şu", "o", "ben", "sen", "biz", "siz", "onlar"
    };

    private static readonly char[] TurkishVowels = { 'a', 'e', 'ı', 'i', 'o', 'ö', 'u', 'ü' };
    
    private static readonly Regex SentenceRegex = new(@"[.!?]+\s*", RegexOptions.Compiled);
    private static readonly Regex WordRegex = new(@"\b[\w']+\b", RegexOptions.Compiled);
    private static readonly Regex ParagraphRegex = new(@"\n\s*\n", RegexOptions.Compiled);

    public async Task<TextAnalysisResult> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TextAnalysisResult
            {
                Statistics = TextStatistics.Calculate(string.Empty),
                Difficulty = TextDifficulty.Easy,
                TargetEducationLevel = EducationCategory.Elementary
            };
        }

        var statistics = TextStatistics.Calculate(text);
        var words = TokenizeWords(text);
        var sentences = TokenizeSentences(text);
        
        // Calculate word frequency
        var wordFrequency = CalculateWordFrequency(words);
        
        // Calculate complex words
        var complexWords = words.Where(IsComplexWord).ToArray();
        var complexWordCount = complexWords.Length;
        var complexWordPercentage = words.Length > 0 ? (double)complexWordCount / words.Length * 100 : 0;
        
        // Calculate average syllables
        var avgSyllables = words.Length > 0 ? (int)Math.Round(words.Average(w => CountSyllables(w))) : 0;
        
        // Calculate readability
        var readabilityScore = CalculateTurkishReadabilityIndex(text);
        
        // Determine difficulty
        var difficulty = CalculateDifficulty(statistics);
        var educationLevel = DetermineTargetEducationLevel(difficulty, statistics);
        
        // Extract keywords and summary
        var keywords = await ExtractKeywordsAsync(text, 10, cancellationToken);
        var summary = await GenerateSummaryAsync(text, 200, cancellationToken);
        
        // Calculate Turkish-specific metrics
        var (verbDensity, nounDensity, adjDensity) = await CalculatePartOfSpeechDensityAsync(words);
        var suffixComplexity = CalculateSuffixComplexity(words);
        
        return new TextAnalysisResult
        {
            Statistics = statistics,
            Difficulty = difficulty,
            TargetEducationLevel = educationLevel,
            ReadabilityScore = readabilityScore,
            DifficultyScore = CalculateDifficultyScore(statistics, complexWordPercentage),
            Keywords = keywords,
            Summary = summary,
            WordFrequency = wordFrequency,
            ComplexWordCount = complexWordCount,
            ComplexWordPercentage = complexWordPercentage,
            AverageSyllablesPerWord = avgSyllables,
            TurkishSuffixComplexity = suffixComplexity,
            VerbDensity = verbDensity,
            NounDensity = nounDensity,
            AdjectiveDensity = adjDensity
        };
    }

    public TextDifficulty CalculateDifficulty(TextStatistics statistics)
    {
        var avgSentenceLength = statistics.AverageSentenceLength;
        var avgWordLength = statistics.AverageWordLength;
        var lexicalDiversity = statistics.LexicalDiversity;
        
        // Scoring based on multiple factors
        var score = 0.0;
        
        // Sentence length scoring
        score += avgSentenceLength switch
        {
            < 10 => 1,
            < 15 => 2,
            < 20 => 3,
            < 25 => 4,
            < 30 => 5,
            _ => 6
        };
        
        // Word length scoring
        score += avgWordLength switch
        {
            < 4 => 1,
            < 5 => 2,
            < 6 => 3,
            < 7 => 4,
            < 8 => 5,
            _ => 6
        };
        
        // Lexical diversity scoring
        score += lexicalDiversity switch
        {
            < 0.3 => 1,
            < 0.4 => 2,
            < 0.5 => 3,
            < 0.6 => 4,
            < 0.7 => 5,
            _ => 6
        };
        
        var avgScore = score / 3;
        
        return avgScore switch
        {
            < 1.5 => TextDifficulty.VeryEasy,
            < 2.5 => TextDifficulty.Easy,
            < 3.5 => TextDifficulty.Medium,
            < 4.5 => TextDifficulty.Hard,
            < 5.5 => TextDifficulty.VeryHard,
            _ => TextDifficulty.Expert
        };
    }

    public EducationCategory DetermineTargetEducationLevel(TextDifficulty difficulty, TextStatistics statistics)
    {
        return difficulty switch
        {
            TextDifficulty.VeryEasy => EducationCategory.Elementary,
            TextDifficulty.Easy => EducationCategory.Elementary,
            TextDifficulty.Medium => EducationCategory.MiddleSchool,
            TextDifficulty.Hard => EducationCategory.HighSchool,
            TextDifficulty.VeryHard => EducationCategory.University,
            TextDifficulty.Expert => EducationCategory.Graduate,
            _ => EducationCategory.Adult
        };
    }

    public double CalculateReadabilityScore(string text)
    {
        return CalculateTurkishReadabilityIndex(text);
    }

    public double CalculateTurkishReadabilityIndex(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 100;
        
        var words = TokenizeWords(text);
        var sentences = TokenizeSentences(text);
        
        if (words.Length == 0 || sentences.Length == 0) return 100;
        
        var avgSyllablesPerWord = words.Average(w => CountSyllables(w));
        var avgWordsPerSentence = (double)words.Length / sentences.Length;
        
        // Modified Ateşman formula for Turkish
        // OKP = 198.825 - 40.175 * (Kelime Sayısı / Cümle Sayısı) - 2.610 * (Hece Sayısı / Kelime Sayısı)
        var readabilityIndex = 198.825 - (40.175 * avgWordsPerSentence) - (2.610 * avgSyllablesPerWord);
        
        // Normalize to 0-100 scale
        return Math.Max(0, Math.Min(100, readabilityIndex));
    }

    public async Task<string[]> ExtractKeywordsAsync(string text, int maxKeywords = 10, CancellationToken cancellationToken = default)
    {
        var words = TokenizeWords(text.ToLowerInvariant());
        var frequency = CalculateWordFrequency(words);
        
        // Remove common words
        var keywords = frequency
            .Where(kvp => !CommonTurkishWords.Contains(kvp.Key) && kvp.Key.Length > 2)
            .OrderByDescending(kvp => kvp.Value)
            .Take(maxKeywords)
            .Select(kvp => kvp.Key)
            .ToArray();
        
        return await Task.FromResult(keywords);
    }

    public async Task<string> GenerateSummaryAsync(string text, int maxLength = 200, CancellationToken cancellationToken = default)
    {
        var sentences = TokenizeSentences(text);
        if (sentences.Length == 0) return string.Empty;
        
        // Simple extractive summarization - take first few sentences
        var summary = new StringBuilder();
        foreach (var sentence in sentences)
        {
            if (summary.Length + sentence.Length > maxLength) break;
            summary.Append(sentence.Trim()).Append(" ");
        }
        
        var result = summary.ToString().Trim();
        if (result.Length > maxLength)
        {
            result = result.Substring(0, maxLength - 3) + "...";
        }
        
        return await Task.FromResult(result);
    }

    public int CountSyllables(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0;
        
        word = word.ToLowerInvariant();
        int syllableCount = 0;
        bool previousWasVowel = false;
        
        foreach (char c in word)
        {
            bool isVowel = TurkishVowels.Contains(c);
            if (isVowel && !previousWasVowel)
            {
                syllableCount++;
            }
            previousWasVowel = isVowel;
        }
        
        return Math.Max(1, syllableCount);
    }

    public string[] TokenizeWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        
        return WordRegex.Matches(text)
            .Select(m => m.Value)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToArray();
    }

    public string[] TokenizeSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        
        return SentenceRegex.Split(text)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToArray();
    }

    public string[] TokenizeParagraphs(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        
        return ParagraphRegex.Split(text)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .ToArray();
    }

    public string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        return text
            .Replace('ı', 'i')
            .Replace('İ', 'I')
            .Replace('ğ', 'g')
            .Replace('Ğ', 'G')
            .Replace('ü', 'u')
            .Replace('Ü', 'U')
            .Replace('ş', 's')
            .Replace('Ş', 'S')
            .Replace('ö', 'o')
            .Replace('Ö', 'O')
            .Replace('ç', 'c')
            .Replace('Ç', 'C');
    }

    public bool IsComplexWord(string word)
    {
        return CountSyllables(word) >= 3;
    }

    public string GetWordRoot(string word)
    {
        // Simplified root extraction - would need a proper morphological analyzer for production
        // This is a basic implementation that removes common Turkish suffixes
        if (string.IsNullOrEmpty(word)) return word;
        
        word = word.ToLowerInvariant();
        
        // Common suffixes to remove (simplified)
        string[] suffixes = { "ler", "lar", "den", "dan", "de", "da", "in", "ın", "un", "ün" };
        
        foreach (var suffix in suffixes.OrderByDescending(s => s.Length))
        {
            if (word.EndsWith(suffix) && word.Length > suffix.Length + 2)
            {
                return word.Substring(0, word.Length - suffix.Length);
            }
        }
        
        return word;
    }

    public string[] GetCommonWords()
    {
        return CommonTurkishWords;
    }

    private Dictionary<string, int> CalculateWordFrequency(string[] words)
    {
        return words
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .GroupBy(w => w.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private double CalculateDifficultyScore(TextStatistics statistics, double complexWordPercentage)
    {
        var wordLengthScore = Math.Min(statistics.AverageWordLength * 10, 30);
        var sentenceLengthScore = Math.Min(statistics.AverageSentenceLength * 2, 30);
        var complexityScore = Math.Min(complexWordPercentage, 40);
        
        return Math.Min(wordLengthScore + sentenceLengthScore + complexityScore, 100);
    }

    private async Task<(double verbDensity, double nounDensity, double adjectiveDensity)> CalculatePartOfSpeechDensityAsync(string[] words)
    {
        // Simplified POS tagging - in production, use a proper NLP library
        // This is a basic heuristic approach
        
        var verbEndings = new[] { "mak", "mek", "yor", "dı", "di", "du", "dü", "tı", "ti", "tu", "tü" };
        var nounEndings = new[] { "lık", "lik", "luk", "lük", "cı", "ci", "cu", "cü" };
        var adjectiveEndings = new[] { "lı", "li", "lu", "lü", "sız", "siz", "suz", "süz" };
        
        int verbCount = 0, nounCount = 0, adjectiveCount = 0;
        
        foreach (var word in words)
        {
            var lowerWord = word.ToLowerInvariant();
            
            if (verbEndings.Any(e => lowerWord.EndsWith(e)))
                verbCount++;
            else if (nounEndings.Any(e => lowerWord.EndsWith(e)))
                nounCount++;
            else if (adjectiveEndings.Any(e => lowerWord.EndsWith(e)))
                adjectiveCount++;
        }
        
        var totalWords = words.Length > 0 ? words.Length : 1;
        
        return await Task.FromResult((
            (double)verbCount / totalWords,
            (double)nounCount / totalWords,
            (double)adjectiveCount / totalWords
        ));
    }

    private int CalculateSuffixComplexity(string[] words)
    {
        // Count average number of suffixes per word (simplified)
        var totalSuffixes = 0;
        
        foreach (var word in words)
        {
            var root = GetWordRoot(word);
            if (root.Length < word.Length)
            {
                totalSuffixes += (word.Length - root.Length) / 2; // Rough estimate
            }
        }
        
        return words.Length > 0 ? totalSuffixes / words.Length : 0;
    }

    // ITurkishTextAnalyzer implementation
    public async Task<IReadOnlyList<string>> ExtractKeywordsAsync(string text)
    {
        var keywords = await ExtractKeywordsAsync(text, 10);
        return keywords;
    }

    public async Task<IReadOnlyList<string>> ExtractSentencesAsync(string text)
    {
        var sentences = TokenizeSentences(text);
        return sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    public async Task<IReadOnlyList<string>> ExtractImportantSentencesAsync(string text)
    {
        var sentences = await ExtractSentencesAsync(text);
        var keywords = await ExtractKeywordsAsync(text);
        
        var importantSentences = new List<string>();
        
        foreach (var sentence in sentences)
        {
            var sentenceLower = sentence.ToLower();
            var keywordMatches = keywords.Count(k => sentenceLower.Contains(k.ToLower()));
            
            // Consider sentence important if it contains multiple keywords or is at beginning/end  
            var sentenceIndex = ((List<string>)sentences).IndexOf(sentence);
            if (keywordMatches >= 2 || sentenceIndex == 0 || sentenceIndex == sentences.Count - 1)
            {
                importantSentences.Add(sentence);
            }
        }
        
        return importantSentences.Take(3).ToList(); // Return top 3 important sentences
    }

    public async Task<string> AnalyzeTopicAsync(string text)
    {
        var keywords = await ExtractKeywordsAsync(text);
        if (!keywords.Any()) return "Genel Konu";
        
        // Simple topic analysis based on most frequent keywords
        var topKeywords = keywords.Take(3);
        return string.Join(", ", topKeywords);
    }

    public async Task<double> CalculateReadabilityScoreAsync(string text)
    {
        return CalculateTurkishReadabilityIndex(text);
    }

    public async Task<int> CountSyllablesAsync(string word)
    {
        return CountSyllables(word);
    }

    public async Task<bool> IsComplexWordAsync(string word)
    {
        return IsComplexWord(word);
    }

    public async Task<IReadOnlyList<string>> TokenizeWordsAsync(string text)
    {
        var words = TokenizeWords(text);
        return words;
    }

    public async Task<Dictionary<string, int>> CalculateWordFrequencyAsync(string text)
    {
        var words = TokenizeWords(text);
        return CalculateWordFrequency(words);
    }
}
using System.Text;
using System.Text.Json;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.ValueObjects;

namespace SpeedReading.Application.Services;

public class TextImportExportService
{
    private readonly IReadingTextRepository _textRepository;
    private readonly ITextAnalysisService _analysisService;

    public TextImportExportService(
        IReadingTextRepository textRepository,
        ITextAnalysisService analysisService)
    {
        _textRepository = textRepository;
        _analysisService = analysisService;
    }

    public async Task<ImportResult> ImportFromJsonAsync(
        string jsonContent, 
        Guid? importedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        
        try
        {
            var importData = JsonSerializer.Deserialize<TextImportData[]>(jsonContent);
            if (importData == null || !importData.Any())
            {
                result.Errors.Add("No valid data found in JSON");
                return result;
            }

            foreach (var item in importData)
            {
                try
                {
                    var text = await CreateTextFromImportDataAsync(item, importedBy, cancellationToken);
                    await _textRepository.AddAsync(text, cancellationToken);
                    result.SuccessCount++;
                    result.ImportedTextIds.Add(text.Id);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Error importing '{item.Title}': {ex.Message}");
                }
            }

            await _textRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"JSON parsing error: {ex.Message}");
        }

        return result;
    }

    public async Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        Guid? importedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            result.Errors.Add("CSV file must contain header and at least one data row");
            return result;
        }

        // Skip header
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var fields = ParseCsvLine(lines[i]);
                if (fields.Length < 4)
                {
                    result.Errors.Add($"Line {i + 1}: Insufficient fields");
                    continue;
                }

                var importData = new TextImportData
                {
                    Title = fields[0],
                    Content = fields[1],
                    Category = fields[2],
                    Author = fields.Length > 3 ? fields[3] : "Anonim",
                    Summary = fields.Length > 4 ? fields[4] : null
                };

                var text = await CreateTextFromImportDataAsync(importData, importedBy, cancellationToken);
                await _textRepository.AddAsync(text, cancellationToken);
                result.SuccessCount++;
                result.ImportedTextIds.Add(text.Id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Line {i + 1}: {ex.Message}");
            }
        }

        await _textRepository.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<string> ExportToJsonAsync(
        TextFilterCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        var texts = criteria != null 
            ? await _textRepository.GetFilteredAsync(criteria, cancellationToken)
            : await _textRepository.GetActiveTextsAsync(0, 1000, cancellationToken);

        var exportData = texts.Select(t => new TextExportData
        {
            Id = t.Id,
            Title = t.Title,
            Content = t.Content,
            Summary = t.Summary,
            Category = t.Category.ToString(),
            Difficulty = t.Difficulty.ToString(),
            EducationLevel = t.TargetEducationLevel.ToString(),
            Author = t.Metadata.Author,
            Publisher = t.Metadata.Publisher,
            PublishDate = t.Metadata.PublishDate,
            Tags = t.Metadata.Tags,
            WordCount = t.Statistics.WordCount,
            ReadabilityScore = t.DifficultyScore,
            CreatedAt = t.CreatedAt,
            PublishedAt = t.PublishedAt
        }).ToArray();

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<string> ExportToCsvAsync(
        TextFilterCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        var texts = criteria != null 
            ? await _textRepository.GetFilteredAsync(criteria, cancellationToken)
            : await _textRepository.GetActiveTextsAsync(0, 1000, cancellationToken);

        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Title,Author,Category,Difficulty,WordCount,Summary");
        
        // Data rows
        foreach (var text in texts)
        {
            csv.AppendLine($"\"{EscapeCsvField(text.Title)}\",\"{EscapeCsvField(text.Metadata.Author)}\",\"{text.Category}\",\"{text.Difficulty}\",{text.Statistics.WordCount},\"{EscapeCsvField(text.Summary ?? "")}\"");
        }

        return csv.ToString();
    }

    public async Task<ImportResult> BulkImportTextsAsync(
        List<BulkTextImport> texts,
        Guid? importedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        var textsToAdd = new List<ReadingText>();

        foreach (var import in texts)
        {
            try
            {
                // Analyze text
                var analysis = await _analysisService.AnalyzeTextAsync(import.Content, cancellationToken);
                
                // Create metadata
                var metadata = new TextMetadata(
                    import.Author ?? "Anonim",
                    import.Publisher,
                    import.PublishDate,
                    import.Source,
                    "tr-TR",
                    import.Tags?.Split(',').Select(t => t.Trim()).ToArray()
                );

                // Determine category and difficulty if not provided
                var category = import.Category ?? DetermineCategory(import.Title, import.Content);
                var difficulty = import.Difficulty ?? analysis.Difficulty;
                var educationLevel = analysis.TargetEducationLevel;

                // Create text entity
                var text = new ReadingText(
                    import.Title,
                    import.Content,
                    category,
                    difficulty,
                    educationLevel,
                    metadata,
                    importedBy
                );

                if (!string.IsNullOrEmpty(import.Summary))
                {
                    text.UpdateContent(import.Title, import.Content, import.Summary);
                }

                if (import.AutoPublish)
                {
                    text.Publish();
                }

                textsToAdd.Add(text);
                result.SuccessCount++;
                result.ImportedTextIds.Add(text.Id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Error importing '{import.Title}': {ex.Message}");
            }
        }

        // Bulk add to repository
        foreach (var text in textsToAdd)
        {
            await _textRepository.AddAsync(text, cancellationToken);
        }

        await _textRepository.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<ReadingText> CreateTextFromImportDataAsync(
        TextImportData data,
        Guid? importedBy,
        CancellationToken cancellationToken)
    {
        // Analyze text
        var analysis = await _analysisService.AnalyzeTextAsync(data.Content, cancellationToken);
        
        // Parse or determine category
        var category = ParseCategory(data.Category) ?? TextCategory.Article;
        
        // Create metadata
        var metadata = new TextMetadata(
            data.Author ?? "Anonim",
            data.Publisher,
            data.PublishDate,
            data.Source,
            "tr-TR",
            data.Tags
        );

        // Create text entity
        var text = new ReadingText(
            data.Title,
            data.Content,
            category,
            analysis.Difficulty,
            analysis.TargetEducationLevel,
            metadata,
            importedBy
        );

        if (!string.IsNullOrEmpty(data.Summary))
        {
            text.UpdateContent(data.Title, data.Content, data.Summary);
        }

        return text;
    }

    private TextCategory? ParseCategory(string? categoryStr)
    {
        if (string.IsNullOrEmpty(categoryStr)) return null;
        
        return Enum.TryParse<TextCategory>(categoryStr, true, out var category) 
            ? category 
            : null;
    }

    private TextCategory DetermineCategory(string title, string content)
    {
        // Simple heuristic-based categorization
        var text = (title + " " + content).ToLowerInvariant();
        
        if (text.Contains("bilim") || text.Contains("deney") || text.Contains("araştırma"))
            return TextCategory.Science;
        if (text.Contains("tarih") || text.Contains("osmanlı") || text.Contains("cumhuriyet"))
            return TextCategory.History;
        if (text.Contains("spor") || text.Contains("futbol") || text.Contains("basketbol"))
            return TextCategory.Sports;
        if (text.Contains("teknoloji") || text.Contains("bilgisayar") || text.Contains("yazılım"))
            return TextCategory.Technology;
        if (text.Contains("sanat") || text.Contains("resim") || text.Contains("müzik"))
            return TextCategory.Art;
        if (text.Contains("şiir") || text.Contains("dize") || text.Contains("mısra"))
            return TextCategory.Poetry;
        if (text.Contains("masal") || text.Contains("bir varmış bir yokmuş"))
            return TextCategory.Tale;
        if (text.Contains("hikaye") || text.Contains("öykü"))
            return TextCategory.Story;
        
        return TextCategory.Article;
    }

    private string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }
        
        fields.Add(currentField.ToString());
        return fields.ToArray();
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains('"'))
        {
            field = field.Replace("\"", "\"\"");
        }
        return field;
    }
}

public class TextImportData
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Category { get; set; }
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public DateTime? PublishDate { get; set; }
    public string? Source { get; set; }
    public string[]? Tags { get; set; }
}

public class TextExportData
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string EducationLevel { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public DateTime? PublishDate { get; set; }
    public string[]? Tags { get; set; }
    public int WordCount { get; set; }
    public double ReadabilityScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class BulkTextImport
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public TextCategory? Category { get; set; }
    public TextDifficulty? Difficulty { get; set; }
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public DateTime? PublishDate { get; set; }
    public string? Source { get; set; }
    public string? Tags { get; set; }
    public bool AutoPublish { get; set; } = false;
}

public class ImportResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<Guid> ImportedTextIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => FailureCount == 0 && Errors.Count == 0;
}
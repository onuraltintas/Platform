using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Entities;

public class Question
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Text { get; private set; } = string.Empty;
    public QuestionType Type { get; private set; }
    public int Points { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsRequired { get; private set; }
    public string? HelpText { get; private set; }
    public string? ImageUrl { get; private set; }
    public string Metadata { get; private set; } = string.Empty;

    public Guid ExerciseId { get; private set; }
    public Exercise Exercise { get; private set; } = null!;

    private readonly List<QuestionOption> _options = new();
    public IReadOnlyList<QuestionOption> Options => _options.AsReadOnly();

    private readonly List<QuestionAnswer> _answers = new();
    public IReadOnlyList<QuestionAnswer> Answers => _answers.AsReadOnly();

    private Question() { }

    public Question(
        string text,
        QuestionType type,
        int points,
        int orderIndex,
        Guid exerciseId,
        bool isRequired = true,
        string? helpText = null,
        string? imageUrl = null)
    {
        Id = Guid.NewGuid();
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Type = type;
        Points = points > 0 ? points : throw new ArgumentException("Points must be positive.", nameof(points));
        OrderIndex = orderIndex;
        ExerciseId = exerciseId;
        IsRequired = isRequired;
        HelpText = helpText;
        ImageUrl = imageUrl;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateText(string text, string? helpText = null)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        HelpText = helpText;
    }

    public void UpdatePoints(int points)
    {
        if (points <= 0) throw new ArgumentException("Points must be positive.", nameof(points));
        Points = points;
    }

    public void UpdateOrder(int orderIndex)
    {
        OrderIndex = orderIndex;
    }

    public void UpdateImage(string? imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void UpdateMetadata(string metadata)
    {
        Metadata = metadata ?? string.Empty;
    }

    public void AddOption(QuestionOption option)
    {
        if (option == null) throw new ArgumentNullException(nameof(option));
        
        if (Type == QuestionType.TrueFalse && _options.Count >= 2)
        {
            throw new InvalidOperationException("True/False questions can only have 2 options.");
        }

        _options.Add(option);
    }

    public void RemoveOption(Guid optionId)
    {
        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option != null)
        {
            _options.Remove(option);
        }
    }

    public void ClearOptions()
    {
        _options.Clear();
    }

    public bool ValidateAnswer(string userAnswer)
    {
        return Type switch
        {
            QuestionType.MultipleChoice => ValidateMultipleChoiceAnswer(userAnswer),
            QuestionType.TrueFalse => ValidateTrueFalseAnswer(userAnswer),
            QuestionType.ShortAnswer => ValidateShortAnswer(userAnswer),
            QuestionType.FillInTheBlank => ValidateFillInTheBlankAnswer(userAnswer),
            QuestionType.Matching => ValidateMatchingAnswer(userAnswer),
            QuestionType.Ordering => ValidateOrderingAnswer(userAnswer),
            _ => false
        };
    }

    public int CalculateScore(string userAnswer)
    {
        if (ValidateAnswer(userAnswer))
        {
            return Points;
        }
        
        // Kısmi puan için özel durumlar
        if (Type == QuestionType.Essay || Type == QuestionType.ShortAnswer)
        {
            return CalculatePartialScore(userAnswer);
        }

        return 0;
    }

    private bool ValidateMultipleChoiceAnswer(string userAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer)) return false;
        
        var correctOptions = _options.Where(o => o.IsCorrect).Select(o => o.Id.ToString());
        var userAnswerIds = userAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        return correctOptions.OrderBy(x => x).SequenceEqual(userAnswerIds.OrderBy(x => x));
    }

    private bool ValidateTrueFalseAnswer(string userAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer)) return false;
        
        var correctOption = _options.FirstOrDefault(o => o.IsCorrect);
        return correctOption != null && userAnswer.Equals(correctOption.Id.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private bool ValidateShortAnswer(string userAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer)) return false;
        
        var correctAnswers = _options.Where(o => o.IsCorrect).Select(o => o.Text.Trim().ToLower());
        return correctAnswers.Any(correct => userAnswer.Trim().ToLower().Contains(correct));
    }

    private bool ValidateFillInTheBlankAnswer(string userAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer)) return false;
        
        var correctAnswers = _options.Where(o => o.IsCorrect).Select(o => o.Text.Trim().ToLower());
        return correctAnswers.Any(correct => correct.Equals(userAnswer.Trim().ToLower()));
    }

    private bool ValidateMatchingAnswer(string userAnswer)
    {
        // Format: "option1:match1,option2:match2"
        if (string.IsNullOrEmpty(userAnswer)) return false;
        
        try
        {
            var userMatches = userAnswer.Split(',')
                .Select(pair => pair.Split(':'))
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

            var correctMatches = _options.Where(o => o.IsCorrect)
                .ToDictionary(o => o.Id.ToString(), o => o.MatchingValue?.Trim() ?? string.Empty);

            return userMatches.Count == correctMatches.Count &&
                   userMatches.All(um => correctMatches.ContainsKey(um.Key) && 
                                        correctMatches[um.Key].Equals(um.Value, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateOrderingAnswer(string userAnswer)
    {
        // Format: "optionId1,optionId2,optionId3"
        if (string.IsNullOrEmpty(userAnswer)) return false;
        
        var userOrder = userAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var correctOrder = _options.OrderBy(o => o.OrderIndex).Select(o => o.Id.ToString()).ToArray();
        
        return userOrder.SequenceEqual(correctOrder);
    }

    private int CalculatePartialScore(string userAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer)) return 0;
        
        // Bu method daha karmaşık bir algoritma ile kısmi puan hesaplayabilir
        // Şimdilik basit bir implementasyon yapıyoruz
        var wordCount = userAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        return Type switch
        {
            QuestionType.Essay when wordCount >= 50 => Points / 2, // %50 kısmi puan
            QuestionType.ShortAnswer when wordCount >= 3 => Points / 3, // %33 kısmi puan
            _ => 0
        };
    }
}
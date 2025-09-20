namespace SpeedReading.Domain.Entities;

public class QuestionOption
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
    public int OrderIndex { get; private set; }
    public string? Explanation { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? MatchingValue { get; private set; } // For matching questions

    public Guid QuestionId { get; private set; }
    public Question Question { get; private set; } = null!;

    private QuestionOption() { }

    public QuestionOption(
        string text,
        bool isCorrect,
        int orderIndex,
        Guid questionId,
        string? explanation = null,
        string? imageUrl = null,
        string? matchingValue = null)
    {
        Id = Guid.NewGuid();
        Text = text ?? throw new ArgumentNullException(nameof(text));
        IsCorrect = isCorrect;
        OrderIndex = orderIndex;
        QuestionId = questionId;
        Explanation = explanation;
        ImageUrl = imageUrl;
        MatchingValue = matchingValue;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateText(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public void SetCorrect(bool isCorrect)
    {
        IsCorrect = isCorrect;
    }

    public void UpdateOrder(int orderIndex)
    {
        OrderIndex = orderIndex;
    }

    public void UpdateExplanation(string? explanation)
    {
        Explanation = explanation;
    }

    public void UpdateImage(string? imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void UpdateMatchingValue(string? matchingValue)
    {
        MatchingValue = matchingValue;
    }
}
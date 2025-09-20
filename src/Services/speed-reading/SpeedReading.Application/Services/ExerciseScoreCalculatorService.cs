using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.Services;

namespace SpeedReading.Application.Services;

public class ExerciseScoreCalculatorService : IExerciseScoreCalculator
{
    public async Task<ExerciseScoreResult> CalculateScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions)
    {
        var startTime = DateTime.UtcNow;
        var result = new ExerciseScoreResult
        {
            CalculatedAt = startTime,
            QuestionScores = new Dictionary<Guid, QuestionScoreResult>()
        };

        var totalScore = 0;
        var maxPossibleScore = questions.Sum(q => q.Points);

        foreach (var question in questions)
        {
            var userAnswer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var questionScore = await CalculateQuestionScoreAsync(question, userAnswer?.UserAnswer ?? string.Empty);
            var isCorrect = await ValidateAnswerAsync(question, userAnswer?.UserAnswer ?? string.Empty);

            var questionResult = new QuestionScoreResult
            {
                QuestionId = question.Id,
                IsCorrect = isCorrect,
                PointsEarned = questionScore,
                MaxPoints = question.Points,
                PartialScorePercentage = (double)questionScore / question.Points * 100,
                Feedback = await GenerateFeedbackAsync(question, userAnswer?.UserAnswer ?? string.Empty, isCorrect)
            };

            result.QuestionScores[question.Id] = questionResult;
            totalScore += questionScore;

            // Update the question answer entity
            if (userAnswer != null)
            {
                userAnswer.SetCorrect(isCorrect, questionScore);
                userAnswer.AddFeedback(questionResult.Feedback);
            }
        }

        result.TotalScore = totalScore;
        result.MaxPossibleScore = maxPossibleScore;
        result.ScorePercentage = maxPossibleScore > 0 ? (double)totalScore / maxPossibleScore * 100 : 0;
        result.IsPassed = result.ScorePercentage >= GetPassingScore(attempt);
        result.CalculationTime = DateTime.UtcNow - startTime;

        return result;
    }

    public async Task<int> CalculateQuestionScoreAsync(Question question, string userAnswer)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
            return 0;

        return question.Type switch
        {
            QuestionType.MultipleChoice => await CalculateMultipleChoiceScoreAsync(question, userAnswer),
            QuestionType.TrueFalse => await CalculateTrueFalseScoreAsync(question, userAnswer),
            QuestionType.ShortAnswer => await CalculateShortAnswerScoreAsync(question, userAnswer),
            QuestionType.Essay => await CalculateEssayScoreAsync(question, userAnswer),
            QuestionType.FillInTheBlank => await CalculateFillInBlankScoreAsync(question, userAnswer),
            QuestionType.Matching => await CalculateMatchingScoreAsync(question, userAnswer),
            QuestionType.Ordering => await CalculateOrderingScoreAsync(question, userAnswer),
            _ => 0
        };
    }

    public async Task<bool> ValidateAnswerAsync(Question question, string userAnswer)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
            return false;

        return question.ValidateAnswer(userAnswer);
    }

    public async Task<ExerciseScoreResult> RecalculateScoreAsync(ExerciseAttempt attempt, IReadOnlyList<Question> questions)
    {
        return await CalculateScoreAsync(attempt, questions);
    }

    private async Task<int> CalculateMultipleChoiceScoreAsync(Question question, string userAnswer)
    {
        var correctOptions = question.Options.Where(o => o.IsCorrect).Select(o => o.Id.ToString()).ToList();
        var userAnswerIds = userAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Exact match for full points
        if (correctOptions.OrderBy(x => x).SequenceEqual(userAnswerIds.OrderBy(x => x)))
        {
            return question.Points;
        }

        // Partial scoring for multiple correct answers
        if (correctOptions.Count > 1)
        {
            var correctCount = userAnswerIds.Intersect(correctOptions).Count();
            var incorrectCount = userAnswerIds.Except(correctOptions).Count();
            var netCorrect = Math.Max(0, correctCount - incorrectCount);
            
            return (int)(question.Points * ((double)netCorrect / correctOptions.Count));
        }

        return 0;
    }

    private async Task<int> CalculateTrueFalseScoreAsync(Question question, string userAnswer)
    {
        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
        if (correctOption != null && userAnswer.Equals(correctOption.Id.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return question.Points;
        }
        return 0;
    }

    private async Task<int> CalculateShortAnswerScoreAsync(Question question, string userAnswer)
    {
        var correctAnswers = question.Options.Where(o => o.IsCorrect).Select(o => o.Text.Trim().ToLower()).ToList();
        var userAnswerLower = userAnswer.Trim().ToLower();

        // Exact match
        if (correctAnswers.Any(correct => correct == userAnswerLower))
        {
            return question.Points;
        }

        // Partial match (contains key words)
        var matchingWords = 0;
        var totalKeyWords = 0;

        foreach (var correctAnswer in correctAnswers)
        {
            var keyWords = correctAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2).ToList();
            
            totalKeyWords += keyWords.Count;
            matchingWords += keyWords.Count(kw => userAnswerLower.Contains(kw));
        }

        if (totalKeyWords > 0 && matchingWords > 0)
        {
            var partialScore = (double)matchingWords / totalKeyWords;
            return (int)(question.Points * partialScore);
        }

        return 0;
    }

    private async Task<int> CalculateEssayScoreAsync(Question question, string userAnswer)
    {
        // Advanced essay scoring - could integrate with AI scoring in the future
        var wordCount = userAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var sentenceCount = userAnswer.Split('.', StringSplitOptions.RemoveEmptyEntries).Length;
        
        // Basic scoring criteria
        var lengthScore = Math.Min(1.0, wordCount / 100.0); // Expect ~100 words
        var structureScore = sentenceCount >= 3 ? 1.0 : sentenceCount / 3.0; // Expect at least 3 sentences
        var keywordScore = await CalculateKeywordPresence(question, userAnswer);
        
        var overallScore = (lengthScore * 0.3 + structureScore * 0.3 + keywordScore * 0.4);
        return (int)(question.Points * overallScore);
    }

    private async Task<int> CalculateFillInBlankScoreAsync(Question question, string userAnswer)
    {
        var correctAnswers = question.Options.Where(o => o.IsCorrect).Select(o => o.Text.Trim().ToLower());
        var userAnswerLower = userAnswer.Trim().ToLower();

        if (correctAnswers.Any(correct => correct == userAnswerLower))
        {
            return question.Points;
        }

        // Check for close matches (fuzzy matching)
        foreach (var correctAnswer in correctAnswers)
        {
            var similarity = CalculateStringSimilarity(userAnswerLower, correctAnswer);
            if (similarity > 0.8) // 80% similarity
            {
                return (int)(question.Points * similarity);
            }
        }

        return 0;
    }

    private async Task<int> CalculateMatchingScoreAsync(Question question, string userAnswer)
    {
        try
        {
            var userMatches = userAnswer.Split(',')
                .Select(pair => pair.Split(':'))
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

            var correctMatches = question.Options.Where(o => o.IsCorrect)
                .ToDictionary(o => o.Id.ToString(), o => o.MatchingValue?.Trim() ?? string.Empty);

            var correctCount = userMatches.Count(um => 
                correctMatches.ContainsKey(um.Key) && 
                correctMatches[um.Key].Equals(um.Value, StringComparison.OrdinalIgnoreCase));

            return (int)(question.Points * ((double)correctCount / correctMatches.Count));
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> CalculateOrderingScoreAsync(Question question, string userAnswer)
    {
        var userOrder = userAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var correctOrder = question.Options.OrderBy(o => o.OrderIndex).Select(o => o.Id.ToString()).ToArray();

        // Exact sequence match
        if (userOrder.SequenceEqual(correctOrder))
        {
            return question.Points;
        }

        // Partial credit for correct adjacencies
        var correctAdjacencies = 0;
        var totalAdjacencies = correctOrder.Length - 1;

        for (int i = 0; i < Math.Min(userOrder.Length - 1, correctOrder.Length - 1); i++)
        {
            var userIndex1 = Array.IndexOf(userOrder, correctOrder[i]);
            var userIndex2 = Array.IndexOf(userOrder, correctOrder[i + 1]);

            if (userIndex1 >= 0 && userIndex2 >= 0 && userIndex2 == userIndex1 + 1)
            {
                correctAdjacencies++;
            }
        }

        if (totalAdjacencies > 0)
        {
            return (int)(question.Points * ((double)correctAdjacencies / totalAdjacencies));
        }

        return 0;
    }

    private async Task<double> CalculateKeywordPresence(Question question, string userAnswer)
    {
        // Extract keywords from question text and correct options
        var questionWords = ExtractKeywords(question.Text);
        var optionWords = question.Options.Where(o => o.IsCorrect)
            .SelectMany(o => ExtractKeywords(o.Text)).ToList();
        
        var allKeywords = questionWords.Concat(optionWords).Distinct().ToList();
        var userAnswerLower = userAnswer.ToLower();
        
        var presentKeywords = allKeywords.Count(kw => userAnswerLower.Contains(kw.ToLower()));
        
        return allKeywords.Count > 0 ? (double)presentKeywords / allKeywords.Count : 0;
    }

    private List<string> ExtractKeywords(string text)
    {
        // Simple keyword extraction - in production, use more sophisticated NLP
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !IsStopWord(w))
            .Select(w => w.Trim().ToLower())
            .Distinct()
            .ToList();

        return words;
    }

    private bool IsStopWord(string word)
    {
        var turkishStopWords = new HashSet<string>
        {
            "bir", "bu", "da", "de", "en", "ile", "için", "ve", "var", "olan",
            "olan", "olarak", "ancak", "fakat", "lakin", "ama", "daha", "çok"
        };

        return turkishStopWords.Contains(word.ToLower());
    }

    private double CalculateStringSimilarity(string str1, string str2)
    {
        // Simple Levenshtein distance-based similarity
        var distance = CalculateLevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        
        return maxLength > 0 ? 1.0 - (double)distance / maxLength : 1.0;
    }

    private int CalculateLevenshteinDistance(string str1, string str2)
    {
        var matrix = new int[str1.Length + 1, str2.Length + 1];

        for (int i = 0; i <= str1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= str2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= str1.Length; i++)
        {
            for (int j = 1; j <= str2.Length; j++)
            {
                int cost = str1[i - 1] == str2[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[str1.Length, str2.Length];
    }

    private double GetPassingScore(ExerciseAttempt attempt)
    {
        // This should come from the Exercise entity, default to 60%
        return 60.0;
    }

    private async Task<string?> GenerateFeedbackAsync(Question question, string userAnswer, bool isCorrect)
    {
        if (isCorrect)
        {
            return "Doğru! Tebrikler.";
        }

        return question.Type switch
        {
            QuestionType.MultipleChoice => "Yanlış cevap. Doğru seçeneği bulmak için metni tekrar gözden geçirin.",
            QuestionType.TrueFalse => "Yanlış. Bu ifadenin doğruluk değerini belirlemek için metindeki bilgileri kontrol edin.",
            QuestionType.ShortAnswer => "Eksik veya yanlış cevap. Soruyu tekrar okuyarak daha detaylı cevap vermeye çalışın.",
            QuestionType.Essay => "Cevabınız geliştirilebilir. Daha fazla detay ve örnek ekleyerek yanıtınızı zenginleştirebilirsiniz.",
            QuestionType.FillInTheBlank => "Boşluk için uygun kelime/ifade bulunamadı. Metindeki ipuçlarını takip edin.",
            QuestionType.Matching => "Eşleştirmede hata var. Her seçeneği dikkatle değerlendirin.",
            QuestionType.Ordering => "Sıralama yanlış. Olayların/bilgilerin mantıklı sırasını düşünün.",
            _ => "Cevabınızı gözden geçiriniz."
        };
    }
}
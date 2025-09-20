using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Services.ExerciseTypes;

public class SpeedReadingHandler : IExerciseTypeHandler
{
    private readonly TurkishTextAnalyzer _textAnalyzer;

    public ExerciseType SupportedType => ExerciseType.SpeedReading;

    public SpeedReadingHandler(TurkishTextAnalyzer textAnalyzer)
    {
        _textAnalyzer = textAnalyzer;
    }

    public async Task<Exercise> CreateExerciseAsync(ExerciseCreationRequest request)
    {
        var instructions = await GenerateInstructionsAsync(request.TargetEducationLevel);
        
        // Speed reading exercises have shorter time limits
        var speedTimeLimit = CalculateSpeedReadingTimeLimit(request.ReadingText.Statistics.WordCount, request.TargetEducationLevel);
        
        var exercise = new Exercise(
            request.Title,
            request.Description,
            instructions,
            ExerciseType.SpeedReading,
            request.TargetEducationLevel,
            request.DifficultyLevel,
            request.CreatedBy,
            request.ReadingText.Id);

        exercise.UpdateSettings(
            speedTimeLimit,
            100,
            request.PassingScore,
            true, // Time limited
            false, // Not randomized - sequence matters
            true,
            request.AllowRetry,
            request.MaxRetries);

        var questions = await GenerateQuestionsAsync(request);
        foreach (var question in questions)
        {
            exercise.AddQuestion(question);
        }

        return exercise;
    }

    public async Task<IReadOnlyList<Question>> GenerateQuestionsAsync(ExerciseCreationRequest request)
    {
        var questions = new List<Question>();
        var text = request.ReadingText.Content;
        var wordCount = request.ReadingText.Statistics.WordCount;
        
        // Speed reading focuses on key information extraction
        questions.Add(await CreateMainTopicQuestionAsync(request.ReadingText, 1));
        questions.Add(await CreateKeyFactsQuestionAsync(request.ReadingText, 2));
        questions.Add(await CreateConclusionQuestionAsync(request.ReadingText, 3));
        
        // Add WPM calculation metadata
        var readingTimeSeconds = CalculateSpeedReadingTimeLimit(wordCount, request.TargetEducationLevel) * 60;
        var targetWPM = (int)(wordCount / (readingTimeSeconds / 60.0));
        
        foreach (var question in questions)
        {
            var metadata = $"{{\"exercise_type\":\"speed_reading\",\"target_wpm\":{targetWPM},\"word_count\":{wordCount}}}";
            question.UpdateMetadata(metadata);
        }

        return questions;
    }

    public async Task<bool> ValidateExerciseAsync(Exercise exercise)
    {
        if (exercise.Type != ExerciseType.SpeedReading)
            return false;

        if (!exercise.Questions.Any())
            return false;

        // Speed reading should have time limits
        if (!exercise.IsTimeLimited || exercise.TimeLimit > 15) // Max 15 minutes
            return false;

        // Should focus on essential comprehension
        if (exercise.Questions.Count > 5) // Max 5 questions for speed
            return false;

        return true;
    }

    public async Task<string> GenerateInstructionsAsync(EducationCategory level)
    {
        var baseInstructions = level switch
        {
            EducationCategory.Elementary => 
                "Metni hızlı okuyun ve temel bilgileri kavramaya odaklanın. Süre sınırlıdır, ana konuyu anlamaya çalışın.",
            
            EducationCategory.MiddleSchool => 
                "Hızlı okuma tekniğini kullanarak metni tarayın. Önemli bilgileri not alın ve sorulara hızlıca cevap verin.",
            
            EducationCategory.HighSchool => 
                "Tarama ve göz atma tekniklerini kullanarak metni hızla inceleyin. Ana fikir ve önemli detayları belirleyin.",
            
            EducationCategory.University => 
                "Akademik hızlı okuma stratejilerini uygulayın. Skimming ve scanning teknikleriyle metni analiz edin.",
            
            EducationCategory.Graduate => 
                "İleri seviye hızlı okuma tekniklerini kullanın. Metinsel ipuçlarını takip ederek ana argümanları hızla belirleyin.",
            
            _ => "Metni hızlı okuyun ve temel bilgileri kavrayın."
        };

        return baseInstructions + "\n\nÖNEMLİ: Bu egzersizde okuma hızınız ölçülmektedir. Zaman sınırı içinde metni tamamlamaya çalışın.";
    }

    private int CalculateSpeedReadingTimeLimit(int wordCount, EducationCategory level)
    {
        // Target WPM by education level
        var targetWPM = level switch
        {
            EducationCategory.Elementary => 150,
            EducationCategory.MiddleSchool => 200,
            EducationCategory.HighSchool => 250,
            EducationCategory.University => 300,
            EducationCategory.Graduate => 350,
            _ => 200
        };

        // Calculate time in minutes, add 2 minutes for questions
        var readingTimeMinutes = (int)Math.Ceiling((double)wordCount / targetWPM);
        return Math.Max(3, readingTimeMinutes + 2); // Minimum 3 minutes
    }

    private async Task<Question> CreateMainTopicQuestionAsync(ReadingText text, int orderIndex)
    {
        var question = new Question(
            "Bu metnin ana konusu nedir?",
            QuestionType.MultipleChoice,
            40,
            orderIndex,
            text.Id);

        // Generate main topic from first paragraph
        var paragraphs = text.Content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var mainTopic = await ExtractMainTopic(paragraphs.FirstOrDefault() ?? text.Content);
        var distractors = await GenerateTopicDistractors(mainTopic);

        question.AddOption(new QuestionOption(mainTopic, true, 1, question.Id));
        
        for (int i = 0; i < distractors.Count && i < 3; i++)
        {
            question.AddOption(new QuestionOption(distractors[i], false, i + 2, question.Id));
        }

        return question;
    }

    private async Task<Question> CreateKeyFactsQuestionAsync(ReadingText text, int orderIndex)
    {
        var question = new Question(
            "Metinde vurgulanan önemli bilgi hangisidir?",
            QuestionType.MultipleChoice,
            30,
            orderIndex,
            text.Id);

        var keyFacts = await ExtractKeyFacts(text.Content);
        var keyFact = keyFacts.FirstOrDefault() ?? "Metinde önemli bir bilgi yer almaktadır.";
        var distractors = await GenerateFactDistractors(keyFact);

        question.AddOption(new QuestionOption(keyFact, true, 1, question.Id));
        
        for (int i = 0; i < distractors.Count && i < 3; i++)
        {
            question.AddOption(new QuestionOption(distractors[i], false, i + 2, question.Id));
        }

        return question;
    }

    private async Task<Question> CreateConclusionQuestionAsync(ReadingText text, int orderIndex)
    {
        var question = new Question(
            "Metnin sonuç bölümünde vurgulanan nokta nedir?",
            QuestionType.MultipleChoice,
            30,
            orderIndex,
            text.Id);

        // Extract conclusion from last paragraph
        var paragraphs = text.Content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var conclusion = await ExtractConclusion(paragraphs.LastOrDefault() ?? text.Content);
        var distractors = await GenerateConclusionDistractors(conclusion);

        question.AddOption(new QuestionOption(conclusion, true, 1, question.Id));
        
        for (int i = 0; i < distractors.Count && i < 3; i++)
        {
            question.AddOption(new QuestionOption(distractors[i], false, i + 2, question.Id));
        }

        return question;
    }

    // Yardımcı metodlar
    private async Task<string> ExtractMainTopic(string text)
    {
        // Gerçek implementasyonda NLP kullanılacak
        var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var firstSentence = sentences.FirstOrDefault()?.Trim() ?? "";
        
        if (firstSentence.Length > 80)
            return firstSentence.Substring(0, 80) + "...";
        
        return firstSentence;
    }

    private async Task<List<string>> GenerateTopicDistractors(string mainTopic)
    {
        return new List<string>
        {
            "Tarihsel olayların kronolojik sıralaması",
            "Bilimsel araştırma metodları",
            "Coğrafi özelliklerin karşılaştırılması"
        };
    }

    private async Task<List<string>> ExtractKeyFacts(string text)
    {
        // Gerçek implementasyonda önemli cümleleri çıkaracak
        var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.Length > 30 && s.Length < 120)
            .Take(3)
            .ToList();

        return sentences.Any() ? sentences : new List<string> { "Önemli bir bilgi yer almaktadır." };
    }

    private async Task<List<string>> GenerateFactDistractors(string keyFact)
    {
        return new List<string>
        {
            "Bu bilgi metinde geçmemektedir.",
            "Metin bu konuda farklı bir görüş sunmaktadır.",
            "Bu ifade metnin bağlamıyla uyumlu değildir."
        };
    }

    private async Task<string> ExtractConclusion(string lastParagraph)
    {
        // Son paragraftan sonuç çıkarma
        if (lastParagraph.Contains("sonuç"))
        {
            var sentences = lastParagraph.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var conclusionSentence = sentences.FirstOrDefault(s => s.Contains("sonuç"));
            if (conclusionSentence != null)
                return conclusionSentence.Trim();
        }

        // Son cümleyi sonuç olarak al
        var allSentences = lastParagraph.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return allSentences.LastOrDefault()?.Trim() ?? "Metin bir sonuçla bitmiştir.";
    }

    private async Task<List<string>> GenerateConclusionDistractors(string conclusion)
    {
        return new List<string>
        {
            "Metnin başında belirtilen görüş tekrar edilmiştir.",
            "Yeni bir problem ortaya atılmıştır.",
            "Okuyucudan bir şey talep edilmiştir."
        };
    }
}
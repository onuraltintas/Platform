using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Services.ExerciseTypes;

public class VocabularyTestHandler : IExerciseTypeHandler
{
    private readonly TurkishTextAnalyzer _textAnalyzer;

    public ExerciseType SupportedType => ExerciseType.VocabularyTest;

    public VocabularyTestHandler(TurkishTextAnalyzer textAnalyzer)
    {
        _textAnalyzer = textAnalyzer;
    }

    public async Task<Exercise> CreateExerciseAsync(ExerciseCreationRequest request)
    {
        var instructions = await GenerateInstructionsAsync(request.TargetEducationLevel);
        
        var exercise = new Exercise(
            request.Title,
            request.Description,
            instructions,
            ExerciseType.VocabularyTest,
            request.TargetEducationLevel,
            request.DifficultyLevel,
            request.CreatedBy,
            request.ReadingText.Id);

        exercise.UpdateSettings(
            request.TimeLimit,
            100,
            request.PassingScore,
            true,
            request.IsRandomized,
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
        var keywords = await _textAnalyzer.ExtractKeywordsAsync(request.ReadingText.Content);
        var questionCount = Math.Min(request.QuestionCount, keywords.Count);
        
        // Kelime anlamı soruları (40%)
        var meaningQuestions = Math.Max(1, (int)(questionCount * 0.4));
        questions.AddRange(await CreateWordMeaningQuestionsAsync(keywords, meaningQuestions, 1));
        
        // Eş anlamlı kelime soruları (30%)
        var synonymQuestions = Math.Max(1, (int)(questionCount * 0.3));
        questions.AddRange(await CreateSynonymQuestionsAsync(keywords, synonymQuestions, meaningQuestions + 1));
        
        // Cümle içinde kullanım soruları (30%)
        var usageQuestions = questionCount - meaningQuestions - synonymQuestions;
        questions.AddRange(await CreateWordUsageQuestionsAsync(keywords, usageQuestions, meaningQuestions + synonymQuestions + 1));

        return questions;
    }

    public async Task<bool> ValidateExerciseAsync(Exercise exercise)
    {
        if (exercise.Type != ExerciseType.VocabularyTest)
            return false;

        if (!exercise.Questions.Any())
            return false;

        // Tüm sorular çoktan seçmeli olmalı
        var allMultipleChoice = exercise.Questions.All(q => q.Type == QuestionType.MultipleChoice);
        
        // Her sorunun en az 3 seçeneği olmalı
        var hasEnoughOptions = exercise.Questions.All(q => q.Options.Count >= 3);
        
        return allMultipleChoice && hasEnoughOptions;
    }

    public async Task<string> GenerateInstructionsAsync(EducationCategory level)
    {
        return level switch
        {
            EducationCategory.Elementary => 
                "Aşağıdaki kelime sorularını cevaplayın. Her soruya en uygun cevabı seçin.",
            
            EducationCategory.MiddleSchool => 
                "Metinde geçen kelimelerin anlamları ve kullanımları hakkındaki soruları cevaplayın. Kelime haznenizi genişletmek için dikkatli okuyun.",
            
            EducationCategory.HighSchool => 
                "Metindeki önemli kelimelerin anlamlarını, eş anlamlarını ve cümle içindeki kullanımlarını değerlendirin. Bağlamdan yola çıkarak en doğru cevapları bulun.",
            
            EducationCategory.University => 
                "Akademik metindeki terminoloji ve kavramları analiz edin. Kelimelerin etimolojik kökenlerini ve anlam alanlarını göz önünde bulundurun.",
            
            EducationCategory.Graduate => 
                "Uzman seviyesindeki kelime bilginizi test edin. Çok anlamlılık, mecaz kullanımları ve disiplinler arası terminolojiyi değerlendirin.",
            
            _ => "Kelime bilginizi test eden soruları cevaplayın."
        };
    }

    private async Task<IReadOnlyList<Question>> CreateWordMeaningQuestionsAsync(IReadOnlyList<string> keywords, int count, int startIndex)
    {
        var questions = new List<Question>();
        var selectedWords = keywords.Take(count).ToList();

        for (int i = 0; i < selectedWords.Count; i++)
        {
            var word = selectedWords[i];
            var question = new Question(
                $"'{word}' kelimesinin anlamı nedir?",
                QuestionType.MultipleChoice,
                20,
                startIndex + i,
                Guid.NewGuid()); // ReadingText ID'si gerçek implementasyonda set edilecek

            question.UpdateMetadata($"{{\"type\":\"word_meaning\",\"word\":\"{word}\",\"difficulty\":\"medium\"}}");

            var correctMeaning = await GetWordDefinition(word);
            var distractors = await GenerateMeaningDistractors(word, correctMeaning);

            question.AddOption(new QuestionOption(correctMeaning, true, 1, question.Id));
            
            for (int j = 0; j < distractors.Count && j < 3; j++)
            {
                question.AddOption(new QuestionOption(distractors[j], false, j + 2, question.Id));
            }

            questions.Add(question);
        }

        return questions;
    }

    private async Task<IReadOnlyList<Question>> CreateSynonymQuestionsAsync(IReadOnlyList<string> keywords, int count, int startIndex)
    {
        var questions = new List<Question>();
        var selectedWords = keywords.Skip(count).Take(count).ToList();

        for (int i = 0; i < selectedWords.Count; i++)
        {
            var word = selectedWords[i];
            var question = new Question(
                $"'{word}' kelimesinin eş anlamlısı hangisidir?",
                QuestionType.MultipleChoice,
                20,
                startIndex + i,
                Guid.NewGuid());

            question.UpdateMetadata($"{{\"type\":\"synonym\",\"word\":\"{word}\",\"difficulty\":\"medium\"}}");

            var synonyms = await GetWordSynonyms(word);
            var correctSynonym = synonyms.FirstOrDefault() ?? $"{word} ile aynı anlamlı kelime";
            var distractors = await GenerateSynonymDistractors(word, correctSynonym);

            question.AddOption(new QuestionOption(correctSynonym, true, 1, question.Id));
            
            for (int j = 0; j < distractors.Count && j < 3; j++)
            {
                question.AddOption(new QuestionOption(distractors[j], false, j + 2, question.Id));
            }

            questions.Add(question);
        }

        return questions;
    }

    private async Task<IReadOnlyList<Question>> CreateWordUsageQuestionsAsync(IReadOnlyList<string> keywords, int count, int startIndex)
    {
        var questions = new List<Question>();
        var selectedWords = keywords.Skip(count * 2).Take(count).ToList();

        for (int i = 0; i < selectedWords.Count; i++)
        {
            var word = selectedWords[i];
            var question = new Question(
                $"'{word}' kelimesi hangi cümlede doğru kullanılmıştır?",
                QuestionType.MultipleChoice,
                20,
                startIndex + i,
                Guid.NewGuid());

            question.UpdateMetadata($"{{\"type\":\"word_usage\",\"word\":\"{word}\",\"difficulty\":\"hard\"}}");

            var correctUsage = await GenerateCorrectUsage(word);
            var incorrectUsages = await GenerateIncorrectUsages(word);

            question.AddOption(new QuestionOption(correctUsage, true, 1, question.Id, "Bu kullanım gramer ve anlam açısından doğrudur."));
            
            for (int j = 0; j < incorrectUsages.Count && j < 3; j++)
            {
                question.AddOption(new QuestionOption(incorrectUsages[j], false, j + 2, question.Id));
            }

            questions.Add(question);
        }

        return questions;
    }

    // Yardımcı metodlar - gerçek implementasyonda sözlük API'si kullanılacak
    private async Task<string> GetWordDefinition(string word)
    {
        // Basit sözlük tanımları - gerçek implementasyonda TDK API'si kullanılacak
        var definitions = new Dictionary<string, string>
        {
            ["gelişim"] = "Büyüme, ilerleme, gelişme süreci",
            ["teknoloji"] = "Bilimsel bilgilerin pratik amaçlarla kullanılması",
            ["eğitim"] = "Bilgi ve beceri kazandırma süreci",
            ["kültür"] = "Bir toplumun yaşayış biçimi ve değerleri",
            ["sanat"] = "Güzel ve estetik olan şeyleri yaratma etkinliği"
        };

        return definitions.GetValueOrDefault(word.ToLower(), $"{word} - önemli bir kavram");
    }

    private async Task<List<string>> GenerateMeaningDistractors(string word, string correct)
    {
        return new List<string>
        {
            "Geçici bir durumu ifade eden kavram",
            "Sayısal bir değeri belirten terim",
            "Coğrafi bir konumu gösteren ifade"
        };
    }

    private async Task<List<string>> GetWordSynonyms(string word)
    {
        var synonyms = new Dictionary<string, List<string>>
        {
            ["gelişim"] = new() { "ilerleme", "büyüme", "gelişme" },
            ["teknoloji"] = new() { "teknik", "bilim", "uygulama" },
            ["eğitim"] = new() { "öğretim", "talim", "tedris" },
            ["kültür"] = new() { "medeniyet", "uygarlık", "irfan" },
            ["sanat"] = new() { "güzel sanatlar", "estetik", "yaratıcılık" }
        };

        return synonyms.GetValueOrDefault(word.ToLower(), new List<string> { $"{word} benzeri" });
    }

    private async Task<List<string>> GenerateSynonymDistractors(string word, string correct)
    {
        return new List<string>
        {
            "Tamamen farklı anlam",
            "Zıt anlam ifadesi",
            "İlgisiz kavram"
        };
    }

    private async Task<string> GenerateCorrectUsage(string word)
    {
        var usages = new Dictionary<string, string>
        {
            ["gelişim"] = "Çocuğun zihinsel gelişimi çok hızlı ilerliyor.",
            ["teknoloji"] = "Modern teknoloji hayatımızı kolaylaştırıyor.",
            ["eğitim"] = "Kaliteli eğitim toplumun temelidir.",
            ["kültür"] = "Her ülkenin kendine özgü kültürü vardır.",
            ["sanat"] = "Sanat insanın ruhsal ihtiyaçlarını karşılar."
        };

        return usages.GetValueOrDefault(word.ToLower(), $"{word} kavramı önemli bir yere sahiptir.");
    }

    private async Task<List<string>> GenerateIncorrectUsages(string word)
    {
        return new List<string>
        {
            $"{word} sayısı çok fazla artmıştır.", // Yanlış bağlam
            $"{word} rengini çok beğendim.", // Anlamsız kullanım
            $"{word} kilosu ne kadar?", // Uyumsuz kullanım
        };
    }
}
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Services;

public class AutomaticQuestionGenerationService
{
    private readonly TurkishTextAnalyzer _textAnalyzer;
    private readonly Random _random = new();

    public AutomaticQuestionGenerationService(TurkishTextAnalyzer textAnalyzer)
    {
        _textAnalyzer = textAnalyzer;
    }

    public async Task<IReadOnlyList<Question>> GenerateQuestionsFromTextAsync(
        ReadingText readingText, 
        int questionCount, 
        EducationCategory targetLevel,
        QuestionGenerationOptions? options = null)
    {
        options ??= new QuestionGenerationOptions();
        var questions = new List<Question>();
        var text = readingText.Content;
        
        // Text analysis
        var sentences = _textAnalyzer.TokenizeSentences(text).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var keywords = await _textAnalyzer.ExtractKeywordsAsync(text, Math.Max(10, questionCount * 2));
        var words = _textAnalyzer.TokenizeWords(text);
        
        // Question distribution based on options
        var distribution = CalculateQuestionDistribution(questionCount, options, targetLevel);
        
        int questionIndex = 1;
        
        // Generate main idea questions
        for (int i = 0; i < distribution.MainIdeaCount && questions.Count < questionCount; i++)
        {
            var question = await GenerateMainIdeaQuestionAsync(text, sentences, paragraphs, questionIndex++, readingText.Id);
            if (question != null) questions.Add(question);
        }
        
        // Generate detail questions
        for (int i = 0; i < distribution.DetailCount && questions.Count < questionCount; i++)
        {
            var question = await GenerateDetailQuestionAsync(sentences, keywords, questionIndex++, readingText.Id);
            if (question != null) questions.Add(question);
        }
        
        // Generate inference questions
        for (int i = 0; i < distribution.InferenceCount && questions.Count < questionCount; i++)
        {
            var question = await GenerateInferenceQuestionAsync(paragraphs, sentences, questionIndex++, readingText.Id);
            if (question != null) questions.Add(question);
        }
        
        // Generate vocabulary questions
        for (int i = 0; i < distribution.VocabularyCount && questions.Count < questionCount; i++)
        {
            var question = await GenerateVocabularyQuestionAsync(keywords, text, questionIndex++, readingText.Id);
            if (question != null) questions.Add(question);
        }
        
        // Generate cause-effect questions
        for (int i = 0; i < distribution.CauseEffectCount && questions.Count < questionCount; i++)
        {
            var question = await GenerateCauseEffectQuestionAsync(sentences, questionIndex++, readingText.Id);
            if (question != null) questions.Add(question);
        }
        
        // Fill remaining with mixed questions if needed
        while (questions.Count < questionCount)
        {
            var questionType = _random.Next(1, 6);
            Question? question = questionType switch
            {
                1 => await GenerateMainIdeaQuestionAsync(text, sentences, paragraphs, questionIndex++, readingText.Id),
                2 => await GenerateDetailQuestionAsync(sentences, keywords, questionIndex++, readingText.Id),
                3 => await GenerateInferenceQuestionAsync(paragraphs, sentences, questionIndex++, readingText.Id),
                4 => await GenerateVocabularyQuestionAsync(keywords, text, questionIndex++, readingText.Id),
                5 => await GenerateCauseEffectQuestionAsync(sentences, questionIndex++, readingText.Id),
                _ => null
            };
            
            if (question != null) questions.Add(question);
            else break; // Prevent infinite loop if can't generate more questions
        }
        
        return questions.Take(questionCount).ToArray();
    }

    private QuestionDistribution CalculateQuestionDistribution(int totalQuestions, QuestionGenerationOptions options, EducationCategory level)
    {
        var distribution = new QuestionDistribution();
        
        // Adjust distribution based on education level
        var (mainIdeaRatio, detailRatio, inferenceRatio, vocabularyRatio, causeEffectRatio) = level switch
        {
            EducationCategory.Elementary => (0.4, 0.4, 0.1, 0.1, 0.0),
            EducationCategory.MiddleSchool => (0.3, 0.3, 0.2, 0.1, 0.1),
            EducationCategory.HighSchool => (0.25, 0.25, 0.25, 0.15, 0.1),
            EducationCategory.University => (0.2, 0.2, 0.3, 0.2, 0.1),
            EducationCategory.Graduate => (0.15, 0.15, 0.35, 0.25, 0.1),
            _ => (0.3, 0.3, 0.2, 0.15, 0.05)
        };
        
        // Apply custom ratios if provided
        if (options.CustomRatios != null)
        {
            var custom = options.CustomRatios;
            mainIdeaRatio = custom.MainIdeaRatio ?? mainIdeaRatio;
            detailRatio = custom.DetailRatio ?? detailRatio;
            inferenceRatio = custom.InferenceRatio ?? inferenceRatio;
            vocabularyRatio = custom.VocabularyRatio ?? vocabularyRatio;
            causeEffectRatio = custom.CauseEffectRatio ?? causeEffectRatio;
        }
        
        distribution.MainIdeaCount = Math.Max(1, (int)(totalQuestions * mainIdeaRatio));
        distribution.DetailCount = Math.Max(1, (int)(totalQuestions * detailRatio));
        distribution.InferenceCount = Math.Max(0, (int)(totalQuestions * inferenceRatio));
        distribution.VocabularyCount = Math.Max(0, (int)(totalQuestions * vocabularyRatio));
        distribution.CauseEffectCount = Math.Max(0, (int)(totalQuestions * causeEffectRatio));
        
        // Adjust if total exceeds target
        var currentTotal = distribution.MainIdeaCount + distribution.DetailCount + 
                          distribution.InferenceCount + distribution.VocabularyCount + 
                          distribution.CauseEffectCount;
        
        if (currentTotal > totalQuestions)
        {
            var excess = currentTotal - totalQuestions;
            // Reduce from least important categories first
            if (distribution.CauseEffectCount > 0 && excess > 0)
            {
                var reduce = Math.Min(distribution.CauseEffectCount, excess);
                distribution.CauseEffectCount -= reduce;
                excess -= reduce;
            }
            if (distribution.VocabularyCount > 0 && excess > 0)
            {
                var reduce = Math.Min(distribution.VocabularyCount, excess);
                distribution.VocabularyCount -= reduce;
                excess -= reduce;
            }
            if (distribution.InferenceCount > 0 && excess > 0)
            {
                var reduce = Math.Min(distribution.InferenceCount, excess);
                distribution.InferenceCount -= reduce;
                excess -= reduce;
            }
        }
        
        return distribution;
    }

    private async Task<Question?> GenerateMainIdeaQuestionAsync(string text, string[] sentences, string[] paragraphs, int orderIndex, Guid readingTextId)
    {
        var questionTexts = new[]
        {
            "Bu metnin ana fikri nedir?",
            "Yazarın temel amacı nedir?",
            "Metnin genel konusu aşağıdakilerden hangisidir?",
            "Bu metinde esas olarak ne anlatılmaktadır?"
        };
        
        var questionText = questionTexts[_random.Next(questionTexts.Length)];
        
        var question = new Question(questionText, QuestionType.MultipleChoice, 25, orderIndex, readingTextId);
        question.UpdateMetadata("{\"type\":\"main_idea\",\"difficulty\":\"medium\",\"auto_generated\":true}");
        
        // Generate correct answer from first paragraph or opening sentences
        var mainIdea = await ExtractMainIdeaAsync(text, paragraphs, sentences);
        var distractors = await GenerateMainIdeaDistractorsAsync(text, mainIdea);
        
        // Shuffle options
        var allOptions = new List<(string text, bool isCorrect)> { (mainIdea, true) };
        allOptions.AddRange(distractors.Select(d => (d, false)));
        allOptions = allOptions.OrderBy(x => _random.Next()).ToList();
        
        for (int i = 0; i < allOptions.Count; i++)
        {
            var (optionText, isCorrect) = allOptions[i];
            var explanation = isCorrect ? "Bu seçenek metnin ana fikrine en uygun cevaptır." : null;
            question.AddOption(new QuestionOption(optionText, isCorrect, i + 1, question.Id, explanation));
        }
        
        return question.Options.Any(o => o.IsCorrect) ? question : null;
    }

    private async Task<Question?> GenerateDetailQuestionAsync(string[] sentences, string[] keywords, int orderIndex, Guid readingTextId)
    {
        var questionTemplates = new[]
        {
            "Metne göre aşağıdakilerden hangisi doğrudur?",
            "Metinde belirtildiğine göre {0} nedir?",
            "Yazar {0} hakkında ne söylemiştir?",
            "Metne göre {0} ile ilgili bilgi hangisidir?"
        };
        
        // Find a sentence with specific details
        var detailSentences = sentences.Where(s => s.Length > 50 && s.Length < 150 && 
            keywords.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase))).ToArray();
        
        if (!detailSentences.Any()) return null;
        
        var selectedSentence = detailSentences[_random.Next(detailSentences.Length)];
        var keyword = keywords.FirstOrDefault(k => selectedSentence.Contains(k, StringComparison.OrdinalIgnoreCase)) ?? "konu";
        
        var questionText = _random.Next(4) == 0 && keyword != "konu" 
            ? string.Format(questionTemplates[_random.Next(1, 4)], keyword)
            : questionTemplates[0];
        
        var question = new Question(questionText, QuestionType.MultipleChoice, 20, orderIndex, readingTextId);
        question.UpdateMetadata("{\"type\":\"detail\",\"difficulty\":\"easy\",\"auto_generated\":true}");
        
        // Create correct answer from selected sentence
        var correctAnswer = await CreateDetailAnswerAsync(selectedSentence);
        var distractors = await GenerateDetailDistractorsAsync(selectedSentence, correctAnswer);
        
        // Add options
        var allOptions = new List<(string text, bool isCorrect)> { (correctAnswer, true) };
        allOptions.AddRange(distractors.Select(d => (d, false)));
        allOptions = allOptions.OrderBy(x => _random.Next()).ToList();
        
        for (int i = 0; i < allOptions.Count; i++)
        {
            var (optionText, isCorrect) = allOptions[i];
            question.AddOption(new QuestionOption(optionText, isCorrect, i + 1, question.Id));
        }
        
        return question.Options.Any(o => o.IsCorrect) ? question : null;
    }

    private async Task<Question?> GenerateInferenceQuestionAsync(string[] paragraphs, string[] sentences, int orderIndex, Guid readingTextId)
    {
        var questionTexts = new[]
        {
            "Bu metinden çıkarılabilecek sonuç nedir?",
            "Yazarın ima ettiği düşünce nedir?",
            "Metinden anlaşıldığına göre hangi çıkarım yapılabilir?",
            "Bu metnin satır aralarında hangi mesaj verilmektedir?"
        };
        
        var questionText = questionTexts[_random.Next(questionTexts.Length)];
        
        var question = new Question(questionText, QuestionType.MultipleChoice, 30, orderIndex, readingTextId);
        question.UpdateMetadata("{\"type\":\"inference\",\"difficulty\":\"hard\",\"auto_generated\":true}");
        
        var inference = await GenerateInferenceAsync(paragraphs, sentences);
        var distractors = await GenerateInferenceDistractorsAsync(paragraphs, inference);
        
        var allOptions = new List<(string text, bool isCorrect)> { (inference, true) };
        allOptions.AddRange(distractors.Select(d => (d, false)));
        allOptions = allOptions.OrderBy(x => _random.Next()).ToList();
        
        for (int i = 0; i < allOptions.Count; i++)
        {
            var (optionText, isCorrect) = allOptions[i];
            var explanation = isCorrect ? "Bu çıkarım metindeki bilgilerle desteklenmektedir." : null;
            question.AddOption(new QuestionOption(optionText, isCorrect, i + 1, question.Id, explanation));
        }
        
        return question.Options.Any(o => o.IsCorrect) ? question : null;
    }

    private async Task<Question?> GenerateVocabularyQuestionAsync(string[] keywords, string text, int orderIndex, Guid readingTextId)
    {
        if (!keywords.Any()) return null;
        
        var selectedWord = keywords[_random.Next(keywords.Length)];
        var questionText = $"Metinde geçen '{selectedWord}' kelimesinin anlamı nedir?";
        
        var question = new Question(questionText, QuestionType.MultipleChoice, 25, orderIndex, readingTextId);
        question.UpdateMetadata($"{{\"type\":\"vocabulary\",\"word\":\"{selectedWord}\",\"difficulty\":\"medium\",\"auto_generated\":true}}");
        
        var correctMeaning = await GetWordMeaningFromContextAsync(selectedWord, text);
        var distractors = await GenerateVocabularyDistractorsAsync(selectedWord, correctMeaning);
        
        var allOptions = new List<(string text, bool isCorrect)> { (correctMeaning, true) };
        allOptions.AddRange(distractors.Select(d => (d, false)));
        allOptions = allOptions.OrderBy(x => _random.Next()).ToList();
        
        for (int i = 0; i < allOptions.Count; i++)
        {
            var (optionText, isCorrect) = allOptions[i];
            question.AddOption(new QuestionOption(optionText, isCorrect, i + 1, question.Id));
        }
        
        return question.Options.Any(o => o.IsCorrect) ? question : null;
    }

    private async Task<Question?> GenerateCauseEffectQuestionAsync(string[] sentences, int orderIndex, Guid readingTextId)
    {
        // Look for sentences with cause-effect indicators
        var causeEffectWords = new[] { "nedeniyle", "sonucunda", "sebebiyle", "dolayısıyla", "böylece", "bu yüzden" };
        var relevantSentences = sentences.Where(s => causeEffectWords.Any(w => s.Contains(w, StringComparison.OrdinalIgnoreCase))).ToArray();
        
        if (!relevantSentences.Any()) return null;
        
        var questionTexts = new[]
        {
            "Metne göre hangi durum hangi sonucu doğurmuştur?",
            "Aşağıdaki neden-sonuç ilişkilerinden hangisi metinde geçmektedir?",
            "Metinde belirtilen sebep-sonuç ilişkisi nedir?"
        };
        
        var questionText = questionTexts[_random.Next(questionTexts.Length)];
        
        var question = new Question(questionText, QuestionType.MultipleChoice, 25, orderIndex, readingTextId);
        question.UpdateMetadata("{\"type\":\"cause_effect\",\"difficulty\":\"medium\",\"auto_generated\":true}");
        
        var selectedSentence = relevantSentences[_random.Next(relevantSentences.Length)];
        var correctAnswer = await ExtractCauseEffectAsync(selectedSentence);
        var distractors = await GenerateCauseEffectDistractorsAsync(correctAnswer);
        
        var allOptions = new List<(string text, bool isCorrect)> { (correctAnswer, true) };
        allOptions.AddRange(distractors.Select(d => (d, false)));
        allOptions = allOptions.OrderBy(x => _random.Next()).ToList();
        
        for (int i = 0; i < allOptions.Count; i++)
        {
            var (optionText, isCorrect) = allOptions[i];
            question.AddOption(new QuestionOption(optionText, isCorrect, i + 1, question.Id));
        }
        
        return question.Options.Any(o => o.IsCorrect) ? question : null;
    }

    // Helper methods for content generation
    private async Task<string> ExtractMainIdeaAsync(string text, string[] paragraphs, string[] sentences)
    {
        // Try to extract main idea from first paragraph or first few sentences
        var firstParagraph = paragraphs.FirstOrDefault() ?? "";
        if (firstParagraph.Length > 100)
        {
            var firstSentences = sentences.Take(2).ToArray();
            var idea = string.Join(" ", firstSentences).Trim();
            if (idea.Length > 200) idea = idea.Substring(0, 200) + "...";
            return $"Metin {idea.ToLower()} konusunu ele almaktadır.";
        }
        
        return "Metnin genel teması belirtilen konu etrafında şekillenmektedir.";
    }

    private async Task<List<string>> GenerateMainIdeaDistractorsAsync(string text, string mainIdea)
    {
        return new List<string>
        {
            "Metin tamamen farklı bir konuyu işlemektedir.",
            "Yazar sadece kişisel görüşlerini paylaşmaktadır.",
            "Metin bilimsel bir araştırma raporu niteliğindedir.",
            "Tarihsel olayların kronolojik bir sunumu yapılmaktadır."
        }.OrderBy(x => _random.Next()).Take(3).ToList();
    }

    private async Task<string> CreateDetailAnswerAsync(string sentence)
    {
        // Extract key information from sentence
        if (sentence.Length > 120)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var keyPart = string.Join(" ", words.Take(Math.Min(15, words.Length)));
            return keyPart.TrimEnd('.', ',', ';') + " bilgisi verilmektedir.";
        }
        
        return sentence.TrimEnd('.', ',', ';') + ".";
    }

    private async Task<List<string>> GenerateDetailDistractorsAsync(string sentence, string correctAnswer)
    {
        return new List<string>
        {
            "Bu bilgi metinde yer almamaktadır.",
            "Metin bu konuda farklı bir bilgi sunmaktadır.",
            "Verilen ifade metnin genel amacıyla çelişmektedir.",
            "Bu detay metinde açıkça belirtilmemiştir."
        }.OrderBy(x => _random.Next()).Take(3).ToList();
    }

    private async Task<string> GenerateInferenceAsync(string[] paragraphs, string[] sentences)
    {
        var inferences = new[]
        {
            "Konuyla ilgili daha fazla araştırma yapılması gerekmektedir.",
            "Bu durumun gelecekte farklı sonuçlar doğurabileceği anlaşılmaktadır.",
            "Yazarın konuya yaklaşımı olumlu bir bakış açısını yansıtmaktadır.",
            "Bu gelişmelerin toplum üzerinde önemli etkileri olacağı düşünülmektedir.",
            "Konu hakkında farklı görüşlerin var olduğu anlaşılmaktadır."
        };
        
        return inferences[_random.Next(inferences.Length)];
    }

    private async Task<List<string>> GenerateInferenceDistractorsAsync(string[] paragraphs, string inference)
    {
        return new List<string>
        {
            "Konu hakkında kesin bilgiler mevcuttur ve tartışmaya gerek yoktur.",
            "Yazarın görüşü konunun tartışmasız olduğu yönündedir.",
            "Bu durumun herhangi bir gelişime açık olmadığı belirtilmektedir.",
            "Metinde verilen bilgiler kesin sonuçlar ortaya koymaktadır."
        }.OrderBy(x => _random.Next()).Take(3).ToList();
    }

    private async Task<string> GetWordMeaningFromContextAsync(string word, string text)
    {
        // Simple context-based meaning extraction
        var wordMeanings = new Dictionary<string, string>
        {
            ["gelişim"] = "İlerleme, büyüme süreci",
            ["teknoloji"] = "Bilimsel bilgilerin uygulanması",
            ["eğitim"] = "Öğretme ve öğrenme süreci",
            ["kültür"] = "Toplumsal yaşam biçimi ve değerler",
            ["sanat"] = "Estetik yaratıcılık faaliyeti",
            ["bilim"] = "Sistemli araştırma ve inceleme",
            ["toplum"] = "İnsanların oluşturduğu sosyal yapı",
            ["çevre"] = "Doğal ve sosyal ortam",
            ["ekonomi"] = "Üretim ve tüketim sistemi",
            ["siyaset"] = "Toplumsal yönetim sanatı"
        };
        
        return wordMeanings.GetValueOrDefault(word.ToLower(), $"Bağlamda kullanılan {word} kavramının anlamı");
    }

    private async Task<List<string>> GenerateVocabularyDistractorsAsync(string word, string correctMeaning)
    {
        return new List<string>
        {
            "Geçici bir durumu ifade eden terim",
            "Sayısal değerleri gösteren kavram",
            "Coğrafi konumları belirten ifade",
            "Zaman dilimlerini ifade eden kelime"
        }.OrderBy(x => _random.Next()).Take(3).ToList();
    }

    private async Task<string> ExtractCauseEffectAsync(string sentence)
    {
        // Simplified cause-effect extraction
        if (sentence.Contains("nedeniyle", StringComparison.OrdinalIgnoreCase))
        {
            return "Belirtilen neden, açıklanan sonucu doğurmuştur.";
        }
        if (sentence.Contains("sonucunda", StringComparison.OrdinalIgnoreCase))
        {
            return "Önceki durum, metinde açıklanan sonucu ortaya çıkarmıştır.";
        }
        
        return "Metinde belirtilen faktör, açıklanan duruma yol açmıştır.";
    }

    private async Task<List<string>> GenerateCauseEffectDistractorsAsync(string correctAnswer)
    {
        return new List<string>
        {
            "İki durum arasında herhangi bir ilişki bulunmamaktadır.",
            "Sonuç, sebep olmaksızın kendiliğinden ortaya çıkmıştır.",
            "Metinde neden-sonuç ilişkisi açıkça belirtilmemiştir.",
            "Bahsedilen durumlar birbirinden bağımsız gelişmiştir."
        }.OrderBy(x => _random.Next()).Take(3).ToList();
    }
}

// Supporting classes
public class QuestionGenerationOptions
{
    public bool PreferMultipleChoice { get; set; } = true;
    public bool IncludeExplanations { get; set; } = true;
    public int MinOptionsPerQuestion { get; set; } = 4;
    public int MaxOptionsPerQuestion { get; set; } = 4;
    public QuestionRatios? CustomRatios { get; set; }
    public string[]? ExcludeQuestionTypes { get; set; }
    public string[]? FocusKeywords { get; set; }
    public bool AllowDuplicateKeywords { get; set; } = false;
}

public class QuestionRatios
{
    public double? MainIdeaRatio { get; set; }
    public double? DetailRatio { get; set; }
    public double? InferenceRatio { get; set; }
    public double? VocabularyRatio { get; set; }
    public double? CauseEffectRatio { get; set; }
}

public class QuestionDistribution
{
    public int MainIdeaCount { get; set; }
    public int DetailCount { get; set; }
    public int InferenceCount { get; set; }
    public int VocabularyCount { get; set; }
    public int CauseEffectCount { get; set; }
    
    public int TotalCount => MainIdeaCount + DetailCount + InferenceCount + VocabularyCount + CauseEffectCount;
}
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Services.ExerciseTypes;

public class ReadingComprehensionHandler : IExerciseTypeHandler
{
    private readonly TurkishTextAnalyzer _textAnalyzer;

    public ExerciseType SupportedType => ExerciseType.ReadingComprehension;

    public ReadingComprehensionHandler(TurkishTextAnalyzer textAnalyzer)
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
            ExerciseType.ReadingComprehension,
            request.TargetEducationLevel,
            request.DifficultyLevel,
            request.CreatedBy,
            request.ReadingText.Id);

        exercise.UpdateSettings(
            request.TimeLimit,
            100, // Max score will be calculated based on questions
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
        var text = request.ReadingText.Content;
        var sentences = _textAnalyzer.TokenizeSentences(text).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        
        // Ana fikir sorusu (1 adet)
        questions.Add(await CreateMainIdeaQuestionAsync(request.ReadingText, sentences, 1));
        
        // Detay soruları (2 adet)
        questions.AddRange(await CreateDetailQuestionsAsync(request.ReadingText, sentences, 2, 2));
        
        // Çıkarım sorusu (1 adet)
        questions.Add(await CreateInferenceQuestionAsync(request.ReadingText, paragraphs, 4));
        
        // Kelime bilgisi sorusu (1 adet)
        questions.Add(await CreateVocabularyQuestionAsync(request.ReadingText, 5));

        return questions;
    }

    public async Task<bool> ValidateExerciseAsync(Exercise exercise)
    {
        if (exercise.Type != ExerciseType.ReadingComprehension)
            return false;

        if (!exercise.Questions.Any())
            return false;

        // En az bir ana fikir sorusu olmalı
        var hasMainIdea = exercise.Questions.Any(q => 
            q.Metadata.Contains("main_idea") || 
            q.Text.Contains("ana fikir") || 
            q.Text.Contains("temel düşünce"));

        // Çeşitli soru türleri olmalı
        var questionTypes = exercise.Questions.Select(q => q.Type).Distinct().Count();
        
        return hasMainIdea && questionTypes >= 2;
    }

    public async Task<string> GenerateInstructionsAsync(EducationCategory level)
    {
        return level switch
        {
            EducationCategory.Elementary => 
                "Metni dikkatle okuyun ve aşağıdaki soruları cevaplayın. Her soruyu okuduktan sonra düşünerek en doğru cevabı seçin.",
            
            EducationCategory.MiddleSchool => 
                "Verilen metni dikkatlice okuyarak anlam bütünlüğünü kavrayın. Soruları cevaplarken metindeki bilgileri ve kendi düşüncelerinizi kullanın.",
            
            EducationCategory.HighSchool => 
                "Metni analitik bir şekilde okuyarak ana fikir, yan düşünceler ve sonuçları belirleyin. Soruları cevaplarken eleştirel düşünme becerilerinizi kullanın.",
            
            EducationCategory.University => 
                "Metni akademik okuma teknikleriyle inceleyerek yazarın amacını, tezini ve argümanlarını belirleyin. Sorularda verilen seçenekleri eleştirel bir gözle değerlendirin.",
            
            EducationCategory.Graduate => 
                "Metni derin analiz teknikleriyle okuyarak implicit ve explicit bilgileri ayırt edin. Interdisipliner perspektifle yaklaşarak soruları cevaplayın.",
            
            _ => "Metni dikkatle okuyun ve soruları cevaplayın."
        };
    }

    private async Task<Question> CreateMainIdeaQuestionAsync(ReadingText text, IReadOnlyList<string> sentences, int orderIndex)
    {
        var question = new Question(
            "Bu metnin ana fikri nedir?",
            QuestionType.MultipleChoice,
            25,
            orderIndex,
            text.Id);

        question.UpdateMetadata("{\"type\":\"main_idea\",\"difficulty\":\"medium\"}");

        // Ana fikir seçenekleri oluştur
        var mainIdea = await GenerateMainIdeaFromText(text.Content);
        var distractors = await GenerateMainIdeaDistractors(text.Content, mainIdea);

        question.AddOption(new QuestionOption(mainIdea, true, 1, question.Id, "Bu seçenek metnin ana fikrine en uygun cevaptır."));
        
        for (int i = 0; i < distractors.Count && i < 3; i++)
        {
            question.AddOption(new QuestionOption(distractors[i], false, i + 2, question.Id));
        }

        return question;
    }

    private async Task<IReadOnlyList<Question>> CreateDetailQuestionsAsync(ReadingText text, IReadOnlyList<string> sentences, int count, int startIndex)
    {
        var questions = new List<Question>();
        var importantSentences = sentences.Where(s => s.Length > 50).Take(count).ToList();

        for (int i = 0; i < count && i < importantSentences.Count; i++)
        {
            var sentence = importantSentences[i];
            var questionText = await GenerateDetailQuestion(sentence);
            
            var question = new Question(
                questionText,
                QuestionType.MultipleChoice,
                20,
                startIndex + i,
                text.Id);

            question.UpdateMetadata("{\"type\":\"detail\",\"difficulty\":\"easy\"}");

            var correctAnswer = await ExtractAnswerFromSentence(sentence);
            var distractors = await GenerateDetailDistractors(sentence, correctAnswer);

            question.AddOption(new QuestionOption(correctAnswer, true, 1, question.Id));
            
            for (int j = 0; j < distractors.Count && j < 3; j++)
            {
                question.AddOption(new QuestionOption(distractors[j], false, j + 2, question.Id));
            }

            questions.Add(question);
        }

        return questions;
    }

    private async Task<Question> CreateInferenceQuestionAsync(ReadingText text, string[] paragraphs, int orderIndex)
    {
        var question = new Question(
            "Metinden çıkarılabilecek sonuç nedir?",
            QuestionType.MultipleChoice,
            30,
            orderIndex,
            text.Id);

        question.UpdateMetadata("{\"type\":\"inference\",\"difficulty\":\"hard\"}");

        var inference = await GenerateInferenceFromText(text.Content);
        var distractors = await GenerateInferenceDistractors(text.Content, inference);

        question.AddOption(new QuestionOption(inference, true, 1, question.Id, "Bu çıkarım metindeki bilgilerle desteklenmektedir."));
        
        for (int i = 0; i < distractors.Count && i < 3; i++)
        {
            question.AddOption(new QuestionOption(distractors[i], false, i + 2, question.Id));
        }

        return question;
    }

    private async Task<Question> CreateVocabularyQuestionAsync(ReadingText text, int orderIndex)
    {
        var words = await _textAnalyzer.ExtractKeywordsAsync(text.Content);
        var targetWord = words.FirstOrDefault() ?? "bilinmeyen";
        
        var question = new Question(
            $"Metinde geçen '{targetWord}' kelimesinin anlamı nedir?",
            QuestionType.MultipleChoice,
            25,
            orderIndex,
            text.Id);

        question.UpdateMetadata("{\"type\":\"vocabulary\",\"difficulty\":\"medium\"}");

        var correctMeaning = await GetWordMeaning(targetWord);
        var distractors = await GenerateVocabularyDistractors(targetWord);

        question.AddOption(new QuestionOption(correctMeaning, true, 1, question.Id));
        
        for (int i = 0; i < distractors.Count && i < 3; i++)
        {
            question.AddOption(new QuestionOption(distractors[i], false, i + 2, question.Id));
        }

        return question;
    }

    // Yardımcı methodlar - gerçek implementasyonda daha sofistike olacak
    private async Task<string> GenerateMainIdeaFromText(string text)
    {
        var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return sentences.FirstOrDefault()?.Trim() + " teması üzerine kurulmuştur." ?? "Metin genel bir konu hakkındadır.";
    }

    private async Task<List<string>> GenerateMainIdeaDistractors(string text, string mainIdea)
    {
        return new List<string>
        {
            "Metnin konusu tamamen farklı bir alandadır.",
            "Yazar sadece kişisel deneyimlerini aktarmaktadır.",
            "Metin bilimsel bir araştırma raporu niteliğindedir."
        };
    }

    private async Task<string> GenerateDetailQuestion(string sentence)
    {
        return $"Metne göre aşağıdakilerden hangisi doğrudur?";
    }

    private async Task<string> ExtractAnswerFromSentence(string sentence)
    {
        return sentence.Length > 100 ? sentence.Substring(0, 100) + "..." : sentence;
    }

    private async Task<List<string>> GenerateDetailDistractors(string sentence, string correct)
    {
        return new List<string>
        {
            "Bu bilgi metinde yer almamaktadır.",
            "Metin bu konuda farklı bir bilgi vermektedir.",
            "Bu ifade metnin genel amacıyla çelişmektedir."
        };
    }

    private async Task<string> GenerateInferenceFromText(string text)
    {
        return "Metinden anlaşıldığına göre, konu hakkında daha fazla araştırma yapılması gereklidir.";
    }

    private async Task<List<string>> GenerateInferenceDistractors(string text, string inference)
    {
        return new List<string>
        {
            "Konu hakkında yeterli bilgi mevcuttur ve daha fazla araştırmaya gerek yoktur.",
            "Metin konuyla ilgili kesin sonuçlar ortaya koymaktadır.",
            "Yazarın görüşü konunun tartışmalı olmadığı yönündedir."
        };
    }

    private async Task<string> GetWordMeaning(string word)
    {
        // Gerçek implementasyonda sözlük servisi kullanılacak
        return $"{word} kelimesi önemli bir kavramı ifade eder.";
    }

    private async Task<List<string>> GenerateVocabularyDistractors(string word)
    {
        return new List<string>
        {
            "Geçici bir durumu ifade eder.",
            "Sayısal bir değeri belirtir.",
            "Coğrafi bir konumu gösterir."
        };
    }
}
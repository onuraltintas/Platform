using SpeedReading.Application.Interfaces;

namespace SpeedReading.Application.Services;

public class AgeGradeValidationService : IAgeGradeValidationService
{
    public bool IsCompatible(int age, int gradeLevel)
    {
        var expectedRange = GetExpectedAgeRange(gradeLevel);
        
        // ±2 yaş toleransı veriyoruz
        return age >= expectedRange.Min - 2 && age <= expectedRange.Max + 2;
    }

    public (int Min, int Max) GetExpectedAgeRange(int gradeLevel)
    {
        return gradeLevel switch
        {
            // İlkokul (1-4)
            1 => (6, 7),
            2 => (7, 8),
            3 => (8, 9),
            4 => (9, 10),
            
            // Ortaokul (5-8)
            5 => (10, 11),
            6 => (11, 12),
            7 => (12, 13),
            8 => (13, 14),
            
            // Lise (9-12)
            9 => (14, 15),
            10 => (15, 16),
            11 => (16, 17),
            12 => (17, 18),
            
            // Üniversite (13-16)
            13 => (18, 19),  // 1. sınıf
            14 => (19, 20),  // 2. sınıf
            15 => (20, 21),  // 3. sınıf
            16 => (21, 22),  // 4. sınıf
            
            // Lisansüstü (17-18)
            17 => (22, 30),  // Yüksek Lisans
            18 => (24, 40),  // Doktora
            
            // Yetişkin Eğitimi (19+)
            19 => (18, 65),  // Yetişkin eğitimi
            
            _ => (6, 65) // Genel yaş aralığı
        };
    }

    public int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        
        // Doğum günü henüz gelmediyse 1 yaş eksilt
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;
            
        return age;
    }

    public string GetCompatibilityMessage(int age, int gradeLevel)
    {
        var expectedRange = GetExpectedAgeRange(gradeLevel);
        
        if (IsCompatible(age, gradeLevel))
        {
            if (age >= expectedRange.Min && age <= expectedRange.Max)
            {
                return "Yaş ve sınıf seviyesi uyumlu.";
            }
            else
            {
                return $"Yaş ve sınıf seviyesi kabul edilebilir aralıkta. (Beklenen: {expectedRange.Min}-{expectedRange.Max} yaş)";
            }
        }
        else
        {
            if (age < expectedRange.Min - 2)
            {
                return $"Bu sınıf seviyesi için yaş çok küçük görünüyor. (Beklenen: {expectedRange.Min}-{expectedRange.Max} yaş)";
            }
            else
            {
                return $"Bu sınıf seviyesi için yaş büyük görünüyor. (Beklenen: {expectedRange.Min}-{expectedRange.Max} yaş)";
            }
        }
    }
}
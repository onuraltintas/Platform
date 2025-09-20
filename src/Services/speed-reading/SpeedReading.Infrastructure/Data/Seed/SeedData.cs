using Microsoft.EntityFrameworkCore;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Infrastructure.Data.Seed;

public static class SeedData
{
    public static async Task SeedAsync(SpeedReadingDbContext context)
    {
        if (!await context.Cities.AnyAsync())
        {
            await SeedCitiesAndDistricts(context);
        }

        if (!await context.EducationLevels.AnyAsync())
        {
            await SeedEducationLevels(context);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCitiesAndDistricts(SpeedReadingDbContext context)
    {
        var cities = new List<City>
        {
            new(1, "İstanbul", "34", "Marmara"),
            new(6, "Ankara", "06", "İç Anadolu"),
            new(35, "İzmir", "35", "Ege"),
            new(7, "Antalya", "07", "Akdeniz"),
            new(16, "Bursa", "16", "Marmara"),
            new(33, "Mersin", "33", "Akdeniz"),
            new(26, "Eskişehir", "26", "İç Anadolu"),
            new(38, "Kayseri", "38", "İç Anadolu"),
            new(61, "Trabzon", "61", "Karadeniz"),
            new(55, "Samsun", "55", "Karadeniz")
        };

        var districts = new List<District>
        {
            // İstanbul
            new(1, 1, "Kadıköy"),
            new(2, 1, "Üsküdar"),
            new(3, 1, "Beşiktaş"),
            new(4, 1, "Şişli"),
            new(5, 1, "Beyoğlu"),
            
            // Ankara
            new(6, 6, "Çankaya"),
            new(7, 6, "Keçiören"),
            new(8, 6, "Yenimahalle"),
            new(9, 6, "Mamak"),
            new(10, 6, "Sincan"),
            
            // İzmir
            new(11, 35, "Konak"),
            new(12, 35, "Karşıyaka"),
            new(13, 35, "Bornova"),
            new(14, 35, "Buca"),
            new(15, 35, "Balçova"),
            
            // Diğer şehirlerin merkez ilçeleri
            new(16, 7, "Muratpaşa"),
            new(17, 16, "Osmangazi"),
            new(18, 33, "Mezitli"),
            new(19, 26, "Odunpazarı"),
            new(20, 38, "Melikgazi")
        };

        context.Cities.AddRange(cities);
        context.Districts.AddRange(districts);
        
        // Add districts to cities
        foreach (var district in districts)
        {
            var city = cities.First(c => c.Id == district.CityId);
            city.AddDistrict(district);
        }
    }

    private static async Task SeedEducationLevels(SpeedReadingDbContext context)
    {
        var educationLevels = new List<EducationLevel>
        {
            // İlkokul (1-4)
            new(1, "1. Sınıf", EducationCategory.Elementary, 1, "6-7 yaş"),
            new(2, "2. Sınıf", EducationCategory.Elementary, 2, "7-8 yaş"),
            new(3, "3. Sınıf", EducationCategory.Elementary, 3, "8-9 yaş"),
            new(4, "4. Sınıf", EducationCategory.Elementary, 4, "9-10 yaş"),
            
            // Ortaokul (5-8)
            new(5, "5. Sınıf", EducationCategory.MiddleSchool, 5, "10-11 yaş"),
            new(6, "6. Sınıf", EducationCategory.MiddleSchool, 6, "11-12 yaş"),
            new(7, "7. Sınıf", EducationCategory.MiddleSchool, 7, "12-13 yaş"),
            new(8, "8. Sınıf", EducationCategory.MiddleSchool, 8, "13-14 yaş"),
            
            // Lise (9-12)
            new(9, "9. Sınıf", EducationCategory.HighSchool, 9, "14-15 yaş"),
            new(10, "10. Sınıf", EducationCategory.HighSchool, 10, "15-16 yaş"),
            new(11, "11. Sınıf", EducationCategory.HighSchool, 11, "16-17 yaş"),
            new(12, "12. Sınıf", EducationCategory.HighSchool, 12, "17-18 yaş"),
            
            // Üniversite (13-16)
            new(13, "Üniversite 1. Yıl", EducationCategory.University, 13, "18-19 yaş"),
            new(14, "Üniversite 2. Yıl", EducationCategory.University, 14, "19-20 yaş"),
            new(15, "Üniversite 3. Yıl", EducationCategory.University, 15, "20-21 yaş"),
            new(16, "Üniversite 4. Yıl", EducationCategory.University, 16, "21-22 yaş"),
            
            // Lisansüstü (17+)
            new(17, "Yüksek Lisans", EducationCategory.Graduate, 17, "22+ yaş"),
            new(18, "Doktora", EducationCategory.Graduate, 18, "24+ yaş"),
            
            // Yetişkin Eğitimi
            new(19, "Yetişkin Eğitimi", EducationCategory.Adult, 19, "18+ yaş")
        };

        context.EducationLevels.AddRange(educationLevels);
    }
}
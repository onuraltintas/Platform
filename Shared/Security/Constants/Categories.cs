namespace EgitimPlatform.Shared.Security.Constants;

public static class Categories
{
    // Eğitim Kategorileri
    public const string SpeedReading = "SpeedReading";          // Hızlı Okuma
    public const string Mathematics = "Mathematics";             // Matematik
    public const string Science = "Science";                    // Fen Bilgisi
    public const string Language = "Language";                  // Dil Eğitimi
    public const string Programming = "Programming";             // Programlama
    public const string PersonalDevelopment = "PersonalDevelopment"; // Kişisel Gelişim
    public const string ArtAndDesign = "ArtAndDesign";         // Sanat ve Tasarım
    public const string Music = "Music";                        // Müzik
    public const string Sports = "Sports";                      // Spor
    public const string Business = "Business";                  // İş Dünyası
    public const string Health = "Health";                      // Sağlık
    public const string Technology = "Technology";              // Teknoloji
    
    // Seviye Kategorileri
    public const string Beginner = "Beginner";                  // Başlangıç
    public const string Intermediate = "Intermediate";          // Orta
    public const string Advanced = "Advanced";                  // İleri
    public const string Expert = "Expert";                      // Uzman
    
    // Yaş Kategorileri
    public const string Children = "Children";                  // Çocuk (6-12)
    public const string Teenager = "Teenager";                  // Genç (13-17)
    public const string Adult = "Adult";                        // Yetişkin (18+)
    public const string Senior = "Senior";                      // Yaşlı (65+)
    
    // Özel Kategoriler
    public const string Premium = "Premium";                    // Premium Üye
    public const string VIP = "VIP";                           // VIP Üye
    public const string Trial = "Trial";                       // Deneme Sürümü
    public const string Enterprise = "Enterprise";              // Kurumsal
    
    public static IEnumerable<string> GetAllCategories()
    {
        return new[]
        {
            // Eğitim Kategorileri
            SpeedReading,
            Mathematics,
            Science,
            Language,
            Programming,
            PersonalDevelopment,
            ArtAndDesign,
            Music,
            Sports,
            Business,
            Health,
            Technology,
            
            // Seviye Kategorileri
            Beginner,
            Intermediate,
            Advanced,
            Expert,
            
            // Yaş Kategorileri
            Children,
            Teenager,
            Adult,
            Senior,
            
            // Özel Kategoriler
            Premium,
            VIP,
            Trial,
            Enterprise
        };
    }
    
    public static IEnumerable<string> GetEducationCategories()
    {
        return new[]
        {
            SpeedReading,
            Mathematics,
            Science,
            Language,
            Programming,
            PersonalDevelopment,
            ArtAndDesign,
            Music,
            Sports,
            Business,
            Health,
            Technology
        };
    }
    
    public static IEnumerable<string> GetLevelCategories()
    {
        return new[]
        {
            Beginner,
            Intermediate,
            Advanced,
            Expert
        };
    }
    
    public static IEnumerable<string> GetAgeCategories()
    {
        return new[]
        {
            Children,
            Teenager,
            Adult,
            Senior
        };
    }
    
    public static IEnumerable<string> GetMembershipCategories()
    {
        return new[]
        {
            Premium,
            VIP,
            Trial,
            Enterprise
        };
    }
    
    public static Dictionary<string, string> GetCategoryDescriptions()
    {
        return new Dictionary<string, string>
        {
            [SpeedReading] = "Hızlı okuma ve anlama teknikleri",
            [Mathematics] = "Matematik ve sayısal beceriler",
            [Science] = "Fen bilimleri ve doğa bilgisi",
            [Language] = "Dil öğrenimi ve iletişim becerileri",
            [Programming] = "Programlama ve yazılım geliştirme",
            [PersonalDevelopment] = "Kişisel gelişim ve yaşam becerileri",
            [ArtAndDesign] = "Sanat, tasarım ve yaratıcılık",
            [Music] = "Müzik eğitimi ve enstrüman öğrenimi",
            [Sports] = "Spor ve fiziksel aktiviteler",
            [Business] = "İş dünyası ve girişimcilik",
            [Health] = "Sağlık ve wellness",
            [Technology] = "Teknoloji ve dijital beceriler",
            
            [Beginner] = "Başlangıç seviyesi",
            [Intermediate] = "Orta seviye",
            [Advanced] = "İleri seviye",
            [Expert] = "Uzman seviye",
            
            [Children] = "6-12 yaş arası çocuklar",
            [Teenager] = "13-17 yaş arası gençler",
            [Adult] = "18+ yaş yetişkinler",
            [Senior] = "65+ yaş üstü bireyler",
            
            [Premium] = "Premium üyelik avantajları",
            [VIP] = "VIP özel hizmetler",
            [Trial] = "Deneme sürümü erişimi",
            [Enterprise] = "Kurumsal çözümler"
        };
    }
}
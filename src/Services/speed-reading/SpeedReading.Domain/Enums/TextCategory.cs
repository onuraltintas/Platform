namespace SpeedReading.Domain.Enums;

public enum TextCategory
{
    Story = 1,          // Hikaye
    Article = 2,        // Makale
    News = 3,           // Haber
    Science = 4,        // Bilim
    Literature = 5,     // Edebiyat
    History = 6,        // Tarih
    Geography = 7,      // Coğrafya
    Philosophy = 8,     // Felsefe
    Technology = 9,     // Teknoloji
    Sports = 10,        // Spor
    Art = 11,           // Sanat
    Biography = 12,     // Biyografi
    Poetry = 13,        // Şiir
    Essay = 14,         // Deneme
    Tale = 15           // Masal
}

public enum TextDifficulty
{
    VeryEasy = 1,       // Çok Kolay (6-8 yaş)
    Easy = 2,           // Kolay (9-11 yaş)
    Medium = 3,         // Orta (12-14 yaş)
    Hard = 4,           // Zor (15-17 yaş)
    VeryHard = 5,       // Çok Zor (18+ yaş)
    Expert = 6          // Uzman (Akademik)
}

public enum TextStatus
{
    Draft = 1,          // Taslak
    UnderReview = 2,    // İnceleniyor
    Approved = 3,       // Onaylandı
    Published = 4,      // Yayında
    Archived = 5        // Arşivlendi
}

public enum TextSource
{
    Original = 1,       // Özgün İçerik
    Licensed = 2,       // Lisanslı
    PublicDomain = 3,   // Kamusal Alan
    UserGenerated = 4,  // Kullanıcı İçeriği
    Educational = 5     // Eğitim Materyali
}
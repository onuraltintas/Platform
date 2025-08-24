using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ContentService.Data;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Shared.Security.Constants;
using EgitimPlatform.Shared.Auditing.Services;
using EgitimPlatform.Shared.Auditing.Models;

namespace SpeedReading.ContentService.Controllers;

[ApiController]
// [Authorize(Policy = Permissions.SpeedReading.ContentManage)] // Temporarily disabled for testing
[Route("api/v1/admin/texts")]
public class TextsAdminController : ControllerBase
{
    private readonly ContentDbContext _db;
    private readonly IAuditService _audit;

    public TextsAdminController(ContentDbContext db, IAuditService audit)
    {
        _db = db; _audit = audit;
    }

    public record TextDto(Guid TextId, string Title, string DifficultyLevel, Guid? LevelId, string? LevelName, int? WordCount, DateTime? UpdatedAt, string? Content);
    public record UpsertTextRequest(string Title, string Content, string DifficultyLevel, Guid? LevelId, string? TagsJson);

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] Guid? levelId = null, [FromQuery] string? difficultyLevel = null)
    {
        var query = _db.Texts.AsNoTracking().Where(t => !t.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Title.Contains(search));
        }
        if (levelId.HasValue)
        {
            query = query.Where(t => t.LevelId == levelId.Value);
        }
        if (!string.IsNullOrWhiteSpace(difficultyLevel))
        {
            query = query.Where(t => t.DifficultyLevel == difficultyLevel);
        }
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TextDto(
                t.TextId,
                t.Title,
                t.DifficultyLevel,
                t.LevelId,
                _db.ReadingLevels.Where(l => l.LevelId == t.LevelId).Select(l => l.LevelName).FirstOrDefault(),
                t.WordCount,
                t.UpdatedAt,
                t.Content))
            .ToListAsync();
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TextDto>> Get(Guid id)
    {
        var t = await _db.Texts.AsNoTracking().FirstOrDefaultAsync(x => x.TextId == id && !x.IsDeleted);
        if (t == null) return NotFound();
        var levelName = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == t.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new TextDto(t.TextId, t.Title, t.DifficultyLevel, t.LevelId, levelName, t.WordCount, t.UpdatedAt, t.Content));
    }

    [HttpPost]
    public async Task<ActionResult<TextDto>> Create([FromBody] UpsertTextRequest request)
    {
        var e = new TextEntity
        {
            TextId = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            DifficultyLevel = request.DifficultyLevel,
            LevelId = request.LevelId,
            WordCount = string.IsNullOrWhiteSpace(request.Content) ? 0 : request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Texts.Add(e);
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry{ EntityType = nameof(TextEntity), EntityId = e.TextId.ToString(), Action = AuditAction.Insert, NewValuesObject = new(){ ["Title"] = e.Title } });
        var levelName = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return CreatedAtAction(nameof(Get), new { id = e.TextId }, new TextDto(e.TextId, e.Title, e.DifficultyLevel, e.LevelId, levelName, e.WordCount, e.UpdatedAt, e.Content));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TextDto>> Update(Guid id, [FromBody] UpsertTextRequest request)
    {
        var e = await _db.Texts.FirstOrDefaultAsync(x => x.TextId == id && !x.IsDeleted);
        if (e == null) return NotFound();
        e.Title = request.Title;
        e.Content = request.Content;
        e.DifficultyLevel = request.DifficultyLevel;
        e.LevelId = request.LevelId;
        e.WordCount = string.IsNullOrWhiteSpace(request.Content) ? 0 : request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry{ EntityType = nameof(TextEntity), EntityId = e.TextId.ToString(), Action = AuditAction.Update, NewValuesObject = new(){ ["Title"] = e.Title } });
        var levelName2 = await _db.ReadingLevels.AsNoTracking().Where(l => l.LevelId == e.LevelId).Select(l => l.LevelName).FirstOrDefaultAsync();
        return Ok(new TextDto(e.TextId, e.Title, e.DifficultyLevel, e.LevelId, levelName2, e.WordCount, e.UpdatedAt, e.Content));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var e = await _db.Texts.FirstOrDefaultAsync(x => x.TextId == id && !x.IsDeleted);
        if (e == null) return NotFound();
        e.IsDeleted = true;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAuditAsync(new AuditEntry{ EntityType = nameof(TextEntity), EntityId = id.ToString(), Action = AuditAction.SoftDelete });
        return NoContent();
    }
}

// Test için authorization'sız controller
[ApiController]
[Route("api/test/texts")]
public class TextsTestController : ControllerBase
{
    private readonly ContentDbContext _db;

    public TextsTestController(ContentDbContext db)
    {
        _db = db;
    }

    public record TestTextDto(Guid TextId, string Title, string DifficultyLevel, int? WordCount);
    public record CreateTestTextRequest(string Title, string Content, string DifficultyLevel);

    [HttpGet]
    public async Task<ActionResult<object>> GetTexts()
    {
        var texts = await _db.Texts.AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TestTextDto(t.TextId, t.Title, t.DifficultyLevel, t.WordCount))
            .ToListAsync();
        return Ok(texts);
    }

    [HttpPost]
    public async Task<ActionResult<TestTextDto>> CreateText([FromBody] CreateTestTextRequest request)
    {
        var levelId = await _db.ReadingLevels.AsNoTracking()
            .Where(l => l.LevelName == "Yetişkin")
            .Select(l => l.LevelId)
            .FirstOrDefaultAsync();

        var text = new TextEntity
        {
            TextId = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            DifficultyLevel = request.DifficultyLevel,
            LevelId = levelId,
            WordCount = string.IsNullOrWhiteSpace(request.Content) ? 0 : 
                       request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Texts.Add(text);
        await _db.SaveChangesAsync();

        return Ok(new TestTextDto(text.TextId, text.Title, text.DifficultyLevel, text.WordCount));
    }

    [HttpPost("sample-data")]
    public async Task<ActionResult> AddSampleData()
    {
        // Level'ları kontrol et
        if (!await _db.ReadingLevels.AnyAsync())
        {
            var levels = new[]
            {
                new ReadingLevelEntity { LevelId = Guid.NewGuid(), LevelName = "Çocuk", MinAge = 9, MaxAge = 12, MinWPM = 150, MaxWPM = 220, TargetComprehension = 60 },
                new ReadingLevelEntity { LevelId = Guid.NewGuid(), LevelName = "Genç", MinAge = 12, MaxAge = 17, MinWPM = 200, MaxWPM = 350, TargetComprehension = 70 },
                new ReadingLevelEntity { LevelId = Guid.NewGuid(), LevelName = "Yetişkin", MinAge = 17, MaxAge = 99, MinWPM = 300, MaxWPM = 500, TargetComprehension = 75 }
            };
            _db.ReadingLevels.AddRange(levels);
            await _db.SaveChangesAsync();
        }

        // Mevcut test metinlerini temizle
        var existingTexts = await _db.Texts.Where(t => t.Title.Contains("Test") || t.Title.Contains("Teknoloji")).ToListAsync();
        _db.Texts.RemoveRange(existingTexts);
        await _db.SaveChangesAsync();

        // Level ID'leri al
        var childLevelId = await _db.ReadingLevels.Where(l => l.LevelName == "Çocuk").Select(l => l.LevelId).FirstAsync();
        var youthLevelId = await _db.ReadingLevels.Where(l => l.LevelName == "Genç").Select(l => l.LevelId).FirstAsync();
        var adultLevelId = await _db.ReadingLevels.Where(l => l.LevelName == "Yetişkin").Select(l => l.LevelId).FirstAsync();

        var sampleTexts = new[]
        {
            new TextEntity
            {
                TextId = Guid.NewGuid(),
                Title = "Teknoloji ve Geleceğin İnsan Yaşamına Etkisi",
                Content = @"Günümüzde teknolojinin insan yaşamına etkisi giderek artmaktadır. Yapay zeka, robotik, biyoteknoloji ve nanoteknoloji gibi alanlar hızla gelişmekte ve toplumsal yapıları derinden etkilemektedir. Bu değişim süreci sadece iş dünyasını değil, eğitim, sağlık, ulaştırma ve iletişim gibi temel yaşam alanlarını da dönüştürmektedir.

Yapay zeka teknolojileri artık günlük yaşamımızın ayrılmaz bir parçası haline gelmiştir. Akıllı telefonlarımızdaki sesli asistanlardan, sosyal medya platformlarındaki algoritmalara kadar, yapay zeka sistemleri sürekli olarak davranışlarımızı analiz etmekte ve yaşam kalitemizi artırmaya yönelik öneriler sunmaktadır. Bununla birlikte, bu teknolojilerin getirdiği kolaylıklar beraberinde yeni endişeleri de doğurmaktadır.

Eğitim alanında dijital dönüşüm süreci hızlanmıştır. Sanal sınıflar, interaktif öğrenme platformları ve kişiselleştirilmiş eğitim içerikleri, geleneksel öğretim yöntemlerini tamamlamaya başlamıştır. Öğrenciler artık zamandan ve mekandan bağımsız olarak bilgiye erişebilmekte, kendi hızlarında öğrenme fırsatı bulabilmektedir.

Sağlık sektöründe ise telemedicine, wearable teknolojiler ve gen tedavileri gibi yenilikler, hastalıkların erken teşhisini ve daha etkili tedavi yöntemlerini mümkün kılmaktadır. Hastalar artık evlerinden doktorlarıyla iletişim kurabilmekte, sürekli sağlık verilerini takip edebilmektedir.

Gelecekte teknoloji ve insan yaşamı arasındaki etkileşim daha da derinleşecektir. Bu nedenle, teknolojik okuryazarlığın artırılması, etik değerlerin korunması ve sürdürülebilir kalkınma ilkelerinin gözetilmesi büyük önem taşımaktadır.",
                DifficultyLevel = "İleri",
                LevelId = adultLevelId,
                WordCount = 274,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TextEntity
            {
                TextId = Guid.NewGuid(),
                Title = "Spor Yapmanın Faydaları ve Sağlıklı Yaşam",
                Content = @"Spor yapmak sadece eğlenceli değil, aynı zamanda sağlığımız için çok faydalıdır. Düzenli egzersiz yapan çocuklar daha güçlü, daha enerjik ve daha mutlu olurlar. Spor sayesinde vücudumuz daha sağlıklı hale gelir ve hastalıklara karşı direncimiz artar.

Futbol, basketbol, yüzme, koşu gibi spor dalları kalp ve akciğerlerimizi güçlendirir. Bu organlar vücudumuzun en önemli parçalarıdır. Kalp kan pompalar ve vücudumuzun her yerine oksijen taşır. Akciğerler ise nefes almamızı sağlar. Spor yaptığımızda kalp daha hızlı çarpar ve akciğerler daha çok çalışır.

Kaslarımız da spor sayesinde gelişir. Koştuğumuzda bacak kasları, yüzdüğümüzde kol kasları çalışır. Jimnastik yaptığımızda ise tüm vücut kasları harekete geçer. Güçlü kaslar günlük işlerimizi daha kolay yapmamızı sağlar.

Spor aynı zamanda zihnimize de iyi gelir. Hareket etmek beynimizde mutluluk hormonu denilen kimyasalları serbest bırakır. Bu yüzden spor yaptıktan sonra kendimizi daha iyi hissederiz. Stres ve üzüntü azalır, enerji seviyemiz artar.

Takım sporları yapmak arkadaşlık kurmamıza yardımcı olur. Birlikte çalışmayı, paylaşmayı ve başkalarına saygı duymayı öğreniriz. Spor yaparken dikkat etmemiz gereken önemli kurallar vardır. Öncelikle uygun spor kıyafetleri giymeliyiz.",
                DifficultyLevel = "Temel",
                LevelId = childLevelId,
                WordCount = 234,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TextEntity
            {
                TextId = Guid.NewGuid(),
                Title = "Uzay Keşifleri ve İnsanlığın Gelecekteki Hedefleri",
                Content = @"İnsanlık tarih boyunca gökyüzüne bakarak merak etmiş ve keşfetmek istemiştir. Bu merak bizi yüzyıllar önce teleskop icat etmeye, daha sonra roketler geliştirmeye ve nihayetinde uzaya çıkmaya yönlendirmiştir. Bugün geldiğimiz nokta, bilim kurgu filmlerindeki sahnelerin gerçek hayatta yaşanmasına çok yakın bir durumdur.

1969 yılında Neil Armstrong ve Buzz Aldrin Ay'a ayak basan ilk insanlar olduklarında, bu sadece Amerika için değil tüm insanlık için önemli bir başarıydı. Bu tarihi an bize uzayın keşfedilebilir olduğunu gösterdi. O günden beri uzay teknolojisi inanılmaz hızla gelişti.

Günümüzde Uluslararası Uzay İstasyonu sayesinde astronotlar uzayda aylar geçirebiliyor ve bilimsel deneyler yapabiliyor. Bu çalışmalar sadece uzay bilimi için değil, tıp, fizik ve biyoloji alanları için de çok değerli sonuçlar ortaya çıkarıyor.

Mars keşfi ise şu anda en heyecan verici projelerin başında geliyor. NASA'nın Mars'a gönderdiği geziciler, kızıl gezegenin yüzeyini inceleyerek yaşam izleri arıyor. Bu robotik keşifler gelecekte insanların Mars'a seyahat etmesi için gerekli bilgileri topluyor.

Telescop teknolojisi de büyük ilerlemeler kaydetmiştir. Hubble Uzay Teleskopu yıllardır evrenin derinliklerinden görüntüler göndererek astronomi bilimini devrimleştirdi. James Webb Uzay Teleskopu ise daha da gelişmiş özellikleri ile evrenin doğuşuna dair sorulara cevap arıyor.",
                DifficultyLevel = "Orta",
                LevelId = youthLevelId,
                WordCount = 286,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _db.Texts.AddRange(sampleTexts);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Sample data added successfully", textsAdded = sampleTexts.Length });
    }
}


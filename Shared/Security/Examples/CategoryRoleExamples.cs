using EgitimPlatform.Shared.Security.Authorization;
using EgitimPlatform.Shared.Security.Constants;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Shared.Security.Examples;

// Örnek Controller - Kategori ve Rol tabanlı yetkilendirme kullanımı
public class SpeedReadingController : ControllerBase
{
    // Sadece SpeedReading kategorisine sahip kullanıcılar erişebilir
    [Category(Categories.SpeedReading)]
    [HttpGet("lessons")]
    public IActionResult GetLessons()
    {
        return Ok("Speed reading lessons");
    }
    
    // Student rolü VE SpeedReading kategorisine sahip kullanıcılar erişebilir
    [RequireRoleAndCategory(Roles.Student, Categories.SpeedReading)]
    [HttpPost("progress")]
    public IActionResult SaveProgress()
    {
        return Ok("Progress saved");
    }
    
    // Teacher rolü VE SpeedReading kategorisine sahip kullanıcılar erişebilir
    [RequireRoleAndCategory(Roles.Teacher, Categories.SpeedReading)]
    [HttpPost("lessons")]
    public IActionResult CreateLesson()
    {
        return Ok("Lesson created");
    }
}

public class MathematicsController : ControllerBase
{
    // Mathematics veya Science kategorilerinden birine sahip kullanıcılar erişebilir
    [RequireAnyCategory(Categories.Mathematics, Categories.Science)]
    [HttpGet("formulas")]
    public IActionResult GetFormulas()
    {
        return Ok("Mathematical formulas");
    }
    
    // Teacher rolü VE Mathematics kategorisine sahip kullanıcılar erişebilir
    [RequireRoleAndCategory(Roles.Teacher, Categories.Mathematics)]
    [HttpPost("assignments")]
    public IActionResult CreateAssignment()
    {
        return Ok("Assignment created");
    }
}

public class PremiumController : ControllerBase
{
    // Premium veya VIP kategorilerinden birine sahip kullanıcılar erişebilir
    [RequireAnyCategory(Categories.Premium, Categories.VIP)]
    [HttpGet("exclusive-content")]
    public IActionResult GetExclusiveContent()
    {
        return Ok("Premium content");
    }
    
    // VIP kategorisine sahip kullanıcılar erişebilir
    [Category(Categories.VIP)]
    [HttpGet("vip-features")]
    public IActionResult GetVipFeatures()
    {
        return Ok("VIP features");
    }
}

// Örnek kullanım senaryoları
public static class CategoryRoleUsageExamples
{
    /*
     * Örnek Kullanıcı Tanımlamaları:
     * 
     * 1. Ahmet (Student):
     *    - Roles: [Student]
     *    - Categories: [SpeedReading, Mathematics, Beginner, Adult, Premium]
     *    
     * 2. Ayşe (Teacher):
     *    - Roles: [Teacher]
     *    - Categories: [SpeedReading, Mathematics, Science, Advanced, Adult]
     *    
     * 3. Mehmet (Student - VIP):
     *    - Roles: [Student]
     *    - Categories: [SpeedReading, Programming, Intermediate, Adult, VIP]
     *    
     * 4. Fatma (Content Creator):
     *    - Roles: [ContentCreator]
     *    - Categories: [ArtAndDesign, Music, Advanced, Adult]
     */
    
    public static void ExampleUsage()
    {
        /*
         * Ahmet'in erişebileceği endpoint'ler:
         * - GET /speed-reading/lessons (SpeedReading kategorisi var)
         * - POST /speed-reading/progress (Student rolü + SpeedReading kategorisi var)
         * - GET /mathematics/formulas (Mathematics kategorisi var)
         * - GET /premium/exclusive-content (Premium kategorisi var)
         * 
         * Ahmet'in erişemeyeceği endpoint'ler:
         * - POST /speed-reading/lessons (Teacher rolü yok)
         * - POST /mathematics/assignments (Teacher rolü yok)
         * - GET /premium/vip-features (VIP kategorisi yok)
         */
        
        /*
         * Ayşe'nin erişebileceği endpoint'ler:
         * - GET /speed-reading/lessons (SpeedReading kategorisi var)
         * - POST /speed-reading/lessons (Teacher rolü + SpeedReading kategorisi var)
         * - GET /mathematics/formulas (Mathematics kategorisi var)
         * - POST /mathematics/assignments (Teacher rolü + Mathematics kategorisi var)
         * 
         * Ayşe'nin erişemeyeceği endpoint'ler:
         * - POST /speed-reading/progress (Student rolü yok)
         * - GET /premium/exclusive-content (Premium/VIP kategorisi yok)
         * - GET /premium/vip-features (VIP kategorisi yok)
         */
        
        /*
         * Mehmet'in erişebileceği endpoint'ler:
         * - GET /speed-reading/lessons (SpeedReading kategorisi var)
         * - POST /speed-reading/progress (Student rolü + SpeedReading kategorisi var)
         * - GET /premium/exclusive-content (VIP kategorisi var)
         * - GET /premium/vip-features (VIP kategorisi var)
         * 
         * Mehmet'in erişemeyeceği endpoint'ler:
         * - POST /speed-reading/lessons (Teacher rolü yok)
         * - GET /mathematics/formulas (Mathematics kategorisi yok)
         * - POST /mathematics/assignments (Teacher rolü yok)
         */
    }
}
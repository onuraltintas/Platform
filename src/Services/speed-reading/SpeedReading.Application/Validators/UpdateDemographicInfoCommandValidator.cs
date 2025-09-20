using FluentValidation;
using SpeedReading.Application.Commands;

namespace SpeedReading.Application.Validators;

public class UpdateDemographicInfoCommandValidator : AbstractValidator<UpdateDemographicInfoCommand>
{
    public UpdateDemographicInfoCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID'si gereklidir.");

        RuleFor(x => x.DateOfBirth)
            .Must(BeValidBirthDate)
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Doğum tarihi geçerli olmalıdır (5-100 yaş arası).");

        RuleFor(x => x.Gender)
            .InclusiveBetween(0, 2)
            .WithMessage("Geçerli bir cinsiyet seçiniz. (0=Belirtilmemiş, 1=Erkek, 2=Kadın)");

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.City))
            .WithMessage("Şehir adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.District)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.District))
            .WithMessage("İlçe adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.GradeLevel)
            .InclusiveBetween(1, 20)
            .When(x => x.GradeLevel.HasValue)
            .WithMessage("Sınıf seviyesi 1-20 arasında olmalıdır.");

        RuleFor(x => x.SchoolType)
            .InclusiveBetween(0, 5)
            .WithMessage("Geçerli bir okul türü seçiniz.");

        // Şehir ve ilçe beraber kontrol edilmeli
        RuleFor(x => x)
            .Must(x => string.IsNullOrEmpty(x.City) || !string.IsNullOrEmpty(x.District))
            .WithMessage("Şehir seçildiğinde ilçe de seçilmelidir.")
            .WithName("District");

        RuleFor(x => x)
            .Must(x => string.IsNullOrEmpty(x.District) || !string.IsNullOrEmpty(x.City))
            .WithMessage("İlçe seçildiğinde şehir de seçilmelidir.")
            .WithName("City");
    }

    private static bool BeValidBirthDate(DateTime? birthDate)
    {
        if (!birthDate.HasValue) return true;
        
        var age = DateTime.Today.Year - birthDate.Value.Year;
        if (birthDate.Value.Date > DateTime.Today.AddYears(-age))
            age--;
            
        return age >= 5 && age <= 100;
    }
}

public class CompleteUserProfileCommandValidator : AbstractValidator<CompleteUserProfileCommand>
{
    public CompleteUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID'si gereklidir.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .WithMessage("Doğum tarihi gereklidir.")
            .Must(BeValidBirthDate)
            .WithMessage("Doğum tarihi geçerli olmalıdır (5-100 yaş arası).");

        RuleFor(x => x.Gender)
            .InclusiveBetween(1, 2)
            .WithMessage("Cinsiyet seçimi zorunludur. (1=Erkek, 2=Kadın)");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("Şehir seçimi zorunludur.")
            .MaximumLength(100)
            .WithMessage("Şehir adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.District)
            .NotEmpty()
            .WithMessage("İlçe seçimi zorunludur.")
            .MaximumLength(100)
            .WithMessage("İlçe adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.GradeLevel)
            .InclusiveBetween(1, 20)
            .WithMessage("Sınıf seviyesi 1-20 arasında olmalıdır.");

        RuleFor(x => x.SchoolType)
            .InclusiveBetween(1, 5)
            .WithMessage("Okul türü seçimi zorunludur.");

        RuleFor(x => x.TargetReadingSpeed)
            .InclusiveBetween(50, 2000)
            .When(x => x.TargetReadingSpeed.HasValue)
            .WithMessage("Hedef okuma hızı 50-2000 WPM arasında olmalıdır.");
    }

    private static bool BeValidBirthDate(DateTime birthDate)
    {
        var age = DateTime.Today.Year - birthDate.Year;
        if (birthDate.Date > DateTime.Today.AddYears(-age))
            age--;
            
        return age >= 5 && age <= 100;
    }
}

public class UpdateReadingPreferencesCommandValidator : AbstractValidator<UpdateReadingPreferencesCommand>
{
    public UpdateReadingPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID'si gereklidir.");

        RuleFor(x => x.TargetReadingSpeed)
            .InclusiveBetween(50, 2000)
            .When(x => x.TargetReadingSpeed.HasValue)
            .WithMessage("Hedef okuma hızı 50-2000 WPM arasında olmalıdır.");

        RuleFor(x => x.FontSize)
            .InclusiveBetween(8, 32)
            .WithMessage("Font boyutu 8-32 arasında olmalıdır.");

        RuleFor(x => x.LineSpacing)
            .InclusiveBetween(1.0f, 3.0f)
            .WithMessage("Satır aralığı 1.0-3.0 arasında olmalıdır.");

        RuleFor(x => x.BackgroundColor)
            .Must(BeValidHexColor)
            .WithMessage("Geçerli bir arka plan rengi kodu giriniz (örn: #FFFFFF).");

        RuleFor(x => x.TextColor)
            .Must(BeValidHexColor)
            .WithMessage("Geçerli bir metin rengi kodu giriniz (örn: #000000).");

        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .WithMessage("Tercih edilen dil belirtilmelidir.")
            .MaximumLength(10)
            .WithMessage("Dil kodu en fazla 10 karakter olabilir.");
    }

    private static bool BeValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color))
            return false;

        if (!color.StartsWith("#"))
            return false;

        if (color.Length != 7)
            return false;

        return color[1..].All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }
}
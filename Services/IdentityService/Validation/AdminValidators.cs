using FluentValidation;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using System.Linq;

namespace EgitimPlatform.Services.IdentityService.Validation;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rol adı zorunludur")
            .MinimumLength(2).WithMessage("Rol adı en az 2 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Rol adı 100 karakteri geçemez")
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Rol adı sadece harf, rakam, nokta, tire, alt çizgi ve boşluk içerebilir");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakteri geçemez")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleForEach(x => x.PermissionIds)
            .NotEmpty().WithMessage("İzin ID'si boş olamaz");
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rol adı zorunludur")
            .MinimumLength(2).WithMessage("Rol adı en az 2 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Rol adı 100 karakteri geçemez")
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Rol adı sadece harf, rakam, nokta, tire, alt çizgi ve boşluk içerebilir");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakteri geçemez")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleForEach(x => x.PermissionIds)
            .NotEmpty().WithMessage("İzin ID'si boş olamaz");
    }
}

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters long")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters")
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Category name can only contain letters, numbers, dots, hyphens, underscores and spaces");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Type)
            .MaximumLength(50).WithMessage("Type must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Type))
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Type can only contain letters, numbers, dots, hyphens, underscores and spaces");
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters long")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters")
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Category name can only contain letters, numbers, dots, hyphens, underscores and spaces");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Type)
            .MaximumLength(50).WithMessage("Type must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Type))
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Type can only contain letters, numbers, dots, hyphens, underscores and spaces");
    }
}

public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İzin adı zorunludur")
            .MinimumLength(2).WithMessage("İzin adı en az 2 karakter olmalıdır")
            .MaximumLength(100).WithMessage("İzin adı 100 karakteri geçemez")
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("İzin adı sadece harf, rakam, nokta, tire, alt çizgi ve boşluk içerebilir");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakteri geçemez")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Group)
            .MaximumLength(50).WithMessage("Grup 50 karakteri geçemez")
            .When(x => !string.IsNullOrEmpty(x.Group))
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Grup sadece harf, rakam, nokta, tire, alt çizgi ve boşluk içerebilir");
    }
}

public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İzin adı zorunludur")
            .MinimumLength(2).WithMessage("İzin adı en az 2 karakter olmalıdır")
            .MaximumLength(100).WithMessage("İzin adı 100 karakteri geçemez")
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("İzin adı sadece harf, rakam, nokta, tire, alt çizgi ve boşluk içerebilir");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakteri geçemez")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Group)
            .MaximumLength(50).WithMessage("Grup 50 karakteri geçemez")
            .When(x => !string.IsNullOrEmpty(x.Group))
            .Matches(@"^[\p{L}0-9._\-\s]+$").WithMessage("Grup sadece harf, rakam, nokta, tire, alt çizgi ve boşluk içerebilir");
    }
}

public class AssignRoleToUserRequestValidator : AbstractValidator<AssignRoleToUserRequest>
{
    public AssignRoleToUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class AssignCategoryToUserRequestValidator : AbstractValidator<AssignCategoryToUserRequest>
{
    public AssignCategoryToUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateUserCategoryRequestValidator : AbstractValidator<UpdateUserCategoryRequest>
{
    public UpdateUserCategoryRequestValidator()
    {
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır")
            .MaximumLength(50).WithMessage("Kullanıcı adı 50 karakteri geçemez")
            .Matches("^[a-zA-Z0-9_.-]+$").WithMessage("Kullanıcı adı sadece harf, rakam, nokta, tire ve alt çizgi içerebilir");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur")
            .EmailAddress().WithMessage("Geçersiz e-posta formatı")
            .MaximumLength(256).WithMessage("E-posta adresi 256 karakteri geçemez");
        
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur")
            .MaximumLength(50).WithMessage("Ad 50 karakteri geçemez")
            .Matches("^[\\p{L}\\s'-]+$").WithMessage("Ad sadece harf, boşluk, tire (-) ve apostrof (') içerebilir");
        
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur")
            .MaximumLength(50).WithMessage("Soyad 50 karakteri geçemez")
            .Matches("^[\\p{L}\\s'-]+$").WithMessage("Soyad sadece harf, boşluk, tire (-) ve apostrof (') içerebilir");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola zorunludur")
            .MinimumLength(8).WithMessage("Parola en az 8 karakter olmalıdır")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Parola en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir");
        
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası zorunludur")
            .Length(11).WithMessage("Telefon numarası 11 haneli olmalıdır")
            .Matches("^[0-9]{11}$").WithMessage("Telefon numarası sadece rakamlardan oluşmalıdır");
        
        RuleForEach(x => x.RoleIds)
            .NotEmpty().WithMessage("Rol ID'si boş olamaz")
            .When(x => x.RoleIds != null && x.RoleIds.Any());
        
        RuleForEach(x => x.CategoryIds)
            .NotEmpty().WithMessage("Kategori ID'si boş olamaz")
            .When(x => x.CategoryIds != null && x.CategoryIds.Any());
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
            .Matches("^[a-zA-Z0-9_.-]+$").WithMessage("Username can only contain letters, numbers, dots, hyphens and underscores");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");
        
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters")
            .Matches("^[\\p{L}\\s'-]+$").WithMessage("First name can only contain letters and spaces");
        
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters")
            .Matches("^[\\p{L}\\s'-]+$").WithMessage("Last name can only contain letters and spaces");
        
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası zorunludur")
            .Length(11).WithMessage("Telefon numarası 11 haneli olmalıdır")
            .Matches("^[0-9]{11}$").WithMessage("Telefon numarası sadece rakamlardan oluşmalıdır");
        
        RuleForEach(x => x.RoleIds)
            .NotEmpty().WithMessage("Role ID cannot be empty")
            .When(x => x.RoleIds != null && x.RoleIds.Any());
        
        RuleForEach(x => x.CategoryIds)
            .NotEmpty().WithMessage("Category ID cannot be empty")
            .When(x => x.CategoryIds != null && x.CategoryIds.Any());
    }
}
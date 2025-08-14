// StyleCop: Dosya başlığı ve çoklu tip uyarılarını projenin mevcut yapısı için bastırıyoruz
// SA1633: File header, SA1402: Single type per file, SA1649: File name should match first type name
// Bu dosya çok sayıda validator içerdiği için tek dosyada tutuluyor
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using EgitimPlatform.Services.IdentityService.Models.DTOs;

[assembly: SuppressMessage("Style", "SA1633:File header is missing or not located at the top of the file", Justification = "Validator dosyası için basitleştirilmiş başlık politikası")]
[assembly: SuppressMessage("Style", "SA1402:File may only contain a single type", Justification = "Validatorlar tek dosyada toplanmıştır")]
[assembly: SuppressMessage("Style", "SA1649:File name should match first type name", Justification = "Çoklu validator tipi tek dosyada")]

namespace EgitimPlatform.Services.IdentityService.Validation;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email or username is required")
            .MaximumLength(256).WithMessage("Email or username must not exceed 256 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
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
            .Matches("^[a-zA-ZçğıöşüÇĞIİÖŞÜ\\s]+$").WithMessage("First name can only contain letters and spaces");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters")
            .Matches("^[a-zA-ZçğıöşüÇĞIİÖŞÜ\\s]+$").WithMessage("Last name can only contain letters and spaces");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit and one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Length(11).WithMessage("Phone number must be 11 digits")
            .Matches("^[0-9]*$").WithMessage("Phone number must contain only digits");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit and one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit and one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");
    }
}

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID token is required");
    }
}

public class GoogleRegisterRequestValidator : AbstractValidator<GoogleRegisterRequest>
{
    public GoogleRegisterRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID token is required");

        RuleForEach(x => x.Categories)
            .NotEmpty().WithMessage("Category name cannot be empty")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");
    }
}
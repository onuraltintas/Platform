using FluentValidation;
using User.Core.DTOs.Requests;

namespace User.Application.Validators;

/// <summary>
/// Validator for CreateUserProfileRequest
/// </summary>
public class CreateUserProfileRequestValidator : AbstractValidator<CreateUserProfileRequest>
{
    /// <summary>
    /// Constructor - Define validation rules
    /// </summary>
    public CreateUserProfileRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required")
            .Length(1, 256)
            .WithMessage("UserId must be between 1 and 256 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .Length(1, 100)
            .WithMessage("First name must be between 1 and 100 characters")
            .Matches("^[a-zA-ZÀ-ÿ\\u0100-\\u017F\\u0180-\\u024F\\u1E00-\\u1EFF\\s'-]+$")
            .WithMessage("First name contains invalid characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .Length(1, 100)
            .WithMessage("Last name must be between 1 and 100 characters")
            .Matches("^[a-zA-ZÀ-ÿ\\u0100-\\u017F\\u0180-\\u024F\\u1E00-\\u1EFF\\s'-]+$")
            .WithMessage("Last name contains invalid characters");

        RuleFor(x => x.PhoneNumber)
            .Matches("^\\+?[1-9]\\d{1,14}$")
            .WithMessage("Phone number must be a valid international format")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Today)
            .WithMessage("Date of birth must be in the past")
            .GreaterThan(DateTime.Today.AddYears(-120))
            .WithMessage("Date of birth cannot be more than 120 years ago")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.Bio)
            .MaximumLength(1000)
            .WithMessage("Bio cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Bio));

        RuleFor(x => x.TimeZone)
            .IsInEnum()
            .WithMessage("Invalid timezone value");

        RuleFor(x => x.Language)
            .IsInEnum()
            .WithMessage("Invalid language code");
    }
}
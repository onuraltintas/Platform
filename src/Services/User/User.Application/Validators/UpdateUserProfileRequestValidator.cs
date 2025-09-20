using FluentValidation;
using User.Core.DTOs.Requests;

namespace User.Application.Validators;

/// <summary>
/// Validator for UpdateUserProfileRequest
/// </summary>
public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    /// <summary>
    /// Constructor - Define validation rules
    /// </summary>
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .Length(1, 100)
            .WithMessage("First name must be between 1 and 100 characters")
            .Matches("^[a-zA-ZÀ-ÿ\\u0100-\\u017F\\u0180-\\u024F\\u1E00-\\u1EFF\\s'-]+$")
            .WithMessage("First name contains invalid characters")
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x => x.LastName)
            .Length(1, 100)
            .WithMessage("Last name must be between 1 and 100 characters")
            .Matches("^[a-zA-ZÀ-ÿ\\u0100-\\u017F\\u0180-\\u024F\\u1E00-\\u1EFF\\s'-]+$")
            .WithMessage("Last name contains invalid characters")
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));

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
            .WithMessage("Invalid timezone value")
            .When(x => x.TimeZone.HasValue);

        RuleFor(x => x.Language)
            .IsInEnum()
            .WithMessage("Invalid language code")
            .When(x => x.Language.HasValue);

        // Custom validation: At least one field must be provided for update
        RuleFor(x => x)
            .Must(HaveAtLeastOneField)
            .WithMessage("At least one field must be provided for update");
    }

    /// <summary>
    /// Check if at least one field is provided for update
    /// </summary>
    /// <param name="request">Update request</param>
    /// <returns>True if at least one field is provided</returns>
    private static bool HaveAtLeastOneField(UpdateUserProfileRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.FirstName) ||
               !string.IsNullOrWhiteSpace(request.LastName) ||
               !string.IsNullOrWhiteSpace(request.PhoneNumber) ||
               request.DateOfBirth.HasValue ||
               !string.IsNullOrWhiteSpace(request.Bio) ||
               request.TimeZone.HasValue ||
               request.Language.HasValue;
    }
}
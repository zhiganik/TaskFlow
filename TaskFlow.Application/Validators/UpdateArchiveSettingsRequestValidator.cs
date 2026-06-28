using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class UpdateArchiveSettingsRequestValidator : AbstractValidator<UpdateArchiveSettingsRequest>
{
    public UpdateArchiveSettingsRequestValidator()
    {
        RuleFor(x => x.ArchiveAfterDays)
            .GreaterThanOrEqualTo(1).WithMessage("ArchiveAfterDays must be at least 1.")
            .LessThanOrEqualTo(365).WithMessage("ArchiveAfterDays cannot exceed 365.");
    }
}

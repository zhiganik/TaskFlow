using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class UpdatePriorityConfigRequestValidator : AbstractValidator<UpdatePriorityConfigRequest>
{
    public UpdatePriorityConfigRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().Matches(@"^#[0-9a-fA-F]{6}$").WithMessage("Color must be a valid hex color.");
    }
}

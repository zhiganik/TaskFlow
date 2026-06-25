using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class CreateColumnRequestValidator : AbstractValidator<CreateColumnRequest>
{
    public CreateColumnRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(50);

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color (e.g. #FF5733).")
            .When(x => x.Color is not null);
    }
}

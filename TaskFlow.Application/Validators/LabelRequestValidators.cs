using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    public CreateLabelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().Matches(@"^#[0-9a-fA-F]{6}$").WithMessage("Color must be a valid hex color.");
    }
}

public class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().Matches(@"^#[0-9a-fA-F]{6}$").WithMessage("Color must be a valid hex color.");
    }
}

public class SetTaskLabelsRequestValidator : AbstractValidator<SetTaskLabelsRequest>
{
    public SetTaskLabelsRequestValidator()
    {
        RuleFor(x => x.LabelIds).NotNull();
        RuleForEach(x => x.LabelIds).NotEqual(Guid.Empty);
    }
}

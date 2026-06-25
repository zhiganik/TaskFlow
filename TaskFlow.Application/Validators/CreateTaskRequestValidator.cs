using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("ColumnId is required.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => x.Description is not null);
    }
}

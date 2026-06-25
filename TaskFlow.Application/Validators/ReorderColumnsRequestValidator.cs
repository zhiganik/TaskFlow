using FluentValidation;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class ReorderColumnsRequestValidator : AbstractValidator<ReorderColumnsRequest>
{
    public ReorderColumnsRequestValidator()
    {
        RuleFor(x => x.ColumnIds)
            .NotEmpty().WithMessage("ColumnIds must not be empty.")
            .Must(ids => ids.Count <= 7).WithMessage("A workspace cannot have more than 7 columns.");
    }
}

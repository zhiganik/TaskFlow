using FluentValidation;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class UpdateMemberRoleRequestValidator : AbstractValidator<UpdateMemberRoleRequest>
{
    public UpdateMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(WorkspaceRole.Owner).WithMessage("Cannot change a member's role to Owner.");
    }
}

using FluentValidation;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Validators;

public class CreateInvitationRequestValidator : AbstractValidator<CreateInvitationRequest>
{
    public CreateInvitationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(WorkspaceRole.Owner).WithMessage("Cannot invite a member as Owner.");
    }
}

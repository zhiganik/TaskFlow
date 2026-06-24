using FluentAssertions;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Validators;

namespace TaskFlow.Tests.Validators;

[TestFixture]
public class InviteMemberRequestValidatorTests
{
    private InviteMemberRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new InviteMemberRequestValidator();

    [Test]
    public async Task Validate_EmptyEmail_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new InviteMemberRequest("", WorkspaceRole.Member));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Test]
    public async Task Validate_InvalidEmail_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new InviteMemberRequest("not-an-email", WorkspaceRole.Member));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Test]
    public async Task Validate_RoleOwner_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new InviteMemberRequest("user@example.com", WorkspaceRole.Owner));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new InviteMemberRequest("user@example.com", WorkspaceRole.Member));

        result.IsValid.Should().BeTrue();
    }
}

[TestFixture]
public class UpdateMemberRoleRequestValidatorTests
{
    private UpdateMemberRoleRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateMemberRoleRequestValidator();

    [Test]
    public async Task Validate_RoleOwner_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new UpdateMemberRoleRequest(WorkspaceRole.Owner));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new UpdateMemberRoleRequest(WorkspaceRole.Admin));

        result.IsValid.Should().BeTrue();
    }
}

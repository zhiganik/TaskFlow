using FluentAssertions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Validators;

namespace TaskFlow.Tests.Validators;

[TestFixture]
public class CreateWorkspaceRequestValidatorTests
{
    private CreateWorkspaceRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateWorkspaceRequestValidator();

    [Test]
    public async Task Validate_EmptyName_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new CreateWorkspaceRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task Validate_NameTooLong_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new CreateWorkspaceRequest(new string('a', 101)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new CreateWorkspaceRequest("Engineering"));

        result.IsValid.Should().BeTrue();
    }
}

[TestFixture]
public class UpdateWorkspaceRequestValidatorTests
{
    private UpdateWorkspaceRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateWorkspaceRequestValidator();

    [Test]
    public async Task Validate_EmptyName_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new UpdateWorkspaceRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new UpdateWorkspaceRequest("Renamed"));

        result.IsValid.Should().BeTrue();
    }
}

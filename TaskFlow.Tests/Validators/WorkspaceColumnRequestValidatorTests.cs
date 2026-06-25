using FluentAssertions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Validators;

namespace TaskFlow.Tests.Validators;

[TestFixture]
public class CreateColumnRequestValidatorTests
{
    private CreateColumnRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateColumnRequestValidator();

    [Test]
    public async Task Validate_EmptyName_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new CreateColumnRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task Validate_NameTooLong_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new CreateColumnRequest(new string('x', 51)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new CreateColumnRequest("In Progress"));

        result.IsValid.Should().BeTrue();
    }
}

[TestFixture]
public class UpdateColumnRequestValidatorTests
{
    private UpdateColumnRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateColumnRequestValidator();

    [Test]
    public async Task Validate_EmptyName_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new UpdateColumnRequest(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new UpdateColumnRequest("Review"));

        result.IsValid.Should().BeTrue();
    }
}

[TestFixture]
public class ReorderColumnsRequestValidatorTests
{
    private ReorderColumnsRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new ReorderColumnsRequestValidator();

    [Test]
    public async Task Validate_EmptyList_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new ReorderColumnsRequest([]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColumnIds");
    }

    [Test]
    public async Task Validate_MoreThanSevenIds_ReturnsError()
    {
        var ids = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToList();
        var result = await _validator.ValidateAsync(new ReorderColumnsRequest(ids));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColumnIds");
    }

    [Test]
    public async Task Validate_SevenIds_Passes()
    {
        var ids = Enumerable.Range(0, 7).Select(_ => Guid.NewGuid()).ToList();
        var result = await _validator.ValidateAsync(new ReorderColumnsRequest(ids));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_OneId_Passes()
    {
        var result = await _validator.ValidateAsync(new ReorderColumnsRequest([Guid.NewGuid()]));

        result.IsValid.Should().BeTrue();
    }
}

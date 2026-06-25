using FluentAssertions;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Validators;

namespace TaskFlow.Tests.Validators;

[TestFixture]
public class CreateTaskRequestValidatorTests
{
    private CreateTaskRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateTaskRequestValidator();

    [Test]
    public async Task Validate_EmptyTitle_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new CreateTaskRequest("", Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public async Task Validate_TitleTooLong_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new CreateTaskRequest(new string('a', 201), Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public async Task Validate_EmptyColumnId_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new CreateTaskRequest("Title", Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColumnId");
    }

    [Test]
    public async Task Validate_DescriptionTooLong_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new CreateTaskRequest("Title", Guid.NewGuid(), Description: new string('x', 2001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Test]
    public async Task Validate_ValidMinimalRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new CreateTaskRequest("Fix bug", Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ValidFullRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new CreateTaskRequest(
            "Fix bug", Guid.NewGuid(),
            Description: "Details here",
            Priority: TaskPriority.High,
            AssigneeId: "user-1",
            DueDate: DateTime.UtcNow.AddDays(7)));

        result.IsValid.Should().BeTrue();
    }
}

[TestFixture]
public class UpdateTaskRequestValidatorTests
{
    private UpdateTaskRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateTaskRequestValidator();

    [Test]
    public async Task Validate_EmptyTitle_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new UpdateTaskRequest("", null, TaskPriority.Medium, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public async Task Validate_DescriptionTooLong_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new UpdateTaskRequest("Title", new string('x', 2001), TaskPriority.Medium, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(
            new UpdateTaskRequest("Fix bug", "Some detail", TaskPriority.Low, "user-1", null));

        result.IsValid.Should().BeTrue();
    }
}

[TestFixture]
public class MoveTaskRequestValidatorTests
{
    private MoveTaskRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new MoveTaskRequestValidator();

    [Test]
    public async Task Validate_EmptyColumnId_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new MoveTaskRequest(Guid.Empty, 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColumnId");
    }

    [Test]
    public async Task Validate_NegativeOrder_ReturnsError()
    {
        var result = await _validator.ValidateAsync(new MoveTaskRequest(Guid.NewGuid(), -1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Order");
    }

    [Test]
    public async Task Validate_ZeroOrder_Passes()
    {
        var result = await _validator.ValidateAsync(new MoveTaskRequest(Guid.NewGuid(), 0));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new MoveTaskRequest(Guid.NewGuid(), 3));

        result.IsValid.Should().BeTrue();
    }
}

# 07 — Testing: NUnit 4, Moq, FluentAssertions

## Philosophy

- **Unit tests only** — services and validators, no integration tests in scope
- All infrastructure mocked with Moq — no live DB, no Redis
- FluentAssertions for readable, descriptive assertions
- Tests are run on the host (`dotnet test` or `make test`) — not in Docker

---

## Project Setup

```xml
<!-- TaskFlow.Tests/TaskFlow.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit"                 Version="4.*" />
    <PackageReference Include="NUnit3TestAdapter"      Version="4.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="Moq"                    Version="4.*" />
    <PackageReference Include="FluentAssertions"        Version="8.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TaskFlow.Application\TaskFlow.Application.csproj" />
    <ProjectReference Include="..\TaskFlow.Infrastructure\TaskFlow.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

---

## Standard Test Class Structure

```csharp
[TestFixture]
public class TaskServiceTests
{
    // Mocks
    private Mock<ITaskRepository> _taskRepoMock = null!;
    private Mock<ICacheService>   _cacheMock    = null!;
    private Mock<ILogger<TaskService>> _loggerMock = null!;

    // System Under Test
    private TaskService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _taskRepoMock = new Mock<ITaskRepository>();
        _cacheMock    = new Mock<ICacheService>();
        _loggerMock   = new Mock<ILogger<TaskService>>();

        _sut = new TaskService(
            _taskRepoMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    // Tests below...
}
```

---

## Test Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
- `CreateTaskAsync_ValidRequest_ReturnsCreatedTaskDto`
- `CreateTaskAsync_ProjectNotFound_ThrowsNotFoundException`
- `GetTasksAsync_FilterByStatus_CallsRepositoryWithFilter`

---

## Test Patterns

### Happy Path
```csharp
[Test]
public async Task CreateTaskAsync_ValidRequest_ReturnsTaskDto()
{
    // Arrange
    var projectId = Guid.NewGuid();
    var request = new CreateTaskRequest(
        Title: "Write tests",
        Description: null,
        Priority: TaskPriority.High,
        AssigneeId: null,
        DueDate: DateTime.UtcNow.AddDays(7));

    _taskRepoMock
        .Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _sut.CreateTaskAsync(request, "user-1", CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Title.Should().Be("Write tests");
    result.Priority.Should().Be(TaskPriority.High);
    result.Status.Should().Be(TaskStatus.Todo);

    _taskRepoMock.Verify(
        r => r.AddAsync(It.IsAny<TaskItem>(), default),
        Times.Once);
}
```

### Exception Path
```csharp
[Test]
public async Task GetTaskAsync_NotFound_ThrowsNotFoundException()
{
    // Arrange
    var taskId = Guid.NewGuid();

    _taskRepoMock
        .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((TaskItem?)null);

    // Act
    var act = async () => await _sut.GetTaskAsync(taskId, CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<NotFoundException>()
        .WithMessage($"*{taskId}*");
}
```

### Validator Tests (no mocks needed)
```csharp
[TestFixture]
public class CreateTaskRequestValidatorTests
{
    private CreateTaskRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateTaskRequestValidator();

    [Test]
    public async Task Validate_EmptyTitle_ReturnsError()
    {
        var request = new CreateTaskRequest("", null, TaskPriority.Low, null, null);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Test]
    public async Task Validate_PastDueDate_ReturnsError()
    {
        var request = new CreateTaskRequest(
            "Title", null, TaskPriority.Low, null,
            DueDate: DateTime.UtcNow.AddDays(-1));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DueDate");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var request = new CreateTaskRequest(
            "Title", null, TaskPriority.High, null,
            DateTime.UtcNow.AddDays(3));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
```

### Parameterized Tests
```csharp
[TestCase(TaskPriority.Low)]
[TestCase(TaskPriority.Medium)]
[TestCase(TaskPriority.High)]
[TestCase(TaskPriority.Critical)]
public async Task CreateTaskAsync_AllPriorities_CreatesSuccessfully(TaskPriority priority)
{
    // Arrange
    var request = new CreateTaskRequest("Title", null, priority, null, null);
    _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default))
                 .Returns(Task.CompletedTask);

    // Act
    var result = await _sut.CreateTaskAsync(request, "user-1", default);

    // Assert
    result.Priority.Should().Be(priority);
}
```

---

## Moq Quick Reference

```csharp
// Setup return value
_mock.Setup(x => x.MethodAsync(It.IsAny<Guid>(), default))
     .ReturnsAsync(someValue);

// Setup null (not found)
_mock.Setup(x => x.GetByIdAsync(id, default))
     .ReturnsAsync((EntityType?)null);

// Setup void/Task method
_mock.Setup(x => x.AddAsync(It.IsAny<Entity>(), default))
     .Returns(Task.CompletedTask);

// Verify called once
_mock.Verify(x => x.AddAsync(It.IsAny<Entity>(), default), Times.Once);

// Verify never called
_mock.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);

// Verify with specific argument
_mock.Verify(x => x.GetByIdAsync(specificId, default), Times.Once);
```

## FluentAssertions Quick Reference

```csharp
result.Should().NotBeNull();
result.Should().BeNull();
result.Title.Should().Be("expected");
result.Status.Should().Be(TaskStatus.Todo);
result.Items.Should().HaveCount(3);
result.Items.Should().ContainSingle(t => t.Id == taskId);
result.Items.Should().BeEmpty();
result.Should().BeOfType<TaskDto>();

// Exception assertions
await act.Should().ThrowAsync<NotFoundException>();
await act.Should().ThrowAsync<NotFoundException>().WithMessage("*keyword*");
await act.Should().NotThrowAsync();

// Collections
list.Should().Contain(item);
list.Should().NotContain(item);
list.Should().OnlyContain(t => t.Status == TaskStatus.Done);
```

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=normal"

# Run specific test class
dotnet test --filter "FullyQualifiedName~TaskServiceTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~CreateTaskAsync_ValidRequest"

# With coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

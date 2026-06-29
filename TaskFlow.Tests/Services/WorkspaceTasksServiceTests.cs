using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Mappings;
using TaskFlow.Application.Services;

namespace TaskFlow.Tests.Services;

[TestFixture]
public class WorkspaceTasksServiceTests
{
    private Mock<IWorkspaceTasksRepository>      _repositoryMock        = null!;
    private Mock<IWorkspaceColumnsRepository>    _columnsRepositoryMock = null!;
    private Mock<IWorkspaceLabelsRepository>     _labelsRepositoryMock  = null!;
    private Mock<IMessagePublisher>              _publisherMock         = null!;
    private Mock<UserManager<AppUser>>           _userManagerMock       = null!;
    private Mock<ILogger<WorkspaceTasksService>> _loggerMock            = null!;
    private IMapper _mapper = null!;

    private WorkspaceTasksService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock        = new Mock<IWorkspaceTasksRepository>();
        _columnsRepositoryMock = new Mock<IWorkspaceColumnsRepository>();
        _labelsRepositoryMock  = new Mock<IWorkspaceLabelsRepository>();
        _publisherMock         = new Mock<IMessagePublisher>();
        _loggerMock            = new Mock<ILogger<WorkspaceTasksService>>();

        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                        .ReturnsAsync((AppUser?)null);

        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

        _mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<WorkspaceTaskProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _sut = new WorkspaceTasksService(
            _repositoryMock.Object,
            _columnsRepositoryMock.Object,
            _labelsRepositoryMock.Object,
            _publisherMock.Object,
            _userManagerMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    private static WorkspaceColumn CreateColumn(Guid workspaceId, string name = "Todo") => new()
    {
        Id          = Guid.NewGuid(),
        WorkspaceId = workspaceId,
        Name        = name,
        Order       = 0
    };

    private static WorkspaceTask CreateTask(Guid workspaceId, WorkspaceColumn column) => new()
    {
        Id          = Guid.NewGuid(),
        WorkspaceId = workspaceId,
        ColumnId    = column.Id,
        Title       = "Test task",
        Priority    = TaskPriority.Medium,
        CreatedById = "user-1",
        CreatedAt   = DateTime.UtcNow,
        Column      = column
    };

    [Test]
    public async Task GetByWorkspaceAsync_ReturnsMappedDtos()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);
        var tasks       = new List<WorkspaceTask>
        {
            CreateTask(workspaceId, column),
            CreateTask(workspaceId, column)
        };

        _repositoryMock
            .Setup(r => r.GetByWorkspaceIdAsync(workspaceId, It.IsAny<TaskFilterQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var result = await _sut.GetByWorkspaceAsync(workspaceId, new TaskFilterQuery(null, null, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t =>
        {
            t.WorkspaceId.Should().Be(workspaceId);
            t.ColumnName.Should().Be("Todo");
        });
    }

    [Test]
    public async Task GetByIdAsync_ValidRequest_ReturnsDto()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);
        var task        = CreateTask(workspaceId, column);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.GetByIdAsync(workspaceId, task.Id, CancellationToken.None);

        result.Id.Should().Be(task.Id);
        result.ColumnName.Should().Be("Todo");
    }

    [Test]
    public async Task GetByIdAsync_TaskNotFound_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceTask?)null);

        var act = async () => await _sut.GetByIdAsync(Guid.NewGuid(), taskId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetByIdAsync_TaskBelongsToDifferentWorkspace_ThrowsNotFoundException()
    {
        var column = CreateColumn(Guid.NewGuid());
        var task   = CreateTask(column.WorkspaceId, column);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var act = async () => await _sut.GetByIdAsync(Guid.NewGuid(), task.Id, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task CreateAsync_ValidRequest_AppendsTaskAtEndOfColumn()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);
        var request     = new CreateTaskRequest("New task", column.Id, Priority: TaskPriority.High);

        _columnsRepositoryMock
            .Setup(r => r.GetByIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(column);
        _repositoryMock
            .Setup(r => r.CountByColumnIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock
            .Setup(r => r.GetNextNumberAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        WorkspaceTask? addedTask = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<WorkspaceTask>(), It.IsAny<CancellationToken>()))
            .Callback<WorkspaceTask, CancellationToken>((t, _) => addedTask = t)
            .ReturnsAsync((WorkspaceTask t, CancellationToken _) => t);
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (addedTask is not null) addedTask.Column = column;
                return addedTask;
            });

        var result = await _sut.CreateAsync(workspaceId, "user-1", request, CancellationToken.None);

        result.Number.Should().Be(1);
        result.Order.Should().Be(2);
        result.Title.Should().Be("New task");
        result.Priority.Should().Be(TaskPriority.High);
        result.ColumnName.Should().Be("Todo");
    }

    [Test]
    public async Task CreateAsync_ColumnNotFound_ThrowsNotFoundException()
    {
        var workspaceId = Guid.NewGuid();
        var columnId    = Guid.NewGuid();

        _columnsRepositoryMock
            .Setup(r => r.GetByIdAsync(columnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceColumn?)null);

        var act = async () => await _sut.CreateAsync(workspaceId, "user-1", new CreateTaskRequest("T", columnId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<WorkspaceTask>(), default), Times.Never);
    }

    [Test]
    public async Task CreateAsync_ColumnBelongsToDifferentWorkspace_ThrowsNotFoundException()
    {
        var column = CreateColumn(Guid.NewGuid()); // column in another workspace

        _columnsRepositoryMock
            .Setup(r => r.GetByIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(column);

        var differentWorkspace = Guid.NewGuid();
        var act = async () => await _sut.CreateAsync(differentWorkspace, "user-1", new CreateTaskRequest("T", column.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<WorkspaceTask>(), default), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesAllFields()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);
        var task        = CreateTask(workspaceId, column);
        var request     = new UpdateTaskRequest("Renamed", "New desc", TaskPriority.Low, null, null);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.UpdateAsync(workspaceId, task.Id, request, "user-1", CancellationToken.None);

        _repositoryMock.Verify(
            r => r.UpdateAsync(
                It.Is<WorkspaceTask>(t =>
                    t.Title == "Renamed" &&
                    t.Description == "New desc" &&
                    t.Priority == TaskPriority.Low),
                default),
            Times.Once);
    }

    [Test]
    public async Task UpdateAsync_TaskNotFound_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceTask?)null);

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), taskId,
            new UpdateTaskRequest("T", null, TaskPriority.Medium, null, null), "user-1", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkspaceTask>(), default), Times.Never);
    }

    [Test]
    public async Task MoveAsync_ValidRequest_MovesTaskToTargetColumn()
    {
        var workspaceId  = Guid.NewGuid();
        var sourceColumn = CreateColumn(workspaceId, "Todo");
        var targetColumn = CreateColumn(workspaceId, "In Progress");

        var task = CreateTask(workspaceId, sourceColumn);

        var reloaded = CreateTask(workspaceId, targetColumn);
        reloaded.Id     = task.Id;
        reloaded.Column = targetColumn;

        _repositoryMock
            .SetupSequence(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task)
            .ReturnsAsync(reloaded);
        _columnsRepositoryMock
            .Setup(r => r.GetByIdAsync(targetColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetColumn);

        var result = await _sut.MoveAsync(workspaceId, task.Id, new MoveTaskRequest(targetColumn.Id), "user-1", CancellationToken.None);

        result.ColumnId.Should().Be(targetColumn.Id);
        result.ColumnName.Should().Be("In Progress");

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkspaceTask>(), default), Times.Once);
        _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<WorkspaceTask>>(), default), Times.Never);
        _repositoryMock.Verify(r => r.GetByColumnIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Test]
    public async Task MoveAsync_SameColumn_ThrowsBadRequestException()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);
        var task        = CreateTask(workspaceId, column);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var act = async () => await _sut.MoveAsync(workspaceId, task.Id, new MoveTaskRequest(column.Id), "user-1", CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkspaceTask>(), default), Times.Never);
    }

    [Test]
    public async Task MoveAsync_TargetColumnNotInWorkspace_ThrowsNotFoundException()
    {
        var workspaceId  = Guid.NewGuid();
        var sourceColumn = CreateColumn(workspaceId);
        var task         = CreateTask(workspaceId, sourceColumn);
        var foreignCol   = CreateColumn(Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _columnsRepositoryMock
            .Setup(r => r.GetByIdAsync(foreignCol.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignCol);

        var act = async () => await _sut.MoveAsync(workspaceId, task.Id, new MoveTaskRequest(foreignCol.Id), "user-1", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkspaceTask>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ActiveTask_SoftDeletesTask()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);
        var task        = CreateTask(workspaceId, column); // Status = Active

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        await _sut.DeleteAsync(workspaceId, task.Id, "user-1", CancellationToken.None);

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<WorkspaceTask>(t => t.Status == WorkspaceTaskStatus.Deleted), default),
            Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_TaskNotFound_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceTask?)null);

        var act = async () => await _sut.DeleteAsync(Guid.NewGuid(), taskId, "user-1", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}

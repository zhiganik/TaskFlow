using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Mappings;
using TaskFlow.Application.Services;

namespace TaskFlow.Tests.Services;

[TestFixture]
public class WorkspaceTasksServiceTests
{
    private Mock<IWorkspaceTasksRepository>   _repositoryMock = null!;
    private Mock<IWorkspaceColumnsRepository> _columnsRepositoryMock = null!;
    private Mock<ILogger<WorkspaceTasksService>> _loggerMock = null!;
    private IMapper _mapper = null!;

    private WorkspaceTasksService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock        = new Mock<IWorkspaceTasksRepository>();
        _columnsRepositoryMock = new Mock<IWorkspaceColumnsRepository>();
        _loggerMock            = new Mock<ILogger<WorkspaceTasksService>>();
        _mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<WorkspaceTaskProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _sut = new WorkspaceTasksService(
            _repositoryMock.Object,
            _columnsRepositoryMock.Object,
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
            .Setup(r => r.GetByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var result = await _sut.GetByWorkspaceAsync(workspaceId, CancellationToken.None);

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

        var result = await _sut.UpdateAsync(workspaceId, task.Id, request, CancellationToken.None);

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
            new UpdateTaskRequest("T", null, TaskPriority.Medium, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkspaceTask>(), default), Times.Never);
    }

    [Test]
    public async Task MoveAsync_ValidRequest_ChangesColumnAndShiftsOtherTasks()
    {
        var workspaceId  = Guid.NewGuid();
        var sourceColumn = CreateColumn(workspaceId, "Todo");
        var targetColumn = CreateColumn(workspaceId, "In Progress");

        var task = CreateTask(workspaceId, sourceColumn);
        task.Order = 0;

        var existing0 = CreateTask(workspaceId, targetColumn); existing0.Order = 0;
        var existing1 = CreateTask(workspaceId, targetColumn); existing1.Order = 1;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _columnsRepositoryMock
            .Setup(r => r.GetByIdAsync(targetColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetColumn);
        _repositoryMock
            .Setup(r => r.GetByColumnIdAsync(targetColumn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing0, existing1]);

        var result = await _sut.MoveAsync(workspaceId, task.Id, new MoveTaskRequest(targetColumn.Id, 0), CancellationToken.None);

        result.ColumnId.Should().Be(targetColumn.Id);
        result.ColumnName.Should().Be("In Progress");
        result.Order.Should().Be(0);
        existing0.Order.Should().Be(1);
        existing1.Order.Should().Be(2);

        _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<WorkspaceTask>>(), default), Times.Once);
    }

    [Test]
    public async Task MoveAsync_TargetColumnNotInWorkspace_ThrowsNotFoundException()
    {
        var workspaceId  = Guid.NewGuid();
        var sourceColumn = CreateColumn(workspaceId);
        var task         = CreateTask(workspaceId, sourceColumn);
        var foreignCol   = CreateColumn(Guid.NewGuid()); // different workspace

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _columnsRepositoryMock
            .Setup(r => r.GetByIdAsync(foreignCol.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignCol);

        var act = async () => await _sut.MoveAsync(workspaceId, task.Id, new MoveTaskRequest(foreignCol.Id, 0), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<WorkspaceTask>>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ValidRequest_DeletesTask()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);
        var task        = CreateTask(workspaceId, column);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        await _sut.DeleteAsync(workspaceId, task.Id, CancellationToken.None);

        _repositoryMock.Verify(r => r.DeleteAsync(task.Id, default), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_TaskNotFound_ThrowsNotFoundException()
    {
        var taskId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceTask?)null);

        var act = async () => await _sut.DeleteAsync(Guid.NewGuid(), taskId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}

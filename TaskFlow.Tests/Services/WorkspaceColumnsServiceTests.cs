using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Mappings;
using TaskFlow.Application.Services;

namespace TaskFlow.Tests.Services;

[TestFixture]
public class WorkspaceColumnsServiceTests
{
    private Mock<IWorkspaceColumnsRepository> _repositoryMock = null!;
    private Mock<IWorkspaceTasksRepository>   _tasksRepositoryMock = null!;
    private Mock<ILogger<WorkspaceColumnsService>> _loggerMock = null!;
    private IMapper _mapper = null!;

    private WorkspaceColumnsService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock      = new Mock<IWorkspaceColumnsRepository>();
        _tasksRepositoryMock = new Mock<IWorkspaceTasksRepository>();
        _loggerMock          = new Mock<ILogger<WorkspaceColumnsService>>();
        _mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<WorkspaceColumnProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _sut = new WorkspaceColumnsService(
            _repositoryMock.Object,
            _tasksRepositoryMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    private static WorkspaceColumn CreateColumn(Guid workspaceId, int order = 0) => new()
    {
        Id          = Guid.NewGuid(),
        WorkspaceId = workspaceId,
        Name        = $"Column {order}",
        Order       = order,
        CreatedAt   = DateTime.UtcNow
    };

    [Test]
    public async Task GetByWorkspaceAsync_ReturnsAllColumnsMapped()
    {
        var workspaceId = Guid.NewGuid();
        var columns = new List<WorkspaceColumn>
        {
            CreateColumn(workspaceId, 0),
            CreateColumn(workspaceId, 1)
        };

        _repositoryMock
            .Setup(r => r.GetByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(columns);

        var result = await _sut.GetByWorkspaceAsync(workspaceId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.WorkspaceId.Should().Be(workspaceId));
    }

    [Test]
    public async Task CreateAsync_ValidRequest_AppendsColumnAtEnd()
    {
        var workspaceId = Guid.NewGuid();
        var request     = new CreateColumnRequest("Review");

        _repositoryMock
            .Setup(r => r.CountByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<WorkspaceColumn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceColumn c, CancellationToken _) => c);

        var result = await _sut.CreateAsync(workspaceId, request, CancellationToken.None);

        result.Name.Should().Be("Review");
        result.Order.Should().Be(3);
        result.WorkspaceId.Should().Be(workspaceId);
    }

    [Test]
    public async Task CreateAsync_LimitReached_ThrowsConflictException()
    {
        var workspaceId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.CountByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var act = async () => await _sut.CreateAsync(workspaceId, new CreateColumnRequest("Extra"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*7*");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<WorkspaceColumn>(), default), Times.Never);
    }

    [Test]
    public async Task RenameAsync_ValidRequest_UpdatesName()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(column);

        var result = await _sut.RenameAsync(workspaceId, column.Id, new UpdateColumnRequest("QA"), CancellationToken.None);

        result.Name.Should().Be("QA");
        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<WorkspaceColumn>(c => c.Name == "QA"), default),
            Times.Once);
    }

    [Test]
    public async Task RenameAsync_ColumnNotFound_ThrowsNotFoundException()
    {
        var workspaceId = Guid.NewGuid();
        var columnId    = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(columnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceColumn?)null);

        var act = async () => await _sut.RenameAsync(workspaceId, columnId, new UpdateColumnRequest("X"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkspaceColumn>(), default), Times.Never);
    }

    [Test]
    public async Task RenameAsync_ColumnBelongsToDifferentWorkspace_ThrowsNotFoundException()
    {
        var column = CreateColumn(Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.GetByIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(column);

        var differentWorkspaceId = Guid.NewGuid();
        var act = async () => await _sut.RenameAsync(differentWorkspaceId, column.Id, new UpdateColumnRequest("X"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ReorderAsync_ValidRequest_SetsOrderByIndex()
    {
        var workspaceId = Guid.NewGuid();
        var col0        = CreateColumn(workspaceId, 0);
        var col1        = CreateColumn(workspaceId, 1);
        var col2        = CreateColumn(workspaceId, 2);

        _repositoryMock
            .Setup(r => r.GetByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([col0, col1, col2]);

        var request = new ReorderColumnsRequest([col2.Id, col1.Id, col0.Id]);

        List<WorkspaceColumn>? captured = null;
        _repositoryMock
            .Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<WorkspaceColumn>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<WorkspaceColumn>, CancellationToken>((cols, _) => captured = cols.ToList());

        await _sut.ReorderAsync(workspaceId, request, CancellationToken.None);

        captured!.Single(c => c.Id == col2.Id).Order.Should().Be(0);
        captured!.Single(c => c.Id == col1.Id).Order.Should().Be(1);
        captured!.Single(c => c.Id == col0.Id).Order.Should().Be(2);
        _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<WorkspaceColumn>>(), default), Times.Once);
    }

    [Test]
    public async Task ReorderAsync_MismatchedIds_ThrowsConflictException()
    {
        var workspaceId = Guid.NewGuid();
        var col0        = CreateColumn(workspaceId, 0);

        _repositoryMock
            .Setup(r => r.GetByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([col0]);

        var request = new ReorderColumnsRequest([Guid.NewGuid()]);

        var act = async () => await _sut.ReorderAsync(workspaceId, request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<WorkspaceColumn>>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ColumnHasTasks_ThrowsConflictException()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(column);
        _tasksRepositoryMock
            .Setup(r => r.CountByColumnIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var act = async () => await _sut.DeleteAsync(workspaceId, column.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*3*");
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_EmptyColumn_DeletesSuccessfully()
    {
        var workspaceId = Guid.NewGuid();
        var column      = CreateColumn(workspaceId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(column);
        _tasksRepositoryMock
            .Setup(r => r.CountByColumnIdAsync(column.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.DeleteAsync(workspaceId, column.Id, CancellationToken.None);

        _repositoryMock.Verify(r => r.DeleteAsync(column.Id, default), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ColumnNotFound_ThrowsNotFoundException()
    {
        var workspaceId = Guid.NewGuid();
        var columnId    = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(columnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceColumn?)null);

        var act = async () => await _sut.DeleteAsync(workspaceId, columnId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Services;

namespace TaskFlow.Tests.Services;

[TestFixture]
public class WorkspacesServiceTests
{
    private Mock<IWorkspacesRepository> _repositoryMock = null!;
    private Mock<ILogger<WorkspacesService>> _loggerMock = null!;

    private WorkspacesService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IWorkspacesRepository>();
        _loggerMock = new Mock<ILogger<WorkspacesService>>();

        _sut = new WorkspacesService(_repositoryMock.Object, _loggerMock.Object);
    }

    private static Workspace CreateWorkspace(string ownerId = "owner-1") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Engineering",
        OwnerId = ownerId,
        CreatedAt = DateTime.UtcNow
    };

    [Test]
    public async Task CreateAsync_ValidRequest_ReturnsWorkspaceDtoOwnedByCaller()
    {
        var request = new CreateWorkspaceRequest("Engineering");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace w, CancellationToken _) => w);

        var result = await _sut.CreateAsync("owner-1", request, CancellationToken.None);

        result.Name.Should().Be("Engineering");
        result.OwnerId.Should().Be("owner-1");

        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Workspace>(w => w.Name == "Engineering" && w.OwnerId == "owner-1"), default),
            Times.Once);
    }

    [Test]
    public async Task GetForUserAsync_ReturnsWorkspacesOwnedByUser()
    {
        var workspaces = new List<Workspace> { CreateWorkspace(), CreateWorkspace() };

        _repositoryMock
            .Setup(r => r.GetByOwnerIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaces);

        var result = await _sut.GetForUserAsync("owner-1", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(w => w.OwnerId == "owner-1");
    }

    [Test]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var workspaceId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var act = async () => await _sut.GetByIdAsync(workspaceId, "owner-1", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{workspaceId}*");
    }

    [Test]
    public async Task GetByIdAsync_CallerIsNotOwner_ThrowsForbiddenException()
    {
        var workspace = CreateWorkspace(ownerId: "owner-1");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var act = async () => await _sut.GetByIdAsync(workspace.Id, "someone-else", CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task UpdateAsync_CallerIsOwner_UpdatesNameAndPersists()
    {
        var workspace = CreateWorkspace(ownerId: "owner-1");
        var request = new UpdateWorkspaceRequest("Renamed");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await _sut.UpdateAsync(workspace.Id, "owner-1", request, CancellationToken.None);

        result.Name.Should().Be("Renamed");

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<Workspace>(w => w.Name == "Renamed"), default),
            Times.Once);
    }

    [Test]
    public async Task UpdateAsync_CallerIsNotOwner_ThrowsForbiddenExceptionAndDoesNotPersist()
    {
        var workspace = CreateWorkspace(ownerId: "owner-1");
        var request = new UpdateWorkspaceRequest("Renamed");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var act = async () => await _sut.UpdateAsync(workspace.Id, "someone-else", request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Workspace>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CallerIsOwner_DeletesWorkspace()
    {
        var workspace = CreateWorkspace(ownerId: "owner-1");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await _sut.DeleteAsync(workspace.Id, "owner-1", CancellationToken.None);

        _repositoryMock.Verify(r => r.DeleteAsync(workspace.Id, default), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CallerIsNotOwner_ThrowsForbiddenExceptionAndDoesNotDelete()
    {
        var workspace = CreateWorkspace(ownerId: "owner-1");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var act = async () => await _sut.DeleteAsync(workspace.Id, "someone-else", CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}

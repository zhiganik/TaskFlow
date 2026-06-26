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
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Mappings;
using TaskFlow.Application.Services;

namespace TaskFlow.Tests.Services;

[TestFixture]
public class WorkspacesServiceTests
{
    private Mock<IWorkspacesRepository> _repositoryMock = null!;
    private Mock<IWorkspaceMembersRepository> _membersRepositoryMock = null!;
    private Mock<ICacheService> _cacheMock = null!;
    private Mock<ILogger<WorkspacesService>> _loggerMock = null!;
    private IMapper _mapper = null!;

    private WorkspacesService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock        = new Mock<IWorkspacesRepository>();
        _membersRepositoryMock = new Mock<IWorkspaceMembersRepository>();
        _cacheMock             = new Mock<ICacheService>();
        _loggerMock            = new Mock<ILogger<WorkspacesService>>();
        _mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<WorkspaceProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _sut = new WorkspacesService(
            _repositoryMock.Object,
            _membersRepositoryMock.Object,
            _cacheMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    private static Workspace CreateWorkspace(string ownerId = "owner-1") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Engineering",
        OwnerId = ownerId,
        CreatedAt = DateTime.UtcNow
    };

    [Test]
    public async Task CreateAsync_ValidRequest_CreatesOwnerMembershipAndReturnsDto()
    {
        var request = new CreateWorkspaceRequest("Engineering");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace w, CancellationToken _) => w);

        var result = await _sut.CreateAsync("owner-1", request, CancellationToken.None);

        result.Name.Should().Be("Engineering");
        result.OwnerId.Should().Be("owner-1");
        result.MyRole.Should().Be(WorkspaceRole.Owner);

        _repositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Workspace>(w =>
                    w.Name == "Engineering" &&
                    w.OwnerId == "owner-1" &&
                    w.Members.Count == 1 &&
                    w.Members.Single().UserId == "owner-1" &&
                    w.Members.Single().Role == WorkspaceRole.Owner),
                default),
            Times.Once);
    }

    [Test]
    public async Task GetForUserAsync_ReturnsWorkspacesWithCallerRole()
    {
        var workspaceA = CreateWorkspace();
        var workspaceB = CreateWorkspace("owner-2");

        var memberships = new List<WorkspaceMember>
        {
            new() { WorkspaceId = workspaceA.Id, UserId = "user-1", Role = WorkspaceRole.Owner, Workspace = workspaceA },
            new() { WorkspaceId = workspaceB.Id, UserId = "user-1", Role = WorkspaceRole.Member, Workspace = workspaceB }
        };

        _membersRepositoryMock
            .Setup(r => r.GetMembershipsForUserAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberships);

        var result = await _sut.GetForUserAsync("user-1", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(w => w.Id == workspaceA.Id && w.MyRole == WorkspaceRole.Owner);
        result.Should().ContainSingle(w => w.Id == workspaceB.Id && w.MyRole == WorkspaceRole.Member);
    }

    [Test]
    public async Task GetByIdAsync_WorkspaceNotFound_ThrowsNotFoundException()
    {
        var workspaceId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var act = async () => await _sut.GetByIdAsync(workspaceId, "user-1", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{workspaceId}*");
    }

    [Test]
    public async Task GetByIdAsync_NoMembership_ThrowsForbiddenException()
    {
        var workspace = CreateWorkspace();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(workspace.Id, "someone-else", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceMember?)null);

        var act = async () => await _sut.GetByIdAsync(workspace.Id, "someone-else", CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task GetByIdAsync_ValidRequest_ReturnsDtoWithCallerRole()
    {
        var workspace = CreateWorkspace();
        var member = new WorkspaceMember { WorkspaceId = workspace.Id, UserId = "admin-1", Role = WorkspaceRole.Admin };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(workspace.Id, "admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _sut.GetByIdAsync(workspace.Id, "admin-1", CancellationToken.None);

        result.MyRole.Should().Be(WorkspaceRole.Admin);
    }

    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesNameAndReturnsOwnerRole()
    {
        var workspace = CreateWorkspace();
        var request = new UpdateWorkspaceRequest("Renamed");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await _sut.UpdateAsync(workspace.Id, request, CancellationToken.None);

        result.Name.Should().Be("Renamed");
        result.MyRole.Should().Be(WorkspaceRole.Owner);

        _membersRepositoryMock.VerifyNoOtherCalls();
        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<Workspace>(w => w.Name == "Renamed"), default),
            Times.Once);
    }

    [Test]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        var workspaceId = Guid.NewGuid();
        var request = new UpdateWorkspaceRequest("Renamed");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var act = async () => await _sut.UpdateAsync(workspaceId, request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Workspace>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ValidRequest_DeletesWorkspace()
    {
        var workspace = CreateWorkspace();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await _sut.DeleteAsync(workspace.Id, CancellationToken.None);

        _repositoryMock.Verify(r => r.DeleteAsync(workspace.Id, default), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        var workspaceId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        var act = async () => await _sut.DeleteAsync(workspaceId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}

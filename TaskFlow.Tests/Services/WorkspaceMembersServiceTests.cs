using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
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
public class WorkspaceMembersServiceTests
{
    private Mock<IWorkspaceMembersRepository>      _membersRepositoryMock = null!;
    private Mock<ICacheService>                    _cacheMock             = null!;
    private Mock<UserManager<AppUser>>             _userManagerMock       = null!;
    private Mock<ILogger<WorkspaceMembersService>> _loggerMock            = null!;
    private IMapper _mapper = null!;

    private WorkspaceMembersService _sut = null!;

    private static readonly Guid WorkspaceId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _membersRepositoryMock = new Mock<IWorkspaceMembersRepository>();
        _cacheMock             = new Mock<ICacheService>();

        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                        .ReturnsAsync((AppUser?)null);

        _loggerMock = new Mock<ILogger<WorkspaceMembersService>>();
        _mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<WorkspaceMemberProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _sut = new WorkspaceMembersService(
            _membersRepositoryMock.Object,
            _cacheMock.Object,
            _userManagerMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    private static AppUser CreateUser(string id = "user-2") => new()
    {
        Id = id,
        Email = "member@example.com",
        UserName = "member@example.com",
        DisplayName = "Jane Member"
    };

    [Test]
    public async Task GetMembersAsync_ReturnsMappedMembers()
    {
        var user = CreateUser();
        var members = new List<WorkspaceMember>
        {
            new() { WorkspaceId = WorkspaceId, UserId = user.Id, Role = WorkspaceRole.Member, JoinedAt = DateTime.UtcNow, User = user }
        };

        _membersRepositoryMock
            .Setup(r => r.GetMembersAsync(WorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var result = await _sut.GetMembersAsync(WorkspaceId, CancellationToken.None);

        result.Should().ContainSingle(m => m.UserId == user.Id && m.DisplayName == "Jane Member" && m.Role == WorkspaceRole.Member);
    }

    [Test]
    public async Task UpdateRoleAsync_NotMember_ThrowsNotFoundException()
    {
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, "user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceMember?)null);

        var act = async () => await _sut.UpdateRoleAsync(
            WorkspaceId, "user-2", new UpdateMemberRoleRequest(WorkspaceRole.Admin), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task UpdateRoleAsync_TargetIsOwner_ThrowsConflictException()
    {
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMember { WorkspaceId = WorkspaceId, UserId = "owner-1", Role = WorkspaceRole.Owner });

        var act = async () => await _sut.UpdateRoleAsync(
            WorkspaceId, "owner-1", new UpdateMemberRoleRequest(WorkspaceRole.Admin), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _membersRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkspaceMember>(), default), Times.Never);
    }

    [Test]
    public async Task UpdateRoleAsync_ValidRequest_UpdatesRoleAndReturnsDto()
    {
        var user = CreateUser();
        var member = new WorkspaceMember { WorkspaceId = WorkspaceId, UserId = user.Id, Role = WorkspaceRole.Member };

        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.UpdateRoleAsync(
            WorkspaceId, user.Id, new UpdateMemberRoleRequest(WorkspaceRole.Admin), CancellationToken.None);

        result.Role.Should().Be(WorkspaceRole.Admin);
        _membersRepositoryMock.Verify(
            r => r.UpdateAsync(It.Is<WorkspaceMember>(m => m.Role == WorkspaceRole.Admin), default),
            Times.Once);
    }

    [Test]
    public async Task RemoveAsync_NotMember_ThrowsNotFoundException()
    {
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, "user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceMember?)null);

        var act = async () => await _sut.RemoveAsync(WorkspaceId, "user-2", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RemoveAsync_TargetIsOwner_ThrowsConflictException()
    {
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMember { WorkspaceId = WorkspaceId, UserId = "owner-1", Role = WorkspaceRole.Owner });

        var act = async () => await _sut.RemoveAsync(WorkspaceId, "owner-1", CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _membersRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    [Test]
    public async Task RemoveAsync_ValidRequest_RemovesMember()
    {
        var user = CreateUser();

        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMember { WorkspaceId = WorkspaceId, UserId = user.Id, Role = WorkspaceRole.Member });

        await _sut.RemoveAsync(WorkspaceId, user.Id, CancellationToken.None);

        _membersRepositoryMock.Verify(r => r.DeleteAsync(WorkspaceId, user.Id, default), Times.Once);
    }
}

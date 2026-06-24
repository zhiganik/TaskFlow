using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using TaskFlow.Api.Authorization;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.Interfaces.Repositories;

namespace TaskFlow.Tests.Authorization;

[TestFixture]
public class WorkspaceRoleHandlerTests
{
    private Mock<IWorkspaceMembersRepository> _membersRepositoryMock = null!;
    private WorkspaceRoleHandler _sut = null!;

    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private const string UserId = "user-1";

    [SetUp]
    public void SetUp()
    {
        _membersRepositoryMock = new Mock<IWorkspaceMembersRepository>();
        _sut = new WorkspaceRoleHandler(_membersRepositoryMock.Object);
    }

    private static HttpContext CreateHttpContext(bool includeWorkspaceId = true, string? userId = UserId)
    {
        var httpContext = new DefaultHttpContext();

        var routeData = new RouteData();
        if (includeWorkspaceId)
            routeData.Values["workspaceId"] = WorkspaceId.ToString();
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });

        var claims = userId is null ? [] : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return httpContext;
    }

    private static AuthorizationHandlerContext CreateContext(
        WorkspaceRoleRequirement requirement, HttpContext httpContext) =>
        new([requirement], httpContext.User, httpContext);

    [TestCase(WorkspaceRole.Owner, WorkspaceRole.Member)]
    [TestCase(WorkspaceRole.Admin, WorkspaceRole.Member)]
    [TestCase(WorkspaceRole.Member, WorkspaceRole.Member)]
    [TestCase(WorkspaceRole.Owner, WorkspaceRole.Owner)]
    public async Task HandleAsync_CallerRoleMeetsMinimum_Succeeds(WorkspaceRole callerRole, WorkspaceRole minimumRole)
    {
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMember { WorkspaceId = WorkspaceId, UserId = UserId, Role = callerRole });

        var requirement = new WorkspaceRoleRequirement(minimumRole);
        var context = CreateContext(requirement, CreateHttpContext());

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_CallerRoleBelowMinimum_DoesNotSucceed()
    {
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMember { WorkspaceId = WorkspaceId, UserId = UserId, Role = WorkspaceRole.Member });

        var requirement = new WorkspaceRoleRequirement(WorkspaceRole.Admin);
        var context = CreateContext(requirement, CreateHttpContext());

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Test]
    public async Task HandleAsync_NoMembership_DoesNotSucceed()
    {
        _membersRepositoryMock
            .Setup(r => r.GetMemberAsync(WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceMember?)null);

        var requirement = new WorkspaceRoleRequirement(WorkspaceRole.Member);
        var context = CreateContext(requirement, CreateHttpContext());

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Test]
    public async Task HandleAsync_MissingWorkspaceIdRouteValue_DoesNotSucceed()
    {
        var requirement = new WorkspaceRoleRequirement(WorkspaceRole.Member);
        var context = CreateContext(requirement, CreateHttpContext(includeWorkspaceId: false));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        _membersRepositoryMock.Verify(
            r => r.GetMemberAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_MissingUserIdClaim_DoesNotSucceed()
    {
        var requirement = new WorkspaceRoleRequirement(WorkspaceRole.Member);
        var context = CreateContext(requirement, CreateHttpContext(userId: null));

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        _membersRepositoryMock.Verify(
            r => r.GetMemberAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_ResourceIsNotHttpContext_DoesNotSucceed()
    {
        var requirement = new WorkspaceRoleRequirement(WorkspaceRole.Member);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), resource: null);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}

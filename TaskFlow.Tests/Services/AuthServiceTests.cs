using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;
using TaskFlow.Application.Services;

namespace TaskFlow.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<UserManager<AppUser>> _userManagerMock = null!;
    private Mock<IJwtService> _jwtServiceMock = null!;
    private Mock<ICacheService> _cacheMock = null!;
    private Mock<ILogger<AuthService>> _loggerMock = null!;
    private JwtOptions _jwtOptions = null!;

    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtServiceMock = new Mock<IJwtService>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _jwtOptions = new JwtOptions
        {
            Issuer = "taskflow-api",
            Audience = "taskflow-clients",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
            Secret = "test-secret"
        };

        _sut = new AuthService(
            _userManagerMock.Object,
            _jwtServiceMock.Object,
            _cacheMock.Object,
            Options.Create(_jwtOptions),
            _loggerMock.Object);
    }

    private static AppUser CreateUser() => new()
    {
        Id = "user-1",
        Email = "user@example.com",
        UserName = "user@example.com",
        DisplayName = "Jane Doe"
    };

    // Mirrors AuthService's private RefreshKey so tests assert behavior, not a hardcoded string.
    private static string RefreshKey(string token) => $"refresh-token:{token}";

    [Test]
    public async Task RegisterAsync_ValidRequest_ReturnsUserDto()
    {
        var request = new RegisterRequest("user@example.com", "Jane Doe", "Passw0rd!");

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.RegisterAsync(request);

        result.Email.Should().Be(request.Email);
        result.DisplayName.Should().Be(request.DisplayName);

        _userManagerMock.Verify(
            m => m.CreateAsync(
                It.Is<AppUser>(u => u.Email == request.Email && u.UserName == request.Email),
                request.Password),
            Times.Once);
    }

    [Test]
    public async Task RegisterAsync_CreateFails_ThrowsConflictException()
    {
        var request = new RegisterRequest("user@example.com", "Jane Doe", "weak");

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "DuplicateUserName", Description = "Email already taken." }));

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already taken*");
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponseAndStoresRefreshToken()
    {
        var user = CreateUser();
        var request = new LoginRequest(user.Email!, "Passw0rd!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        _jwtServiceMock.Setup(s => s.GenerateAccessToken(user)).Returns("access-token");
        _jwtServiceMock.Setup(s => s.GenerateRefreshToken()).Returns("refresh-token");

        var result = await _sut.LoginAsync(request);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.UserId.Should().Be(user.Id);

        _cacheMock.Verify(c => c.SetAsync(
                RefreshKey("refresh-token"), user.Id,
                TimeSpan.FromDays(_jwtOptions.RefreshTokenExpiryDays),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _userManagerMock.Verify(m => m.AccessFailedAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Test]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest("missing@example.com", "Passw0rd!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((AppUser?)null);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Test]
    public async Task LoginAsync_AccountLockedOut_ThrowsGenericUnauthorizedException()
    {
        var user = CreateUser();
        var request = new LoginRequest(user.Email!, "Passw0rd!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var act = async () => await _sut.LoginAsync(request);

        // Lockout must look identical to a wrong password — no account-state leakage.
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");

        _userManagerMock.Verify(m => m.CheckPasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task LoginAsync_WrongPassword_RecordsAccessFailureAndThrowsUnauthorizedException()
    {
        var user = CreateUser();
        var request = new LoginRequest(user.Email!, "WrongPassword!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");

        _userManagerMock.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Test]
    public async Task RefreshAsync_ValidToken_RevokesOldTokenAndIssuesNewOnes()
    {
        var user = CreateUser();
        var request = new RefreshTokenRequest("old-refresh-token");

        _cacheMock.Setup(c => c.GetAsync<string>(RefreshKey("old-refresh-token"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.Id);
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        _jwtServiceMock.Setup(s => s.GenerateAccessToken(user)).Returns("new-access-token");
        _jwtServiceMock.Setup(s => s.GenerateRefreshToken()).Returns("new-refresh-token");

        var result = await _sut.RefreshAsync(request);

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");

        _cacheMock.Verify(
            c => c.InvalidateAsync(RefreshKey("old-refresh-token"), It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(c => c.SetAsync(
                RefreshKey("new-refresh-token"), user.Id,
                TimeSpan.FromDays(_jwtOptions.RefreshTokenExpiryDays),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RefreshAsync_TokenNotFoundInCache_ThrowsUnauthorizedException()
    {
        var request = new RefreshTokenRequest("bogus-token");

        _cacheMock.Setup(c => c.GetAsync<string>(RefreshKey("bogus-token"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.RefreshAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>();

        _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RefreshAsync_UserNoLongerExists_ThrowsUnauthorizedExceptionButStillRevokesToken()
    {
        var request = new RefreshTokenRequest("old-refresh-token");

        _cacheMock.Setup(c => c.GetAsync<string>(RefreshKey("old-refresh-token"), It.IsAny<CancellationToken>()))
            .ReturnsAsync("deleted-user-id");
        _userManagerMock.Setup(m => m.FindByIdAsync("deleted-user-id")).ReturnsAsync((AppUser?)null);

        var act = async () => await _sut.RefreshAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>();

        _cacheMock.Verify(
            c => c.InvalidateAsync(RefreshKey("old-refresh-token"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task LogoutAsync_InvalidatesRefreshToken()
    {
        var request = new RefreshTokenRequest("some-refresh-token");

        await _sut.LogoutAsync(request);

        _cacheMock.Verify(
            c => c.InvalidateAsync(RefreshKey("some-refresh-token"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

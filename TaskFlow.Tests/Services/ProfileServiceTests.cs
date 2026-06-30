using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Mappings;
using TaskFlow.Application.Services;

namespace TaskFlow.Tests.Services;

[TestFixture]
public class ProfileServiceTests
{
    private Mock<UserManager<AppUser>> _userManagerMock = null!;
    private Mock<IWorkspaceMembersRepository> _membersRepoMock = null!;
    private Mock<ICacheService> _cacheMock = null!;
    private Mock<ILogger<ProfileService>> _loggerMock = null!;
    private IMapper _mapper = null!;
    private ProfileService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _userManagerMock
            .Setup(m => m.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());
        _userManagerMock
            .Setup(m => m.NormalizeName(It.IsAny<string>()))
            .Returns<string>(n => n.ToUpperInvariant());

        _membersRepoMock = new Mock<IWorkspaceMembersRepository>();
        _membersRepoMock
            .Setup(r => r.GetMembershipsForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _cacheMock = new Mock<ICacheService>();
        _cacheMock
            .Setup(c => c.InvalidateManyAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _loggerMock = new Mock<ILogger<ProfileService>>();

        _mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<UserProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        _sut = new ProfileService(
            _userManagerMock.Object,
            new Mock<IBlobService>().Object,
            new Mock<ITemporaryFileStore>().Object,
            new Mock<IMessagePublisher>().Object,
            _membersRepoMock.Object,
            _cacheMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    private static AppUser CreateUser() => new()
    {
        Id = "user-1",
        Email = "user@example.com",
        UserName = "user@example.com",
        DisplayName = "Jane Doe",
        AvatarColor = "#818cf8",
    };

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAsync_UserExists_ReturnsMappedUserDto()
    {
        var user = CreateUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.GetAsync(user.Id);

        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.DisplayName.Should().Be(user.DisplayName);
        result.AvatarColor.Should().Be(user.AvatarColor);
    }

    [Test]
    public async Task GetAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((AppUser?)null);

        var act = async () => await _sut.GetAsync("missing");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_AllFieldsChanged_UpdatesAndReturnsDto()
    {
        var user = CreateUser();
        var request = new UpdateProfileRequest("New Name", "new@example.com");

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((AppUser?)null);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateAsync(user.Id, request);

        result.DisplayName.Should().Be("New Name");
        result.Email.Should().Be("new@example.com");

        _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_EmailUnchanged_SkipsEmailUniquenessCheck()
    {
        var user = CreateUser();
        var request = new UpdateProfileRequest("Jane Doe", user.Email!);

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _sut.UpdateAsync(user.Id, request);

        _userManagerMock.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_EmailChangedToExistingEmail_ThrowsConflictException()
    {
        var user = CreateUser();
        var other = new AppUser { Id = "user-2", Email = "taken@example.com" };
        var request = new UpdateProfileRequest("Jane Doe", "taken@example.com");

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.FindByEmailAsync("taken@example.com")).ReturnsAsync(other);

        var act = async () => await _sut.UpdateAsync(user.Id, request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already in use*");
    }

    [Test]
    public async Task UpdateAsync_EmailChanged_NormalizesEmailAndUsername()
    {
        var user = CreateUser();
        var request = new UpdateProfileRequest("Jane Doe", "NEW@example.com");

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((AppUser?)null);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _sut.UpdateAsync(user.Id, request);

        user.Email.Should().Be("NEW@example.com");
        user.NormalizedEmail.Should().Be("NEW@EXAMPLE.COM");
        user.UserName.Should().Be("NEW@example.com");
        user.NormalizedUserName.Should().Be("NEW@EXAMPLE.COM");
    }

    [Test]
    public async Task UpdateAsync_IdentityUpdateFails_ThrowsBadRequestException()
    {
        var user = CreateUser();
        var request = new UpdateProfileRequest("Jane Doe", user.Email!);

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "SomeError", Description = "Something went wrong." }));

        var act = async () => await _sut.UpdateAsync(user.Id, request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Something went wrong.");
    }

    [Test]
    public async Task UpdateAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((AppUser?)null);

        var act = async () => await _sut.UpdateAsync("missing",
            new UpdateProfileRequest("Name", "a@b.com"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Test]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_Succeeds()
    {
        var user = CreateUser();
        var request = new ChangePasswordRequest("OldPassw0rd", "NewPassw0rd");

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock
            .Setup(m => m.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var act = async () => await _sut.ChangePasswordAsync(user.Id, request);

        await act.Should().NotThrowAsync();

        _userManagerMock.Verify(
            m => m.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword),
            Times.Once);
    }

    [Test]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsBadRequestException()
    {
        var user = CreateUser();
        var request = new ChangePasswordRequest("WrongPassword", "NewPassw0rd");

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock
            .Setup(m => m.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password." }));

        var act = async () => await _sut.ChangePasswordAsync(user.Id, request);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Incorrect password.");
    }

    [Test]
    public async Task ChangePasswordAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((AppUser?)null);

        var act = async () => await _sut.ChangePasswordAsync("missing",
            new ChangePasswordRequest("Old", "New"));

        await act.Should().ThrowAsync<NotFoundException>();

        _userManagerMock.Verify(
            m => m.ChangePasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}

using FluentAssertions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Validators;

namespace TaskFlow.Tests.Validators;

[TestFixture]
public class UpdateProfileRequestValidatorTests
{
    private UpdateProfileRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateProfileRequestValidator();

    // ── DisplayName ───────────────────────────────────────────────────────────

    [Test]
    public async Task Validate_EmptyDisplayName_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new UpdateProfileRequest("", "user@example.com"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Test]
    public async Task Validate_DisplayNameTooLong_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new UpdateProfileRequest(new string('a', 101), "user@example.com"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    // ── Email ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Validate_EmptyEmail_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new UpdateProfileRequest("Jane Doe", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Test]
    public async Task Validate_InvalidEmail_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new UpdateProfileRequest("Jane Doe", "not-an-email"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // ── Valid ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(
            new UpdateProfileRequest("Jane Doe", "user@example.com"));

        result.IsValid.Should().BeTrue();
    }
}

[TestFixture]
public class ChangePasswordRequestValidatorTests
{
    private ChangePasswordRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new ChangePasswordRequestValidator();

    // ── CurrentPassword ───────────────────────────────────────────────────────

    [Test]
    public async Task Validate_EmptyCurrentPassword_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new ChangePasswordRequest("", "NewPassw0rd"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
    }

    // ── NewPassword ───────────────────────────────────────────────────────────

    [Test]
    public async Task Validate_EmptyNewPassword_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new ChangePasswordRequest("OldPassw0rd", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Test]
    public async Task Validate_NewPasswordTooShort_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new ChangePasswordRequest("OldPassw0rd", "Ab1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Test]
    public async Task Validate_NewPasswordNoDigit_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new ChangePasswordRequest("OldPassw0rd", "Abcdefgh"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Test]
    public async Task Validate_NewPasswordNoUppercase_ReturnsError()
    {
        var result = await _validator.ValidateAsync(
            new ChangePasswordRequest("OldPassw0rd", "abcdef1g"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    // ── Valid ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(
            new ChangePasswordRequest("OldPassw0rd", "NewPassw0rd"));

        result.IsValid.Should().BeTrue();
    }
}

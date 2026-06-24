using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshTokenValidator) : ControllerBase
{
    /// <summary>Register a new account.</summary>
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);
        var result = await authService.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Log in and receive an access token and refresh token.</summary>
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);
        var result = await authService.LoginAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Exchange a refresh token for a new access token and refresh token.</summary>
    [HttpPost("refresh")]
    [SwaggerOperation(Summary = "Refresh access token", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        await refreshTokenValidator.ValidateAndThrowAsync(request, ct);
        var result = await authService.RefreshAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Revoke a refresh token.</summary>
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Logout", Tags = new[] { "Auth" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await refreshTokenValidator.ValidateAndThrowAsync(request, ct);
        await authService.LogoutAsync(request, ct);
        return NoContent();
    }
}

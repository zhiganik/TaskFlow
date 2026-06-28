using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class ProfileController(
    IProfileService profileService,
    IBlobService blobService,
    IOptions<StorageOptions> storageOpts, ILogger<ProfileController> logger) : ControllerBase
{
    private readonly StorageOptions _storage = storageOpts.Value;

    /// <summary>Get the authenticated user's profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await profileService.GetAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Update display name, email, or avatar color.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await profileService.UpdateAsync(User.GetUserId(), request, ct);
        return Ok(result);
    }

    /// <summary>Change password using the current password for verification.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await profileService.ChangePasswordAsync(User.GetUserId(), request, ct);
        return NoContent();
    }

    /// <summary>Upload a custom avatar image (image/* multipart, max 10 MB). Returns 200 with Pending status while the worker processes it.</summary>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        var req = new UploadFileRequest(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
        var result = await profileService.UploadAvatarAsync(User.GetUserId(), req, ct);
        return Ok(result);
    }

    /// <summary>Remove the custom avatar and revert to the color-based initials display.</summary>
    [HttpDelete("avatar")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveAvatar(CancellationToken ct)
    {
        var result = await profileService.RemoveAvatarAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Serve a processed avatar image. Requires authentication — the filename is a GUID, not guessable, but access is still restricted to signed-in users.</summary>
    [HttpGet("avatar/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvatar(string fileName, CancellationToken ct)
    {
        var fullPath = Path.Combine(_storage.BasePath, "avatars", fileName);
        
        try
        {
            var stream = await blobService.ReadAsync(fullPath, ct);
            return File(stream, "image/jpeg", enableRangeProcessing: false);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }
}

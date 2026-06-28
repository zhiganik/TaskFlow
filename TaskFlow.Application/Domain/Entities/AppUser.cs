using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Entities;

public class AppUser : IdentityUser
{
    public string      DisplayName  { get; set; } = string.Empty;
    public string      AvatarColor  { get; set; } = "#818cf8";
    public string?     AvatarPath   { get; set; }
    public AvatarStatus AvatarStatus { get; set; } = AvatarStatus.None;
    public DateTime    CreatedAt    { get; set; }
}

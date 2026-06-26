using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Application.Domain.Entities;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#818cf8";
    public DateTime CreatedAt { get; set; }
}
using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Application.Domain.Entities;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
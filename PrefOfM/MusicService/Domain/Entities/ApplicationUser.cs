using Microsoft.AspNetCore.Identity;

namespace MusicService.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateOnly? Birthday { get; set; }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Domain.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateOnly? Birthday { get; set; }
}
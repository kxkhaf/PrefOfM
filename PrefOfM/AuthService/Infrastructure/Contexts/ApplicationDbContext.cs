using AuthService.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{

    public DbSet<ApplicationUser> ApplicationUsers { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("ApplicationUsers", t => 
                t.HasCheckConstraint(
                    name: "CK_ApplicationUsers_Birthday_NotInFuture",
                    sql: "\"Birthday\" <= CURRENT_DATE"));
        });
    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.Auth.Entities;

namespace BKM.Utility.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Auth feature.
/// Owns: ASP.NET Identity tables (Users, Roles, Claims, Logins, Tokens),
///       RefreshTokens, Permissions, RolePermissions.
/// Completely independent — no other feature's tables are present.
/// Connection string key: "Auth" (falls back to "DefaultConnection").
/// </summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<AppUser, AppRole, string>(options)
{
    public DbSet<RefreshToken>   RefreshTokens   => Set<RefreshToken>();
    public DbSet<Permission>     Permissions     => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // provisions all Identity tables

        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("Permissions", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_Permissions_Name");
            e.Property(x => x.Resource).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.RoleId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.RoleId, x.PermissionId })
             .IsUnique()
             .HasDatabaseName("UX_RolePermissions_RoleId_PermissionId");
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).HasDatabaseName("IX_RefreshTokens_TokenHash");
            e.HasIndex(x => x.UserId).HasDatabaseName("IX_RefreshTokens_UserId");
            e.Property(x => x.DeviceInfo).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}

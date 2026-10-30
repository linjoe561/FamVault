using FamVault.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Modules.Identity.Infrastructure;

public sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    // 不用 "identity"：它是 T-SQL 保留字，手寫 SQL 時每次都得加中括號。
    public const string Schema = "auth";

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<AppUser>(user =>
        {
            user.ToTable("Users");
            user.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        });
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        builder.Entity<RefreshToken>(token =>
        {
            token.ToTable("RefreshTokens");
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // 角色是固定的，用 Migration 種子資料建立；Id 與 ConcurrencyStamp 必須是常數，否則每次都會產生新的 Migration。
        builder.Entity<AppRole>().HasData(
            new AppRole
            {
                Id = new Guid("8f1c2b1e-3d5a-4c7e-9a10-000000000001"),
                Name = RoleNames.Admin,
                NormalizedName = RoleNames.Admin.ToUpperInvariant(),
                ConcurrencyStamp = "role-admin",
            },
            new AppRole
            {
                Id = new Guid("8f1c2b1e-3d5a-4c7e-9a10-000000000002"),
                Name = RoleNames.Member,
                NormalizedName = RoleNames.Member.ToUpperInvariant(),
                ConcurrencyStamp = "role-member",
            });
    }
}

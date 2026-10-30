using FamVault.SharedKernel.Auth;
using FamVault.SharedKernel.Domain;
using FamVault.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamVault.Tests.SharedKernel;

public class AuditableEntityInterceptorTests
{
    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId => userId;
        public string? DisplayName => null;
        public bool IsInRole(string role) => false;
    }

    private sealed class Note : AuditableEntity
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Note> Notes => Set<Note>();
    }

    [Fact]
    public async Task Fills_created_fields_on_add_and_updated_fields_on_modify()
    {
        var userId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(new FakeCurrentUser(userId), TimeProvider.System))
            .Options;
        await using var db = new TestDbContext(options);

        var note = new Note { Text = "牛奶" };
        db.Notes.Add(note);
        await db.SaveChangesAsync();

        Assert.Equal(userId, note.CreatedBy);
        Assert.NotEqual(default, note.CreatedAt);
        Assert.Null(note.UpdatedAt);

        note.Text = "鮮奶";
        await db.SaveChangesAsync();

        Assert.Equal(userId, note.UpdatedBy);
        Assert.NotNull(note.UpdatedAt);
    }
}

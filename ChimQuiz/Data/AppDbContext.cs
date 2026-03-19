using ChimQuiz.Models;
using Microsoft.EntityFrameworkCore;

namespace ChimQuiz.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Pseudo).IsUnique();
            e.Property(p => p.Pseudo).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<GameSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne<Player>().WithMany().HasForeignKey(s => s.PlayerId);
        });
    }
}

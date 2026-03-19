using ChimQuiz.Models;
using Microsoft.EntityFrameworkCore;

namespace ChimQuiz.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Player> Players => Set<Player>();
        public DbSet<GameSession> GameSessions => Set<GameSession>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<Player>(e =>
            {
                _ = e.HasKey(p => p.Id);
                _ = e.HasIndex(p => p.Pseudo).IsUnique();
                _ = e.Property(p => p.Pseudo).HasMaxLength(30).IsRequired();
            });

            _ = modelBuilder.Entity<GameSession>(e =>
            {
                _ = e.HasKey(s => s.Id);
                _ = e.HasOne<Player>().WithMany().HasForeignKey(s => s.PlayerId);
            });
        }
    }
}

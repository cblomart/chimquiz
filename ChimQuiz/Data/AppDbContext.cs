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
                e.ToContainer("Players");
                e.HasPartitionKey(p => p.Id);
                e.HasNoDiscriminator();
                // Computed client-side properties — not stored in Cosmos
                e.Ignore(p => p.RankName);
                e.Ignore(p => p.RankEmoji);
                e.Ignore(p => p.XpForCurrentRank);
                e.Ignore(p => p.XpForNextRank);
                e.Ignore(p => p.RankProgressPercent);
            });

            _ = modelBuilder.Entity<GameSession>(e =>
            {
                e.ToContainer("GameSessions");
                e.HasPartitionKey(s => s.PlayerId);
                e.HasNoDiscriminator();
                // Computed client-side property — not stored in Cosmos
                e.Ignore(s => s.WeekStart);
            });
        }
    }
}

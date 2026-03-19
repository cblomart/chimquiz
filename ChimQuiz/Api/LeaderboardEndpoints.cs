using ChimQuiz.Data;
using ChimQuiz.Models;
using Microsoft.EntityFrameworkCore;

namespace ChimQuiz.Api
{
    public static class LeaderboardEndpoints
    {
        public static RouteGroupBuilder MapLeaderboardApi(this RouteGroupBuilder group)
        {
            _ = group.MapGet("/leaderboard/alltime", GetAllTime);
            _ = group.MapGet("/leaderboard/weekly", GetWeekly);
            return group;
        }

        private static async Task<IResult> GetAllTime(AppDbContext db)
        {
            // Materialize first: RankEmoji/RankName are computed C# properties, not stored in Cosmos
            List<Player> players = await db.Players
                .OrderByDescending(p => p.BestSessionXp)
                .Take(10)
                .ToListAsync();

            var scores = players.Select(p => new
            {
                p.Pseudo,
                Score = p.BestSessionXp,
                p.TotalXp,
                p.RankEmoji,
                p.RankName,
                p.CurrentStreak
            });
            return Results.Ok(scores);
        }

        private static async Task<IResult> GetWeekly(AppDbContext db)
        {
            DateTime weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;

            // Pseudo and RankEmoji are denormalized into GameSession — no cross-container JOIN needed
            var scores = await db.GameSessions
                .Where(s => s.IsCompleted && s.StartedAt >= weekStart)
                .OrderByDescending(s => s.XpEarned)
                .Take(10)
                .Select(s => new
                {
                    s.Pseudo,
                    s.RankEmoji,
                    Score = s.XpEarned,
                    s.CorrectAnswers,
                    s.MaxCombo
                })
                .ToListAsync();
            return Results.Ok(scores);
        }
    }
}

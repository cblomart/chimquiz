using ChimQuiz.Data;
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
            var scores = await db.Players
                .OrderByDescending(p => p.BestSessionXp)
                .Take(10)
                .Select(p => new
                {
                    p.Pseudo,
                    Score = p.BestSessionXp,
                    p.TotalXp,
                    p.RankEmoji,
                    p.RankName,
                    p.CurrentStreak
                })
                .ToListAsync();
            return Results.Ok(scores);
        }

        private static async Task<IResult> GetWeekly(AppDbContext db)
        {
            DateTime weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;
            var scores = await db.GameSessions
                .Where(s => s.IsCompleted && s.StartedAt >= weekStart)
                .Join(db.Players, s => s.PlayerId, p => p.Id, (s, p) => new
                {
                    p.Pseudo,
                    p.RankEmoji,
                    Score = s.XpEarned,
                    s.CorrectAnswers,
                    s.MaxCombo
                })
                .OrderByDescending(x => x.Score)
                .Take(10)
                .ToListAsync();
            return Results.Ok(scores);
        }
    }
}

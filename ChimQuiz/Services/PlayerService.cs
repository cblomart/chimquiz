using System.Text.RegularExpressions;
using ChimQuiz.Data;
using ChimQuiz.Models;
using Microsoft.EntityFrameworkCore;

namespace ChimQuiz.Services
{
    public partial class PlayerService(AppDbContext db)
    {
        private static readonly string[] Adjectives =
        [
            "Brillant", "Curieux", "Atomique", "Électrique", "Quantique",
            "Cosmique", "Ionique", "Dynamique", "Génial", "Épique",
            "Nucléaire", "Fulgural", "Magnétique", "Énergique", "Fantastique"
        ];

        private static readonly string[] ElementNicknames =
        [
            "Hydro", "Hélium", "Carbone", "Azote", "Néon",
            "Sodium", "Fer", "Or", "Argent", "Cuivre",
            "Radium", "Uranium", "Krypton", "Xénon", "Pluton"
        ];

        [GeneratedRegex(@"^[a-zA-Z0-9À-ÿ_-]{3,30}$")]
        private static partial Regex PseudoRegex();

        private readonly Random _random = new();

        public async Task<Player?> GetPlayerAsync(Guid id)
        {
            return await db.Players.FindAsync(id);
        }

        public async Task<Player> CreatePlayerAsync(string pseudo)
        {
            ValidatePseudo(pseudo);

            if (await db.Players.AnyAsync(p => p.Pseudo == pseudo))
            {
                throw new ArgumentException($"Le pseudo '{pseudo}' est déjà utilisé.");
            }

            Player player = new() { Pseudo = pseudo };
            _ = db.Players.Add(player);
            _ = await db.SaveChangesAsync();
            return player;
        }

        public async Task<Player> UpdatePseudoAsync(Guid id, string newPseudo)
        {
            ValidatePseudo(newPseudo);

            Player player = await db.Players.FindAsync(id)
                ?? throw new ArgumentException("Joueur introuvable.");

            if (await db.Players.AnyAsync(p => p.Pseudo == newPseudo && p.Id != id))
            {
                throw new ArgumentException($"Le pseudo '{newPseudo}' est déjà utilisé.");
            }

            player.Pseudo = newPseudo;
            _ = await db.SaveChangesAsync();
            return player;
        }

        public async Task UpdateStreakAsync(Guid playerId)
        {
            Player? player = await db.Players.FindAsync(playerId);
            if (player is null)
            {
                return;
            }

            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (player.LastPlayedDate == today)
            {
                return; // Already played today
            }

            if (player.LastPlayedDate == today.AddDays(-1))
            {
                // Consecutive day
                player.CurrentStreak++;
            }
            else
            {
                // Streak broken
                player.CurrentStreak = 1;
            }

            if (player.CurrentStreak > player.MaxStreak)
            {
                player.MaxStreak = player.CurrentStreak;
            }

            player.LastPlayedDate = today;
            _ = await db.SaveChangesAsync();
        }

        public async Task AddXpAsync(Guid playerId, int xp, int sessionXp)
        {
            Player? player = await db.Players.FindAsync(playerId);
            if (player is null)
            {
                return;
            }

            player.TotalXp += xp;
            if (sessionXp > player.BestSessionXp)
            {
                player.BestSessionXp = sessionXp;
            }

            _ = await db.SaveChangesAsync();
        }

        public async Task SaveCompletedSessionAsync(Guid playerId, QuizSessionState state)
        {
            Player? player = await db.Players.FindAsync(playerId);
            if (player is null)
            {
                return;
            }

            // Save game session record
            GameSession session = new()
            {
                Id = state.SessionId,
                PlayerId = playerId,
                StartedAt = state.StartedAt,
                EndedAt = DateTime.UtcNow,
                QuestionsTotal = state.Questions.Count,
                CorrectAnswers = state.CorrectCount,
                XpEarned = state.TotalXp,
                MaxCombo = state.MaxCombo,
                IsCompleted = true
            };

            // Avoid duplicate session saves (idempotent)
            if (!await db.GameSessions.AnyAsync(s => s.Id == session.Id))
            {
                _ = db.GameSessions.Add(session);
            }

            // Update player stats
            player.TotalXp += state.TotalXp;
            if (state.TotalXp > player.BestSessionXp)
            {
                player.BestSessionXp = state.TotalXp;
            }

            // Update streak
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (player.LastPlayedDate != today)
            {
                if (player.LastPlayedDate == today.AddDays(-1))
                {
                    player.CurrentStreak++;
                }
                else
                {
                    player.CurrentStreak = 1;
                }

                if (player.CurrentStreak > player.MaxStreak)
                {
                    player.MaxStreak = player.CurrentStreak;
                }

                player.LastPlayedDate = today;
            }

            _ = await db.SaveChangesAsync();
        }

        public string GeneratePseudo()
        {
            string adj = Adjectives[_random.Next(Adjectives.Length)];
            string elem = ElementNicknames[_random.Next(ElementNicknames.Length)];
            int num = _random.Next(10, 100);
            return $"{adj}{elem}{num}";
        }

        private static void ValidatePseudo(string pseudo)
        {
            if (string.IsNullOrWhiteSpace(pseudo))
            {
                throw new ArgumentException("Le pseudo ne peut pas être vide.");
            }

            if (!PseudoRegex().IsMatch(pseudo))
            {
                throw new ArgumentException("Le pseudo doit contenir entre 3 et 30 caractères (lettres, chiffres, tirets, underscores).");
            }
        }
    }
}

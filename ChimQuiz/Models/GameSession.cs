namespace ChimQuiz.Models
{
    public class GameSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PlayerId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public int QuestionsTotal { get; set; } = 10;
        public int CorrectAnswers { get; set; }
        public int XpEarned { get; set; }
        public int MaxCombo { get; set; }
        public bool IsCompleted { get; set; }
        // Denormalized from Player at session save time (avoids cross-container JOIN in Cosmos)
        public string Pseudo { get; set; } = string.Empty;
        public string RankEmoji { get; set; } = string.Empty;
        public DateTime WeekStart => StartedAt.AddDays(-(int)StartedAt.DayOfWeek).Date;
    }
}

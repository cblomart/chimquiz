namespace ChimQuiz.Models
{
    public class Player
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Pseudo { get; set; } = "";
        public int TotalXp { get; set; }
        public int BestSessionXp { get; set; }
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public DateOnly? LastPlayedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string RankName => TotalXp switch
        {
            < 300 => "Apprenti Chimiste",
            < 900 => "Chimiste Junior",
            < 2000 => "Chimiste",
            < 4500 => "Chimiste Expert",
            < 9000 => "Maître Chimiste",
            _ => "Lauréat Nobel"
        };

        public string RankEmoji => TotalXp switch
        {
            < 300 => "⚗️",
            < 900 => "🧪",
            < 2000 => "🔬",
            < 4500 => "⚛️",
            < 9000 => "🏆",
            _ => "🥇"
        };

        public int XpForCurrentRank => TotalXp switch
        {
            < 300 => 0,
            < 900 => 300,
            < 2000 => 900,
            < 4500 => 2000,
            < 9000 => 4500,
            _ => 9000
        };

        public int XpForNextRank => TotalXp switch
        {
            < 300 => 300,
            < 900 => 900,
            < 2000 => 2000,
            < 4500 => 4500,
            < 9000 => 9000,
            _ => int.MaxValue
        };

        public int RankProgressPercent
        {
            get
            {
                if (XpForNextRank == int.MaxValue)
                {
                    return 100;
                }

                int range = XpForNextRank - XpForCurrentRank;
                int progress = TotalXp - XpForCurrentRank;
                return range == 0 ? 100 : (int)(progress * 100.0 / range);
            }
        }
    }
}

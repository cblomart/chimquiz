using System.Text.Json.Serialization;

namespace ChimQuiz.Models
{
    public enum QuestionType { NameToSymbol, SymbolToName, SymbolToNameTyped, NameToSymbolTyped }

    public class QuizQuestionState
    {
        public int ElementId { get; set; }
        public QuestionType Type { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public List<string> Choices { get; set; } = [];
        public string Prompt { get; set; } = "";
        public string DisplayValue { get; set; } = "";
        public string? FunFact { get; set; }
        public string CommonUse { get; set; } = "";
        public string WhereToFind { get; set; } = "";
        [JsonIgnore]
        public bool IsTyped => Type is QuestionType.SymbolToNameTyped or QuestionType.NameToSymbolTyped;
    }

    public class QuizSessionState
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public Guid PlayerId { get; set; }
        public List<QuizQuestionState> Questions { get; set; } = [];
        public int CurrentIndex { get; set; }
        public int ComboCount { get; set; }
        public int MaxCombo { get; set; }
        public int TotalXp { get; set; }
        public int CorrectCount { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }
        public List<int> WrongElementIds { get; set; } = [];
        public int RevengeStartIndex { get; set; } = -1; // -1 = no revenge round
    }
}

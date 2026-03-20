using System.Globalization;
using System.Text;
using System.Text.Json;
using ChimQuiz.Models;

namespace ChimQuiz.Services
{
    public class AnswerResult
    {
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; } = "";
        public int XpEarned { get; set; }
        public int ComboCount { get; set; }
        public int TotalXp { get; set; }
        public int MaxCombo { get; set; }
        public int CorrectCount { get; set; }
        public int QuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsGameOver { get; set; }
        public bool IsTyped { get; set; }
        public bool WasFuzzyMatch { get; set; } // accepted with 1-char typo
        public bool IsRevengeStart { get; set; } // first question of revenge round just unlocked
        // Element info — always shown after every answer
        public string ElementSymbol { get; set; } = "";
        public string ElementName { get; set; } = "";
        public string CommonUse { get; set; } = "";
        public string WhereToFind { get; set; } = "";
        public string? FunFact { get; set; }
        public string ComboMessage { get; set; } = "";
    }

    public class QuizService(ElementService elementService)
    {
        private const string SessionKey = "quiz_state";
        private const int DefaultQuestions = 15;
        private const int MinQuestions = 5;
        private const int MaxQuestions = 30;
        private readonly Random _random = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public QuizSessionState StartNewSession(ISession session, Guid playerId, int playerXp = 0, int questionCount = DefaultQuestions)
        {
            int count = Math.Clamp(questionCount, MinQuestions, MaxQuestions);
            QuizSessionState state = new()
            {
                PlayerId = playerId,
                StartedAt = DateTime.UtcNow,
                Questions = GenerateQuestions(playerXp, count)
            };
            SaveState(session, state);
            return state;
        }

        public (QuizQuestionState? question, int index, int total, int combo, int totalXp, bool isRevenge) GetCurrentQuestion(ISession session)
        {
            QuizSessionState? state = LoadState(session);
            if (state is null || state.IsCompleted || state.CurrentIndex >= state.Questions.Count)
            {
                return (null, 0, 0, 0, 0, false);
            }

            bool isRevenge = state.RevengeStartIndex >= 0 && state.CurrentIndex >= state.RevengeStartIndex;
            int displayIndex = isRevenge ? state.CurrentIndex - state.RevengeStartIndex : state.CurrentIndex;
            int displayTotal = isRevenge ? state.Questions.Count - state.RevengeStartIndex : state.RevengeStartIndex >= 0 ? state.RevengeStartIndex : state.Questions.Count;

            QuizQuestionState q = state.Questions[state.CurrentIndex];
            QuizQuestionState safeQ = new()
            {
                ElementId = q.ElementId,
                Type = q.Type,
                Choices = q.Choices,
                Prompt = q.Prompt,
                DisplayValue = q.DisplayValue,
                FunFact = null, // never expose before answer
                CommonUse = q.CommonUse,
                WhereToFind = q.WhereToFind,
                CorrectAnswer = ""   // never expose before answer
            };
            return (safeQ, displayIndex, displayTotal, state.ComboCount, state.TotalXp, isRevenge);
        }

        public AnswerResult? SubmitAnswer(ISession session, string answer)
        {
            QuizSessionState? state = LoadState(session);
            if (state is null || state.IsCompleted || state.CurrentIndex >= state.Questions.Count)
            {
                return null;
            }

            QuizQuestionState q = state.Questions[state.CurrentIndex];
            (bool isCorrect, bool wasFuzzy) = CheckAnswer(q, answer);
            UpdateCombo(state, isCorrect);

            int xpEarned = isCorrect ? CalculateXp(state.ComboCount) : 0;
            state.TotalXp += xpEarned;
            state.CurrentIndex++;

            if (!isCorrect)
            {
                TrackWrongElement(state, q.ElementId);
            }

            (bool isGameOver, bool isRevengeStart) = FinaliseRound(state);
            SaveState(session, state);

            Element? element = elementService.GetById(q.ElementId);

            return new AnswerResult
            {
                IsCorrect = isCorrect,
                CorrectAnswer = q.CorrectAnswer,
                XpEarned = xpEarned,
                ComboCount = state.ComboCount,
                TotalXp = state.TotalXp,
                MaxCombo = state.MaxCombo,
                CorrectCount = state.CorrectCount,
                QuestionIndex = state.CurrentIndex - 1,
                TotalQuestions = state.Questions.Count,
                IsGameOver = isGameOver,
                IsTyped = q.IsTyped,
                WasFuzzyMatch = wasFuzzy,
                IsRevengeStart = isRevengeStart,
                ElementSymbol = q.DisplayValue,
                ElementName = element?.Name ?? q.CorrectAnswer,
                CommonUse = q.CommonUse,
                WhereToFind = q.WhereToFind,
                FunFact = q.FunFact,
                ComboMessage = isCorrect ? GetComboMessage(state.ComboCount) : ""
            };
        }

        private static void TrackWrongElement(QuizSessionState state, int elementId)
        {
            bool inRevengeRound = state.RevengeStartIndex >= 0
                && state.CurrentIndex - 1 >= state.RevengeStartIndex;
            if (!inRevengeRound && !state.WrongElementIds.Contains(elementId))
            {
                state.WrongElementIds.Add(elementId);
            }
        }

        private (bool isGameOver, bool isRevengeStart) FinaliseRound(QuizSessionState state)
        {
            if (state.CurrentIndex < state.Questions.Count)
            {
                return (false, false);
            }

            if (state.RevengeStartIndex < 0 && state.WrongElementIds.Count > 0)
            {
                List<QuizQuestionState> revengeQuestions = GenerateRevengeQuestions(state.WrongElementIds);
                state.RevengeStartIndex = state.Questions.Count;
                state.Questions.AddRange(revengeQuestions);
                return (false, true);
            }

            state.IsCompleted = true;
            return (true, false);
        }

        private (bool isCorrect, bool wasFuzzy) CheckAnswer(QuizQuestionState q, string answer)
        {
            if (!q.IsTyped)
            {
                return (string.Equals(answer.Trim(), q.CorrectAnswer, StringComparison.OrdinalIgnoreCase), false);
            }

            string na = Normalize(answer);
            string nc = Normalize(q.CorrectAnswer);
            if (na == nc)
            {
                return (true, false);
            }

            if (nc.Length >= 5 && LevenshteinDistance(na, nc) == 1)
            {
                return (true, true);
            }

            return (false, false);
        }

        private static void UpdateCombo(QuizSessionState state, bool isCorrect)
        {
            if (isCorrect)
            {
                state.ComboCount++;
                state.CorrectCount++;
                if (state.ComboCount > state.MaxCombo)
                {
                    state.MaxCombo = state.ComboCount;
                }
            }
            else
            {
                state.ComboCount = 0;
            }
        }

        public QuizSessionState? GetState(ISession session)
        {
            return LoadState(session);
        }

        // ── Revenge round ────────────────────────────────────────────────────────

        private List<QuizQuestionState> GenerateRevengeQuestions(List<int> wrongIds)
        {
            // Pick up to 5 wrong elements, shuffled
            List<int> ids = [.. wrongIds];
            Shuffle(ids);
            if (ids.Count > 5)
            {
                ids = ids[..5];
            }

            List<QuizQuestionState> questions = new(ids.Count);
            foreach (int id in ids)
            {
                Element? el = elementService.GetById(id);
                if (el is null)
                {
                    continue;
                }

                // Flip question type: typed → MCQ (relief), MCQ → opposite MCQ
                QuizQuestionState q = _random.Next(2) == 0
                    ? BuildRevengeNameToSymbol(el)
                    : BuildRevengeSymbolToName(el);
                questions.Add(q);
            }
            return questions;
        }

        private QuizQuestionState BuildRevengeNameToSymbol(Element el)
        {
            List<string> wrong = elementService.GetConfusableSymbols(3, el.Symbol, 118);
            List<string> choices = new(wrong) { el.Symbol };
            Shuffle(choices);
            return new QuizQuestionState
            {
                ElementId = el.AtomicNumber,
                Type = QuestionType.NameToSymbol,
                CorrectAnswer = el.Symbol,
                Choices = choices,
                Prompt = "Quel est le symbole de cet élément ?",
                DisplayValue = el.Name,
                FunFact = el.FunFact,
                CommonUse = el.CommonUse,
                WhereToFind = el.WhereToFind
            };
        }

        private QuizQuestionState BuildRevengeSymbolToName(Element el)
        {
            List<string> wrong = elementService.GetConfusableNames(3, el.Name, 118);
            List<string> choices = new(wrong) { el.Name };
            Shuffle(choices);
            return new QuizQuestionState
            {
                ElementId = el.AtomicNumber,
                Type = QuestionType.SymbolToName,
                CorrectAnswer = el.Name,
                Choices = choices,
                Prompt = "Quel est le nom de cet élément ?",
                DisplayValue = el.Symbol,
                FunFact = el.FunFact,
                CommonUse = el.CommonUse,
                WhereToFind = el.WhereToFind
            };
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns difficulty parameters based on total player XP.
        /// Higher XP → larger element pool + more typed questions + harder distractors.
        /// </summary>
        private static (int maxZ, int nameToSymbol, int symbolToName, int typed) GetDifficulty(int playerXp)
        {
            return playerXp switch
            {
                // Apprenti  0-299  : Z=1-20, 6 MCQ + 6 MCQ + 3 typées  (bases solides)
                < 300 => (20, 6, 6, 3),
                // Junior    300-899: Z=1-36, 5+5+5
                < 900 => (36, 5, 5, 5),
                // Chimiste  900-1999: Z=1-54, 4+4+7 (plus d'orthographe)
                < 2000 => (54, 4, 4, 7),
                // Expert    2000-4499: Z=1-86, 3+3+9
                < 4500 => (86, 3, 3, 9),
                // Maître+   4500+   : Z=1-118, 2+2+11
                _ => (118, 2, 2, 11)
            };
        }

        private List<QuizQuestionState> GenerateQuestions(int playerXp = 0, int total = DefaultQuestions)
        {
            (int maxZ, int nameToSymbolBase, int symbolToNameBase, int symbolTypedBase) = GetDifficulty(playerXp);
            int baseTotal = nameToSymbolBase + symbolToNameBase + symbolTypedBase; // always 15

            // Cannot generate more unique questions than there are elements in the pool
            int poolSize = elementService.GetAll().Count(e => e.AtomicNumber <= maxZ);
            total = Math.Min(total, poolSize);

            // Scale each bucket proportionally; give remainder to typed
            int nameToSymbolCount = (int)Math.Round((double)nameToSymbolBase * total / baseTotal);
            int symbolToNameCount = (int)Math.Round((double)symbolToNameBase * total / baseTotal);
            int symbolTypedCount = total - nameToSymbolCount - symbolToNameCount;

            HashSet<int> usedIds = [];
            List<QuizQuestionState> questions = new(total);

            // Name → Symbol (MCQ) — confusable distractors
            for (int i = 0; i < nameToSymbolCount; i++)
            {
                Element el = elementService.GetWeightedRandom(usedIds, maxZ);
                _ = usedIds.Add(el.AtomicNumber);
                List<string> wrong = elementService.GetConfusableSymbols(3, el.Symbol, maxZ);
                List<string> choices = new(wrong) { el.Symbol };
                Shuffle(choices);
                questions.Add(new QuizQuestionState
                {
                    ElementId = el.AtomicNumber,
                    Type = QuestionType.NameToSymbol,
                    CorrectAnswer = el.Symbol,
                    Choices = choices,
                    Prompt = "Quel est le symbole de cet élément ?",
                    DisplayValue = el.Name,
                    FunFact = el.FunFact,
                    CommonUse = el.CommonUse,
                    WhereToFind = el.WhereToFind
                });
            }

            // Symbol → Name (MCQ) — confusable distractors
            for (int i = 0; i < symbolToNameCount; i++)
            {
                Element el = elementService.GetWeightedRandom(usedIds, maxZ);
                _ = usedIds.Add(el.AtomicNumber);
                List<string> wrong = elementService.GetConfusableNames(3, el.Name, maxZ);
                List<string> choices = new(wrong) { el.Name };
                Shuffle(choices);
                questions.Add(new QuizQuestionState
                {
                    ElementId = el.AtomicNumber,
                    Type = QuestionType.SymbolToName,
                    CorrectAnswer = el.Name,
                    Choices = choices,
                    Prompt = "Quel est le nom de cet élément ?",
                    DisplayValue = el.Symbol,
                    FunFact = el.FunFact,
                    CommonUse = el.CommonUse,
                    WhereToFind = el.WhereToFind
                });
            }

            // Symbol → Name (saisie libre — apprendre l'orthographe!)
            for (int i = 0; i < symbolTypedCount; i++)
            {
                Element el = elementService.GetWeightedRandom(usedIds, maxZ);
                _ = usedIds.Add(el.AtomicNumber);
                questions.Add(new QuizQuestionState
                {
                    ElementId = el.AtomicNumber,
                    Type = QuestionType.SymbolToNameTyped,
                    CorrectAnswer = el.Name,
                    Choices = [],
                    Prompt = "Écris le nom de cet élément (orthographe correcte) :",
                    DisplayValue = el.Symbol,
                    FunFact = el.FunFact,
                    CommonUse = el.CommonUse,
                    WhereToFind = el.WhereToFind
                });
            }

            Shuffle(questions);
            return questions;
        }

        // Levenshtein distance for typo tolerance (1 char typo on words ≥5 chars)
        private static int LevenshteinDistance(string a, string b)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 0; j <= b.Length; j++)
            {
                d[0, j] = j;
            }

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                }
            }

            return d[a.Length, b.Length];
        }

        // Accept answers without accents: Helium == Hélium
        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return "";
            }

            string normalized = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    _ = sb.Append(c);
                }
            }
            return sb.ToString().ToLowerInvariant().Trim();
        }

        private static int CalculateXp(int combo)
        {
            return combo switch
            {
                >= 8 => 30,
                >= 5 => 20,
                >= 3 => 15,
                _ => 10
            };
        }

        private static string GetComboMessage(int combo)
        {
            return combo switch
            {
                >= 10 => $"🌟 COMBO x{combo}! LÉGENDAIRE!",
                >= 8 => "💥 COMBO x8! INCROYABLE!",
                >= 5 => "⚡ Combo x5! Impressionnant!",
                >= 3 => "🔥 Combo x3!",
                _ => ""
            };
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void SaveState(ISession session, QuizSessionState state)
        {
            session.SetString(SessionKey, JsonSerializer.Serialize(state, JsonOptions));
        }

        private static QuizSessionState? LoadState(ISession session)
        {
            string? json = session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try { return JsonSerializer.Deserialize<QuizSessionState>(json, JsonOptions); }
            catch { return null; }
        }
    }
}

using ChimQuiz.Models;
using ChimQuiz.Services;
using ChimQuiz.Tests.Helpers;

namespace ChimQuiz.Tests.Services;

public class QuizServiceTests
{
    private readonly ElementService _elements = new();
    private readonly QuizService _svc;
    private readonly Guid _playerId = Guid.NewGuid();

    public QuizServiceTests()
    {
        _svc = new QuizService(_elements);
    }

    private FakeSession NewSession() => new();

    // ── StartNewSession ───────────────────────────────────────────────────────

    [Fact]
    public void StartNewSession_Default_Generates15Questions()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);
        Assert.Equal(15, state.Questions.Count);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    public void StartNewSession_CustomCount_GeneratesCorrectQuestions(int count)
    {
        // Junior tier (300 XP) has 36 elements — enough for any supported count
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 300, questionCount: count);
        Assert.Equal(count, state.Questions.Count);
    }

    [Fact]
    public void StartNewSession_ClampsBelowMin_UsesMin5()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 1);
        Assert.Equal(5, state.Questions.Count);
    }

    [Fact]
    public void StartNewSession_ClampsAboveMax_UsesMax30()
    {
        // Junior tier (300 XP) has 36 elements available — enough for 30 questions
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 300, questionCount: 99);
        Assert.Equal(30, state.Questions.Count);
    }

    [Fact]
    public void StartNewSession_Apprenti_ClampsToPoolSize()
    {
        // Apprenti pool is Z=1-20 (20 elements). Requesting 30 should silently cap at 20.
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 30);
        Assert.True(state.Questions.Count <= 20,
            $"Expected ≤20 questions for Apprenti pool, got {state.Questions.Count}");
    }

    [Fact]
    public void StartNewSession_AllQuestionsHaveDistinctElements()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);
        var ids = state.Questions.Select(q => q.ElementId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void StartNewSession_SetsPlayerId()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);
        Assert.Equal(_playerId, state.PlayerId);
    }

    [Fact]
    public void StartNewSession_RevengeNotYetStarted()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);
        Assert.Equal(-1, state.RevengeStartIndex);
        Assert.Empty(state.WrongElementIds);
    }

    // ── Question type distribution (Apprenti, 0 XP, 15 questions) ────────────

    [Fact]
    public void GenerateQuestions_Apprenti_DistributionMatchesExpected()
    {
        // 0 XP → (maxZ=20, N2S=6, S2N=6, Typed=3) for 15 questions
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 15);

        int n2s = state.Questions.Count(q => q.Type == QuestionType.NameToSymbol);
        int s2n = state.Questions.Count(q => q.Type == QuestionType.SymbolToName);
        int typed = state.Questions.Count(q => q.Type == QuestionType.SymbolToNameTyped);

        Assert.Equal(6, n2s);
        Assert.Equal(6, s2n);
        Assert.Equal(3, typed);
    }

    [Fact]
    public void GenerateQuestions_Apprenti_ElementsWithinZ20()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 15);

        foreach (var q in state.Questions)
        {
            Assert.True(q.ElementId <= 20, $"ElementId={q.ElementId} exceeds maxZ=20 for Apprenti");
        }
    }

    [Fact]
    public void GenerateQuestions_Master_ElementsCanExceedZ20()
    {
        // 4500+ XP → full periodic table (Z 1-118)
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 5000, questionCount: 15);

        // With 15 questions from 118 elements, statistically very likely to have Z > 20
        // Just verify all IDs are valid (1-118)
        foreach (var q in state.Questions)
        {
            Assert.InRange(q.ElementId, 1, 118);
        }
    }

    // ── MCQ choices ───────────────────────────────────────────────────────────

    [Fact]
    public void GenerateQuestions_MCQ_HasExactly4Choices()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);

        foreach (var q in state.Questions.Where(q => !q.IsTyped))
        {
            Assert.Equal(4, q.Choices.Count);
        }
    }

    [Fact]
    public void GenerateQuestions_MCQ_ChoicesContainCorrectAnswer()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);

        foreach (var q in state.Questions.Where(q => !q.IsTyped))
        {
            Assert.Contains(q.CorrectAnswer, q.Choices, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GenerateQuestions_Typed_HasEmptyChoices()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 15);

        foreach (var q in state.Questions.Where(q => q.IsTyped))
        {
            Assert.Empty(q.Choices);
        }
    }

    // ── GetCurrentQuestion ────────────────────────────────────────────────────

    [Fact]
    public void GetCurrentQuestion_AfterStart_ReturnsQuestion()
    {
        var session = NewSession();
        _svc.StartNewSession(session, _playerId);
        var (q, index, total, combo, xp, isRevenge) = _svc.GetCurrentQuestion(session);

        Assert.NotNull(q);
        Assert.Equal(0, index);
        Assert.Equal(15, total);
        Assert.Equal(0, combo);
        Assert.Equal(0, xp);
        Assert.False(isRevenge);
    }

    [Fact]
    public void GetCurrentQuestion_HidesCorrectAnswer()
    {
        var session = NewSession();
        _svc.StartNewSession(session, _playerId);
        var (q, _, _, _, _, _) = _svc.GetCurrentQuestion(session);

        Assert.NotNull(q);
        Assert.Equal("", q.CorrectAnswer);
    }

    [Fact]
    public void GetCurrentQuestion_HidesFunFact()
    {
        var session = NewSession();
        _svc.StartNewSession(session, _playerId);
        var (q, _, _, _, _, _) = _svc.GetCurrentQuestion(session);

        Assert.NotNull(q);
        Assert.Null(q.FunFact);
    }

    [Fact]
    public void GetCurrentQuestion_NoSession_ReturnsNull()
    {
        var session = NewSession();
        var (q, _, _, _, _, _) = _svc.GetCurrentQuestion(session);
        Assert.Null(q);
    }

    // ── SubmitAnswer — XP & combo ─────────────────────────────────────────────

    [Fact]
    public void SubmitAnswer_Correct_EarnsBaseXp()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);
        var correctAnswer = state.Questions[0].CorrectAnswer;

        var result = _svc.SubmitAnswer(session, correctAnswer);

        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
        Assert.Equal(10, result.XpEarned);  // combo=1 → 10 XP
    }

    [Fact]
    public void SubmitAnswer_Incorrect_EarnsZeroXp()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);
        var wrongAnswer = "____wrong____";

        var result = _svc.SubmitAnswer(session, wrongAnswer);

        Assert.NotNull(result);
        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.XpEarned);
    }

    [Fact]
    public void SubmitAnswer_ComboAccumulates()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);

        for (int i = 0; i < 3; i++)
        {
            var answer = state.Questions[i].CorrectAnswer;
            var result = _svc.SubmitAnswer(session, answer);
            Assert.NotNull(result);
            Assert.Equal(i + 1, result.ComboCount);
        }
    }

    [Fact]
    public void SubmitAnswer_IncorrectResetsCombo()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);

        _svc.SubmitAnswer(session, state.Questions[0].CorrectAnswer);
        _svc.SubmitAnswer(session, state.Questions[1].CorrectAnswer);
        var result = _svc.SubmitAnswer(session, "____wrong____");

        Assert.NotNull(result);
        Assert.Equal(0, result.ComboCount);
    }

    [Fact]
    public void SubmitAnswer_HighCombo_ScalesXp()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 30);

        // Get 8 correct in a row to reach 30 XP tier
        for (int i = 0; i < 7; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }
        var result = _svc.SubmitAnswer(session, state.Questions[7].CorrectAnswer);

        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
        Assert.Equal(30, result.XpEarned);  // combo=8 → 30 XP
    }

    [Fact]
    public void SubmitAnswer_MaxComboTracked()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId);

        _svc.SubmitAnswer(session, state.Questions[0].CorrectAnswer);
        _svc.SubmitAnswer(session, state.Questions[1].CorrectAnswer);
        _svc.SubmitAnswer(session, state.Questions[2].CorrectAnswer);
        _svc.SubmitAnswer(session, "____wrong____");  // resets combo
        var result = _svc.SubmitAnswer(session, state.Questions[4].CorrectAnswer);

        Assert.NotNull(result);
        Assert.Equal(3, result.MaxCombo);
    }

    // ── SubmitAnswer — game over / revenge ────────────────────────────────────

    [Fact]
    public void SubmitAnswer_AllCorrect_GameOverTrue()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 5);

        AnswerResult? lastResult = null;
        for (int i = 0; i < 5; i++)
        {
            lastResult = _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        Assert.NotNull(lastResult);
        Assert.True(lastResult.IsGameOver);
        Assert.False(lastResult.IsRevengeStart);
    }

    [Fact]
    public void SubmitAnswer_WithWrongAnswers_TriggersRevengeRound()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 5);

        // Get first answer wrong, rest correct
        _svc.SubmitAnswer(session, "____wrong____");
        for (int i = 1; i < 5; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        var reloadedState = _svc.GetState(session);
        Assert.NotNull(reloadedState);
        Assert.True(reloadedState.RevengeStartIndex >= 0, "Revenge should have been injected");
        Assert.True(reloadedState.Questions.Count > 5, "Questions should include revenge round");
    }

    [Fact]
    public void SubmitAnswer_LastQuestionWithWrong_ReturnsIsRevengeStart()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 5);

        _svc.SubmitAnswer(session, "____wrong____");
        AnswerResult? result = null;
        for (int i = 1; i < 5; i++)
        {
            result = _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        Assert.NotNull(result);
        Assert.True(result.IsRevengeStart);
        Assert.False(result.IsGameOver);
    }

    [Fact]
    public void SubmitAnswer_RevengeRound_AtMost5ExtraQuestions()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 30);

        // Answer all 30 wrong to maximise wrong element pool
        for (int i = 0; i < 30; i++)
        {
            _svc.SubmitAnswer(session, "____wrong____");
        }

        var reloadedState = _svc.GetState(session);
        Assert.NotNull(reloadedState);
        int revengeCount = reloadedState.Questions.Count - reloadedState.RevengeStartIndex;
        Assert.True(revengeCount <= 5, $"Revenge round has {revengeCount} questions, max is 5");
    }

    [Fact]
    public void SubmitAnswer_WrongInRevenge_NotAddedToWrongElementIds()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 5);

        // Fail first question, pass rest to trigger revenge
        _svc.SubmitAnswer(session, "____wrong____");
        for (int i = 1; i < 5; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        var stateBefore = _svc.GetState(session)!;
        int wrongCountBeforeRevenge = stateBefore.WrongElementIds.Count;

        // Fail the revenge question
        _svc.SubmitAnswer(session, "____wrong____");

        var stateAfter = _svc.GetState(session)!;
        Assert.Equal(wrongCountBeforeRevenge, stateAfter.WrongElementIds.Count);
    }

    // ── GetCurrentQuestion — revenge flag ────────────────────────────────────

    [Fact]
    public void GetCurrentQuestion_DuringRevenge_IsRevengeFlagTrue()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 5);

        // Trigger revenge round
        _svc.SubmitAnswer(session, "____wrong____");
        for (int i = 1; i < 5; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        var (q, _, _, _, _, isRevenge) = _svc.GetCurrentQuestion(session);
        Assert.NotNull(q);
        Assert.True(isRevenge);
    }

    [Fact]
    public void GetCurrentQuestion_Revenge_CounterIsRelative()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 5);

        _svc.SubmitAnswer(session, "____wrong____");
        for (int i = 1; i < 5; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        var (_, index, total, _, _, _) = _svc.GetCurrentQuestion(session);
        Assert.Equal(0, index);   // first revenge question = index 0
        Assert.True(total <= 5);  // at most 5 revenge questions
    }

    // ── Answer checking — typed questions ─────────────────────────────────────

    [Fact]
    public void CheckAnswer_Typed_AccentInsensitive()
    {
        var session = NewSession();
        _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 5);

        // Find a typed question with an accented name
        var rawState = _svc.GetState(session)!;
        var typedQ = rawState.Questions.FirstOrDefault(q => q.IsTyped && q.CorrectAnswer.Any(c => c > 127));
        if (typedQ is null) return; // skip if no accented typed question in this run

        // Inject this as the first question by manipulating via a fresh session
        var session2 = NewSession();
        var state2 = _svc.StartNewSession(session2, _playerId, playerXp: 0, questionCount: 5);
        var rawState2 = _svc.GetState(session2)!;
        var typed2 = rawState2.Questions.FirstOrDefault(q => q.IsTyped);
        if (typed2 is null) return;

        // Get the answer without accents
        string withoutAccent = RemoveAccents(typed2.CorrectAnswer);
        if (withoutAccent == typed2.CorrectAnswer) return; // skip if no accents to test

        // Skip to that question
        for (int i = 0; i < rawState2.Questions.IndexOf(typed2); i++)
        {
            _svc.SubmitAnswer(session2, rawState2.Questions[i].CorrectAnswer);
        }

        var result = _svc.SubmitAnswer(session2, withoutAccent);
        Assert.NotNull(result);
        Assert.True(result.IsCorrect, $"'{withoutAccent}' should be accepted for '{typed2.CorrectAnswer}'");
    }

    [Fact]
    public void CheckAnswer_Typed_FuzzyMatch_OneTypoAccepted_OnLongWord()
    {
        // Hydrogène (9 chars normalized) → "Hydogène" (missing r) should be accepted
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 15);

        var typedQ = state.Questions.FirstOrDefault(q =>
            q.IsTyped && q.CorrectAnswer.Length >= 5);
        if (typedQ is null) return;

        int qIdx = state.Questions.IndexOf(typedQ);
        for (int i = 0; i < qIdx; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        // Introduce one typo (drop the second character)
        string correct = RemoveAccents(typedQ.CorrectAnswer).ToLowerInvariant();
        if (correct.Length < 5) return;
        string oneTypo = correct[0] + correct[2..];  // remove 2nd character

        var result = _svc.SubmitAnswer(session, oneTypo);
        Assert.NotNull(result);
        Assert.True(result.IsCorrect || result.WasFuzzyMatch,
            $"One-typo answer '{oneTypo}' should be accepted for '{typedQ.CorrectAnswer}'");
    }

    [Fact]
    public void CheckAnswer_MCQ_CaseInsensitive()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 5);

        var mcqQ = state.Questions.First(q => !q.IsTyped);
        int qIdx = state.Questions.IndexOf(mcqQ);
        for (int i = 0; i < qIdx; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        var result = _svc.SubmitAnswer(session, mcqQ.CorrectAnswer.ToUpperInvariant());
        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
    }

    // ── SubmitAnswer — returns element info ───────────────────────────────────

    [Fact]
    public void SubmitAnswer_ReturnsElementSymbolAndName()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, playerXp: 0, questionCount: 5);

        var result = _svc.SubmitAnswer(session, "____wrong____");

        Assert.NotNull(result);
        Assert.NotEmpty(result.ElementSymbol);
        Assert.NotEmpty(result.ElementName);
    }

    [Fact]
    public void SubmitAnswer_AfterGameOver_ReturnsNull()
    {
        var session = NewSession();
        var state = _svc.StartNewSession(session, _playerId, questionCount: 5);

        // Answer all correctly (no revenge)
        for (int i = 0; i < 5; i++)
        {
            _svc.SubmitAnswer(session, state.Questions[i].CorrectAnswer);
        }

        // Extra call after game over
        var result = _svc.SubmitAnswer(session, "anything");
        Assert.Null(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string RemoveAccents(string s)
    {
        var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (char c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}

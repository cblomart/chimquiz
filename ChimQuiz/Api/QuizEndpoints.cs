using ChimQuiz.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChimQuiz.Api;

public static class QuizEndpoints
{
    private const string PlayerCookieName = "chimquiz_player";

    public static RouteGroupBuilder MapQuizApi(this RouteGroupBuilder group)
    {
        group.MapPost("/quiz/start", StartQuiz);
        group.MapGet("/quiz/question", GetQuestion);
        group.MapPost("/quiz/answer", SubmitAnswer);
        return group;
    }

    private static async Task<IResult> StartQuiz(HttpContext ctx, QuizService quizService, PlayerService playerService)
    {
        if (!ctx.Request.Cookies.TryGetValue(PlayerCookieName, out var idStr) ||
            !Guid.TryParse(idStr, out var playerId))
            return Results.Unauthorized();

        // Adapt difficulty to current player XP
        var player   = await playerService.GetPlayerAsync(playerId);
        var playerXp = player?.TotalXp ?? 0;

        var state = quizService.StartNewSession(ctx.Session, playerId, playerXp);
        return Results.Ok(new { state.SessionId, TotalQuestions = state.Questions.Count });
    }

    private static IResult GetQuestion(HttpContext ctx, QuizService quizService)
    {
        var (question, index, total, combo, totalXp) = quizService.GetCurrentQuestion(ctx.Session);
        if (question is null) return Results.NotFound(new { error = "Aucune session active" });

        return Results.Ok(new
        {
            question.Prompt,
            question.DisplayValue,
            question.Choices,
            Type = question.Type.ToString(),
            QuestionNumber   = index + 1,
            TotalQuestions   = total,
            ComboCount       = combo,
            TotalXp          = totalXp,
            ComboMultiplier  = combo switch { >= 8 => "x3", >= 5 => "x2", >= 3 => "x1.5", _ => "x1" }
        });
    }

    private static async Task<IResult> SubmitAnswer(
        HttpContext ctx,
        QuizService quizService,
        PlayerService playerService,
        [FromBody] AnswerRequest req)
    {
        if (req?.Answer is null)
            return Results.BadRequest(new { error = "Réponse invalide" });

        // Sanitize input (empty string = timeout/skip → treated as wrong answer)
        var trimmed = req.Answer.Trim();
        var answer = trimmed[..Math.Min(trimmed.Length, 100)];

        var result = quizService.SubmitAnswer(ctx.Session, answer);
        if (result is null) return Results.NotFound(new { error = "Aucune session active" });

        // Save session to DB when game over
        if (result.IsGameOver)
        {
            if (ctx.Request.Cookies.TryGetValue(PlayerCookieName, out var idStr) &&
                Guid.TryParse(idStr, out var playerId))
            {
                var state = quizService.GetState(ctx.Session);
                if (state is not null)
                {
                    await playerService.SaveCompletedSessionAsync(playerId, state);
                }
            }
        }

        return Results.Ok(result);
    }

    private record AnswerRequest(string Answer);
}

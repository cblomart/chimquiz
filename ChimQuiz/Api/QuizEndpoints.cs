using ChimQuiz.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChimQuiz.Api
{
    public static class QuizEndpoints
    {
        private const string PlayerCookieName = "chimquiz_player";

        public static RouteGroupBuilder MapQuizApi(this RouteGroupBuilder group)
        {
            _ = group.MapPost("/quiz/start", StartQuiz);
            _ = group.MapGet("/quiz/question", GetQuestion);
            _ = group.MapPost("/quiz/answer", SubmitAnswer);
            return group;
        }

        private static async Task<IResult> StartQuiz(HttpContext ctx, QuizService quizService, PlayerService playerService, [FromBody] StartQuizRequest? req)
        {
            if (!ctx.Request.Cookies.TryGetValue(PlayerCookieName, out string? idStr) ||
                !Guid.TryParse(idStr, out Guid playerId))
            {
                return Results.Unauthorized();
            }

            // Adapt difficulty to current player XP
            Models.Player? player = await playerService.GetPlayerAsync(playerId);
            int playerXp = player?.TotalXp ?? 0;

            Models.QuizSessionState state = quizService.StartNewSession(ctx.Session, playerId, playerXp, req?.QuestionCount ?? 15);
            return Results.Ok(new { state.SessionId, TotalQuestions = state.Questions.Count });
        }

        private static IResult GetQuestion(HttpContext ctx, QuizService quizService)
        {
            (Models.QuizQuestionState? question, int index, int total, int combo, int totalXp, bool isRevenge) = quizService.GetCurrentQuestion(ctx.Session);
            return question is null
                ? Results.NotFound(new { error = "Aucune session active" })
                : Results.Ok(new
                {
                    question.Prompt,
                    question.DisplayValue,
                    question.Choices,
                    Type = question.Type.ToString(),
                    QuestionNumber = index + 1,
                    TotalQuestions = total,
                    ComboCount = combo,
                    TotalXp = totalXp,
                    ComboMultiplier = combo switch { >= 8 => "x3", >= 5 => "x2", >= 3 => "x1.5", _ => "x1" },
                    IsRevenge = isRevenge
                });
        }

        private static async Task<IResult> SubmitAnswer(
            HttpContext ctx,
            QuizService quizService,
            PlayerService playerService,
            [FromBody] AnswerRequest req)
        {
            if (req?.Answer is null)
            {
                return Results.BadRequest(new { error = "Réponse invalide" });
            }

            // Sanitize input (empty string = timeout/skip → treated as wrong answer)
            string trimmed = req.Answer.Trim();
            string answer = trimmed[..Math.Min(trimmed.Length, 100)];

            AnswerResult? result = quizService.SubmitAnswer(ctx.Session, answer);
            if (result is null)
            {
                return Results.NotFound(new { error = "Aucune session active" });
            }

            // Save session to DB when game over
            if (result.IsGameOver)
            {
                if (ctx.Request.Cookies.TryGetValue(PlayerCookieName, out string? idStr) &&
                    Guid.TryParse(idStr, out Guid playerId))
                {
                    Models.QuizSessionState? state = quizService.GetState(ctx.Session);
                    if (state is not null)
                    {
                        await playerService.SaveCompletedSessionAsync(playerId, state);
                    }
                }
            }

            return Results.Ok(result);
        }

        private sealed record AnswerRequest(string Answer);
        private sealed record StartQuizRequest(int QuestionCount);
    }
}

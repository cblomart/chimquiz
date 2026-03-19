using ChimQuiz.Services;
using System.ComponentModel.DataAnnotations;

namespace ChimQuiz.Api
{
    public static class PlayerEndpoints
    {
        private const string PlayerCookieName = "chimquiz_player";

        public static RouteGroupBuilder MapPlayerApi(this RouteGroupBuilder group)
        {
            _ = group.MapGet("/player/me", GetMe);
            _ = group.MapPost("/player/create", CreatePlayer);
            _ = group.MapPatch("/player/pseudo", UpdatePseudo);
            return group;
        }

        private static async Task<IResult> GetMe(HttpContext ctx, PlayerService playerService)
        {
            if (!ctx.Request.Cookies.TryGetValue(PlayerCookieName, out string? idStr) ||
                !Guid.TryParse(idStr, out Guid playerId))
            {
                return Results.NotFound();
            }

            Models.Player? player = await playerService.GetPlayerAsync(playerId);
            return player is null
                ? Results.NotFound()
                : Results.Ok(new
                {
                    player.Id,
                    player.Pseudo,
                    player.TotalXp,
                    player.BestSessionXp,
                    player.CurrentStreak,
                    player.MaxStreak,
                    player.RankName,
                    player.RankEmoji,
                    player.RankProgressPercent,
                    player.XpForCurrentRank,
                    player.XpForNextRank
                });
        }

        private static async Task<IResult> CreatePlayer(HttpContext ctx, PlayerService playerService, CreatePlayerRequest? req)
        {
            string pseudo = string.IsNullOrWhiteSpace(req?.Pseudo)
                ? playerService.GeneratePseudo()
                : req.Pseudo.Trim();

            try
            {
                Models.Player player = await playerService.CreatePlayerAsync(pseudo);
                SetPlayerCookie(ctx, player.Id);
                return Results.Ok(new
                {
                    player.Id,
                    player.Pseudo,
                    player.TotalXp,
                    player.RankName,
                    player.RankEmoji,
                    player.RankProgressPercent
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        private static async Task<IResult> UpdatePseudo(HttpContext ctx, PlayerService playerService, UpdatePseudoRequest req)
        {
            if (!ctx.Request.Cookies.TryGetValue(PlayerCookieName, out string? idStr) ||
                !Guid.TryParse(idStr, out Guid playerId))
            {
                return Results.Unauthorized();
            }

            try
            {
                Models.Player player = await playerService.UpdatePseudoAsync(playerId, req.Pseudo.Trim());
                return Results.Ok(new { player.Pseudo });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        private static void SetPlayerCookie(HttpContext ctx, Guid playerId)
        {
            ctx.Response.Cookies.Append(PlayerCookieName, playerId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                Path = "/"
            });
        }

        private sealed record CreatePlayerRequest(string? Pseudo);
        private sealed record UpdatePseudoRequest([Required][StringLength(30, MinimumLength = 3)] string Pseudo);
    }
}

using ChimQuiz.Services;
using System.ComponentModel.DataAnnotations;

namespace ChimQuiz.Api;

public static class PlayerEndpoints
{
    private const string PlayerCookieName = "chimquiz_player";

    public static RouteGroupBuilder MapPlayerApi(this RouteGroupBuilder group)
    {
        group.MapGet("/player/me", GetMe);
        group.MapPost("/player/create", CreatePlayer);
        group.MapPatch("/player/pseudo", UpdatePseudo);
        return group;
    }

    private static async Task<IResult> GetMe(HttpContext ctx, PlayerService playerService)
    {
        if (!ctx.Request.Cookies.TryGetValue(PlayerCookieName, out var idStr) ||
            !Guid.TryParse(idStr, out var playerId))
            return Results.NotFound();

        var player = await playerService.GetPlayerAsync(playerId);
        if (player is null) return Results.NotFound();

        return Results.Ok(new
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
        var pseudo = string.IsNullOrWhiteSpace(req?.Pseudo)
            ? playerService.GeneratePseudo()
            : req.Pseudo.Trim();

        try
        {
            var player = await playerService.CreatePlayerAsync(pseudo);
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
        if (!ctx.Request.Cookies.TryGetValue(PlayerCookieName, out var idStr) ||
            !Guid.TryParse(idStr, out var playerId))
            return Results.Unauthorized();

        try
        {
            var player = await playerService.UpdatePseudoAsync(playerId, req.Pseudo.Trim());
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
            Expires  = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            Path = "/"
        });
    }

    private record CreatePlayerRequest(string? Pseudo);
    private record UpdatePseudoRequest([Required][StringLength(30, MinimumLength = 3)] string Pseudo);
}

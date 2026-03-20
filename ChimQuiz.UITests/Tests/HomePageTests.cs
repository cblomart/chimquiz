namespace ChimQuiz.UITests.Tests;

#pragma warning disable CA1001 // factory is disposed via IAsyncLifetime.DisposeAsync

public sealed class HomePageTests : IAsyncLifetime
{
    private readonly PlaywrightWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitPlaywrightAsync();
    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ── Page structure ────────────────────────────────────────────────────────

    [Fact]
    public async Task HomePage_HasCorrectTitle()
    {
        await using var ctx = await _factory.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_factory.ServerAddress);
        await Assertions.Expect(page).ToHaveTitleAsync("Apprends la Chimie en Jouant - ChimQuiz");
    }

    [Fact]
    public async Task HomePage_ShowsPseudoInput()
    {
        await using var ctx = await _factory.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_factory.ServerAddress);
        await Assertions.Expect(page.Locator("#pseudo-input")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_ShowsStartButton()
    {
        await using var ctx = await _factory.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_factory.ServerAddress);
        await Assertions.Expect(page.Locator("#start-btn")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_ShowsQuestionCountSelector()
    {
        await using var ctx = await _factory.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_factory.ServerAddress);
        await Assertions.Expect(page.Locator("#question-count")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_EmptyInput_UsesPlaceholderPseudo_AndNavigatesToQuiz()
    {
        // Clicking start without typing uses the auto-generated placeholder pseudo
        await using var ctx = await _factory.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_factory.ServerAddress);

        await page.ClickAsync("#start-btn");
        await page.WaitForURLAsync("**/quiz", new() { Timeout = 10_000 });
    }
}

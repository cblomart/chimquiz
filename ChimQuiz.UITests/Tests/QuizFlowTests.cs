namespace ChimQuiz.UITests.Tests;

#pragma warning disable CA1001 // factory is disposed via IAsyncLifetime.DisposeAsync

public sealed class QuizFlowTests : IAsyncLifetime
{
    private readonly PlaywrightWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitPlaywrightAsync();
    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ── Registration → quiz navigation ────────────────────────────────────────

    [Fact]
    public async Task Register_NavigatesToQuizPage()
    {
        var page = await StartAsync();
        await Assertions.Expect(page.Locator("#question-counter")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task QuizPage_ShowsQuestion1Counter()
    {
        var page = await StartAsync();
        await Assertions.Expect(page.Locator("#question-counter"))
            .ToContainTextAsync("Question 1");
    }

    // ── Question rendering ────────────────────────────────────────────────────

    [Fact]
    public async Task QuizPage_ShowsNonEmptyPromptAndDisplayValue()
    {
        var page = await StartAsync();
        await Assertions.Expect(page.Locator("#question-prompt")).Not.ToBeEmptyAsync();
        await Assertions.Expect(page.Locator("#display-value")).Not.ToBeEmptyAsync();
    }

    [Fact]
    public async Task QuizPage_ShowsMCQChoicesOrTypedInput()
    {
        var page = await StartAsync();

        var mcqVisible   = await page.Locator("#choice-0").IsVisibleAsync();
        var typedVisible = await page.Locator("#typed-answer").IsVisibleAsync();
        Assert.True(mcqVisible || typedVisible,
            "Expected MCQ choices or typed input to be visible");
    }

    // ── Answer → info card ────────────────────────────────────────────────────

    [Fact]
    public async Task AnsweringQuestion_ShowsElementInfoCard()
    {
        var page = await StartAsync();
        await AnswerAsync(page);

        await Assertions.Expect(page.Locator("#element-info-card"))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    [Fact]
    public async Task InfoCard_ShowsAnswerVerdict()
    {
        var page = await StartAsync();
        await AnswerAsync(page);

        await page.Locator("#element-info-card").WaitForAsync(new() { Timeout = 5_000 });
        await Assertions.Expect(page.Locator("#answer-verdict")).ToBeVisibleAsync();
    }

    // ── J'ai lu! ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task JaiLu_AdvancesGameToQuestion2OrGameOver()
    {
        var page = await StartAsync();
        await AnswerAsync(page);
        await page.Locator("#element-info-card").WaitForAsync(new() { Timeout = 5_000 });

        await page.ClickAsync("button:has-text(\"J'ai lu\")");

        // After J'ai lu the info card should hide (hideInfoCard is called on next question load)
        await Assertions.Expect(page.Locator("#element-info-card"))
            .ToBeHiddenAsync(new() { Timeout = 10_000 });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Registers a unique player, picks 5 questions, navigates to quiz, waits for Q1.</summary>
    private async Task<IPage> StartAsync()
    {
        var ctx  = await _factory.Browser.NewContextAsync();
        var page = await ctx.NewPageAsync();

        await page.GotoAsync(_factory.ServerAddress);
        await page.ClickAsync("[data-count='5']");

        var pseudo = "UITest_" + Guid.NewGuid().ToString("N")[..8];
        await page.FillAsync("#pseudo-input", pseudo);
        await page.ClickAsync("#start-btn");
        await page.WaitForURLAsync("**/quiz", new() { Timeout = 10_000 });

        // Wait until at least one interactive input is ready (CSP-safe selector approach)
        await Task.WhenAny(
            page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }),
            page.Locator("#typed-answer:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }));

        return page;
    }

    private static async Task AnswerAsync(IPage page)
    {
        var mcqVisible = await page.Locator("#choices-grid")
            .EvaluateAsync<bool>("el => el.style.display !== 'none'");

        if (mcqVisible)
            await page.ClickAsync("#choice-0");
        else
        {
            await page.FillAsync("#typed-answer", "H");
            await page.ClickAsync("#submit-typed");
        }
    }
}

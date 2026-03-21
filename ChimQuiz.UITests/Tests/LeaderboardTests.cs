namespace ChimQuiz.UITests.Tests
{
#pragma warning disable CA1001 // factory is disposed via IAsyncLifetime.DisposeAsync

    public sealed class LeaderboardTests : IAsyncLifetime
    {
        private readonly PlaywrightWebApplicationFactory _factory = new();

        public async Task InitializeAsync()
        {
            await _factory.InitPlaywrightAsync();
        }

        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        // ── Page structure ────────────────────────────────────────────────────────

        [Fact]
        public async Task LeaderboardPage_HasCorrectTitle()
        {
            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync();
            IPage page = await ctx.NewPageAsync();
            await page.GotoAsync(_factory.ServerAddress + "/leaderboard");
            await Assertions.Expect(page).ToHaveTitleAsync("Classement - ChimQuiz");
        }

        [Fact]
        public async Task LeaderboardPage_AlltimeTab_IsActiveByDefault()
        {
            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync();
            IPage page = await ctx.NewPageAsync();
            await page.GotoAsync(_factory.ServerAddress + "/leaderboard");

            // All-time tab should carry tab-active class and weekly panel should be hidden
            await Assertions.Expect(page.Locator("#tab-alltime.tab-active")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#leaderboard-weekly")).ToBeHiddenAsync();
        }

        [Fact]
        public async Task LeaderboardPage_WeeklyTab_ShowsWeeklyPanel()
        {
            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync();
            IPage page = await ctx.NewPageAsync();
            await page.GotoAsync(_factory.ServerAddress + "/leaderboard");

            await page.ClickAsync("#tab-weekly");

            await Assertions.Expect(page.Locator("#leaderboard-weekly")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#leaderboard-alltime")).ToBeHiddenAsync();
        }

        // ── Player appears after game ─────────────────────────────────────────────

        [Fact]
        public async Task LeaderboardPage_ShowsPlayer_AfterCompletedGame()
        {
            string pseudo = "LBTest_" + Guid.NewGuid().ToString("N")[..8];

            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync();
            IPage page = await ctx.NewPageAsync();

            // Complete a full 5-question game
            await page.GotoAsync(_factory.ServerAddress);
            await page.ClickAsync("[data-count='5']");
            await page.FillAsync("#pseudo-input", pseudo);
            await page.ClickAsync("#start-btn");
            await page.WaitForURLAsync("**/quiz", new() { Timeout = 10_000 });
            await WaitForNextInputAsync(page);

            bool isGameOver = false;
            for (int i = 0; i < 10 && !isGameOver; i++)
            {
                isGameOver = await AnswerAndAdvanceAsync(page);
                if (!isGameOver)
                {
                    await WaitForNextInputAsync(page);
                }
            }

            Assert.True(isGameOver, "Game should be over after completing all questions");

            // Navigate to leaderboard and check the player appears
            await page.GotoAsync(_factory.ServerAddress + "/leaderboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait for leaderboard table to load (spinner replaced with table)
            await page.Locator("#leaderboard-alltime table").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

            string? content = await page.Locator("#leaderboard-alltime").TextContentAsync();
            Assert.Contains(pseudo, content ?? "");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static async Task WaitForNextInputAsync(IPage page)
        {
            await Task.WhenAny(
                page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }),
                page.Locator("#typed-answer:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }));
        }

        private static async Task<bool> AnswerAndAdvanceAsync(IPage page)
        {
            // Dismiss revenge overlay if it appeared after the last original question.
            if (await page.Locator("#revenge-overlay").IsVisibleAsync())
            {
                await page.ClickAsync(".revenge-btn");
                await page.Locator("#revenge-overlay").WaitForAsync(
                    new() { State = WaitForSelectorState.Hidden, Timeout = 10_000 });
                return false;
            }

            bool mcqVisible = await page.Locator("#choices-grid").IsVisibleAsync();
            if (mcqVisible)
            {
                await page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 10_000 });
                await page.ClickAsync("#choice-0");
            }
            else
            {
                await page.FillAsync("#typed-answer", "H");
                await page.ClickAsync("#submit-typed");
            }

            await page.Locator("#element-info-card").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });
            await page.ClickAsync("button:has-text(\"J'ai lu\")");

            // Poll until info card hides (next question) or game-over overlay appears.
            // WaitForFunctionAsync is avoided because the CSP blocks unsafe-eval.
            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(10);
            while (DateTimeOffset.UtcNow < deadline)
            {
                bool cardHidden = !await page.Locator("#element-info-card").IsVisibleAsync();
                bool gameOver = await page.Locator("#game-over").IsVisibleAsync();
                if (cardHidden || gameOver)
                {
                    return gameOver;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException("Game did not advance within 10 s after clicking J'ai lu");
        }
    }
}

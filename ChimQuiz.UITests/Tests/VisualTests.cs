namespace ChimQuiz.UITests.Tests
{
#pragma warning disable CA1001 // factory is disposed via IAsyncLifetime.DisposeAsync

    [Collection("UITests")]
    public sealed class VisualTests : IAsyncLifetime
    {
        private readonly PlaywrightWebApplicationFactory _factory = new();

        public async Task InitializeAsync()
        {
            await _factory.InitPlaywrightAsync();
        }

        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        // ── Viewport breakpoints ──────────────────────────────────────────────────

        public static TheoryData<int, int, string> Viewports => new()
        {
            { 390,  844,  "mobile"  },
            { 768,  1024, "tablet"  },
            { 1280, 800,  "laptop"  },
            { 1920, 1080, "desktop" },
        };

        // ── Home page ─────────────────────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Viewports))]
        public async Task HomePage_NoHorizontalOverflow(int width, int height, string label)
        {
            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync(
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                });
            IPage page = await ctx.NewPageAsync();
            await page.GotoAsync(_factory.ServerAddress);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            bool overflows = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(overflows,
                $"Home page: horizontal overflow at {label} ({width}×{height})");

            await TakeScreenshotAsync(page, $"home-{label}");
        }

        // ── Quiz page — question visible ──────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Viewports))]
        public async Task QuizPage_Question_NoHorizontalOverflow(int width, int height, string label)
        {
            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync(
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                });
            IPage page = await StartAsync(ctx);

            await TakeScreenshotAsync(page, $"quiz-question-{label}");

            bool overflows = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(overflows,
                $"Quiz question: horizontal overflow at {label} ({width}×{height})");
        }

        // ── Quiz page — info card visible ─────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Viewports))]
        public async Task QuizPage_InfoCard_NoOverflow(int width, int height, string label)
        {
            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync(
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                });
            IPage page = await StartAsync(ctx);
            await AnswerAsync(page);

            await page.Locator("#element-info-card").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5_000 });

            // Info card must fit inside its container without causing scroll
            await TakeScreenshotAsync(page, $"quiz-infocard-{label}");

            bool cardFits = await page.EvalOnSelectorAsync<bool>(
                "#question-card",
                "el => el.scrollHeight <= el.clientHeight + 2");  // +2px tolerance for rounding
            Assert.True(cardFits,
                $"Info card causes #question-card to scroll at {label} ({width}×{height})");

            bool overflows = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(overflows,
                $"Info card: horizontal overflow at {label} ({width}×{height})");
        }

        // ── Leaderboard page ──────────────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Viewports))]
        public async Task LeaderboardPage_NoHorizontalOverflow(int width, int height, string label)
        {
            await using IBrowserContext ctx = await _factory.Browser.NewContextAsync(
                new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                });
            IPage page = await ctx.NewPageAsync();
            await page.GotoAsync(_factory.ServerAddress + "/leaderboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await TakeScreenshotAsync(page, $"leaderboard-{label}");

            bool overflows = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(overflows,
                $"Leaderboard page: horizontal overflow at {label} ({width}×{height})");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private async Task<IPage> StartAsync(IBrowserContext ctx)
        {
            IPage page = await ctx.NewPageAsync();
            await page.GotoAsync(_factory.ServerAddress);
            await page.ClickAsync("[data-count='5']");

            string pseudo = "Visual_" + Guid.NewGuid().ToString("N")[..8];
            await page.FillAsync("#pseudo-input", pseudo);
            await page.ClickAsync("#start-btn");
            await page.WaitForURLAsync("**/quiz", new PageWaitForURLOptions { Timeout = 10_000 });

            await Task.WhenAny(
                page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }),
                page.Locator("#typed-answer:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }));

            return page;
        }

        private static async Task AnswerAsync(IPage page)
        {
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
        }

        private static async Task TakeScreenshotAsync(IPage page, string name)
        {
            try
            {
                string dir = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "screenshots"));
                Directory.CreateDirectory(dir);
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(dir, $"{name}.png"),
                    FullPage = false,
                });
            }
            catch
            {
                // Screenshot failures must never block test assertions
            }
        }
    }
}

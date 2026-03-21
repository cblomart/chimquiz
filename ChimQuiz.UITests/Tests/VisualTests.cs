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

            // Screenshot 1 : infocard à l'apparition
            await TakeScreenshotAsync(page, $"quiz-infocard-{label}");

            // Screenshot 2 : après 6,5 s — le bonus +5XP (Curieux(se) !) est disponible
            await page.WaitForTimeoutAsync(6_500);
            await TakeScreenshotAsync(page, $"quiz-infocard-bonus-{label}");

            // Info card must fit inside its container without causing scroll

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

        // ── Capture de tous les types de questions ────────────────────────────────

        [Fact]
        public async Task QuizPage_AllQuestionTypes_Screenshots()
        {
            // Capture les 4 types de questions à chacun des 4 viewports.
            (int Width, int Height, string Label)[] viewports =
            [
                (390,  844,  "mobile"),
                (768,  1024, "tablet"),
                (1280, 800,  "laptop"),
                (1920, 1080, "desktop"),
            ];

            HashSet<string> capturedAny = [];

            foreach ((int width, int height, string vLabel) in viewports)
            {
                await using IBrowserContext ctx = await _factory.Browser.NewContextAsync(
                    new BrowserNewContextOptions
                    {
                        ViewportSize = new ViewportSize { Width = width, Height = height },
                    });

                // 30 questions maximise la probabilité de voir les 4 types.
                IPage page = await StartAsync(ctx, 30);
                HashSet<string> capturedThisViewport = [];

                for (int i = 0; i < 30 && capturedThisViewport.Count < 4; i++)
                {
                    string type = await DetectQuestionTypeAsync(page);

                    if (capturedThisViewport.Add(type))
                    {
                        // Attendre la fin de l'animation d'entrée (fadeIn 350-400 ms)
                        await page.WaitForTimeoutAsync(500);
                        await TakeScreenshotAsync(page, $"quiz-type-{type}-{vLabel}");
                        capturedAny.Add(type);
                    }

                    bool isGameOver = await AnswerAndAdvanceAsync(page);
                    if (isGameOver)
                    {
                        break;
                    }

                    await Task.WhenAny(
                        page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }),
                        page.Locator("#typed-answer:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }));
                }
            }

            // On exige au moins les 2 familles (MCQ + typed) pour que la revue IA soit utile.
            Assert.True(capturedAny.Count >= 2,
                $"Seulement {capturedAny.Count} type(s) de question capturé(s) : {string.Join(", ", capturedAny)}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private async Task<IPage> StartAsync(IBrowserContext ctx, int questionCount = 5)
        {
            IPage page = await ctx.NewPageAsync();
            await page.GotoAsync(_factory.ServerAddress);
            await page.ClickAsync($"[data-count='{questionCount}']");

            string pseudo = "Visual_" + Guid.NewGuid().ToString("N")[..8];
            await page.FillAsync("#pseudo-input", pseudo);
            await page.ClickAsync("#start-btn");
            await page.WaitForURLAsync("**/quiz", new PageWaitForURLOptions { Timeout = 10_000 });

            await Task.WhenAny(
                page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }),
                page.Locator("#typed-answer:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }));

            return page;
        }

        /// <summary>Détecte le type de question affiché (MCQ vs typed, name vs symbol).</summary>
        private static async Task<string> DetectQuestionTypeAsync(IPage page)
        {
            bool typedVisible = await page.Locator("#typed-answer").IsVisibleAsync();
            if (typedVisible)
            {
                string? maxLen = await page.GetAttributeAsync("#typed-answer", "maxlength");
                return maxLen == "3" ? "name-to-symbol-typed" : "symbol-to-name-typed";
            }

            string? prompt = await page.Locator("#question-prompt").TextContentAsync();
            return (prompt ?? "").Contains("symbole", StringComparison.OrdinalIgnoreCase)
                ? "name-to-symbol-mcq"
                : "symbol-to-name-mcq";
        }

        /// <summary>Répond à la question courante, clique J'ai lu, renvoie true si game over.</summary>
        private static async Task<bool> AnswerAndAdvanceAsync(IPage page)
        {
            if (await page.Locator("#revenge-overlay").IsVisibleAsync())
            {
                await page.ClickAsync(".revenge-btn");
                await page.Locator("#revenge-overlay").WaitForAsync(
                    new() { State = WaitForSelectorState.Hidden, Timeout = 10_000 });
                return false;
            }

            await AnswerAsync(page);

            // Attendre l'infocard (ou game-over si la réponse a expiré et le jeu avance seul).
            // 20 s couvre le timer MCQ (15 s) + marge réseau.
            DateTimeOffset infoDeadline = DateTimeOffset.UtcNow.AddSeconds(20);
            while (DateTimeOffset.UtcNow < infoDeadline)
            {
                if (await page.Locator("#element-info-card").IsVisibleAsync()) break;
                if (await page.Locator("#game-over").IsVisibleAsync()) return true;
                await Task.Delay(150);
            }

            if (!await page.Locator("#element-info-card").IsVisibleAsync()) return true;
            await page.ClickAsync("button:has-text(\"J'ai lu\")");

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

        private static async Task AnswerAsync(IPage page)
        {
            bool mcqVisible = await page.Locator("#choices-grid").IsVisibleAsync();
            try
            {
                if (mcqVisible)
                {
                    await page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 5_000 });
                    await page.ClickAsync("#choice-0", new PageClickOptions { Timeout = 5_000 });
                }
                else
                {
                    await page.FillAsync("#typed-answer", "H", new PageFillOptions { Timeout = 5_000 });
                    await page.ClickAsync("#submit-typed", new PageClickOptions { Timeout = 5_000 });
                }
            }
            catch (Exception)
            {
                // Question timer may have fired before we could interact.
            }
        }

        private static async Task TakeScreenshotAsync(IPage page, string name)
        {
            try
            {
                // Ensure web fonts (Orbitron, Nunito) are loaded before taking the screenshot
                await page.EvaluateAsync("() => document.fonts.ready");

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

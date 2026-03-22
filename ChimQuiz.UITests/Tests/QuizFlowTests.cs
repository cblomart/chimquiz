namespace ChimQuiz.UITests.Tests
{
#pragma warning disable CA1001 // factory is disposed via IAsyncLifetime.DisposeAsync

    [Collection("UITests")]
    public sealed class QuizFlowTests : IAsyncLifetime
    {
        private readonly PlaywrightWebApplicationFactory _factory = new();

        public async Task InitializeAsync()
        {
            await _factory.InitPlaywrightAsync();
        }

        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        // ── Registration → quiz navigation ────────────────────────────────────────

        [Fact]
        public async Task Register_NavigatesToQuizPage()
        {
            IPage page = await StartAsync();
            await Assertions.Expect(page.Locator("#question-counter")).ToBeVisibleAsync();
        }

        [Fact]
        public async Task QuizPage_ShowsQuestion1Counter()
        {
            IPage page = await StartAsync();
            await Assertions.Expect(page.Locator("#question-counter"))
                .ToContainTextAsync("Question 1");
        }

        // ── Question rendering ────────────────────────────────────────────────────

        [Fact]
        public async Task QuizPage_ShowsNonEmptyPromptAndDisplayValue()
        {
            IPage page = await StartAsync();
            await Assertions.Expect(page.Locator("#question-prompt")).Not.ToBeEmptyAsync();
            await Assertions.Expect(page.Locator("#display-value")).Not.ToBeEmptyAsync();
        }

        [Fact]
        public async Task QuizPage_ShowsMCQChoicesOrTypedInput()
        {
            IPage page = await StartAsync();

            bool mcqVisible = await page.Locator("#choice-0").IsVisibleAsync();
            bool typedVisible = await page.Locator("#typed-answer").IsVisibleAsync();
            Assert.True(mcqVisible || typedVisible,
                "Expected MCQ choices or typed input to be visible");
        }

        // ── Answer → info card ────────────────────────────────────────────────────

        [Fact]
        public async Task AnsweringQuestion_ShowsElementInfoCard()
        {
            IPage page = await StartAsync();
            await AnswerAsync(page);

            await Assertions.Expect(page.Locator("#element-info-card"))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });
        }

        [Fact]
        public async Task InfoCard_ShowsAnswerVerdict()
        {
            IPage page = await StartAsync();
            await AnswerAsync(page);

            await page.Locator("#element-info-card").WaitForAsync(new() { Timeout = 5_000 });
            await Assertions.Expect(page.Locator("#answer-verdict")).ToBeVisibleAsync();
        }

        // ── J'ai lu! ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task JaiLu_AdvancesGameToQuestion2OrGameOver()
        {
            IPage page = await StartAsync();
            await AnswerAsync(page);
            await page.Locator("#element-info-card").WaitForAsync(new() { Timeout = 5_000 });

            await page.ClickAsync("button:has-text(\"J'ai lu\")");

            // After J'ai lu the info card should hide (hideInfoCard is called on next question load)
            await Assertions.Expect(page.Locator("#element-info-card"))
                .ToBeHiddenAsync(new() { Timeout = 10_000 });
        }

        // ── MCQ question rendering ────────────────────────────────────────────────

        [Fact]
        public async Task QuizPage_MCQ_HasFourLabelledChoices()
        {
            IPage page = await StartAsync();

            // With 5 questions: 4 MCQ + 1 typed — loop until we find an MCQ question
            for (int i = 0; i < 5; i++)
            {
                bool mcqVisible = await page.Locator("#choices-grid").IsVisibleAsync();
                if (mcqVisible)
                {
                    string[] letters = ["A", "B", "C", "D"];
                    for (int j = 0; j < 4; j++)
                    {
                        await Assertions.Expect(page.Locator($"#choice-{j}")).ToBeVisibleAsync();
                        await Assertions.Expect(page.Locator($"#choice-{j} .choice-letter"))
                            .ToHaveTextAsync(letters[j]);
                    }
                    return;
                }

                bool isGameOver = await AnswerAndAdvanceAsync(page);
                if (isGameOver)
                {
                    break;
                }

                await WaitForNextInputAsync(page);
            }

            Assert.Fail("No MCQ question found in 5-question game");
        }

        [Fact]
        public async Task QuizPage_NameToSymbolTyped_HasMaxLength3AndSymbolPlaceholder()
        {
            // Use 15 questions to guarantee at least one NameToSymbolTyped appears.
            IPage page = await StartAsync(15);

            for (int i = 0; i < 15; i++)
            {
                bool typedVisible = await page.Locator("#typed-answer").IsVisibleAsync();
                if (typedVisible)
                {
                    string? maxLen = await page.GetAttributeAsync("#typed-answer", "maxlength");
                    string? placeholder = await page.GetAttributeAsync("#typed-answer", "placeholder");

                    if (maxLen == "3")
                    {
                        Assert.Equal("3", maxLen);
                        Assert.Contains("Symbole", placeholder ?? "");
                        return;
                    }
                }

                bool isGameOver = await AnswerAndAdvanceAsync(page);
                if (isGameOver)
                {
                    break;
                }

                await WaitForNextInputAsync(page);
            }

            Assert.Fail("No NameToSymbolTyped question (maxlength=3) found in 15-question game");
        }

        // ── Info card content ─────────────────────────────────────────────────────

        [Fact]
        public async Task InfoCard_ShowsAtomicStructure_WithPositiveValues()
        {
            IPage page = await StartAsync();
            await AnswerAsync(page);
            await page.Locator("#element-info-card").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });

            string? protons = await page.Locator("#info-protons").TextContentAsync();
            string? neutrons = await page.Locator("#info-neutrons").TextContentAsync();
            string? electrons = await page.Locator("#info-electrons").TextContentAsync();

            Assert.True(int.TryParse(protons?.Trim(), out int p) && p > 0,
                $"Protons should be a positive integer, got '{protons}'");
            Assert.True(int.TryParse(neutrons?.Trim(), out int n) && n >= 0,
                $"Neutrons should be a non-negative integer, got '{neutrons}'");
            Assert.True(int.TryParse(electrons?.Trim(), out int e) && e > 0,
                $"Electrons should be a positive integer, got '{electrons}'");
        }

        [Fact]
        public async Task InfoCard_TimerLabel_CountsDown()
        {
            IPage page = await StartAsync();
            await AnswerAsync(page);
            await page.Locator("#element-info-card").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });

            string? initial = await page.Locator("#info-timer-label").TextContentAsync();
            await Task.Delay(2_500);
            string? later = await page.Locator("#info-timer-label").TextContentAsync();

            Assert.NotEqual(initial, later);
            int initialSecs = int.Parse(initial!.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture);
            int laterSecs = int.Parse(later!.TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(laterSecs < initialSecs,
                $"Timer should count down: {initial} → {later}");
        }

        // ── Game over ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GameOver_AppearsAfterCompletingAllQuestions()
        {
            IPage page = await StartAsync();

            bool isGameOver = false;
            for (int i = 0; i < 20 && !isGameOver; i++)
            {
                isGameOver = await AnswerAndAdvanceAsync(page);
                if (!isGameOver)
                {
                    await WaitForNextInputAsync(page);
                }
            }

            Assert.True(isGameOver, "Game over overlay should appear after completing all questions");
            await Assertions.Expect(page.Locator("#result-correct")).ToBeVisibleAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Registers a unique player, picks questions, navigates to quiz, waits for Q1.</summary>
        private async Task<IPage> StartAsync(int questionCount = 5)
        {
            IBrowserContext ctx = await _factory.Browser.NewContextAsync();
            IPage page = await ctx.NewPageAsync();

            await page.GotoAsync(_factory.ServerAddress);
            await page.ClickAsync($"[data-count='{questionCount}']");

            string pseudo = "UITest_" + Guid.NewGuid().ToString("N")[..8];
            await page.FillAsync("#pseudo-input", pseudo);
            await page.ClickAsync("#start-btn");
            await page.WaitForURLAsync("**/quiz", new() { Timeout = 10_000 });

            await WaitForNextInputAsync(page);
            return page;
        }

        /// <summary>Waits until at least one interactive input is ready.</summary>
        private static async Task WaitForNextInputAsync(IPage page)
        {
            await Task.WhenAny(
                page.Locator("#choice-0:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }),
                page.Locator("#typed-answer:not([disabled])").WaitForAsync(new() { Timeout = 10_000 }));
        }

        /// <summary>Answers the current question, clicks J'ai lu, returns true if game over.</summary>
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

            await AnswerAsync(page);

            DateTimeOffset infoDeadline = DateTimeOffset.UtcNow.AddSeconds(20);
            while (DateTimeOffset.UtcNow < infoDeadline)
            {
                if (await page.Locator("#element-info-card").IsVisibleAsync())
                {
                    break;
                }

                if (await page.Locator("#game-over").IsVisibleAsync())
                {
                    return true;
                }

                await Task.Delay(150);
            }

            if (!await page.Locator("#element-info-card").IsVisibleAsync())
            {
                return true;
            }

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

        private static async Task AnswerAsync(IPage page)
        {
            // IsVisibleAsync checks CSS visibility + display (not just inline style)
            bool mcqVisible = await page.Locator("#choices-grid").IsVisibleAsync();

            try
            {
                if (mcqVisible)
                {
                    // Wait for button to be stable before clicking
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
                // Question timer may have fired before we could click — the info card
                // will appear via onQuestionTimeout, so AnswerAndAdvanceAsync can still proceed.
            }
        }
    }
}

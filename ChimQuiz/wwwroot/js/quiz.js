(() => {
  // ChimQuiz/wwwroot/js/src/api.js
  async function apiFetch(url, method = "GET", body = null) {
    const headers = { "Content-Type": "application/json" };
    if (method !== "GET") {
      const tokenMeta = document.querySelector('meta[name="__RequestVerificationToken"]');
      if (tokenMeta) headers["X-CSRF-TOKEN"] = tokenMeta.content;
    }
    const options = { method, headers, credentials: "same-origin" };
    if (body !== null) options.body = JSON.stringify(body);
    return fetch(url, options);
  }
  function escHtml(str) {
    if (str == null) return "";
    return String(str).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;");
  }

  // ChimQuiz/wwwroot/js/src/player.js
  async function initPlayer() {
    try {
      const res = await apiFetch("/api/player/me");
      if (res.ok) {
        window.player = await res.json();
        updateNavPlayer(window.player);
      }
    } catch (_) {
    }
  }
  function updateNavPlayer(player) {
    const el = document.getElementById("player-nav-info");
    if (!el || !player) return;
    el.textContent = `${player.rankEmoji} ${player.pseudo} \xB7 ${player.totalXp} XP`;
    el.style.display = "";
  }

  // ChimQuiz/wwwroot/js/src/ui.js
  function updateComboDisplay(combo) {
    const el = document.getElementById("combo-display");
    if (!el) return;
    if (combo >= 3) {
      el.style.display = "";
      el.textContent = combo >= 10 ? `\u{1F31F} COMBO x${combo}` : combo >= 8 ? `\u{1F4A5} COMBO x${combo}` : combo >= 5 ? `\u26A1 Combo x${combo}` : `\u{1F525} Combo x${combo}`;
      el.classList.add("on-fire");
    } else {
      el.style.display = "none";
      el.classList.remove("on-fire");
    }
  }
  function animateXpGain(amount, comboMessage) {
    const container = document.getElementById("xp-float-container");
    if (!container) return;
    const xpEl = document.getElementById("xp-display");
    const rect = xpEl ? xpEl.getBoundingClientRect() : { left: window.innerWidth / 2, top: window.innerHeight / 2 };
    const el = document.createElement("div");
    el.className = "xp-float";
    el.textContent = comboMessage ? `+${amount} XP  ${comboMessage}` : `+${amount} XP`;
    el.style.left = `${rect.left + rect.width / 2}px`;
    el.style.top = `${rect.top}px`;
    container.appendChild(el);
    setTimeout(() => el.remove(), 1300);
  }
  function animateCounter(el, from, to, duration) {
    const start = performance.now();
    function step(now) {
      const p = Math.min((now - start) / duration, 1);
      el.textContent = Math.round(from + (to - from) * (1 - Math.pow(1 - p, 3)));
      if (p < 1) requestAnimationFrame(step);
    }
    requestAnimationFrame(step);
  }
  function showError(el, msg) {
    if (!el) return;
    el.textContent = msg;
    el.style.display = "block";
  }

  // ChimQuiz/wwwroot/js/src/home.js
  function initHomePage() {
    const input = document.getElementById("pseudo-input");
    const startBtn = document.getElementById("start-btn");
    const errorEl = document.getElementById("pseudo-error");
    if (window.player) {
      input.value = window.player.pseudo;
    } else {
      input.placeholder = generateClientPseudo();
    }
    let selectedCount = parseInt(sessionStorage.getItem("questionCount") || "15", 10);
    const qcountBtns = document.querySelectorAll(".qcount-btn");
    const statQcount = document.getElementById("stat-qcount");
    function setCount(n) {
      selectedCount = n;
      sessionStorage.setItem("questionCount", n);
      qcountBtns.forEach((b) => b.classList.toggle("qcount-btn--active", parseInt(b.dataset.count, 10) === n));
      if (statQcount) statQcount.textContent = n;
    }
    setCount(selectedCount);
    qcountBtns.forEach((b) => b.addEventListener("click", () => setCount(parseInt(b.dataset.count, 10))));
    startBtn.addEventListener("click", async () => {
      const pseudo = input.value.trim() || input.placeholder;
      if (errorEl) errorEl.style.display = "none";
      startBtn.disabled = true;
      startBtn.textContent = "\u23F3 Chargement...";
      try {
        if (window.player) {
          window.location.href = "/quiz";
          return;
        }
        const res = await apiFetch("/api/player/create", "POST", { pseudo });
        const data = await res.json();
        if (!res.ok) {
          const msg = data.error?.includes("d\xE9j\xE0 utilis\xE9") ? `\u{1F512} Ce pseudo est d\xE9j\xE0 pris ! Choisis-en un autre.` : data.error || "Pseudo invalide.";
          showError(errorEl, msg);
          return;
        }
        window.player = data;
        window.location.href = "/quiz";
      } catch (_) {
        showError(errorEl, "Erreur r\xE9seau. R\xE9essaie.");
      } finally {
        startBtn.disabled = false;
        startBtn.innerHTML = '<span class="btn-icon">\u26A1</span>Jouer !';
      }
    });
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") startBtn.click();
    });
  }
  function generateClientPseudo() {
    const adj = ["Brillant", "Curieux", "Atomique", "\xC9lectrique", "Quantique", "Cosmique", "Ionique", "Dynamique"];
    const elem = ["Hydro", "Carbone", "N\xE9on", "Fer", "Or", "Argent", "Krypton", "Radium"];
    return `${adj[~~(Math.random() * adj.length)]}${elem[~~(Math.random() * elem.length)]}${~~(Math.random() * 90) + 10}`;
  }

  // ChimQuiz/wwwroot/js/src/state.js
  var state = {
    currentQuestion: null,
    gameActive: false,
    animating: false,
    lastResult: null,
    // replaces window._lastResult
    cardShownAt: 0,
    nextQuestionTimer: null,
    timerInterval: null,
    questionTimerTimeout: null,
    questionTimerInterval: null,
    questionStartedAt: 0,
    // timestamp when timer started
    onQuestionTimeout: null
    // callback set by game.js to break circular dep with idle.js
  };

  // ChimQuiz/wwwroot/js/src/timers.js
  var ANSWER_TIME_MCQ = 15;
  var ANSWER_TIME_TYPED = 25;
  var INFO_DISPLAY_SECONDS = 12;
  var BONUS_XP_THRESHOLD_MS = 6e3;
  function clearTimers() {
    clearTimeout(state.nextQuestionTimer);
    clearInterval(state.timerInterval);
    state.nextQuestionTimer = null;
    state.timerInterval = null;
  }
  function clearQuestionTimer() {
    clearTimeout(state.questionTimerTimeout);
    clearInterval(state.questionTimerInterval);
    state.questionTimerTimeout = null;
    state.questionTimerInterval = null;
  }
  function startInfoTimer(seconds) {
    clearInterval(state.timerInterval);
    state.timerInterval = null;
    let remaining = seconds;
    const fill = document.getElementById("info-timer-fill");
    const label = document.getElementById("info-timer-label");
    if (fill) {
      fill.style.transition = "none";
      fill.style.width = "100%";
      void fill.offsetHeight;
      fill.style.transition = `width ${seconds}s linear`;
      fill.style.width = "0%";
    }
    if (label) label.textContent = `${seconds}s`;
    state.timerInterval = setInterval(() => {
      remaining--;
      if (label) label.textContent = `${Math.max(0, remaining)}s`;
    }, 1e3);
  }
  function startQuestionTimer(seconds, onTimeout) {
    clearQuestionTimer();
    state.questionStartedAt = Date.now();
    const fill = document.getElementById("question-timer-fill");
    const count = document.getElementById("question-timer-count");
    const area = document.getElementById("question-timer-area");
    if (area) area.className = "question-timer-area";
    if (count) count.textContent = seconds;
    if (fill) {
      fill.style.transition = "none";
      fill.style.width = "100%";
      void fill.offsetHeight;
      fill.style.transition = `width ${seconds}s linear`;
      fill.style.width = "0%";
    }
    let remaining = seconds;
    state.questionTimerInterval = setInterval(() => {
      remaining--;
      if (count) count.textContent = Math.max(0, remaining);
      const pct = remaining / seconds;
      if (area) {
        if (pct <= 0.2) area.className = "question-timer-area urgent";
        else if (pct <= 0.4) area.className = "question-timer-area warning";
        else area.className = "question-timer-area";
      }
    }, 1e3);
    state.questionTimerTimeout = setTimeout(() => {
      clearInterval(state.questionTimerInterval);
      state.questionTimerInterval = null;
      if (onTimeout) onTimeout();
    }, seconds * 1e3);
  }

  // ChimQuiz/wwwroot/js/src/idle.js
  var IDLE_TIMEOUT_MS = 2 * 60 * 1e3;
  var idleTimer = null;
  function resetIdleTimer() {
    if (!state.gameActive) return;
    clearTimeout(idleTimer);
    idleTimer = setTimeout(showIdleModal, IDLE_TIMEOUT_MS);
  }
  function showIdleModal() {
    if (!state.gameActive) return;
    clearQuestionTimer();
    clearTimeout(state.nextQuestionTimer);
    state.nextQuestionTimer = null;
    document.getElementById("idle-modal").style.display = "flex";
  }
  function dismissIdle() {
    document.getElementById("idle-modal").style.display = "none";
    resetIdleTimer();
    const infoCard = document.getElementById("element-info-card");
    const infoVisible = infoCard && infoCard.style.display !== "none";
    if (!infoVisible && state.currentQuestion) {
      const isTyped = state.currentQuestion.type === "SymbolToNameTyped";
      startQuestionTimer(
        isTyped ? ANSWER_TIME_TYPED : ANSWER_TIME_MCQ,
        state.onQuestionTimeout
      );
    }
  }
  function initIdleDetection() {
    ["click", "keydown", "touchstart", "mousemove"].forEach(
      (evt) => document.addEventListener(evt, resetIdleTimer, { passive: true })
    );
    resetIdleTimer();
  }

  // ChimQuiz/wwwroot/js/src/game.js
  async function initQuizPage() {
    if (window.player) {
      const pseudoEl = document.getElementById("quiz-pseudo");
      const rankEl = document.getElementById("quiz-rank");
      const streakEl = document.getElementById("quiz-streak");
      if (pseudoEl) pseudoEl.textContent = window.player.pseudo;
      if (rankEl) rankEl.textContent = `${window.player.rankEmoji} ${window.player.rankName}`;
      if (streakEl) streakEl.textContent = window.player.currentStreak;
    }
    const questionCount = parseInt(sessionStorage.getItem("questionCount") || "15", 10);
    const startRes = await apiFetch("/api/quiz/start", "POST", { questionCount });
    if (!startRes.ok) {
      if (startRes.status === 401) {
        window.location.href = "/";
        return;
      }
      console.error("Failed to start quiz");
      return;
    }
    const typedInput = document.getElementById("typed-answer");
    if (typedInput) {
      typedInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter") submitTypedAnswer();
      });
    }
    state.gameActive = true;
    state.onQuestionTimeout = onQuestionTimeout;
    initIdleDetection();
    await loadNextQuestion();
  }
  async function loadNextQuestion() {
    hideInfoCard();
    const res = await apiFetch("/api/quiz/question");
    if (!res.ok) return;
    state.currentQuestion = await res.json();
    renderQuestion(state.currentQuestion);
  }
  function setupTypedInput() {
    const typedInput = document.getElementById("typed-answer");
    const submitBtn = document.getElementById("submit-typed");
    if (typedInput) {
      typedInput.value = "";
      typedInput.disabled = false;
      typedInput.className = "typed-answer-input";
      setTimeout(() => typedInput.focus(), 350);
    }
    if (submitBtn) submitBtn.disabled = false;
  }
  function setupMCQButtons(choices) {
    const letters = ["A", "B", "C", "D"];
    for (let i = 0; i < 4; i++) {
      const btn = document.getElementById(`choice-${i}`);
      if (!btn) continue;
      btn.className = "choice-btn";
      btn.disabled = false;
      btn.dataset.value = choices[i] || "";
      const textEl = btn.querySelector(".choice-text");
      const letterEl = btn.querySelector(".choice-letter");
      if (textEl) textEl.textContent = choices[i] || "";
      if (letterEl) letterEl.textContent = letters[i];
    }
  }
  function renderQuestionHeader(q) {
    const revengeBadge = document.getElementById("revenge-badge");
    if (revengeBadge) revengeBadge.style.display = q.isRevenge ? "inline-block" : "none";
    const counterEl = document.getElementById("question-counter");
    if (counterEl) counterEl.textContent = q.isRevenge ? `Revanche ${q.questionNumber} / ${q.totalQuestions}` : `Question ${q.questionNumber} / ${q.totalQuestions}`;
    const progressBar = document.getElementById("progress-bar");
    if (progressBar) progressBar.style.width = `${(q.questionNumber - 1) / q.totalQuestions * 100}%`;
    const xpDisplay = document.getElementById("xp-display");
    if (xpDisplay) xpDisplay.textContent = `${q.totalXp} XP`;
    const multEl = document.getElementById("combo-multiplier");
    if (multEl) multEl.textContent = q.comboMultiplier;
    updateComboDisplay(q.comboCount);
  }
  function renderQuestion(q) {
    const isTyped = q.type === "SymbolToNameTyped";
    const promptEl = document.getElementById("question-prompt");
    const displayEl = document.getElementById("display-value");
    const choicesGrid = document.getElementById("choices-grid");
    const typedArea = document.getElementById("typed-input-area");
    renderQuestionHeader(q);
    if (promptEl) promptEl.textContent = q.prompt;
    if (displayEl) displayEl.textContent = q.displayValue;
    if (choicesGrid) choicesGrid.style.display = isTyped ? "none" : "grid";
    if (typedArea) typedArea.style.display = isTyped ? "flex" : "none";
    if (isTyped) {
      setupTypedInput();
    } else {
      setupMCQButtons(q.choices);
    }
    const card = document.getElementById("question-card");
    if (card) {
      card.style.animation = "none";
      void card.offsetHeight;
      card.style.animation = "fadeIn 0.35s ease forwards";
    }
    startQuestionTimer(isTyped ? ANSWER_TIME_TYPED : ANSWER_TIME_MCQ, onQuestionTimeout);
  }
  async function selectAnswer(answer) {
    if (state.animating || !state.gameActive) return;
    state.animating = true;
    clearQuestionTimer();
    const elapsed = Date.now() - state.questionStartedAt;
    const isSpeedBonus = elapsed < ANSWER_TIME_MCQ * 1e3 / 3;
    for (let i = 0; i < 4; i++) {
      const btn = document.getElementById(`choice-${i}`);
      if (btn) btn.disabled = true;
    }
    const res = await apiFetch("/api/quiz/answer", "POST", { answer });
    if (!res.ok) {
      state.animating = false;
      return;
    }
    const result = await res.json();
    if (isSpeedBonus && result.isCorrect) animateXpGain(2, "\u26A1 Rapide !");
    handleAnswerResult(result, answer);
  }
  async function submitTypedAnswer() {
    if (state.animating || !state.gameActive) return;
    const typedInput = document.getElementById("typed-answer");
    const submitBtn = document.getElementById("submit-typed");
    const answer = typedInput?.value?.trim() ?? "";
    if (!answer) {
      typedInput?.classList.add("shake");
      setTimeout(() => typedInput?.classList.remove("shake"), 500);
      return;
    }
    state.animating = true;
    clearQuestionTimer();
    const elapsed = Date.now() - state.questionStartedAt;
    const isSpeedBonus = elapsed < ANSWER_TIME_TYPED * 1e3 / 3;
    if (typedInput) typedInput.disabled = true;
    if (submitBtn) submitBtn.disabled = true;
    const res = await apiFetch("/api/quiz/answer", "POST", { answer });
    if (!res.ok) {
      state.animating = false;
      return;
    }
    const result = await res.json();
    if (isSpeedBonus && result.isCorrect) animateXpGain(2, "\u26A1 Rapide !");
    if (typedInput) {
      typedInput.classList.add(result.isCorrect ? "typed-correct" : "typed-incorrect");
    }
    handleAnswerResult(result, answer);
  }
  async function onQuestionTimeout() {
    if (state.animating || !state.gameActive) return;
    state.animating = true;
    clearQuestionTimer();
    for (let i = 0; i < 4; i++) {
      const btn = document.getElementById(`choice-${i}`);
      if (btn) btn.disabled = true;
    }
    const typedInput = document.getElementById("typed-answer");
    const submitBtn = document.getElementById("submit-typed");
    if (typedInput) typedInput.disabled = true;
    if (submitBtn) submitBtn.disabled = true;
    const res = await apiFetch("/api/quiz/answer", "POST", { answer: "" });
    if (!res.ok) {
      state.animating = false;
      return;
    }
    const result = await res.json();
    result._isTimeout = true;
    handleAnswerResult(result, "");
  }
  function handleAnswerResult(result, givenAnswer) {
    result._givenAnswer = givenAnswer;
    state.lastResult = result;
    if (!result.isTyped) {
      for (let i = 0; i < 4; i++) {
        const btn = document.getElementById(`choice-${i}`);
        if (!btn) continue;
        if (btn.dataset.value === result.correctAnswer) {
          btn.classList.add("correct");
        } else if (btn.dataset.value === givenAnswer && !result.isCorrect) {
          btn.classList.add("incorrect");
        }
      }
    }
    const xpDisplay = document.getElementById("xp-display");
    if (xpDisplay) xpDisplay.textContent = `${result.totalXp} XP`;
    const sessionXpEl = document.getElementById("quiz-session-xp");
    if (sessionXpEl) sessionXpEl.textContent = result.totalXp;
    if (result.isCorrect) animateXpGain(result.xpEarned, result.comboMessage);
    updateComboDisplay(result.comboCount);
    const progressBar = document.getElementById("progress-bar");
    if (progressBar) progressBar.style.width = `${(result.questionIndex + 1) / result.totalQuestions * 100}%`;
    if (result.isRevengeStart) {
      setTimeout(() => showRevengeOverlay(result), 400);
    } else {
      setTimeout(() => showInfoCard(result), 400);
    }
  }
  function renderVerdict(result) {
    const verdict = document.getElementById("answer-verdict");
    if (!verdict) return;
    if (result._isTimeout) {
      verdict.textContent = `\u23F1\uFE0F Temps \xE9coul\xE9 ! R\xE9ponse : ${result.correctAnswer}`;
      verdict.className = "answer-verdict verdict-incorrect";
    } else if (result.isCorrect && result.wasFuzzyMatch) {
      verdict.textContent = "\u2705 Presque parfait !";
      verdict.className = "answer-verdict verdict-fuzzy";
    } else if (result.isCorrect) {
      verdict.textContent = result.comboMessage || "\u2705 Correct !";
      verdict.className = "answer-verdict verdict-correct";
    } else {
      verdict.textContent = `\u274C R\xE9ponse : ${result.correctAnswer}`;
      verdict.className = "answer-verdict verdict-incorrect";
    }
  }
  function renderSpellingCorrection(result) {
    const block = document.getElementById("spelling-correction");
    const given = document.getElementById("spelling-given");
    const correct = document.getElementById("spelling-correct");
    const show = result.isCorrect && result.wasFuzzyMatch && result._givenAnswer;
    if (show) {
      if (given) given.textContent = result._givenAnswer;
      if (correct) correct.textContent = result.correctAnswer;
    }
    if (block) block.style.display = show ? "flex" : "none";
  }
  function setElementIdentity(result) {
    const symbol = document.getElementById("info-symbol");
    const atomNum = document.getElementById("info-atomic-number");
    const name = document.getElementById("info-name");
    if (symbol) symbol.textContent = result.elementSymbol;
    if (atomNum) atomNum.textContent = `#${state.currentQuestion?.elementId ?? ""}`;
    if (name) name.textContent = result.elementName;
  }
  function renderFunFact(result) {
    const factBlock = document.getElementById("info-fact-block");
    const factTxt = document.getElementById("info-fact-text");
    if (factTxt && result.funFact) factTxt.textContent = result.funFact;
    if (factBlock) factBlock.style.display = result.funFact ? "block" : "none";
  }
  function renderElementDetails(result) {
    const useText = document.getElementById("info-use-text");
    const where = document.getElementById("info-where-text");
    if (useText) useText.textContent = result.commonUse || "";
    if (where) where.textContent = result.whereToFind || "";
  }
  function showInfoCard(result) {
    const card = document.getElementById("element-info-card");
    if (!card) return;
    ["question-area", "choices-grid", "typed-input-area", "question-timer-area"].forEach((id) => {
      const el = document.getElementById(id);
      if (el) el.style.visibility = "hidden";
    });
    setElementIdentity(result);
    renderVerdict(result);
    renderSpellingCorrection(result);
    renderFunFact(result);
    renderElementDetails(result);
    if (result.isTyped && result.isCorrect && !result.wasFuzzyMatch) {
      animateXpGain(3, "\u270D\uFE0F Parfait !");
    }
    state.cardShownAt = Date.now();
    card.style.display = "flex";
    card.style.animation = "none";
    void card.offsetHeight;
    card.style.animation = "slideUp 0.35s ease forwards";
    startInfoTimer(INFO_DISPLAY_SECONDS);
    scheduleNextQuestion(result, INFO_DISPLAY_SECONDS * 1e3);
  }
  function hideInfoCard() {
    const card = document.getElementById("element-info-card");
    if (card) card.style.display = "none";
    ["question-area", "choices-grid", "typed-input-area", "question-timer-area"].forEach((id) => {
      const el = document.getElementById(id);
      if (el) el.style.visibility = "";
    });
    clearTimers();
  }
  function scheduleNextQuestion(result, delayMs) {
    clearTimeout(state.nextQuestionTimer);
    state.nextQuestionTimer = setTimeout(async () => {
      state.cardShownAt = 0;
      clearTimers();
      if (result.isGameOver) {
        state.gameActive = false;
        await showGameOver(result);
      } else {
        await loadNextQuestion();
      }
      state.animating = false;
    }, delayMs);
  }
  async function nextQuestion() {
    clearTimers();
    const result = state.lastResult;
    if (!result) return;
    const elapsed = state.cardShownAt ? Date.now() - state.cardShownAt : 0;
    state.cardShownAt = 0;
    if (elapsed >= BONUS_XP_THRESHOLD_MS) {
      animateXpGain(5, "\u{1F4DA} Curieux(se) !");
    }
    if (result.isGameOver) {
      state.gameActive = false;
      await showGameOver(result);
    } else {
      await loadNextQuestion();
    }
    state.animating = false;
  }
  function showRevengeOverlay(result) {
    clearTimers();
    hideInfoCard();
    showInfoCard(result);
    clearTimeout(state.nextQuestionTimer);
    state.nextQuestionTimer = setTimeout(() => {
      hideInfoCard();
      const overlay = document.getElementById("revenge-overlay");
      const countEl = document.getElementById("revenge-count");
      const listEl = document.getElementById("revenge-elements");
      if (!overlay) return;
      if (countEl) countEl.textContent = result.totalQuestions || "?";
      if (listEl) listEl.innerHTML = "";
      overlay.style.display = "flex";
      overlay.style.animation = "none";
      void overlay.offsetHeight;
      overlay.style.animation = "fadeIn 0.4s ease forwards";
    }, 13e3);
  }
  async function startRevenge() {
    const overlay = document.getElementById("revenge-overlay");
    if (overlay) overlay.style.display = "none";
    state.animating = false;
    state.gameActive = true;
    await loadNextQuestion();
  }
  async function showGameOver(result) {
    const overlay = document.getElementById("game-over");
    if (!overlay) return;
    const correctEl = document.getElementById("result-correct");
    const xpEl = document.getElementById("result-xp");
    const comboEl = document.getElementById("result-combo");
    if (correctEl) correctEl.textContent = `${result.correctCount}/${result.totalQuestions}`;
    if (comboEl) comboEl.textContent = `x${result.maxCombo}`;
    if (xpEl) {
      xpEl.textContent = "0";
      animateCounter(xpEl, 0, result.totalXp, 1e3);
    }
    const pct = result.correctCount / result.totalQuestions;
    const emojiEl = document.getElementById("game-over-emoji");
    if (emojiEl) {
      emojiEl.textContent = pct >= 0.9 ? "\u{1F31F}" : pct >= 0.7 ? "\u{1F3C6}" : pct >= 0.5 ? "\u{1F3AF}" : "\u{1F9EA}";
    }
    try {
      const playerRes = await apiFetch("/api/player/me");
      if (playerRes.ok) {
        const player = await playerRes.json();
        window.player = player;
        updateNavPlayer(player);
        renderRankSummary(player);
      }
    } catch (_) {
    }
    overlay.style.display = "flex";
  }
  function renderRankSummary(player) {
    const rankDisplay = document.getElementById("rank-display");
    const currentLabel = document.getElementById("rank-current-label");
    const nextLabel = document.getElementById("rank-next-label");
    const progressFill = document.getElementById("rank-progress-fill");
    const rankNameDisp = document.getElementById("rank-name-display");
    if (rankDisplay) rankDisplay.textContent = player.rankEmoji;
    if (rankNameDisp) rankNameDisp.textContent = `${player.rankName} \u2014 ${player.totalXp} XP total`;
    if (currentLabel) currentLabel.textContent = `${player.xpForCurrentRank} XP`;
    if (nextLabel) nextLabel.textContent = player.xpForNextRank === 2147483647 ? "Max" : `${player.xpForNextRank} XP`;
    setTimeout(() => {
      if (progressFill) progressFill.style.width = `${player.rankProgressPercent}%`;
    }, 300);
  }
  async function replayGame() {
    const overlay = document.getElementById("game-over");
    if (overlay) overlay.style.display = "none";
    clearTimers();
    state.animating = false;
    state.gameActive = false;
    state.lastResult = null;
    hideInfoCard();
    await initQuizPage();
  }

  // ChimQuiz/wwwroot/js/src/leaderboard.js
  var currentTab = "alltime";
  var leaderboardData = { alltime: null, weekly: null };
  async function initLeaderboardPage() {
    await Promise.all([loadLeaderboardTab("alltime"), loadLeaderboardTab("weekly")]);
  }
  async function loadLeaderboardTab(tab) {
    const endpoint = tab === "alltime" ? "/api/leaderboard/alltime" : "/api/leaderboard/weekly";
    try {
      const res = await apiFetch(endpoint);
      const data = res.ok ? await res.json() : [];
      leaderboardData[tab] = data;
      if (tab === currentTab) renderLeaderboard(tab, data);
    } catch (_) {
      leaderboardData[tab] = [];
      if (tab === currentTab) renderLeaderboard(tab, []);
    }
  }
  function switchTab(tab) {
    if (tab === currentTab) return;
    currentTab = tab;
    document.querySelectorAll(".tab-btn").forEach((btn) => btn.classList.remove("tab-active"));
    const activeBtn = document.getElementById(`tab-${tab}`);
    if (activeBtn) activeBtn.classList.add("tab-active");
    const alltime = document.getElementById("leaderboard-alltime");
    const weekly = document.getElementById("leaderboard-weekly");
    if (alltime) alltime.style.display = tab === "alltime" ? "block" : "none";
    if (weekly) weekly.style.display = tab === "weekly" ? "block" : "none";
    if (leaderboardData[tab]) renderLeaderboard(tab, leaderboardData[tab]);
  }
  function renderLeaderboard(tab, scores) {
    const container = document.getElementById(`leaderboard-${tab}`);
    if (!container) return;
    if (!scores || scores.length === 0) {
      container.innerHTML = `<div class="leaderboard-empty"><div class="leaderboard-empty-icon">\u{1F9EA}</div><p>Aucune partie enregistr\xE9e.<br/>Sois le premier \xE0 jouer !</p></div>`;
      return;
    }
    const myPseudo = window.player?.pseudo ?? "";
    const rankIcons = ["\u{1F947}", "\u{1F948}", "\u{1F949}"];
    const rankClasses = ["gold", "silver", "bronze"];
    const isAlltime = tab === "alltime";
    const rows = scores.map((s, i) => {
      const isMe = s.pseudo === myPseudo;
      const rankCell = i < 3 ? `<td class="rank-cell ${rankClasses[i]}">${rankIcons[i]}</td>` : `<td class="rank-cell">${i + 1}</td>`;
      return isAlltime ? `<tr${isMe ? ' class="is-me"' : ""}>${rankCell}
                <td class="pseudo-cell">${escHtml(s.rankEmoji)} <strong>${escHtml(s.pseudo)}</strong></td>
                <td class="score-cell">${s.score} XP</td>
                <td>${escHtml(s.rankName)}</td>
                <td class="streak-cell">\u{1F525} ${s.currentStreak}</td></tr>` : `<tr${isMe ? ' class="is-me"' : ""}>${rankCell}
                <td class="pseudo-cell">${escHtml(s.rankEmoji)} <strong>${escHtml(s.pseudo)}</strong></td>
                <td class="score-cell">${s.score} XP</td>
                <td>${s.correctAnswers}/15 \u2705</td>
                <td>Combo x${s.maxCombo}</td></tr>`;
    }).join("");
    const headers = isAlltime ? "<th>#</th><th>Joueur</th><th>Meilleur score</th><th>Rang</th><th>S\xE9rie</th>" : "<th>#</th><th>Joueur</th><th>Score</th><th>Pr\xE9cision</th><th>Combo</th>";
    container.innerHTML = `<table class="leaderboard-table glass-card"><thead><tr>${headers}</tr></thead><tbody>${rows}</tbody></table>`;
    const myPosition = document.getElementById("my-position");
    const myPositionText = document.getElementById("my-position-text");
    if (myPseudo && myPosition && myPositionText) {
      const idx = scores.findIndex((s) => s.pseudo === myPseudo);
      if (idx >= 0) {
        myPositionText.textContent = `#${idx + 1} \u2013 ${scores[idx].score} XP`;
        myPosition.style.display = "flex";
      } else {
        myPosition.style.display = "none";
      }
    }
  }

  // ChimQuiz/wwwroot/js/src/main.js
  document.addEventListener("DOMContentLoaded", async () => {
    await initPlayer();
    if (document.getElementById("start-btn")) {
      initHomePage();
    }
    if (document.getElementById("question-card")) {
      await initQuizPage();
    }
    if (document.getElementById("leaderboard-alltime")) {
      await initLeaderboardPage();
    }
  });
  window.selectAnswer = selectAnswer;
  window.submitTypedAnswer = submitTypedAnswer;
  window.nextQuestion = nextQuestion;
  window.replayGame = replayGame;
  window.switchTab = switchTab;
  window.dismissIdle = dismissIdle;
  window.startRevenge = startRevenge;
})();

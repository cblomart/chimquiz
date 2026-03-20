/**
 * ChimQuiz – Quiz UI Logic (vanilla JS, no framework)
 */

// ── Global state ──────────────────────────────────────────────────────────────
let currentQuestion = null;
let gameActive = false;
let animating = false;
let totalQuestions = 15;
let nextQuestionTimer = null;
let timerInterval = null;
window.player = null;

// ── Question timer ────────────────────────────────────────────────────────────
const ANSWER_TIME_MCQ   = 15; // seconds
const ANSWER_TIME_TYPED = 25;
let questionTimerTimeout  = null;
let questionTimerInterval = null;
let questionStartedAt     = 0; // timestamp when timer started

// ── Bootstrap ─────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', async () => {
    await initPlayer();

    if (document.getElementById('start-btn')) {
        initHomePage();
    }
    if (document.getElementById('question-card')) {
        await initQuizPage();
    }
    if (document.getElementById('leaderboard-alltime')) {
        await initLeaderboardPage();
    }
});

// ── Player initialisation ─────────────────────────────────────────────────────
async function initPlayer() {
    try {
        const res = await apiFetch('/api/player/me');
        if (res.ok) {
            window.player = await res.json();
            updateNavPlayer(window.player);
        }
    } catch (_) {}
}

function updateNavPlayer(player) {
    const el = document.getElementById('player-nav-info');
    if (!el || !player) return;
    el.textContent = `${player.rankEmoji} ${player.pseudo} · ${player.totalXp} XP`;
    el.style.display = '';
}

// ── Home Page ─────────────────────────────────────────────────────────────────
function initHomePage() {
    const input    = document.getElementById('pseudo-input');
    const startBtn = document.getElementById('start-btn');
    const errorEl  = document.getElementById('pseudo-error');

    if (window.player) {
        input.value = window.player.pseudo;
    } else {
        input.placeholder = generateClientPseudo();
    }

    // Question count selector
    let selectedCount = parseInt(sessionStorage.getItem('questionCount') || '15', 10);
    const qcountBtns = document.querySelectorAll('.qcount-btn');
    const statQcount = document.getElementById('stat-qcount');

    function setCount(n) {
        selectedCount = n;
        sessionStorage.setItem('questionCount', n);
        qcountBtns.forEach(b => b.classList.toggle('qcount-btn--active', parseInt(b.dataset.count, 10) === n));
        if (statQcount) statQcount.textContent = n;
    }
    setCount(selectedCount);
    qcountBtns.forEach(b => b.addEventListener('click', () => setCount(parseInt(b.dataset.count, 10))));

    startBtn.addEventListener('click', async () => {
        const pseudo = input.value.trim() || input.placeholder;
        if (errorEl) errorEl.style.display = 'none';

        startBtn.disabled = true;
        startBtn.textContent = '⏳ Chargement...';

        try {
            if (window.player) {
                window.location.href = '/quiz';
                return;
            }
            const res  = await apiFetch('/api/player/create', 'POST', { pseudo });
            const data = await res.json();
            if (!res.ok) {
                const msg = data.error?.includes('déjà utilisé')
                    ? `🔒 Ce pseudo est déjà pris ! Choisis-en un autre.`
                    : data.error || 'Pseudo invalide.';
                showError(errorEl, msg);
                return;
            }
            window.player = data;
            window.location.href = '/quiz';
        } catch (_) {
            showError(errorEl, 'Erreur réseau. Réessaie.');
        } finally {
            startBtn.disabled = false;
            startBtn.innerHTML = '<span class="btn-icon">⚡</span>Jouer !';
        }
    });

    input.addEventListener('keydown', (e) => { if (e.key === 'Enter') startBtn.click(); });
}

function generateClientPseudo() {
    const adj  = ['Brillant', 'Curieux', 'Atomique', 'Électrique', 'Quantique', 'Cosmique', 'Ionique', 'Dynamique'];
    const elem = ['Hydro', 'Carbone', 'Néon', 'Fer', 'Or', 'Argent', 'Krypton', 'Radium'];
    return `${adj[~~(Math.random()*adj.length)]}${elem[~~(Math.random()*elem.length)]}${~~(Math.random()*90)+10}`;
}

function showError(el, msg) {
    if (!el) return;
    el.textContent = msg;
    el.style.display = 'block';
}

// ── Quiz Page ─────────────────────────────────────────────────────────────────
async function initQuizPage() {
    if (window.player) {
        const pseudoEl = document.getElementById('quiz-pseudo');
        const rankEl   = document.getElementById('quiz-rank');
        const streakEl = document.getElementById('quiz-streak');
        if (pseudoEl) pseudoEl.textContent = window.player.pseudo;
        if (rankEl)   rankEl.textContent   = `${window.player.rankEmoji} ${window.player.rankName}`;
        if (streakEl) streakEl.textContent = window.player.currentStreak;
    }

    const questionCount = parseInt(sessionStorage.getItem('questionCount') || '15', 10);
    const startRes = await apiFetch('/api/quiz/start', 'POST', { questionCount });
    if (!startRes.ok) {
        if (startRes.status === 401) { window.location.href = '/'; return; }
        console.error('Failed to start quiz');
        return;
    }

    // Persistent Enter key handler for typed questions
    const typedInput = document.getElementById('typed-answer');
    if (typedInput) {
        typedInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') submitTypedAnswer();
        });
    }

    gameActive = true;
    initIdleDetection();
    await loadNextQuestion();
}

async function loadNextQuestion() {
    hideInfoCard();
    const res = await apiFetch('/api/quiz/question');
    if (!res.ok) return;
    currentQuestion = await res.json();
    totalQuestions  = currentQuestion.totalQuestions;
    renderQuestion(currentQuestion);
}

function setupTypedInput() {
    const typedInput = document.getElementById('typed-answer');
    const submitBtn  = document.getElementById('submit-typed');
    if (typedInput) {
        typedInput.value    = '';
        typedInput.disabled = false;
        typedInput.className = 'typed-answer-input';
        setTimeout(() => typedInput.focus(), 350);
    }
    if (submitBtn) submitBtn.disabled = false;
}

function setupMCQButtons(choices) {
    const letters = ['A', 'B', 'C', 'D'];
    for (let i = 0; i < 4; i++) {
        const btn = document.getElementById(`choice-${i}`);
        if (!btn) continue;
        btn.className = 'choice-btn';
        btn.disabled  = false;
        btn.dataset.value = choices[i] || '';
        const textEl   = btn.querySelector('.choice-text');
        const letterEl = btn.querySelector('.choice-letter');
        if (textEl)   textEl.textContent   = choices[i] || '';
        if (letterEl) letterEl.textContent = letters[i];
    }
}

function renderQuestion(q) {
    const isTyped = q.type === 'SymbolToNameTyped';

    const promptEl    = document.getElementById('question-prompt');
    const displayEl   = document.getElementById('display-value');
    const counterEl   = document.getElementById('question-counter');
    const progressBar = document.getElementById('progress-bar');
    const xpDisplay   = document.getElementById('xp-display');
    const multEl      = document.getElementById('combo-multiplier');
    const choicesGrid = document.getElementById('choices-grid');
    const typedArea   = document.getElementById('typed-input-area');

    const revengeBadge = document.getElementById('revenge-badge');
    if (revengeBadge) revengeBadge.style.display = q.isRevenge ? 'inline-block' : 'none';

    if (promptEl)    promptEl.textContent    = q.prompt;
    if (displayEl)   displayEl.textContent   = q.displayValue;
    if (counterEl)   counterEl.textContent   = q.isRevenge
        ? `Revanche ${q.questionNumber} / ${q.totalQuestions}`
        : `Question ${q.questionNumber} / ${q.totalQuestions}`;
    if (progressBar) progressBar.style.width = `${((q.questionNumber - 1) / q.totalQuestions) * 100}%`;
    if (xpDisplay)   xpDisplay.textContent   = `${q.totalXp} XP`;
    if (multEl)      multEl.textContent      = q.comboMultiplier;

    updateComboDisplay(q.comboCount);

    if (choicesGrid) choicesGrid.style.display = isTyped ? 'none' : 'grid';
    if (typedArea)   typedArea.style.display   = isTyped ? 'flex' : 'none';

    if (isTyped) {
        setupTypedInput();
    } else {
        setupMCQButtons(q.choices);
    }

    // Animate card in
    const card = document.getElementById('question-card');
    if (card) {
        card.style.animation = 'none';
        void card.offsetHeight;
        card.style.animation = 'fadeIn 0.35s ease forwards';
    }

    // Start question countdown
    startQuestionTimer(isTyped ? ANSWER_TIME_TYPED : ANSWER_TIME_MCQ);
}


// ── Answer submission ─────────────────────────────────────────────────────────

/** MCQ button clicked */
async function selectAnswer(answer) {
    if (animating || !gameActive) return;
    animating = true;
    clearQuestionTimer();

    const elapsed = Date.now() - questionStartedAt;
    const isSpeedBonus = elapsed < (ANSWER_TIME_MCQ * 1000 / 3);

    for (let i = 0; i < 4; i++) {
        const btn = document.getElementById(`choice-${i}`);
        if (btn) btn.disabled = true;
    }

    const res = await apiFetch('/api/quiz/answer', 'POST', { answer });
    if (!res.ok) { animating = false; return; }

    const result = await res.json();
    if (isSpeedBonus && result.isCorrect) animateXpGain(2, '⚡ Rapide !');
    handleAnswerResult(result, answer);
}

/** Typed input submit */
async function submitTypedAnswer() {
    if (animating || !gameActive) return;

    const typedInput = document.getElementById('typed-answer');
    const submitBtn  = document.getElementById('submit-typed');
    const answer     = typedInput?.value?.trim() ?? '';

    if (!answer) {
        typedInput?.classList.add('shake');
        setTimeout(() => typedInput?.classList.remove('shake'), 500);
        return;
    }

    animating = true;
    clearQuestionTimer();
    const elapsed = Date.now() - questionStartedAt;
    const isSpeedBonus = elapsed < (ANSWER_TIME_TYPED * 1000 / 3);

    if (typedInput) typedInput.disabled = true;
    if (submitBtn)  submitBtn.disabled  = true;

    const res = await apiFetch('/api/quiz/answer', 'POST', { answer });
    if (!res.ok) { animating = false; return; }

    const result = await res.json();

    if (isSpeedBonus && result.isCorrect) animateXpGain(2, '⚡ Rapide !');

    // Show typed feedback on the input
    if (typedInput) {
        typedInput.classList.add(result.isCorrect ? 'typed-correct' : 'typed-incorrect');
    }

    handleAnswerResult(result, answer);
}

/** Called when question timer reaches zero */
async function onQuestionTimeout() {
    if (animating || !gameActive) return;
    animating = true;
    clearQuestionTimer();

    // Disable all inputs
    for (let i = 0; i < 4; i++) {
        const btn = document.getElementById(`choice-${i}`);
        if (btn) btn.disabled = true;
    }
    const typedInput = document.getElementById('typed-answer');
    const submitBtn  = document.getElementById('submit-typed');
    if (typedInput) typedInput.disabled = true;
    if (submitBtn)  submitBtn.disabled  = true;

    // Submit empty = wrong answer
    const res = await apiFetch('/api/quiz/answer', 'POST', { answer: '' });
    if (!res.ok) { animating = false; return; }

    const result = await res.json();
    // Override verdict display to show timeout message
    result._isTimeout = true;
    handleAnswerResult(result, '');
}

/** Process result: show feedback, then show info card */
function handleAnswerResult(result, givenAnswer) {
    result._givenAnswer = givenAnswer; // store for spelling correction display
    window._lastResult = result;
    // Highlight MCQ buttons
    if (!result.isTyped) {
        for (let i = 0; i < 4; i++) {
            const btn = document.getElementById(`choice-${i}`);
            if (!btn) continue;
            if (btn.dataset.value === result.correctAnswer) {
                btn.classList.add('correct');
            } else if (btn.dataset.value === givenAnswer && !result.isCorrect) {
                btn.classList.add('incorrect');
            }
        }
    }

    // Update XP
    const xpDisplay = document.getElementById('xp-display');
    if (xpDisplay) xpDisplay.textContent = `${result.totalXp} XP`;

    const sessionXpEl = document.getElementById('quiz-session-xp');
    if (sessionXpEl) sessionXpEl.textContent = result.totalXp;

    if (result.isCorrect) animateXpGain(result.xpEarned, result.comboMessage);

    updateComboDisplay(result.comboCount);

    // Progress bar
    const progressBar = document.getElementById('progress-bar');
    if (progressBar) progressBar.style.width = `${(result.questionIndex + 1) / result.totalQuestions * 100}%`;

    // Show element info card (always, after every answer) — replaces question in-place
    if (result.isRevengeStart) {
        setTimeout(() => showRevengeOverlay(result), 400);
    } else {
        setTimeout(() => showInfoCard(result), 400);
    }
}

// ── Element Info Card ─────────────────────────────────────────────────────────

const INFO_DISPLAY_SECONDS = 12;
const BONUS_XP_THRESHOLD_MS = 6000; // stay ≥6s → show bonus XP visual
let cardShownAt = 0;

function renderVerdict(result) {
    const verdict = document.getElementById('answer-verdict');
    if (!verdict) return;
    if (result._isTimeout) {
        verdict.textContent = `⏱️ Temps écoulé ! Réponse : ${result.correctAnswer}`;
        verdict.className   = 'answer-verdict verdict-incorrect';
    } else if (result.isCorrect && result.wasFuzzyMatch) {
        verdict.textContent = '✅ Presque parfait !';
        verdict.className   = 'answer-verdict verdict-fuzzy';
    } else if (result.isCorrect) {
        verdict.textContent = result.comboMessage || '✅ Correct !';
        verdict.className   = 'answer-verdict verdict-correct';
    } else {
        verdict.textContent = `❌ Réponse : ${result.correctAnswer}`;
        verdict.className   = 'answer-verdict verdict-incorrect';
    }
}

function renderSpellingCorrection(result) {
    const block   = document.getElementById('spelling-correction');
    const given   = document.getElementById('spelling-given');
    const correct = document.getElementById('spelling-correct');
    const show    = result.isCorrect && result.wasFuzzyMatch && result._givenAnswer;
    if (show) {
        if (given)   given.textContent   = result._givenAnswer;
        if (correct) correct.textContent = result.correctAnswer;
    }
    if (block) block.style.display = show ? 'flex' : 'none';
}

function setElementIdentity(result) {
    const symbol  = document.getElementById('info-symbol');
    const atomNum = document.getElementById('info-atomic-number');
    const name    = document.getElementById('info-name');
    if (symbol)  symbol.textContent  = result.elementSymbol;
    if (atomNum) atomNum.textContent = `#${currentQuestion?.elementId ?? ''}`;
    if (name)    name.textContent    = result.elementName;
}

function renderFunFact(result) {
    const factBlock = document.getElementById('info-fact-block');
    const factTxt   = document.getElementById('info-fact-text');
    if (factTxt && result.funFact) factTxt.textContent = result.funFact;
    if (factBlock) factBlock.style.display = result.funFact ? 'block' : 'none';
}

function renderElementDetails(result) {
    const useText = document.getElementById('info-use-text');
    const where   = document.getElementById('info-where-text');
    if (useText) useText.textContent = result.commonUse   || '';
    if (where)   where.textContent   = result.whereToFind || '';
}

function showInfoCard(result) {
    const card = document.getElementById('element-info-card');
    if (!card) return;

    ['question-area', 'choices-grid', 'typed-input-area', 'question-timer-area'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.style.visibility = 'hidden';
    });

    setElementIdentity(result);
    renderVerdict(result);
    renderSpellingCorrection(result);
    renderFunFact(result);
    renderElementDetails(result);

    if (result.isTyped && result.isCorrect && !result.wasFuzzyMatch) {
        animateXpGain(3, '✍️ Parfait !');
    }

    cardShownAt = Date.now();
    card.style.display = 'flex';
    card.style.animation = 'none';
    void card.offsetHeight;
    card.style.animation = 'slideUp 0.35s ease forwards';

    startInfoTimer(INFO_DISPLAY_SECONDS);
    scheduleNextQuestion(result, INFO_DISPLAY_SECONDS * 1000);
}

function hideInfoCard() {
    const card = document.getElementById('element-info-card');
    if (card) card.style.display = 'none';
    // Restore question area visibility
    ['question-area', 'choices-grid', 'typed-input-area', 'question-timer-area'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.style.visibility = '';
    });
    clearTimers();
}

function startInfoTimer(seconds) {
    clearInterval(timerInterval); // only clear the countdown interval, not the auto-advance timer
    timerInterval = null;
    let remaining = seconds;
    const fill    = document.getElementById('info-timer-fill');
    const label   = document.getElementById('info-timer-label');
    const nextLbl = document.getElementById('info-next-label');

    if (fill) {
        fill.style.transition = 'none';
        fill.style.width = '100%';
        void fill.offsetHeight;
        fill.style.transition = `width ${seconds}s linear`;
        fill.style.width = '0%';
    }
    if (label) label.textContent = `${seconds}s`;

    timerInterval = setInterval(() => {
        remaining--;
        if (label) label.textContent = `${Math.max(0, remaining)}s`;

        // Bonus XP visual unlock at 6s elapsed (≥ threshold)
        if (remaining === seconds - BONUS_XP_THRESHOLD_MS / 1000 && nextLbl) {
            nextLbl.textContent = '📚 J\'ai lu ! (+5 XP)';
        }
    }, 1000);
}

function scheduleNextQuestion(result, delayMs) {
    clearTimeout(nextQuestionTimer);
    nextQuestionTimer = setTimeout(async () => {
        clearTimers();
        if (result.isGameOver) {
            gameActive = false;
            await showGameOver(result);
        } else {
            await loadNextQuestion();
        }
        animating = false;
    }, delayMs);
}

/** Called by "J'ai lu !" button — skip the timer */
async function nextQuestion() {
    clearTimers();
    const result = window._lastResult;
    if (!result) return;

    // If the user stayed ≥ BONUS_XP_THRESHOLD_MS, show a visual bonus XP reward
    const elapsed = cardShownAt ? Date.now() - cardShownAt : 0;
    cardShownAt = 0; // reset before any await so double-clicks don't re-trigger
    if (elapsed >= BONUS_XP_THRESHOLD_MS) {
        animateXpGain(5, '📚 Curieux(se) !');
    }

    if (result.isGameOver) {
        gameActive = false;
        await showGameOver(result);
    } else {
        await loadNextQuestion();
    }
    animating = false;
}

function clearTimers() {
    clearTimeout(nextQuestionTimer);
    clearInterval(timerInterval);
    nextQuestionTimer = null;
    timerInterval = null;
}

function clearQuestionTimer() {
    clearTimeout(questionTimerTimeout);
    clearInterval(questionTimerInterval);
    questionTimerTimeout  = null;
    questionTimerInterval = null;
}

function startQuestionTimer(seconds) {
    clearQuestionTimer();
    questionStartedAt = Date.now();

    const fill    = document.getElementById('question-timer-fill');
    const count   = document.getElementById('question-timer-count');
    const area    = document.getElementById('question-timer-area');

    if (area)  area.className  = 'question-timer-area';
    if (count) count.textContent = seconds;
    if (fill) {
        fill.style.transition = 'none';
        fill.style.width = '100%';
        void fill.offsetHeight;
        fill.style.transition = `width ${seconds}s linear`;
        fill.style.width = '0%';
    }

    let remaining = seconds;
    questionTimerInterval = setInterval(() => {
        remaining--;
        if (count) count.textContent = Math.max(0, remaining);

        // Color urgency
        const pct = remaining / seconds;
        if (area) {
            if (pct <= 0.2)       area.className = 'question-timer-area urgent';
            else if (pct <= 0.4)  area.className = 'question-timer-area warning';
            else                  area.className = 'question-timer-area';
        }
    }, 1000);

    questionTimerTimeout = setTimeout(() => {
        clearInterval(questionTimerInterval);
        questionTimerInterval = null;
        onQuestionTimeout();
    }, seconds * 1000);
}

// Patch scheduleNextQuestion to store last result
const _origSchedule = scheduleNextQuestion;
window.scheduleNextQuestion = scheduleNextQuestion;

// ── Revenge Round ─────────────────────────────────────────────────────────────

function showRevengeOverlay(result) {
    clearTimers();
    hideInfoCard();

    // Show the last answer info briefly first, then transition
    showInfoCard(result);

    // After info card times out, show revenge overlay
    clearTimeout(nextQuestionTimer);
    nextQuestionTimer = setTimeout(() => {
        hideInfoCard();
        const overlay    = document.getElementById('revenge-overlay');
        const countEl    = document.getElementById('revenge-count');
        const listEl     = document.getElementById('revenge-elements');
        if (!overlay) return;

        // Populate wrong elements list (already loaded in next question)
        if (countEl) countEl.textContent = result.totalQuestions || '?';
        if (listEl)  listEl.innerHTML = '';

        overlay.style.display = 'flex';
        overlay.style.animation = 'none';
        void overlay.offsetHeight;
        overlay.style.animation = 'fadeIn 0.4s ease forwards';
    }, 13000); // after info card auto-advances
}

async function startRevenge() {
    const overlay = document.getElementById('revenge-overlay');
    if (overlay) overlay.style.display = 'none';
    animating = false;
    gameActive = true;
    await loadNextQuestion();
}

// ── Game Over ─────────────────────────────────────────────────────────────────
async function showGameOver(result) {
    const overlay  = document.getElementById('game-over');
    if (!overlay) return;

    const correctEl = document.getElementById('result-correct');
    const xpEl      = document.getElementById('result-xp');
    const comboEl   = document.getElementById('result-combo');

    if (correctEl) correctEl.textContent = `${result.correctCount}/${result.totalQuestions}`;
    if (comboEl)   comboEl.textContent   = `x${result.maxCombo}`;

    if (xpEl) {
        xpEl.textContent = '0';
        animateCounter(xpEl, 0, result.totalXp, 1000);
    }

    // Choose emoji based on score
    const pct = result.correctCount / result.totalQuestions;
    const emojiEl = document.getElementById('game-over-emoji');
    if (emojiEl) {
        emojiEl.textContent = pct >= 0.9 ? '🌟' : pct >= 0.7 ? '🏆' : pct >= 0.5 ? '🎯' : '🧪';
    }

    try {
        const playerRes = await apiFetch('/api/player/me');
        if (playerRes.ok) {
            const player = await playerRes.json();
            window.player = player;
            updateNavPlayer(player);
            renderRankSummary(player);
        }
    } catch (_) {}

    overlay.style.display = 'flex';
}

function renderRankSummary(player) {
    const rankDisplay  = document.getElementById('rank-display');
    const currentLabel = document.getElementById('rank-current-label');
    const nextLabel    = document.getElementById('rank-next-label');
    const progressFill = document.getElementById('rank-progress-fill');
    const rankNameDisp = document.getElementById('rank-name-display');

    if (rankDisplay)  rankDisplay.textContent  = player.rankEmoji;
    if (rankNameDisp) rankNameDisp.textContent = `${player.rankName} — ${player.totalXp} XP total`;
    if (currentLabel) currentLabel.textContent = `${player.xpForCurrentRank} XP`;
    if (nextLabel)    nextLabel.textContent    = player.xpForNextRank === 2147483647 ? 'Max' : `${player.xpForNextRank} XP`;

    setTimeout(() => {
        if (progressFill) progressFill.style.width = `${player.rankProgressPercent}%`;
    }, 300);
}

async function replayGame() {
    const overlay = document.getElementById('game-over');
    if (overlay) overlay.style.display = 'none';
    clearTimers();
    animating = false;
    gameActive = false;
    window._lastResult = null;
    hideInfoCard();
    await initQuizPage();
}

// ── Leaderboard Page ──────────────────────────────────────────────────────────
let currentTab = 'alltime';
let leaderboardData = { alltime: null, weekly: null };

async function initLeaderboardPage() {
    await Promise.all([loadLeaderboardTab('alltime'), loadLeaderboardTab('weekly')]);
}

async function loadLeaderboardTab(tab) {
    const endpoint = tab === 'alltime' ? '/api/leaderboard/alltime' : '/api/leaderboard/weekly';
    try {
        const res  = await apiFetch(endpoint);
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
    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('tab-active'));
    const activeBtn = document.getElementById(`tab-${tab}`);
    if (activeBtn) activeBtn.classList.add('tab-active');
    const alltime = document.getElementById('leaderboard-alltime');
    const weekly  = document.getElementById('leaderboard-weekly');
    if (alltime) alltime.style.display = tab === 'alltime' ? 'block' : 'none';
    if (weekly)  weekly.style.display  = tab === 'weekly'  ? 'block' : 'none';
    if (leaderboardData[tab]) renderLeaderboard(tab, leaderboardData[tab]);
}

function renderLeaderboard(tab, scores) {
    const container = document.getElementById(`leaderboard-${tab}`);
    if (!container) return;

    if (!scores || scores.length === 0) {
        container.innerHTML = `<div class="leaderboard-empty"><div class="leaderboard-empty-icon">🧪</div><p>Aucune partie enregistrée.<br/>Sois le premier à jouer !</p></div>`;
        return;
    }

    const myPseudo    = window.player?.pseudo ?? '';
    const rankIcons   = ['🥇', '🥈', '🥉'];
    const rankClasses = ['gold', 'silver', 'bronze'];
    const isAlltime   = tab === 'alltime';

    const rows = scores.map((s, i) => {
        const isMe     = s.pseudo === myPseudo;
        const rankCell = i < 3
            ? `<td class="rank-cell ${rankClasses[i]}">${rankIcons[i]}</td>`
            : `<td class="rank-cell">${i + 1}</td>`;

        return isAlltime
            ? `<tr${isMe ? ' class="is-me"' : ''}>${rankCell}
                <td class="pseudo-cell">${escHtml(s.rankEmoji)} <strong>${escHtml(s.pseudo)}</strong></td>
                <td class="score-cell">${s.score} XP</td>
                <td>${escHtml(s.rankName)}</td>
                <td class="streak-cell">🔥 ${s.currentStreak}</td></tr>`
            : `<tr${isMe ? ' class="is-me"' : ''}>${rankCell}
                <td class="pseudo-cell">${escHtml(s.rankEmoji)} <strong>${escHtml(s.pseudo)}</strong></td>
                <td class="score-cell">${s.score} XP</td>
                <td>${s.correctAnswers}/15 ✅</td>
                <td>Combo x${s.maxCombo}</td></tr>`;
    }).join('');

    const headers = isAlltime
        ? '<th>#</th><th>Joueur</th><th>Meilleur score</th><th>Rang</th><th>Série</th>'
        : '<th>#</th><th>Joueur</th><th>Score</th><th>Précision</th><th>Combo</th>';

    container.innerHTML = `<table class="leaderboard-table glass-card"><thead><tr>${headers}</tr></thead><tbody>${rows}</tbody></table>`;

    const myPosition     = document.getElementById('my-position');
    const myPositionText = document.getElementById('my-position-text');
    if (myPseudo && myPosition && myPositionText) {
        const idx = scores.findIndex(s => s.pseudo === myPseudo);
        if (idx >= 0) {
            myPositionText.textContent = `#${idx + 1} – ${scores[idx].score} XP`;
            myPosition.style.display = 'flex';
        } else {
            myPosition.style.display = 'none';
        }
    }
}

// ── UI Helpers ────────────────────────────────────────────────────────────────
function updateComboDisplay(combo) {
    const el = document.getElementById('combo-display');
    if (!el) return;
    if (combo >= 3) {
        el.style.display = '';
        el.textContent = combo >= 10 ? `🌟 COMBO x${combo}`
                       : combo >= 8  ? `💥 COMBO x${combo}`
                       : combo >= 5  ? `⚡ Combo x${combo}`
                                     : `🔥 Combo x${combo}`;
        el.classList.add('on-fire');
    } else {
        el.style.display = 'none';
        el.classList.remove('on-fire');
    }
}

function animateXpGain(amount, comboMessage) {
    const container = document.getElementById('xp-float-container');
    if (!container) return;
    const xpEl = document.getElementById('xp-display');
    const rect  = xpEl ? xpEl.getBoundingClientRect() : { left: window.innerWidth / 2, top: window.innerHeight / 2 };
    const el    = document.createElement('div');
    el.className  = 'xp-float';
    el.textContent = comboMessage ? `+${amount} XP  ${comboMessage}` : `+${amount} XP`;
    el.style.left = `${rect.left + rect.width / 2}px`;
    el.style.top  = `${rect.top}px`;
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

// ── API Fetch Helper ──────────────────────────────────────────────────────────
async function apiFetch(url, method = 'GET', body = null) {
    const headers = { 'Content-Type': 'application/json' };
    if (method !== 'GET') {
        const tokenMeta = document.querySelector('meta[name="__RequestVerificationToken"]');
        if (tokenMeta) headers['X-CSRF-TOKEN'] = tokenMeta.content;
    }
    const options = { method, headers, credentials: 'same-origin' };
    if (body !== null) options.body = JSON.stringify(body);
    return fetch(url, options);
}

function escHtml(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

// ── Idle detection ────────────────────────────────────────────────────────────
const IDLE_TIMEOUT_MS = 2 * 60 * 1000; // 2 minutes
let idleTimer = null;

function resetIdleTimer() {
    if (!gameActive) return;
    clearTimeout(idleTimer);
    idleTimer = setTimeout(showIdleModal, IDLE_TIMEOUT_MS);
}

function showIdleModal() {
    if (!gameActive) return;
    // Pause question timer while modal is shown
    clearQuestionTimer();
    clearTimeout(nextQuestionTimer);
    nextQuestionTimer = null;
    document.getElementById('idle-modal').style.display = 'flex';
}

function dismissIdle() {
    document.getElementById('idle-modal').style.display = 'none';
    resetIdleTimer();
    // If a question was active (no info card showing), restart its timer
    const infoCard = document.getElementById('element-info-card');
    const infoVisible = infoCard && infoCard.style.display !== 'none';
    if (!infoVisible && currentQuestion) {
        const isTyped = currentQuestion.type === 'SymbolToNameTyped';
        startQuestionTimer(isTyped ? ANSWER_TIME_TYPED : ANSWER_TIME_MCQ);
    }
}

function initIdleDetection() {
    ['click', 'keydown', 'touchstart', 'mousemove'].forEach(evt =>
        document.addEventListener(evt, resetIdleTimer, { passive: true })
    );
    resetIdleTimer();
}

// ── Globals (called from HTML onclick / other pages) ──────────────────────────
window.selectAnswer      = selectAnswer;
window.submitTypedAnswer = submitTypedAnswer;
window.nextQuestion      = nextQuestion;
window.replayGame        = replayGame;
window.switchTab         = switchTab;
window.dismissIdle       = dismissIdle;

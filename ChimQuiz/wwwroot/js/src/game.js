import { state } from './state.js';
import { apiFetch } from './api.js';
import { updateComboDisplay, animateXpGain, animateCounter } from './ui.js';
import {
    clearTimers, clearQuestionTimer,
    startInfoTimer, startQuestionTimer,
    ANSWER_TIME_MCQ, ANSWER_TIME_TYPED, INFO_DISPLAY_SECONDS, BONUS_XP_THRESHOLD_MS,
} from './timers.js';
import { initIdleDetection } from './idle.js';
import { updateNavPlayer } from './player.js';

// ── Quiz Page ─────────────────────────────────────────────────────────────────
export async function initQuizPage() {
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

    state.gameActive = true;

    // Register the onQuestionTimeout callback so idle.js can use it
    state.onQuestionTimeout = onQuestionTimeout;

    initIdleDetection();
    await loadNextQuestion();
}

export async function loadNextQuestion() {
    hideInfoCard();
    const res = await apiFetch('/api/quiz/question');
    if (!res.ok) return;
    state.currentQuestion = await res.json();
    renderQuestion(state.currentQuestion);
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

function renderQuestionHeader(q) {
    const revengeBadge = document.getElementById('revenge-badge');
    if (revengeBadge) revengeBadge.style.display = q.isRevenge ? 'inline-block' : 'none';

    const counterEl = document.getElementById('question-counter');
    if (counterEl) counterEl.textContent = q.isRevenge
        ? `Revanche ${q.questionNumber} / ${q.totalQuestions}`
        : `Question ${q.questionNumber} / ${q.totalQuestions}`;

    const progressBar = document.getElementById('progress-bar');
    if (progressBar) progressBar.style.width = `${((q.questionNumber - 1) / q.totalQuestions) * 100}%`;

    const xpDisplay = document.getElementById('xp-display');
    if (xpDisplay) xpDisplay.textContent = `${q.totalXp} XP`;

    const multEl = document.getElementById('combo-multiplier');
    if (multEl) multEl.textContent = q.comboMultiplier;

    updateComboDisplay(q.comboCount);
}

function renderQuestion(q) {
    const isTyped = q.type === 'SymbolToNameTyped';

    const promptEl    = document.getElementById('question-prompt');
    const displayEl   = document.getElementById('display-value');
    const choicesGrid = document.getElementById('choices-grid');
    const typedArea   = document.getElementById('typed-input-area');

    renderQuestionHeader(q);

    if (promptEl)  promptEl.textContent  = q.prompt;
    if (displayEl) displayEl.textContent = q.displayValue;

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
    startQuestionTimer(isTyped ? ANSWER_TIME_TYPED : ANSWER_TIME_MCQ, onQuestionTimeout);
}

// ── Answer submission ─────────────────────────────────────────────────────────

/** MCQ button clicked */
export async function selectAnswer(answer) {
    if (state.animating || !state.gameActive) return;
    state.animating = true;
    clearQuestionTimer();

    const elapsed = Date.now() - state.questionStartedAt;
    const isSpeedBonus = elapsed < (ANSWER_TIME_MCQ * 1000 / 3);

    for (let i = 0; i < 4; i++) {
        const btn = document.getElementById(`choice-${i}`);
        if (btn) btn.disabled = true;
    }

    const res = await apiFetch('/api/quiz/answer', 'POST', { answer });
    if (!res.ok) { state.animating = false; return; }

    const result = await res.json();
    if (isSpeedBonus && result.isCorrect) animateXpGain(2, '⚡ Rapide !');
    handleAnswerResult(result, answer);
}

/** Typed input submit */
export async function submitTypedAnswer() {
    if (state.animating || !state.gameActive) return;

    const typedInput = document.getElementById('typed-answer');
    const submitBtn  = document.getElementById('submit-typed');
    const answer     = typedInput?.value?.trim() ?? '';

    if (!answer) {
        typedInput?.classList.add('shake');
        setTimeout(() => typedInput?.classList.remove('shake'), 500);
        return;
    }

    state.animating = true;
    clearQuestionTimer();
    const elapsed = Date.now() - state.questionStartedAt;
    const isSpeedBonus = elapsed < (ANSWER_TIME_TYPED * 1000 / 3);

    if (typedInput) typedInput.disabled = true;
    if (submitBtn)  submitBtn.disabled  = true;

    const res = await apiFetch('/api/quiz/answer', 'POST', { answer });
    if (!res.ok) { state.animating = false; return; }

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
    if (state.animating || !state.gameActive) return;
    state.animating = true;
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
    if (!res.ok) { state.animating = false; return; }

    const result = await res.json();
    // Override verdict display to show timeout message
    result._isTimeout = true;
    handleAnswerResult(result, '');
}

/** Process result: show feedback, then show info card */
function handleAnswerResult(result, givenAnswer) {
    result._givenAnswer = givenAnswer; // store for spelling correction display
    state.lastResult = result;
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
    if (atomNum) atomNum.textContent = `#${state.currentQuestion?.elementId ?? ''}`;
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

    state.cardShownAt = Date.now();
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

/** Called by "J'ai lu !" button — skip the timer */
export async function nextQuestion() {
    clearTimers();
    const result = state.lastResult;
    if (!result) return;

    // If the user stayed ≥ BONUS_XP_THRESHOLD_MS, show a visual bonus XP reward
    const elapsed = state.cardShownAt ? Date.now() - state.cardShownAt : 0;
    state.cardShownAt = 0; // reset before any await so double-clicks don't re-trigger
    if (elapsed >= BONUS_XP_THRESHOLD_MS) {
        animateXpGain(5, '📚 Curieux(se) !');
    }

    if (result.isGameOver) {
        state.gameActive = false;
        await showGameOver(result);
    } else {
        await loadNextQuestion();
    }
    state.animating = false;
}

// ── Revenge Round ─────────────────────────────────────────────────────────────

export function showRevengeOverlay(result) {
    clearTimers();
    hideInfoCard();

    // Show the last answer info briefly first, then transition
    showInfoCard(result);

    // After info card times out, show revenge overlay
    clearTimeout(state.nextQuestionTimer);
    state.nextQuestionTimer = setTimeout(() => {
        hideInfoCard();
        const overlay = document.getElementById('revenge-overlay');
        const countEl = document.getElementById('revenge-count');
        const listEl  = document.getElementById('revenge-elements');
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

export async function startRevenge() {
    const overlay = document.getElementById('revenge-overlay');
    if (overlay) overlay.style.display = 'none';
    state.animating = false;
    state.gameActive = true;
    await loadNextQuestion();
}

// ── Game Over ─────────────────────────────────────────────────────────────────
export async function showGameOver(result) {
    const overlay = document.getElementById('game-over');
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

export async function replayGame() {
    const overlay = document.getElementById('game-over');
    if (overlay) overlay.style.display = 'none';
    clearTimers();
    state.animating = false;
    state.gameActive = false;
    state.lastResult = null;
    hideInfoCard();
    await initQuizPage();
}

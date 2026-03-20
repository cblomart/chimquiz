import { state } from './state.js';
import { clearQuestionTimer, startQuestionTimer, clearTimers, ANSWER_TIME_MCQ, ANSWER_TIME_TYPED } from './timers.js';

// ── Idle detection ────────────────────────────────────────────────────────────
const IDLE_TIMEOUT_MS = 2 * 60 * 1000; // 2 minutes
let idleTimer = null;

export function resetIdleTimer() {
    if (!state.gameActive) return;
    clearTimeout(idleTimer);
    idleTimer = setTimeout(showIdleModal, IDLE_TIMEOUT_MS);
}

function showIdleModal() {
    if (!state.gameActive) return;
    // Pause question timer while modal is shown
    clearQuestionTimer();
    clearTimeout(state.nextQuestionTimer);
    state.nextQuestionTimer = null;
    document.getElementById('idle-modal').style.display = 'flex';
}

export function dismissIdle() {
    document.getElementById('idle-modal').style.display = 'none';
    resetIdleTimer();
    // If a question was active (no info card showing), restart its timer
    const infoCard = document.getElementById('element-info-card');
    const infoVisible = infoCard && infoCard.style.display !== 'none';
    if (!infoVisible && state.currentQuestion) {
        const isTyped = state.currentQuestion.type === 'SymbolToNameTyped';
        startQuestionTimer(
            isTyped ? ANSWER_TIME_TYPED : ANSWER_TIME_MCQ,
            state.onQuestionTimeout
        );
    }
}

export function initIdleDetection() {
    ['click', 'keydown', 'touchstart', 'mousemove'].forEach(evt =>
        document.addEventListener(evt, resetIdleTimer, { passive: true })
    );
    resetIdleTimer();
}

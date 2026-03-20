import { state } from './state.js';

// ── Constants ─────────────────────────────────────────────────────────────────
export const ANSWER_TIME_MCQ        = 15; // seconds
export const ANSWER_TIME_TYPED      = 25;
export const INFO_DISPLAY_SECONDS   = 12;
export const BONUS_XP_THRESHOLD_MS  = 6000; // stay ≥6s → show bonus XP visual

// ── Info / next-question timers ───────────────────────────────────────────────
export function clearTimers() {
    clearTimeout(state.nextQuestionTimer);
    clearInterval(state.timerInterval);
    clearTimeout(state.infoCardTimer);
    state.nextQuestionTimer = null;
    state.timerInterval = null;
    state.infoCardTimer = null;
}

// ── Question timer ────────────────────────────────────────────────────────────
export function clearQuestionTimer() {
    clearTimeout(state.questionTimerTimeout);
    clearInterval(state.questionTimerInterval);
    state.questionTimerTimeout  = null;
    state.questionTimerInterval = null;
}

export function startInfoTimer(seconds) {
    clearInterval(state.timerInterval); // only clear the countdown interval, not the auto-advance timer
    state.timerInterval = null;
    let remaining = seconds;
    const fill    = document.getElementById('info-timer-fill');
    const label   = document.getElementById('info-timer-label');

    if (fill) {
        fill.style.transition = 'none';
        fill.style.width = '100%';
        void fill.offsetHeight;
        fill.style.transition = `width ${seconds}s linear`;
        fill.style.width = '0%';
    }
    if (label) label.textContent = `${seconds}s`;

    state.timerInterval = setInterval(() => {
        remaining--;
        if (label) label.textContent = `${Math.max(0, remaining)}s`;
    }, 1000);
}

export function startQuestionTimer(seconds, onTimeout) {
    clearQuestionTimer();
    state.questionStartedAt = Date.now();

    const fill  = document.getElementById('question-timer-fill');
    const count = document.getElementById('question-timer-count');
    const area  = document.getElementById('question-timer-area');

    if (area)  area.className    = 'question-timer-area';
    if (count) count.textContent = seconds;
    if (fill) {
        fill.style.transition = 'none';
        fill.style.width = '100%';
        void fill.offsetHeight;
        fill.style.transition = `width ${seconds}s linear`;
        fill.style.width = '0%';
    }

    let remaining = seconds;
    state.questionTimerInterval = setInterval(() => {
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

    state.questionTimerTimeout = setTimeout(() => {
        clearInterval(state.questionTimerInterval);
        state.questionTimerInterval = null;
        if (onTimeout) onTimeout();
    }, seconds * 1000);
}

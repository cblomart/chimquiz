// ── Global state ──────────────────────────────────────────────────────────────
export const state = {
    currentQuestion: null,
    gameActive: false,
    animating: false,
    lastResult: null,           // replaces window._lastResult

    cardShownAt: 0,
    infoCardTimer: null,

    nextQuestionTimer: null,
    timerInterval: null,

    questionTimerTimeout: null,
    questionTimerInterval: null,
    questionStartedAt: 0,       // timestamp when timer started

    onQuestionTimeout: null,    // callback set by game.js to break circular dep with idle.js
    bonusReadyTimer: null,
};

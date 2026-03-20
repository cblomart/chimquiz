/**
 * ChimQuiz – Entry point
 * Imports all modules and bootstraps the application.
 */

import { initPlayer } from './player.js';
import { initHomePage } from './home.js';
import {
    initQuizPage,
    loadNextQuestion,
    selectAnswer,
    submitTypedAnswer,
    nextQuestion,
    showRevengeOverlay,
    startRevenge,
    showGameOver,
    replayGame,
} from './game.js';
import { initLeaderboardPage, switchTab } from './leaderboard.js';
import { dismissIdle } from './idle.js';

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

// ── Globals (called from HTML onclick / other pages) ──────────────────────────
window.selectAnswer      = selectAnswer;
window.submitTypedAnswer = submitTypedAnswer;
window.nextQuestion      = nextQuestion;
window.replayGame        = replayGame;
window.switchTab         = switchTab;
window.dismissIdle       = dismissIdle;
window.startRevenge      = startRevenge;

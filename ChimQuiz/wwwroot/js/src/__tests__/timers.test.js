import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { state } from '../state.js';
import { clearTimers, clearQuestionTimer, BONUS_XP_THRESHOLD_MS } from '../timers.js';

// ── Constants ─────────────────────────────────────────────────────────────────

describe('constants', () => {
    it('BONUS_XP_THRESHOLD_MS is 6000ms', () => {
        expect(BONUS_XP_THRESHOLD_MS).toBe(6000);
    });
});

// ── clearTimers ───────────────────────────────────────────────────────────────

describe('clearTimers', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        state.nextQuestionTimer = null;
        state.timerInterval     = null;
        state.infoCardTimer     = null;
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('is safe to call when all timers are already null', () => {
        expect(() => clearTimers()).not.toThrow();
    });

    // infoCardTimer — key for the "+5 XP J'ai lu" bonus fix

    it('cancels a pending infoCardTimer so its callback never fires', () => {
        const spy = vi.fn();
        state.infoCardTimer = setTimeout(spy, 400);
        clearTimers();
        vi.runAllTimers();
        expect(spy).not.toHaveBeenCalled();
    });

    it('sets infoCardTimer to null', () => {
        state.infoCardTimer = setTimeout(() => {}, 400);
        clearTimers();
        expect(state.infoCardTimer).toBeNull();
    });

    // nextQuestionTimer

    it('cancels a pending nextQuestionTimer so its callback never fires', () => {
        const spy = vi.fn();
        state.nextQuestionTimer = setTimeout(spy, 10_000);
        clearTimers();
        vi.runAllTimers();
        expect(spy).not.toHaveBeenCalled();
    });

    it('sets nextQuestionTimer to null', () => {
        state.nextQuestionTimer = setTimeout(() => {}, 10_000);
        clearTimers();
        expect(state.nextQuestionTimer).toBeNull();
    });

    // timerInterval

    it('stops a running timerInterval so it never fires again', () => {
        const spy = vi.fn();
        state.timerInterval = setInterval(spy, 1000);
        clearTimers();
        vi.runAllTimers();
        expect(spy).not.toHaveBeenCalled();
    });

    it('sets timerInterval to null', () => {
        state.timerInterval = setInterval(() => {}, 1000);
        clearTimers();
        expect(state.timerInterval).toBeNull();
    });
});

// ── clearQuestionTimer ────────────────────────────────────────────────────────

describe('clearQuestionTimer', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        state.questionTimerTimeout  = null;
        state.questionTimerInterval = null;
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('is safe to call when timers are already null', () => {
        expect(() => clearQuestionTimer()).not.toThrow();
    });

    it('cancels a pending questionTimerTimeout', () => {
        const spy = vi.fn();
        state.questionTimerTimeout = setTimeout(spy, 15_000);
        clearQuestionTimer();
        vi.runAllTimers();
        expect(spy).not.toHaveBeenCalled();
    });

    it('stops a running questionTimerInterval', () => {
        const spy = vi.fn();
        state.questionTimerInterval = setInterval(spy, 1000);
        clearQuestionTimer();
        vi.runAllTimers();
        expect(spy).not.toHaveBeenCalled();
    });

    it('sets both question timer fields to null', () => {
        state.questionTimerTimeout  = setTimeout(() => {}, 15_000);
        state.questionTimerInterval = setInterval(() => {}, 1000);
        clearQuestionTimer();
        expect(state.questionTimerTimeout).toBeNull();
        expect(state.questionTimerInterval).toBeNull();
    });
});

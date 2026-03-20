import { describe, it, expect } from 'vitest';
import { state } from '../state.js';

// ── Initial shape ─────────────────────────────────────────────────────────────

describe('state — initial shape', () => {
    it('gameActive starts false', () => { expect(state.gameActive).toBe(false); });
    it('animating starts false',  () => { expect(state.animating).toBe(false); });
    it('cardShownAt starts 0',    () => { expect(state.cardShownAt).toBe(0); });
    it('infoCardTimer starts null',     () => { expect(state.infoCardTimer).toBeNull(); });
    it('nextQuestionTimer starts null', () => { expect(state.nextQuestionTimer).toBeNull(); });
    it('timerInterval starts null',     () => { expect(state.timerInterval).toBeNull(); });
    it('questionTimerTimeout starts null',  () => { expect(state.questionTimerTimeout).toBeNull(); });
    it('questionTimerInterval starts null', () => { expect(state.questionTimerInterval).toBeNull(); });
    it('questionStartedAt starts 0', () => { expect(state.questionStartedAt).toBe(0); });
});

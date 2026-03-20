// ── UI Helpers ────────────────────────────────────────────────────────────────
export function updateComboDisplay(combo) {
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

export function animateXpGain(amount, comboMessage) {
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

export function animateCounter(el, from, to, duration) {
    const start = performance.now();
    function step(now) {
        const p = Math.min((now - start) / duration, 1);
        el.textContent = Math.round(from + (to - from) * (1 - Math.pow(1 - p, 3)));
        if (p < 1) requestAnimationFrame(step);
    }
    requestAnimationFrame(step);
}

export function showError(el, msg) {
    if (!el) return;
    el.textContent = msg;
    el.style.display = 'block';
}

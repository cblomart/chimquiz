import { apiFetch } from './api.js';

// ── Player initialisation ─────────────────────────────────────────────────────
export async function initPlayer() {
    try {
        const res = await apiFetch('/api/player/me');
        if (res.ok) {
            window.player = await res.json();
            updateNavPlayer(window.player);
        }
    } catch (_) {}
}

export function updateNavPlayer(player) {
    const el = document.getElementById('player-nav-info');
    if (!el || !player) return;
    el.textContent = `${player.rankEmoji} ${player.pseudo} · ${player.totalXp} XP`;
    el.style.display = '';
}

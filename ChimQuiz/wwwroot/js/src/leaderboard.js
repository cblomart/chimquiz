import { apiFetch, escHtml } from './api.js';

// ── Leaderboard Page ──────────────────────────────────────────────────────────
let currentTab = 'alltime';
let leaderboardData = { alltime: null, weekly: null };

export async function initLeaderboardPage() {
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

export function switchTab(tab) {
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

        const scoreStr = s.score === 0 ? '—' : `${s.score} XP`;
        const scoreCls = s.score === 0 ? ' score-cell--zero' : '';
        return isAlltime
            ? `<tr${isMe ? ' class="is-me"' : ''}>${rankCell}
                <td class="pseudo-cell">${escHtml(s.rankEmoji)} <strong>${escHtml(s.pseudo)}</strong></td>
                <td class="score-cell${scoreCls}">${scoreStr}</td>
                <td class="rank-name-cell">${escHtml(s.rankName)}</td>
                <td class="streak-cell">🔥 ${s.currentStreak}</td></tr>`
            : `<tr${isMe ? ' class="is-me"' : ''}>${rankCell}
                <td class="pseudo-cell">${escHtml(s.rankEmoji)} <strong>${escHtml(s.pseudo)}</strong></td>
                <td class="score-cell${scoreCls}">${scoreStr}</td>
                <td>${s.correctAnswers}/15 ✅</td>
                <td>Combo x${s.maxCombo}</td></tr>`;
    }).join('');

    const headers = isAlltime
        ? '<th>#</th><th>Joueur</th><th>XP total</th><th>Rang</th><th class="streak-header"><span>Série</span></th>'
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

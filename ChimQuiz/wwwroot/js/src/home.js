import { apiFetch } from './api.js';
import { showError } from './ui.js';

// ── Home Page ─────────────────────────────────────────────────────────────────
export function initHomePage() {
    const input    = document.getElementById('pseudo-input');
    const startBtn = document.getElementById('start-btn');
    const errorEl  = document.getElementById('pseudo-error');

    if (window.player) {
        input.value = window.player.pseudo;
    } else {
        input.placeholder = generateClientPseudo();
    }

    // Question count selector
    let selectedCount = parseInt(sessionStorage.getItem('questionCount') || '15', 10);
    const qcountBtns = document.querySelectorAll('.qcount-btn');
    const statQcount = document.getElementById('stat-qcount');

    function setCount(n) {
        selectedCount = n;
        sessionStorage.setItem('questionCount', n);
        qcountBtns.forEach(b => b.classList.toggle('qcount-btn--active', parseInt(b.dataset.count, 10) === n));
        if (statQcount) statQcount.textContent = n;
    }
    setCount(selectedCount);
    qcountBtns.forEach(b => b.addEventListener('click', () => setCount(parseInt(b.dataset.count, 10))));

    startBtn.addEventListener('click', async () => {
        const pseudo = input.value.trim() || input.placeholder;
        if (errorEl) errorEl.style.display = 'none';

        startBtn.disabled = true;
        startBtn.textContent = '⏳ Chargement...';

        try {
            if (window.player) {
                window.location.href = '/quiz';
                return;
            }
            const res  = await apiFetch('/api/player/create', 'POST', { pseudo });
            const data = await res.json();
            if (!res.ok) {
                const msg = data.error?.includes('déjà utilisé')
                    ? `🔒 Ce pseudo est déjà pris ! Choisis-en un autre.`
                    : data.error || 'Pseudo invalide.';
                showError(errorEl, msg);
                return;
            }
            window.player = data;
            window.location.href = '/quiz';
        } catch (_) {
            showError(errorEl, 'Erreur réseau. Réessaie.');
        } finally {
            startBtn.disabled = false;
            startBtn.innerHTML = '<span class="btn-icon">⚡</span>Jouer !';
        }
    });

    input.addEventListener('keydown', (e) => { if (e.key === 'Enter') startBtn.click(); });
}

function generateClientPseudo() {
    const adj  = ['Brillant', 'Curieux', 'Atomique', 'Électrique', 'Quantique', 'Cosmique', 'Ionique', 'Dynamique'];
    const elem = ['Hydro', 'Carbone', 'Néon', 'Fer', 'Or', 'Argent', 'Krypton', 'Radium'];
    return `${adj[~~(Math.random()*adj.length)]}${elem[~~(Math.random()*elem.length)]}${~~(Math.random()*90)+10}`;
}

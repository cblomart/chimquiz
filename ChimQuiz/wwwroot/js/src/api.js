// ── API Fetch Helper ──────────────────────────────────────────────────────────
export async function apiFetch(url, method = 'GET', body = null) {
    const headers = { 'Content-Type': 'application/json' };
    if (method !== 'GET') {
        const tokenMeta = document.querySelector('meta[name="__RequestVerificationToken"]');
        if (tokenMeta) headers['X-CSRF-TOKEN'] = tokenMeta.content;
    }
    const options = { method, headers, credentials: 'same-origin' };
    if (body !== null) options.body = JSON.stringify(body);
    return fetch(url, options);
}

export function escHtml(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

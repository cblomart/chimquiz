#!/usr/bin/env bash
# Revue IA visuelle avant release — génère les screenshots puis appelle Claude Opus.
# Usage : ./review-ai.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"
REPORT="$SCRIPT_DIR/ChimQuiz.UITests/screenshots/ai-review.md"

# ── Charger .env si présent ───────────────────────────────────────────────────
if [[ -f "$ENV_FILE" ]]; then
    set -a
    # shellcheck source=/dev/null
    source "$ENV_FILE"
    set +a
fi

# ── Vérifier la clé API ───────────────────────────────────────────────────────
if [[ -z "${ANTHROPIC_API_KEY:-}" ]]; then
    echo "❌  ANTHROPIC_API_KEY non définie."
    echo "    Crée un fichier .env à la racine du projet :"
    echo "    cp .env.example .env  # puis édite .env avec ta clé"
    exit 1
fi

echo "✅  Clé API détectée."
echo

# ── Étape 1 : screenshots ─────────────────────────────────────────────────────
echo "📸  Étape 1/2 — Génération des screenshots (VisualTests)..."
dotnet test "$SCRIPT_DIR/ChimQuiz.UITests/ChimQuiz.UITests.csproj" \
    --filter "Category!=AIReview" \
    --verbosity quiet
echo

# ── Étape 2 : revue IA ───────────────────────────────────────────────────────
echo "🤖  Étape 2/2 — Revue Claude Opus (6 analyses, ~2 min, ~\$1.40)..."
dotnet test "$SCRIPT_DIR/ChimQuiz.UITests/ChimQuiz.UITests.csproj" \
    --filter "Category=AIReview" \
    --verbosity normal
echo

# ── Ouvrir le rapport ────────────────────────────────────────────────────────
if [[ -f "$REPORT" ]]; then
    echo "📄  Rapport : $REPORT"
    open "$REPORT" 2>/dev/null || echo "    (ouvre le fichier manuellement)"
fi

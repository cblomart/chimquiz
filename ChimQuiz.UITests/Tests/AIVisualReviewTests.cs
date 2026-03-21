using System.Globalization;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace ChimQuiz.UITests.Tests
{
    /// <summary>
    /// Revue qualité visuelle assistée par Claude Opus.
    /// Prérequis : avoir exécuté les VisualTests au préalable pour générer les screenshots.
    /// Lancement : ANTHROPIC_API_KEY=sk-... dotnet test --filter Category=AIReview
    /// </summary>
    [Collection("UITests")]
    [Trait("Category", "AIReview")]
    public sealed class AIVisualReviewTests
    {
        private readonly ITestOutputHelper _output;

        public AIVisualReviewTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private const string Model = "claude-opus-4-6";
        private const string ApiUrl = "https://api.anthropic.com/v1/messages";

        private static string ScreenshotsDir => Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "screenshots"));

        [Fact]
        public async Task GenerateAIVisualReport()
        {
            string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "ANTHROPIC_API_KEY requis. Lancement : ANTHROPIC_API_KEY=sk-... dotnet test --filter Category=AIReview");
            }

            if (!Directory.Exists(ScreenshotsDir))
            {
                throw new InvalidOperationException(
                    $"Dossier screenshots introuvable : {ScreenshotsDir}\n" +
                    "Exécute d'abord : dotnet test --filter Category!=AIReview");
            }

            using HttpClient http = new() { Timeout = TimeSpan.FromMinutes(3) };

            StringBuilder report = new();
            report.AppendLine("# ChimQuiz — Revue IA avant release");
            report.AppendLine();
            report.AppendLine(CultureInfo.InvariantCulture, $"**Date :** {DateTime.Now:yyyy-MM-dd HH:mm}  ");
            report.AppendLine(CultureInfo.InvariantCulture, $"**Modèle :** {Model}  ");
            report.AppendLine(CultureInfo.InvariantCulture, $"**Screenshots :** {ScreenshotsDir}");
            report.AppendLine();
            report.AppendLine("---");
            report.AppendLine();

            // ── 1–4 : Cohérence & bugs visuels par page ──────────────────────────
            await RunPageAnalysisAsync(http, apiKey, report, "Page d'accueil", "home");
            await RunPageAnalysisAsync(http, apiKey, report, "Quiz — Fiche info", "quiz-infocard");
            await RunPageAnalysisAsync(http, apiKey, report, "Leaderboard", "leaderboard");

            // ── 5 : Tous les types de questions ──────────────────────────────────
            await RunQuestionTypesAnalysisAsync(http, apiKey, report);

            // ── 6 : Psychologie adolescent ────────────────────────────────────────
            await RunPsychologyAnalysisAsync(http, apiKey, report);

            // ── 7 : Flow d'apprentissage ──────────────────────────────────────────
            await RunLearningFlowAnalysisAsync(http, apiKey, report);

            string reportPath = Path.Combine(ScreenshotsDir, "ai-review.md");
            await File.WriteAllTextAsync(reportPath, report.ToString(), Encoding.UTF8);

            _output.WriteLine($"\nRapport généré : {reportPath}\n");
            _output.WriteLine(report.ToString());
        }

        // ── Analyses ─────────────────────────────────────────────────────────────

        private async Task RunPageAnalysisAsync(
            HttpClient http,
            string apiKey,
            StringBuilder report,
            string pageLabel,
            string prefix)
        {
            string[] viewports = ["mobile", "tablet", "laptop", "desktop"];
            List<(string Path, string Label)> images = [];
            foreach (string viewport in viewports)
            {
                string path = Path.Combine(ScreenshotsDir, $"{prefix}-{viewport}.png");
                if (File.Exists(path))
                {
                    images.Add((path, $"{pageLabel} — {viewport}"));
                }
            }

            report.AppendLine(CultureInfo.InvariantCulture, $"## {pageLabel}");
            report.AppendLine();

            if (images.Count == 0)
            {
                report.AppendLine("⚠️ Aucun screenshot disponible pour cette page.");
                report.AppendLine();
                return;
            }

            string prompt = $"""
                Tu es expert en UX et contrôle qualité visuel pour applications web.
                Les images ci-dessus montrent la page "{pageLabel}" de ChimQuiz (quiz de chimie pour lycéens français),
                aux viewports mobile (390px), tablette (768px), laptop (1280px) et desktop (1920px).

                Charte graphique de référence :
                - Dark sci-fi : fond #0d0d1a, cartes en verre semi-transparent (blur + border rgba)
                - Gradient signature : cyan #4cc9f0 → violet #7c3aed → rose #f72585
                - Typographies : Orbitron (compteurs, titres sci-fi) + Nunito (corps de texte chaleureux)
                - Boutons : gradient violet→cyan, radius 12px, hover translateY(-2px)

                Analyse ces 3 aspects et réponds avec des sections claires :

                ### 1. Bugs visuels ✅/⚠️/❌
                Éléments tronqués, chevauchements, texte illisible, espaces anormaux, alignements cassés ?

                ### 2. Cohérence cross-viewport ✅/⚠️/❌
                Le design s'adapte-t-il correctement à chaque taille d'écran ?
                La charte graphique est-elle respectée uniformément ?

                ### 3. Lisibilité ✅/⚠️/❌
                Contrastes, tailles de police et hiérarchie visuelle sont-ils adéquats ?

                Pour chaque problème détecté, donne une suggestion concrète et actionnable.
                """;

            string result = await CallClaudeAsync(http, apiKey, images, prompt);
            report.AppendLine(result);
            report.AppendLine();
            report.AppendLine("---");
            report.AppendLine();

            _output.WriteLine($"[{pageLabel}] analyse terminée.");
        }

        private async Task RunQuestionTypesAnalysisAsync(
            HttpClient http,
            string apiKey,
            StringBuilder report)
        {
            string[] types =
            [
                "name-to-symbol-mcq",
                "symbol-to-name-mcq",
                "name-to-symbol-typed",
                "symbol-to-name-typed",
            ];
            List<(string Path, string Label)> images = [];
            foreach (string type in types)
            {
                string path = Path.Combine(ScreenshotsDir, $"quiz-type-{type}.png");
                if (File.Exists(path))
                {
                    images.Add((path, type));
                }
            }

            report.AppendLine("## Types de questions — Couverture visuelle");
            report.AppendLine();

            if (images.Count == 0)
            {
                report.AppendLine("⚠️ Aucun screenshot de type de question disponible.");
                report.AppendLine();
                return;
            }

            string prompt = $"""
                Tu es expert en UX et contrôle qualité visuel pour applications web.
                Les images ci-dessus montrent les {images.Count} type(s) de questions présents dans ChimQuiz
                (quiz de chimie pour lycéens français), tous capturés sur mobile (390px).

                Les 4 types possibles sont :
                - name-to-symbol-mcq : on affiche le NOM de l'élément, on choisit parmi 4 SYMBOLES (MCQ)
                - symbol-to-name-mcq : on affiche le SYMBOLE, on choisit parmi 4 NOMS (MCQ)
                - name-to-symbol-typed : on affiche le NOM, on tape le SYMBOLE (saisie libre, max 3 chars)
                - symbol-to-name-typed : on affiche le SYMBOLE, on tape le NOM (saisie libre)

                Charte graphique : dark sci-fi, gradient cyan→violet→rose, Orbitron pour les valeurs, Nunito pour le texte.

                Analyse chaque type de question visible :

                ### 1. Lisibilité de la valeur affichée ✅/⚠️/❌
                La valeur centrale (symbole ou nom de l'élément) est-elle immédiatement visible et lisible ?
                Le gradient text sur fond sombre est-il suffisamment contrasté ?

                ### 2. Clarté de l'interaction ✅/⚠️/❌
                Pour le MCQ : les 4 choix sont-ils clairement identifiables comme boutons cliquables ?
                Pour le typed : le champ de saisie est-il intuitif ? le placeholder aide-t-il ?

                ### 3. Cohérence entre les types ✅/⚠️/❌
                Les différents types partagent-ils la même identité visuelle ?
                Le passage de l'un à l'autre serait-il naturel pour un joueur ?

                Pour chaque problème : suggestion concrète et actionnable.
                """;

            string result = await CallClaudeAsync(http, apiKey, images, prompt);
            report.AppendLine(result);
            report.AppendLine();
            report.AppendLine("---");
            report.AppendLine();

            _output.WriteLine("[Types de questions] analyse terminée.");
        }

        private async Task RunPsychologyAnalysisAsync(
            HttpClient http,
            string apiKey,
            StringBuilder report)
        {
            string[] names = ["home-mobile", "quiz-question-mobile", "quiz-infocard-mobile", "quiz-question-tablet"];
            List<(string Path, string Label)> images = [];
            foreach (string name in names)
            {
                string path = Path.Combine(ScreenshotsDir, $"{name}.png");
                if (File.Exists(path))
                {
                    images.Add((path, name));
                }
            }

            report.AppendLine("## Psychologie adolescent — Attrait & Rétention");
            report.AppendLine();

            if (images.Count == 0)
            {
                report.AppendLine("⚠️ Aucun screenshot disponible.");
                report.AppendLine();
                return;
            }

            const string Prompt = """
                Tu es psychologue spécialiste des adolescents et expert en UX pour le public 14-17 ans.
                Les images montrent ChimQuiz : page d'accueil, quiz en cours (question), et fiche d'info après réponse.

                ChimQuiz est un quiz de chimie sur le tableau périodique, destiné aux lycéens français.
                L'objectif de la gamification : timer de 15s par question, XP, combo multiplicateur, système de rangs,
                barre de progression, tour revanche pour les éléments ratés, série (streak).

                Analyse sur deux axes :

                ### 1. Attrait initial ✅/⚠️/❌
                Ce design donne-t-il envie à un ado de 14-17 ans de jouer ?
                - Identifie les éléments qui attirent (esthétique, dynamisme, modernité perçue)
                - Identifie les éléments qui pourraient rebuter ou paraître trop "scolaires"
                - Compare à ce qu'un ado connaît (jeux mobiles, réseaux sociaux)

                ### 2. Rétention & motivation ✅/⚠️/❌
                Les mécaniques de gamification sont-elles visibles, compréhensibles et motivantes ?
                - Le timer crée-t-il le bon niveau de stress positif (challenge) sans être anxiogène ?
                - XP, combo, rang et série donnent-ils un sentiment de progression suffisant ?
                - Manque-t-il des éléments de rétention classiques (social, partage, défis) ?

                Base ton analyse sur les ressorts psychologiques adolescents :
                besoin de stimulation, compétition, reconnaissance par les pairs, sentiment de progression rapide,
                autonomie, identité numérique.

                Format : verdict global + points forts + points d'amélioration priorisés par impact potentiel.
                """;

            string result = await CallClaudeAsync(http, apiKey, images, Prompt);
            report.AppendLine(result);
            report.AppendLine();
            report.AppendLine("---");
            report.AppendLine();

            _output.WriteLine("[Psychologie ado] analyse terminée.");
        }

        private async Task RunLearningFlowAnalysisAsync(
            HttpClient http,
            string apiKey,
            StringBuilder report)
        {
            string[] names = ["quiz-question-mobile", "quiz-infocard-mobile"];
            List<(string Path, string Label)> images = [];
            foreach (string name in names)
            {
                string path = Path.Combine(ScreenshotsDir, $"{name}.png");
                if (File.Exists(path))
                {
                    images.Add((path, name));
                }
            }

            report.AppendLine("## Flow d'apprentissage");
            report.AppendLine();

            if (images.Count == 0)
            {
                report.AppendLine("⚠️ Aucun screenshot disponible.");
                report.AppendLine();
                return;
            }

            const string Prompt = """
                Tu es pédagogue spécialiste de l'apprentissage par le jeu (game-based learning) et de la mémorisation.
                La séquence ci-dessus montre le flow complet d'une question dans ChimQuiz :

                Étape 1 — La question :
                  • Un prompt texte ("Quel est le symbole de cet élément ?", etc.)
                  • La valeur à identifier (nom, symbole, ou numéro atomique affiché en grand)
                  • 4 choix MCQ (lettres A/B/C/D) OU un champ de saisie libre
                  • Un timer de 15s qui décompte visuellement

                Étape 2 — La fiche d'info (après réponse ou timeout) :
                  • Verdict correct/incorrect avec correction si besoin
                  • Identité de l'élément : symbole, nom, numéro atomique, structure (p⁺ n⁰ e⁻)
                  • Un fait intéressant sur l'élément
                  • Son utilisation principale
                  • Un timer auto-avance de 12s (le joueur peut aussi cliquer "J'ai lu !")

                Analyse ces 3 dimensions pédagogiques :

                ### 1. Encodage mémoriel ✅/⚠️/❌
                La fiche d'info favorise-t-elle la mémorisation de l'élément ?
                - Le temps d'exposition (12s auto) est-il suffisant ?
                - La richesse des informations (structure atomique, fait, usage) crée-t-elle un ancrage contextuel ?
                - L'effet de testing (répondre puis voir la correction) est-il bien exploité ?

                ### 2. Charge cognitive & rythme ✅/⚠️/❌
                Le flow question → réponse → info → suivant est-il bien équilibré ?
                - Le timer de 15s crée-t-il trop de pression pour une apprentissage efficace ?
                - La quantité d'information dans la fiche est-elle optimale (ni trop, ni trop peu) ?
                - Le timer auto de 12s respecte-t-il le rythme naturel de lecture d'un ado ?

                ### 3. Transfert & compréhension durable ✅/⚠️/❌
                Les informations présentées construisent-elles une compréhension durable ?
                - Le lien entre symbole, nom et structure atomique est-il rendu explicite ?
                - Le fait intéressant et l'utilisation ancrent-ils l'élément dans un contexte réel ?
                - Manque-t-il des éléments pour favoriser le transfert (analogies, contexte historique) ?

                Format : analyse structurée avec recommandations concrètes et actionnables, classées par priorité.
                """;

            string result = await CallClaudeAsync(http, apiKey, images, Prompt);
            report.AppendLine(result);
            report.AppendLine();

            _output.WriteLine("[Flow apprentissage] analyse terminée.");
        }

        // ── HTTP helper ───────────────────────────────────────────────────────────

        private static async Task<string> CallClaudeAsync(
            HttpClient http,
            string apiKey,
            IReadOnlyList<(string Path, string Label)> images,
            string prompt)
        {
            // Build multimodal content: interleave images with their labels
            List<object> content = [];
            foreach ((string path, string label) in images)
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                string b64 = Convert.ToBase64String(bytes);
                content.Add(new
                {
                    type = "image",
                    source = new { type = "base64", media_type = "image/png", data = b64 },
                });
                content.Add(new { type = "text", text = $"↑ {label}" });
            }
            content.Add(new { type = "text", text = prompt });

            string requestJson = JsonSerializer.Serialize(new
            {
                model = Model,
                max_tokens = 2048,
                messages = new[] { new { role = "user", content } },
            });

            using HttpRequestMessage request = new(HttpMethod.Post, ApiUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            using HttpResponseMessage response = await http.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Claude API error {(int)response.StatusCode}: {responseJson}");
            }

            using JsonDocument doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
    }
}

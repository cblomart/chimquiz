# ChimQuiz ⚗️

Quiz interactif sur les éléments chimiques du tableau périodique, conçu pour les adolescents (13-17 ans).

## Fonctionnalités

- **3 types de questions** mélangées aléatoirement :
  - Nom → Symbole (QCM)
  - Symbole → Nom (QCM)
  - Symbole → Nom (saisie libre, orthographe évaluée)
- **Difficulté adaptative** selon le niveau du joueur : pool d'éléments et mix de questions qui s'élargissent avec l'XP
- **Distracteurs intelligents** : les mauvais choix sont confusables (même 1ère lettre, même longueur)
- **Timer par question** : 15s MCQ / 25s saisie libre, avec barre de compte à rebours colorée (bleu → jaune → rouge)
- **Système de combo et XP** : bonus vitesse ⚡, bonus lecture 📚, bonus orthographe parfaite ✍️
- **Carte info après chaque réponse** : fun fact 🤯, utilisation, où trouver l'élément — 12s de lecture
- **6 rangs** progressifs : ⚗️ Apprenti Chimiste → 🥇 Lauréat Nobel
- **Classement** global et hebdomadaire (top 10)
- **Pseudo unique** lié à l'appareil via cookie HttpOnly — aucune inscription requise
- **Détection d'inactivité** : modale "Tu es encore là ?" après 2 min sans interaction

## Stack technique

- **Backend** : ASP.NET Core 9, Razor Pages + Minimal APIs
- **Base de données** : SQLite via EF Core (persistence légère, zéro configuration)
- **Frontend** : Vanilla JS, CSS glassmorphism dark-neon (aucun framework)
- **Sécurité** : rate limiting, headers sécurité (CSP, X-Frame-Options…), SameSite=Strict, antiforgery
- **Déploiement** : Docker multi-stage, docker-compose avec volume nommé

## Lancement local

### Prérequis

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

```bash
cd ChimQuiz
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Ouvrir [http://localhost:5000](http://localhost:5000)

### Docker

```bash
docker-compose up --build
```

Ouvrir [http://localhost:8080](http://localhost:8080)

La base de données SQLite est persistée dans le volume `chimquiz_data`.

## Structure du projet

```
chimquiz/
├── ChimQuiz/
│   ├── Api/                  # Minimal API endpoints (player, quiz, leaderboard)
│   ├── Data/                 # EF Core DbContext
│   ├── Middleware/           # Security headers
│   ├── Models/               # Element, Player, QuizSessionState…
│   ├── Pages/                # Razor Pages (Index, Quiz, Leaderboard)
│   ├── Services/             # ElementService, QuizService, PlayerService
│   └── wwwroot/              # JS, CSS, images
├── mendeleev_basic.csv       # Source des 118 éléments
├── Dockerfile
└── docker-compose.yml
```

## Système de rangs

| Rang | XP requis | Sessions ~moyennes |
|---|---|---|
| ⚗️ Apprenti Chimiste | 0 | — |
| 🧪 Chimiste Junior | 300 | ~2 |
| 🔬 Chimiste | 900 | ~6 |
| ⚛️ Chimiste Expert | 2 000 | ~13 |
| 🏆 Maître Chimiste | 4 500 | ~30 |
| 🥇 Lauréat Nobel | 9 000 | ~60 |

## Licence

MIT — voir [LICENSE](LICENSE)

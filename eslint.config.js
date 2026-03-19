import security from "eslint-plugin-security";

export default [
  {
    files: ["ChimQuiz/wwwroot/js/**/*.js"],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: "script",
      globals: {
        // Browser globals
        window: "readonly",
        document: "readonly",
        fetch: "readonly",
        setTimeout: "readonly",
        clearTimeout: "readonly",
        setInterval: "readonly",
        clearInterval: "readonly",
        console: "readonly",
        performance: "readonly",
        requestAnimationFrame: "readonly",
        URL: "readonly",
      },
    },
    plugins: { security },
    rules: {
      // ── Security rules ────────────────────────────────────────
      ...security.configs.recommended.rules,

      // ── Complexity ────────────────────────────────────────────
      complexity: ["error", { max: 15 }],
      "max-depth": ["warn", 4],
      "max-lines-per-function": ["warn", { max: 60, skipBlankLines: true, skipComments: true }],

      // ── Common bugs ───────────────────────────────────────────
      "no-unused-vars": ["warn", { varsIgnorePattern: "^_", argsIgnorePattern: "^_" }],
      "no-undef": "error",
      eqeqeq: ["error", "always"],
      "no-eval": "error",
      "no-implied-eval": "error",
      "no-new-func": "error",
    },
  },
];

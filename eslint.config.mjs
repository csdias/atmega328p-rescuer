import js from '@eslint/js';
import globals from 'globals';
import tseslint from 'typescript-eslint';
import reactHooks from 'eslint-plugin-react-hooks';

/**
 * Lint for the three front-end workspaces.
 *
 * The reason this exists: `eslint-plugin-react-hooks` finds hooks by the `use` prefix on
 * the name. While the shared hook was called `usarBancada` and the clock one
 * `usarRelogio`, the Rules of Hooks were not checked in either — a `useState` inside an
 * `if` would have gone through without a word. Renaming them to `useBench` and `useClock`
 * put them back under the rule; this config is what makes the rule run at all.
 */
export default tseslint.config(
  {
    ignores: [
      '**/node_modules/**',
      '**/dist/**',
      '**/build/**',
      // The Vite build lands in the API's wwwroot: bundled output, not source.
      'ATMegaPestaV1.Api/wwwroot/**',
      // Worktrees of other sessions carry their own copy of the tree.
      '.claude/**',
      '**/bin/**',
      '**/obj/**',
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    // Build tooling runs on Node and predates ESM here.
    files: ['**/*.config.js', '**/*.cjs'],
    languageOptions: {
      globals: { ...globals.node },
      sourceType: 'commonjs',
    },
    rules: {
      '@typescript-eslint/no-require-imports': 'off',
    },
  },
  {
    files: [
      'frontend-shared/**/*.{ts,tsx}',
      'frontend-react/**/*.{ts,tsx}',
      'frontend-mobile/**/*.{ts,tsx}',
    ],
    plugins: { 'react-hooks': reactHooks },
    languageOptions: {
      globals: { ...globals.browser, ...globals.es2021 },
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      // The bench talks to hardware: an unused variable is usually a rename left halfway,
      // not dead decoration. Underscore-prefixed is the deliberate opt-out.
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
    },
  },
);

import { defineConfig, devices } from "@playwright/test";

/**
 * Config Playwright (TASK-004).
 *
 * `testDir` fica em `e2e/` (separado de `src/**\/*.test.ts`, que é Vitest —
 * TASK-001). O helper de rede desligada + cache limpo (`e2e/utils/offline.ts`)
 * é reaproveitado pelos smoke tests offline de lotes futuros (Lote 13:
 * TASK-068/088), então já nasce aqui em vez de ser recriado depois.
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL: "http://localhost:4173",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    // Servidor de dev (Vite), não o build de produção: o placeholder e2e
    // aqui só precisa do shell React renderizado. Testes que exercitam o SW
    // Workbox de verdade (Lote 13 — TASK-066/067/068) rodam contra
    // `vite build` + `vite preview` dentro da própria tarefa, quando o
    // pipeline PWA (TASK-003) já estiver concluído.
    command: "npm run dev -- --port 4173 --strictPort",
    url: "http://localhost:4173",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});

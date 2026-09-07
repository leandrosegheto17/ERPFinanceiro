import { expect, test } from "@playwright/test";

import { clearBrowserCache, goOffline } from "./utils/offline";

/**
 * Placeholder e2e (TASK-004): confirma que o Playwright está corretamente
 * configurado contra o build real (via `webServer`, ver `playwright.config.ts`)
 * e que o helper de rede desligada + cache limpo (`e2e/utils/offline.ts`)
 * funciona de ponta a ponta. Testes de PWA offline "de verdade" (Service
 * Worker + cache Workbox) entram no Lote 13 (TASK-066/067/068), depois que
 * TASK-003 estiver concluída.
 */
test("renderiza o shell do app", async ({ page }) => {
  await page.goto("/");
  await expect(page.locator("main")).toHaveText("Estudo Bíblico");
});

test("cache limpo + rede desligada bloqueia nova navegação", async ({
  page,
}) => {
  // Popula qualquer cache HTTP existente com uma visita normal.
  await page.goto("/");
  await expect(page.locator("main")).toHaveText("Estudo Bíblico");

  // Limpa o cache do navegador e desliga a rede: sem Service Worker
  // registrado ainda (isso só chega em TASK-003/066), uma navegação nessas
  // condições deve falhar por falta de rede — prova de que o mecanismo
  // desliga a rede e limpa o cache de verdade, não é um no-op.
  await clearBrowserCache(page);
  await goOffline(page);

  await expect(page.goto("/")).rejects.toThrow();
});

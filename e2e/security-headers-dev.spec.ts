import { expect, test } from "@playwright/test";

/**
 * Correção pós-fechamento de TASK-034 (regressão encontrada durante
 * TASK-035/Lote 6): confirma, via Playwright real contra `vite dev`
 * (mesmo `webServer` de `playwright.config.ts`, porta 4173), que:
 *
 *   1. A resposta HTTP do dev server NÃO inclui `Content-Security-Policy`
 *      (decisão: dev não aplica CSP nenhuma — ver `securityHeaders.ts`,
 *      `applyDevSecurityHeaders`).
 *   2. O app monta normalmente — nenhuma violação de CSP bloqueando o
 *      preamble inline do React Fast Refresh (causa raiz da regressão).
 *      `e2e/placeholder.spec.ts` já cobre a montagem em si; este teste é a
 *      prova direta e específica de que não há violação de CSP registrada
 *      no console do navegador durante o carregamento.
 */
test("vite dev: sem CSP na resposta HTTP e sem violação de CSP ao montar o app", async ({
  page,
  baseURL,
}) => {
  const response = await page.request.get(baseURL ?? "http://localhost:4173/");
  expect(response.headers()["content-security-policy"]).toBeUndefined();

  const cspViolations: string[] = [];
  page.on("console", (msg) => {
    const text = msg.text();
    if (/content security policy|csp/i.test(text)) {
      cspViolations.push(text);
    }
  });
  page.on("pageerror", (err) => {
    if (/content security policy|csp/i.test(err.message)) {
      cspViolations.push(err.message);
    }
  });

  await page.goto("/");
  await expect(page.locator("main")).toHaveText("Estudo Bíblico");

  expect(cspViolations).toEqual([]);
});

import type { Page } from "@playwright/test";

/**
 * Helper de "rede desligada + cache limpo" (TASK-004).
 *
 * Reaproveitado pelos smoke tests offline de PWA de lotes futuros (Lote 13 —
 * TASK-068 "T-01/T-02 com rede desligada e cache limpo"; Lote 15 — TASK-088
 * "primeiro cartão ≤1s, rede desligada, caches limpos"), para não recriar essa
 * lógica em cada tarefa.
 *
 * `Network.clearBrowserCache`/`clearBrowserCookies` via CDP limpam o cache
 * HTTP/disco do Chromium antes de qualquer navegação; `context.setOffline`
 * desliga a rede depois disso — nessa ordem, porque limpar o cache com a rede
 * já desligada não teria efeito (não há requisição de rede para popular
 * cache).
 */
export async function clearBrowserCache(page: Page): Promise<void> {
  const client = await page.context().newCDPSession(page);
  await client.send("Network.clearBrowserCache");
  await client.send("Network.clearBrowserCookies");
  await client.detach();
}

export async function goOffline(page: Page): Promise<void> {
  await page.context().setOffline(true);
}

export async function goOnline(page: Page): Promise<void> {
  await page.context().setOffline(false);
}

/**
 * Limpa cache/cookies e desliga a rede, na ordem correta. Uso típico:
 * navegar com rede normal para popular o cache (ex.: Service Worker), depois
 * chamar `withCleanCacheOffline` numa nova página/aba antes de repetir a
 * navegação, para provar que o resultado vem do cache/SW e não da rede.
 */
export async function withCleanCacheOffline(page: Page): Promise<void> {
  await clearBrowserCache(page);
  await goOffline(page);
}

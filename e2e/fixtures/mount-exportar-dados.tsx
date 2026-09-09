import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import { AppDatabase } from "../../src/core/storage";
import { BlocoExportarDados } from "../../src/features/identity";

/**
 * Harness de e2e (TASK-039, Tela T-13 "Baixar meus dados", CA-10.4) —
 * dev-only, nunca referenciado por `index.html` nem alcançável a partir dele
 * (Vite só empacota, no build de produção, o que é alcançável a partir do
 * `index.html` real — `vite.config.ts` não declara nenhum `rollupOptions.input`
 * extra para este arquivo).
 *
 * Existe porque `src/features/shell/App.tsx` ainda é um placeholder sem
 * roteador (Lote 0) — nenhuma tela do produto está montada de verdade ainda,
 * então não há rota real para o Playwright navegar e exercitar um download
 * de arquivo em um navegador de verdade. Mesmo padrão pragmático já usado
 * por `e2e/identity-signup.spec.ts`/`identity-signin.spec.ts` (TASK-035/037,
 * que exercitam a função/gateway diretamente, sem tela montada) — adaptado
 * aqui porque este critério de aceite específico ("e2e baixa JSON") só pode
 * ser provado com um download de navegador real: o único pedaço que precisa
 * de DOM de verdade é `downloadJsonFile` (Blob + `<a download>` sintético),
 * não uma tela roteada inteira.
 *
 * Semente determinística via query string:
 * - `dbName` (obrigatório): nome único de banco Dexie por execução de teste,
 *   evitando colisão entre testes rodando em paralelo (`fullyParallel: true`
 *   em `playwright.config.ts`);
 * - `seed=1` (opcional): grava um registro de progresso e um de preferência
 *   conhecidos antes de montar `BlocoExportarDados`, para que o teste e2e
 *   possa afirmar sobre o conteúdo real do JSON baixado sem manipular
 *   IndexedDB a partir do lado do Node.
 */
const params = new URLSearchParams(window.location.search);
const dbName = params.get("dbName");
if (!dbName) {
  throw new Error("Harness exportar-dados.html requer ?dbName=<nome único por teste>");
}

const db = new AppDatabase(dbName);

async function boot() {
  await db.open();

  if (params.get("seed") === "1") {
    await db.progress.add({
      planId: "plano-entrelacado-90",
      dayNumber: 3,
      completedAt: "2026-09-01T12:00:00.000Z",
    });
    await db.preferences.put({
      key: "tema",
      value: "escuro",
      updatedAt: "2026-09-01T08:00:00.000Z",
    });
  }

  const rootElement = document.getElementById("root");
  if (!rootElement) {
    throw new Error("Elemento #root não encontrado no harness exportar-dados.html");
  }

  createRoot(rootElement).render(
    <StrictMode>
      <BlocoExportarDados db={db} />
    </StrictMode>,
  );
}

void boot();

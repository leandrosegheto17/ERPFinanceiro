import { expect, test } from "@playwright/test";

/**
 * TASK-039 (Lote 6 — Identidade): e2e de `BlocoExportarDados` (Tela T-13,
 * "Baixar meus dados", CA-10.4). Critério de aceite: "e2e baixa JSON com
 * progresso/preferências/esboços".
 *
 * Navega até um harness dev-only (`e2e/fixtures/exportar-dados.html`, ver o
 * comentário desse arquivo para o porquê de não navegar até uma tela real
 * roteada — `App.tsx` ainda é um placeholder sem roteador) que grava
 * progresso/preferências conhecidos em IndexedDB (Dexie) e monta
 * `BlocoExportarDados` de verdade. Clica em "Exportar meus dados", captura o
 * download real do navegador (evento `page.on("download")` do Playwright) e
 * valida que o arquivo baixado é um JSON válido contendo os três campos do
 * critério de aceite.
 */

function uniqueDbName(): string {
  return `e2e-export-data-${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

test.describe("Exportação de dados em JSON — Tela T-13 (TASK-039)", () => {
  test("baixa um JSON válido com progresso, preferências e esboços", async ({ page }) => {
    const dbName = uniqueDbName();
    await page.goto(`/e2e/fixtures/exportar-dados.html?dbName=${dbName}&seed=1`);

    const exportButton = page.getByRole("button", { name: "Exportar meus dados" });
    await expect(exportButton).toBeEnabled();

    const downloadPromise = page.waitForEvent("download");
    await exportButton.click();
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toMatch(
      /^meus-dados-estudobiblico-\d{4}-\d{2}-\d{2}\.json$/,
    );

    const downloadStream = await download.createReadStream();
    expect(downloadStream).not.toBeNull();
    const chunks: Buffer[] = [];
    for await (const chunk of downloadStream!) {
      chunks.push(chunk as Buffer);
    }
    const fileContent = Buffer.concat(chunks).toString("utf-8");

    // Prova central do critério de aceite: o arquivo baixado é JSON válido.
    const parsed = JSON.parse(fileContent) as {
      progresso: unknown[];
      preferencias: unknown[];
      esbocos: unknown[];
    };

    expect(parsed.progresso).toEqual([
      {
        planId: "plano-entrelacado-90",
        dayNumber: 3,
        completedAt: "2026-09-01T12:00:00.000Z",
      },
    ]);
    expect(parsed.preferencias).toEqual([
      { key: "tema", value: "escuro", updatedAt: "2026-09-01T08:00:00.000Z" },
    ]);
    // `esbocos` sempre presente no formato, mesmo vazio nesta fase (Fase 2 /
    // Lote 14 é quem populará quando `core/storage` ganhar as stores de
    // esboço) — ver `src/features/identity/export-data.ts`.
    expect(parsed.esbocos).toEqual([]);

    await expect(page.getByRole("status")).toHaveText(/Arquivo baixado/);
  });

  test("sem dado local nenhum, ainda baixa um JSON válido com os três campos (vazios)", async ({
    page,
  }) => {
    const dbName = uniqueDbName();
    // Sem `seed=1`: conta recém-criada, nenhum progresso/preferência local
    // ainda. UX-SPEC.md marca "T-13 Vazio: N/A" — não existe estado
    // "nada para exportar" distinto, porque o arquivo é sempre válido
    // (ver comentário de `BlocoExportarDados.tsx`).
    await page.goto(`/e2e/fixtures/exportar-dados.html?dbName=${dbName}`);

    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("button", { name: "Exportar meus dados" }).click();
    const download = await downloadPromise;

    const downloadStream = await download.createReadStream();
    const chunks: Buffer[] = [];
    for await (const chunk of downloadStream!) {
      chunks.push(chunk as Buffer);
    }
    const parsed = JSON.parse(Buffer.concat(chunks).toString("utf-8")) as {
      progresso: unknown[];
      preferencias: unknown[];
      esbocos: unknown[];
    };

    expect(parsed.progresso).toEqual([]);
    expect(parsed.preferencias).toEqual([]);
    expect(parsed.esbocos).toEqual([]);
  });
});

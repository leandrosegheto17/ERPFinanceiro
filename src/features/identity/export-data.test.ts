/**
 * TASK-039 (Lote 6 — Identidade), Tela T-13 "Baixar meus dados" (CA-10.4).
 * Mesmo padrão de `dexie-auth-storage.test.ts`/`db.test.ts`: `fake-indexeddb/auto`
 * provê `indexedDB` para o Dexie rodar fora do navegador.
 */
import "fake-indexeddb/auto";

import { afterEach, describe, expect, it } from "vitest";

import { AppDatabase } from "../../core/storage";

import { collectDataExportPayload } from "./export-data";

describe("collectDataExportPayload (TASK-039, CA-10.4)", () => {
  const openDatabases: AppDatabase[] = [];

  function open(): AppDatabase {
    const db = new AppDatabase(`export-data-${crypto.randomUUID()}`);
    openDatabases.push(db);
    return db;
  }

  afterEach(async () => {
    while (openDatabases.length > 0) {
      const db = openDatabases.pop();
      db?.close();
      if (db) {
        await db.delete();
      }
    }
  });

  it("monta um payload válido mesmo sem nenhum dado local (conta recém-criada)", async () => {
    const db = open();
    await db.open();

    const payload = await collectDataExportPayload(db);

    expect(payload.schemaVersion).toBe(1);
    expect(typeof payload.exportadoEm).toBe("string");
    expect(() => new Date(payload.exportadoEm).toISOString()).not.toThrow();
    expect(payload.progresso).toEqual([]);
    expect(payload.preferencias).toEqual([]);
    // Critério de aceite ("e2e baixa JSON com progresso/preferências/esboços"):
    // o campo `esbocos` sempre existe no formato, mesmo vazio — Fase 2
    // (Lote 14) o populará quando `core/storage` ganhar as stores de esboço.
    expect(payload.esbocos).toEqual([]);
    expect(payload.notas.esbocos).toMatch(/Fase 2/);
  });

  it("inclui progresso e preferências reais gravados localmente", async () => {
    const db = open();
    await db.open();

    await db.progress.add({
      planId: "plano-entrelacado-90",
      dayNumber: 3,
      completedAt: "2026-09-01T12:00:00.000Z",
    });
    await db.progress.add({
      planId: "plano-entrelacado-90",
      dayNumber: 4,
      completedAt: "2026-09-02T12:00:00.000Z",
      syncedAt: "2026-09-02T12:05:00.000Z",
    });
    await db.preferences.put({
      key: "tema",
      value: "escuro",
      updatedAt: "2026-09-01T08:00:00.000Z",
    });

    const payload = await collectDataExportPayload(db);

    expect(payload.progresso).toHaveLength(2);
    expect(payload.progresso).toContainEqual({
      planId: "plano-entrelacado-90",
      dayNumber: 3,
      completedAt: "2026-09-01T12:00:00.000Z",
    });
    expect(payload.progresso).toContainEqual({
      planId: "plano-entrelacado-90",
      dayNumber: 4,
      completedAt: "2026-09-02T12:00:00.000Z",
      syncedAt: "2026-09-02T12:05:00.000Z",
    });
    expect(payload.preferencias).toEqual([
      { key: "tema", value: "escuro", updatedAt: "2026-09-01T08:00:00.000Z" },
    ]);
  });

  it("o JSON serializado (JSON.stringify) é válido e re-analisável (round-trip)", async () => {
    const db = open();
    await db.open();
    await db.preferences.put({ key: "tema", value: "claro", updatedAt: "2026-09-01T08:00:00.000Z" });

    const payload = await collectDataExportPayload(db);
    const json = JSON.stringify(payload);
    const parsed = JSON.parse(json) as unknown;

    expect(parsed).toEqual(payload);
  });
});

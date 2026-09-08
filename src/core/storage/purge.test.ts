/**
 * TASK-017 — orçamento de quota + expurgo LRU (RNF-03, RT-08, ADR-006).
 *
 * Critério de aceite: "Teste de quota excedida confirma ordem de expurgo
 * declarada". A ordem declarada em ADR-006 é: (1) `corpusChapters` por LRU
 * (`lastAccessedAt` mais antigo primeiro); (2)/(3) dado de Módulo 2, fora de
 * escopo desta Fase 1 (ver cabeçalho de `purge.ts`); qualquer outra store
 * (dado do titular ou fila de sincronização) nunca é expurgada.
 */
import "fake-indexeddb/auto";

import { afterEach, describe, expect, it } from "vitest";

import { AppDatabase } from "./db";
import { purgeIfOverBudget } from "./purge";
import { STORAGE_BUDGET_BYTES, isOverBudget, type StorageUsageEstimate } from "./quota";

describe("core/storage — orçamento de quota e expurgo LRU (TASK-017)", () => {
  const openDatabases: AppDatabase[] = [];

  function open(): AppDatabase {
    const db = new AppDatabase(`estudobiblico-purge-${crypto.randomUUID()}`);
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

  async function seedCorpusChapters(db: AppDatabase): Promise<void> {
    // Inserido fora de ordem cronológica de propósito, para o teste não
    // depender (por acidente) da ordem de inserção — só de `lastAccessedAt`.
    await db.corpusChapters.bulkPut([
      { bookId: "GEN", chapter: 3, verses: [], lastAccessedAt: "2026-03-01T00:00:00.000Z" },
      { bookId: "GEN", chapter: 1, verses: [], lastAccessedAt: "2026-01-01T00:00:00.000Z" },
      { bookId: "GEN", chapter: 2, verses: [], lastAccessedAt: "2026-02-01T00:00:00.000Z" },
    ]);
  }

  const overBudgetEstimate: StorageUsageEstimate = {
    usageBytes: STORAGE_BUDGET_BYTES + 1,
    quotaBytes: Number.POSITIVE_INFINITY,
  };
  const underBudgetEstimate: StorageUsageEstimate = {
    usageBytes: 1,
    quotaBytes: Number.POSITIVE_INFINITY,
  };

  it("não expurga nada quando a quota não está excedida", async () => {
    const db = open();
    await db.open();
    await seedCorpusChapters(db);

    const purged = await purgeIfOverBudget(db, { estimator: async () => underBudgetEstimate });

    expect(purged).toEqual([]);
    expect(await db.corpusChapters.count()).toBe(3);
  });

  it("expurga corpusChapters por LRU, mais antigo (lastAccessedAt) primeiro", async () => {
    const db = open();
    await db.open();
    await seedCorpusChapters(db);

    const purged = await purgeIfOverBudget(db, {
      estimator: async () => overBudgetEstimate,
      maxRecordsPerRun: 2,
    });

    // Ordem de expurgo declarada (ADR-006, prioridade 1): LRU dentro de
    // corpusChapters — GEN 1 (jan) antes de GEN 2 (fev), nunca GEN 3 (mar,
    // o mais recentemente acessado) primeiro.
    expect(purged).toEqual([
      { store: "corpusChapters", bookId: "GEN", chapter: 1, lastAccessedAt: "2026-01-01T00:00:00.000Z" },
      { store: "corpusChapters", bookId: "GEN", chapter: 2, lastAccessedAt: "2026-02-01T00:00:00.000Z" },
    ]);

    const remaining = await db.corpusChapters.toArray();
    expect(remaining).toHaveLength(1);
    expect(remaining[0]?.chapter).toBe(3);
  });

  it("continua expurgando até esvaziar corpusChapters se a quota permanecer excedida", async () => {
    const db = open();
    await db.open();
    await seedCorpusChapters(db);

    const purged = await purgeIfOverBudget(db, { estimator: async () => overBudgetEstimate });

    expect(purged.map((record) => record.chapter)).toEqual([1, 2, 3]);
    expect(await db.corpusChapters.count()).toBe(0);
  });

  it("nunca expurga dado do titular ou fila de sincronização, mesmo com quota excedida e cache já vazio (ADR-006)", async () => {
    const db = open();
    await db.open();

    await db.progress.add({ planId: "plan-1", dayNumber: 1, completedAt: "2026-01-01T00:00:00.000Z" });
    await db.anonProgress.add({ planId: "plan-1", dayNumber: 2, completedAt: "2026-01-02T00:00:00.000Z" });
    await db.preferences.put({ key: "reminderTime", value: "07:00", updatedAt: "2026-01-01T00:00:00.000Z" });
    await db.outbox.put({
      id: "mutation-1",
      entityType: "progress",
      entityId: "1",
      operation: "create",
      payload: {},
      baseRev: null,
      createdAt: "2026-01-01T00:00:00.000Z",
      status: "pending",
      retryCount: 0,
    });
    await db.eventQueue.add({ type: "day_completed", payload: {}, occurredAt: "2026-01-01T00:00:00.000Z" });
    await db.contentBundle.put({ id: "default", bundle: {}, importedAt: "2026-01-01T00:00:00.000Z" });
    await db.corpusBundle.put({
      id: "default",
      manifestVersion: "1",
      books: [],
      importedAt: "2026-01-01T00:00:00.000Z",
    });

    const purged = await purgeIfOverBudget(db, { estimator: async () => overBudgetEstimate });

    expect(purged).toEqual([]);
    expect(await db.progress.count()).toBe(1);
    expect(await db.anonProgress.count()).toBe(1);
    expect(await db.preferences.count()).toBe(1);
    expect(await db.outbox.count()).toBe(1);
    expect(await db.eventQueue.count()).toBe(1);
    expect(await db.contentBundle.count()).toBe(1);
    expect(await db.corpusBundle.count()).toBe(1);
  });

  it("respeita a quota real do navegador quando ela é mais apertada que o teto de 50 MB (RT-08)", () => {
    const tightBrowserQuota: StorageUsageEstimate = { usageBytes: 1024, quotaBytes: 1024 };
    expect(isOverBudget(tightBrowserQuota, STORAGE_BUDGET_BYTES)).toBe(true);

    const roomyBrowserQuota: StorageUsageEstimate = { usageBytes: 1024, quotaBytes: 1_000_000_000 };
    expect(isOverBudget(roomyBrowserQuota, STORAGE_BUDGET_BYTES)).toBe(false);
  });
});

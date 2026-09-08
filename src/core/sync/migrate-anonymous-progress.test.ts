/**
 * TASK-022 — `core/sync`: migração de progresso anônimo (RN-07).
 *
 * Critério de aceite: "3 dias locais aparecem em `plan_progress` após criar
 * conta" — nesta camada local (Lote 3), isso é "aparecem em `progress`" (a
 * store Dexie do titular, antes do envio ao servidor por `sendPendingMutations`,
 * TASK-019/backend do Lote 5-6).
 */
import "fake-indexeddb/auto";

import { afterEach, describe, expect, it } from "vitest";

import { AppDatabase } from "../storage/db";
import { migrateAnonymousProgress } from "./migrate-anonymous-progress";

describe("migrateAnonymousProgress — migração de progresso anônimo (TASK-022, RN-07)", () => {
  const openDatabases: AppDatabase[] = [];

  function open(name: string): AppDatabase {
    const db = new AppDatabase(name);
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

  it("migra 3 dias anônimos para `progress`, limpa `anonProgress` e enfileira 3 mutações na outbox", async () => {
    const db = open(`estudobiblico-migrate-anon-${crypto.randomUUID()}`);
    await db.open();

    await db.anonProgress.bulkAdd([
      { planId: "plan-genesis", dayNumber: 1, completedAt: "2026-09-01T10:00:00.000Z" },
      { planId: "plan-genesis", dayNumber: 2, completedAt: "2026-09-02T10:00:00.000Z" },
      { planId: "plan-genesis", dayNumber: 3, completedAt: "2026-09-03T10:00:00.000Z" },
    ]);

    const result = await migrateAnonymousProgress(db);

    expect(result.migrated).toHaveLength(3);

    const progressRecords = await db.progress.toArray();
    expect(progressRecords).toHaveLength(3);
    const byDay = new Map(progressRecords.map((record) => [record.dayNumber, record]));
    expect(byDay.get(1)).toMatchObject({
      planId: "plan-genesis",
      dayNumber: 1,
      completedAt: "2026-09-01T10:00:00.000Z",
    });
    expect(byDay.get(2)).toMatchObject({
      planId: "plan-genesis",
      dayNumber: 2,
      completedAt: "2026-09-02T10:00:00.000Z",
    });
    expect(byDay.get(3)).toMatchObject({
      planId: "plan-genesis",
      dayNumber: 3,
      completedAt: "2026-09-03T10:00:00.000Z",
    });

    const anonAfter = await db.anonProgress.toArray();
    expect(anonAfter).toHaveLength(0);

    const outboxRecords = await db.outbox.toArray();
    expect(outboxRecords).toHaveLength(3);
    for (const record of outboxRecords) {
      expect(record.entityType).toBe("progress");
      expect(record.operation).toBe("create");
      expect(record.status).toBe("pending");
      expect(record.baseRev).toBeNull();
    }
    const outboxEntityIds = outboxRecords.map((record) => record.entityId).sort();
    expect(outboxEntityIds).toEqual(["plan-genesis:1", "plan-genesis:2", "plan-genesis:3"]);
  });

  it("é idempotente: uma segunda chamada com `anonProgress` já vazio não faz nada", async () => {
    const db = open(`estudobiblico-migrate-anon-idempotent-${crypto.randomUUID()}`);
    await db.open();

    await db.anonProgress.bulkAdd([
      { planId: "plan-genesis", dayNumber: 1, completedAt: "2026-09-01T10:00:00.000Z" },
    ]);

    const first = await migrateAnonymousProgress(db);
    expect(first.migrated).toHaveLength(1);

    const second = await migrateAnonymousProgress(db);
    expect(second.migrated).toHaveLength(0);

    const progressRecords = await db.progress.toArray();
    expect(progressRecords).toHaveLength(1);

    const outboxRecords = await db.outbox.toArray();
    expect(outboxRecords).toHaveLength(1);
  });

  it("sem nenhum progresso anônimo, não grava nada em `progress` nem na outbox", async () => {
    const db = open(`estudobiblico-migrate-anon-empty-${crypto.randomUUID()}`);
    await db.open();

    const result = await migrateAnonymousProgress(db);

    expect(result.migrated).toHaveLength(0);
    expect(await db.progress.count()).toBe(0);
    expect(await db.outbox.count()).toBe(0);
  });
});

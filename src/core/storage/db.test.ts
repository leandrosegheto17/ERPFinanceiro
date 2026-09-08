/**
 * TASK-016 — schema Dexie versionado das stores de Fase 1 (SDD.md §5.2).
 *
 * Critério de aceite: "Migração de versão testada". A biblioteca de storage
 * (Dexie, DI-15) roda sobre IndexedDB, indisponível em Node puro — por isso
 * `fake-indexeddb/auto` (dependência mínima, padrão do ecossistema Dexie para
 * testes fora do navegador) provê `indexedDB`/`IDBKeyRange` globais antes de
 * qualquer `Dexie`/`AppDatabase` ser instanciado.
 */
import "fake-indexeddb/auto";

import Dexie from "dexie";
import { afterEach, describe, expect, it } from "vitest";

import { AppDatabase } from "./db";
import type { OutboxRecord } from "./types";

describe("AppDatabase — schema Dexie versionado (TASK-016)", () => {
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

  it("declara as stores de Fase 1 esperadas (SDD.md §5.2, exceto Módulo 2)", async () => {
    const dbName = `estudobiblico-schema-${crypto.randomUUID()}`;
    const db = open(dbName);
    await db.open();

    const storeNames = db.tables.map((table) => table.name).sort();
    expect(storeNames).toEqual(
      [
        "anonProgress",
        "contentBundle",
        "corpusBundle",
        "corpusChapters",
        "eventQueue",
        "outbox",
        "preferences",
        "progress",
      ].sort(),
    );

    // Fase 2 (Módulo 2, ADR-012/DI-13, TASK-070) fica fora deste schema.
    expect(storeNames).not.toContain("outlines");
    expect(storeNames).not.toContain("outlineSnapshots");
    expect(storeNames).not.toContain("presentationState");

    db.close();
  });

  it("grava e lê um registro real em cada store de Fase 1 (smoke test do schema)", async () => {
    const dbName = `estudobiblico-smoke-${crypto.randomUUID()}`;
    const db = open(dbName);
    await db.open();

    await db.corpusChapters.put({
      bookId: "GEN",
      chapter: 1,
      verses: [{ number: 1, text: "No princípio..." }],
      lastAccessedAt: new Date().toISOString(),
    });
    await db.corpusBundle.put({ id: "default", manifestVersion: "1", books: [], importedAt: new Date().toISOString() });
    await db.contentBundle.put({ id: "default", bundle: {}, importedAt: new Date().toISOString() });
    const anonProgressId = await db.anonProgress.add({
      planId: "plan-1",
      dayNumber: 1,
      completedAt: new Date().toISOString(),
    });
    const progressId = await db.progress.add({
      planId: "plan-1",
      dayNumber: 1,
      completedAt: new Date().toISOString(),
    });
    await db.preferences.put({ key: "theme", value: "dark", updatedAt: new Date().toISOString() });
    await db.outbox.put({
      id: "mutation-1",
      entityType: "progress",
      entityId: "plan-1:1",
      operation: "create",
      payload: {},
      baseRev: null,
      createdAt: new Date().toISOString(),
      status: "pending",
      retryCount: 0,
    });
    const eventId = await db.eventQueue.add({
      type: "day_completed",
      payload: {},
      occurredAt: new Date().toISOString(),
    });

    expect(await db.corpusChapters.get({ bookId: "GEN", chapter: 1 })).toBeDefined();
    expect(await db.corpusBundle.get("default")).toBeDefined();
    expect(await db.contentBundle.get("default")).toBeDefined();
    expect(await db.anonProgress.get(anonProgressId)).toBeDefined();
    expect(await db.progress.get(progressId)).toBeDefined();
    expect(await db.preferences.get("theme")).toBeDefined();
    expect(await db.outbox.get("mutation-1")).toBeDefined();
    expect(await db.eventQueue.get(eventId)).toBeDefined();

    db.close();
  });

  it("migração real v1 → v2 preserva dados existentes da outbox e back-preenche retryCount", async () => {
    const dbName = `estudobiblico-migration-${crypto.randomUUID()}`;

    // 1) Abre o banco só com o schema v1 (API de versionamento do Dexie,
    //    definida diretamente aqui para simular um dispositivo real que já
    //    rodou a v1 do app antes da v2 existir) e grava uma mutação pendente,
    //    exatamente como a outbox real faria antes da migração.
    const dbV1 = new Dexie(dbName);
    dbV1.version(1).stores({
      corpusChapters: "[bookId+chapter], lastAccessedAt",
      corpusBundle: "id",
      contentBundle: "id",
      anonProgress: "++id, [planId+dayNumber]",
      progress: "++id, [planId+dayNumber], syncedAt",
      preferences: "key, updatedAt",
      outbox: "id, createdAt, status",
      eventQueue: "++id, occurredAt, sentAt",
    });
    await dbV1.open();
    const outboxV1 = dbV1.table("outbox");
    await outboxV1.put({
      id: "pending-mutation-1",
      entityType: "progress",
      entityId: "plan-1:1",
      operation: "create",
      payload: { dayNumber: 1 },
      baseRev: null,
      createdAt: "2026-01-01T00:00:00.000Z",
      status: "pending",
      // Nota: nenhum `retryCount` aqui — é exatamente o dado legado que a
      // migração de v2 precisa back-preencher sem perder o registro.
    });
    expect(await outboxV1.count()).toBe(1);
    dbV1.close();

    // 2) Reabre o **mesmo banco** (mesmo nome, mesmo IndexedDB) com o schema
    //    real de produção (`AppDatabase`, v1 + v2), forçando o Dexie a rodar
    //    a migração de versão de verdade — não é simulação: é o mesmo
    //    mecanismo de `db.version(2).stores(...).upgrade(...)` usado em produção.
    const dbV2 = open(dbName);
    await dbV2.open();

    expect(dbV2.verno).toBe(2);

    const migrated = await dbV2.outbox.get("pending-mutation-1");
    expect(migrated).toBeDefined();
    expect(migrated?.entityId).toBe("plan-1:1");
    expect(migrated?.payload).toEqual({ dayNumber: 1 });
    expect(migrated?.status).toBe("pending");
    // Prova de que a migração back-preencheu o campo novo, sem apagar o
    // registro pendente de sincronização real do usuário.
    expect(migrated?.retryCount).toBe(0);

    // 3) O índice novo de v2 (`[status+createdAt]`) funciona de fato sobre o
    //    dado migrado — não só o campo, o índice também foi criado.
    const pendingInOrder: OutboxRecord[] = await dbV2.outbox
      .where("[status+createdAt]")
      .between(["pending", Dexie.minKey], ["pending", Dexie.maxKey])
      .toArray();
    expect(pendingInOrder).toHaveLength(1);
    expect(pendingInOrder[0]?.id).toBe("pending-mutation-1");

    dbV2.close();
  });

  it("uma segunda mutação gravada após a migração já nasce com retryCount explícito", async () => {
    const dbName = `estudobiblico-post-migration-${crypto.randomUUID()}`;
    const db = open(dbName);
    await db.open();

    await db.outbox.put({
      id: "mutation-post-migration",
      entityType: "preference",
      entityId: "theme",
      operation: "update",
      payload: { value: "dark" },
      baseRev: 3,
      createdAt: new Date().toISOString(),
      status: "pending",
      retryCount: 0,
    });

    const record = await db.outbox.get("mutation-post-migration");
    expect(record?.retryCount).toBe(0);

    db.close();
  });
});

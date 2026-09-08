/**
 * TASK-018 — `core/sync`: outbox local (mutação com id próprio e `baseRev`).
 *
 * Critério de aceite: "Mutação persiste sem rede e sobrevive a reload".
 *  - "sem rede": prova estrutural — `enqueueMutation` não importa/chama nada
 *    de rede (`fetch`, cliente HTTP, SDK de backend); só escreve na store
 *    local `outbox` via Dexie. O teste roda em Node/Vitest sem nenhum
 *    servidor no ar e sem mockar rede, e mesmo assim a mutação persiste —
 *    confirmando que a persistência não depende de nenhuma chamada de rede.
 *  - "sobrevive a reload": fecha a conexão Dexie e reabre o **mesmo**
 *    IndexedDB (mesmo nome de banco), mesmo padrão do teste de migração de
 *    TASK-016 (`src/core/storage/db.test.ts`), e confirma que a mutação
 *    enfileirada antes do fechamento ainda está lá depois.
 */
import "fake-indexeddb/auto";

import { afterEach, describe, expect, it } from "vitest";

import { AppDatabase } from "../storage/db";
import { enqueueMutation } from "./enqueue-mutation";

describe("enqueueMutation — outbox local (TASK-018)", () => {
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

  it("persiste a mutação sem nenhuma chamada de rede (fetch sobrescrito para lançar)", async () => {
    const originalFetch = globalThis.fetch;
    (globalThis as { fetch?: unknown }).fetch = () => {
      throw new Error("enqueueMutation não deveria chamar fetch (sem rede)");
    };

    const dbName = `estudobiblico-sync-no-network-${crypto.randomUUID()}`;
    const db = open(dbName);
    await db.open();

    try {
      const record = await enqueueMutation(db, {
        entityType: "progress",
        entityId: "plan-1:1",
        operation: "create",
        payload: { dayNumber: 1 },
        baseRev: null,
      });

      const stored = await db.outbox.get(record.id);
      expect(stored).toBeDefined();
      expect(stored?.entityId).toBe("plan-1:1");
      expect(stored?.status).toBe("pending");
    } finally {
      (globalThis as { fetch?: unknown }).fetch = originalFetch;
    }
  });

  it("gera um id próprio para a mutação, distinto do id da entidade", async () => {
    const dbName = `estudobiblico-sync-id-${crypto.randomUUID()}`;
    const db = open(dbName);
    await db.open();

    const record = await enqueueMutation(db, {
      entityType: "preference",
      entityId: "theme",
      operation: "update",
      payload: { value: "dark" },
      baseRev: 2,
    });

    expect(record.id).not.toBe(record.entityId);
    expect(record.id).toMatch(/^[0-9a-f-]{36}$/i);
    expect(record.baseRev).toBe(2);
    expect(record.retryCount).toBe(0);
    expect(record.status).toBe("pending");
  });

  it("duas mutações da mesma entidade recebem ids próprios distintos", async () => {
    const dbName = `estudobiblico-sync-multi-${crypto.randomUUID()}`;
    const db = open(dbName);
    await db.open();

    const first = await enqueueMutation(db, {
      entityType: "progress",
      entityId: "plan-1:1",
      operation: "create",
      payload: { dayNumber: 1 },
      baseRev: null,
    });
    const second = await enqueueMutation(db, {
      entityType: "progress",
      entityId: "plan-1:1",
      operation: "update",
      payload: { dayNumber: 1, note: "revisado" },
      baseRev: 1,
    });

    expect(first.id).not.toBe(second.id);
    expect(await db.outbox.count()).toBe(2);
  });

  it("mutação enfileirada sem rede sobrevive a um reload do banco (fecha e reabre o mesmo IndexedDB)", async () => {
    const dbName = `estudobiblico-sync-reload-${crypto.randomUUID()}`;

    // 1) Abre o banco, enfileira a mutação e fecha a conexão — simula o app
    //    sendo recarregado (aba fechada/reaberta) logo depois da mutação.
    const dbBeforeReload = open(dbName);
    await dbBeforeReload.open();

    const enqueued = await enqueueMutation(dbBeforeReload, {
      entityType: "progress",
      entityId: "plan-1:7",
      operation: "create",
      payload: { dayNumber: 7 },
      baseRev: null,
    });

    dbBeforeReload.close();

    // 2) Reabre uma conexão nova sobre o **mesmo** IndexedDB (mesmo nome de
    //    banco) — mesmo mecanismo usado pelo teste de migração de TASK-016
    //    para provar persistência real, não em memória por instância.
    const dbAfterReload = open(dbName);
    await dbAfterReload.open();

    const survived = await dbAfterReload.outbox.get(enqueued.id);

    expect(survived).toBeDefined();
    expect(survived?.entityType).toBe("progress");
    expect(survived?.entityId).toBe("plan-1:7");
    expect(survived?.operation).toBe("create");
    expect(survived?.payload).toEqual({ dayNumber: 7 });
    expect(survived?.baseRev).toBeNull();
    expect(survived?.status).toBe("pending");
    expect(survived?.retryCount).toBe(0);
  });
});

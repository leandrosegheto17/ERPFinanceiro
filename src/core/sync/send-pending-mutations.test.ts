/**
 * TASK-019 — `core/sync`: envio em lote idempotente com gate de rede/sessão.
 *
 * Critério de aceite: "Reenvio de lote não duplica no servidor mock".
 *  - Idempotência (prova central): `InMemorySyncServer.sendBatch` recebe o
 *    **mesmo lote** (mesmos `id`s de mutação) duas vezes e só registra cada
 *    mutação uma vez — provado tanto no nível do servidor mock isoladamente
 *    quanto no fluxo completo (`sendPendingMutations`), incluindo o cenário
 *    realista descrito em ADR-007: o servidor já recebeu a mutação, mas o
 *    cliente local ainda não sabe (ex.: confirmação perdida) e tenta reenviar.
 *  - Gate de rede/sessão: sem rede ou sem sessão, nenhuma chamada de rede
 *    ocorre (nem `client.sendBatch` é invocado) e nada é marcado `"sent"`.
 */
import "fake-indexeddb/auto";

import { afterEach, describe, expect, it, vi } from "vitest";

import { AppDatabase } from "../storage/db";
import { InMemorySyncServer } from "./__fixtures__/in-memory-sync-server";
import { enqueueMutation } from "./enqueue-mutation";
import { sendPendingMutations } from "./send-pending-mutations";

describe("sendPendingMutations — envio em lote idempotente (TASK-019)", () => {
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

  describe("idempotência — reenvio do mesmo lote não duplica no servidor mock", () => {
    it("servidor mock, isolado: sendBatch chamado duas vezes com o mesmo lote registra cada mutação uma única vez", async () => {
      const db = open(`estudobiblico-sync-idempotent-server-${crypto.randomUUID()}`);
      await db.open();
      const server = new InMemorySyncServer();

      const record = await enqueueMutation(db, {
        entityType: "progress",
        entityId: "plan-1:1",
        operation: "create",
        payload: { dayNumber: 1 },
        baseRev: null,
      });

      const first = await server.sendBatch([record]);
      const second = await server.sendBatch([record]);

      expect(first.outcomes.get(record.id)).toBe("accepted");
      expect(second.outcomes.get(record.id)).toBe("duplicate");
      expect(server.receivedCount()).toBe(1);
      expect(server.receivedMutations()).toHaveLength(1);
    });

    it("fluxo completo: reenviar o mesmo lote depois que o servidor já o recebeu (confirmação local perdida) não duplica", async () => {
      // Simula o cenário citado em ADR-007: o servidor recebeu a mutação, mas
      // por alguma falha o cliente nunca chegou a marcá-la "sent" localmente
      // (ex.: aba fechada entre a resposta de rede e a escrita no Dexie) — a
      // mutação continua "pending" e é reenviada no lote seguinte.
      const db = open(`estudobiblico-sync-idempotent-flow-${crypto.randomUUID()}`);
      await db.open();
      const server = new InMemorySyncServer();

      const record = await enqueueMutation(db, {
        entityType: "progress",
        entityId: "plan-1:1",
        operation: "create",
        payload: { dayNumber: 1 },
        baseRev: null,
      });

      // 1) Servidor já recebeu a mutação (chamada direta, fora do fluxo local
      //    normal) — mas a outbox local continua "pending" (confirmação "perdida").
      await server.sendBatch([record]);
      expect((await db.outbox.get(record.id))?.status).toBe("pending");

      // 2) Reenvio do mesmo lote pelo caminho real (`sendPendingMutations`,
      //    rede e sessão presentes).
      const result = await sendPendingMutations(db, server, {
        isOnline: () => true,
        hasSession: () => true,
      });

      expect(result.sent).toEqual([record.id]);
      expect(result.rejected).toEqual([]);
      expect((await db.outbox.get(record.id))?.status).toBe("sent");
      // Prova central do critério de aceite: mesmo reenviado, o servidor
      // continua com uma única mutação registrada, nunca duas.
      expect(server.receivedCount()).toBe(1);
    });

    it("fluxo completo: duas chamadas consecutivas de sendPendingMutations com o mesmo lote pendente não duplicam no servidor", async () => {
      const db = open(`estudobiblico-sync-idempotent-consecutive-${crypto.randomUUID()}`);
      await db.open();
      const server = new InMemorySyncServer();

      const first = await enqueueMutation(db, {
        entityType: "progress",
        entityId: "plan-1:1",
        operation: "create",
        payload: { dayNumber: 1 },
        baseRev: null,
      });
      const second = await enqueueMutation(db, {
        entityType: "preference",
        entityId: "theme",
        operation: "update",
        payload: { value: "dark" },
        baseRev: 1,
      });

      const gate = { isOnline: () => true, hasSession: () => true };

      const firstCall = await sendPendingMutations(db, server, gate);
      expect([...firstCall.sent].sort()).toEqual([first.id, second.id].sort());
      expect(server.receivedCount()).toBe(2);

      // Reenvio do "mesmo lote": nada mudou na outbox (já "sent"), então o
      // segundo envio não encontra mutação pendente para reenviar — outra
      // forma pela qual o sistema garante que o servidor nunca vê duplicata.
      const secondCall = await sendPendingMutations(db, server, gate);
      expect(secondCall.attempted).toBe(0);
      expect(secondCall.sent).toEqual([]);
      expect(server.receivedCount()).toBe(2);
    });
  });

  describe("gate de rede/sessão", () => {
    it("não envia quando não há rede — client.sendBatch nunca é chamado", async () => {
      const db = open(`estudobiblico-sync-gate-offline-${crypto.randomUUID()}`);
      await db.open();
      const server = new InMemorySyncServer();
      const sendBatchSpy = vi.spyOn(server, "sendBatch");

      await enqueueMutation(db, {
        entityType: "progress",
        entityId: "plan-1:1",
        operation: "create",
        payload: { dayNumber: 1 },
        baseRev: null,
      });

      const result = await sendPendingMutations(db, server, {
        isOnline: () => false,
        hasSession: () => true,
      });

      expect(result).toEqual({ attempted: 0, sent: [], rejected: [], skippedReason: "offline" });
      expect(sendBatchSpy).not.toHaveBeenCalled();
      expect(await db.outbox.where("status").equals("pending").count()).toBe(1);
    });

    it("não envia quando não há sessão — client.sendBatch nunca é chamado", async () => {
      const db = open(`estudobiblico-sync-gate-no-session-${crypto.randomUUID()}`);
      await db.open();
      const server = new InMemorySyncServer();
      const sendBatchSpy = vi.spyOn(server, "sendBatch");

      await enqueueMutation(db, {
        entityType: "preference",
        entityId: "theme",
        operation: "update",
        payload: { value: "dark" },
        baseRev: null,
      });

      const result = await sendPendingMutations(db, server, {
        isOnline: () => true,
        hasSession: () => false,
      });

      expect(result).toEqual({ attempted: 0, sent: [], rejected: [], skippedReason: "no-session" });
      expect(sendBatchSpy).not.toHaveBeenCalled();
      expect(await db.outbox.where("status").equals("pending").count()).toBe(1);
    });

    it("envia normalmente quando há rede e sessão", async () => {
      const db = open(`estudobiblico-sync-gate-ok-${crypto.randomUUID()}`);
      await db.open();
      const server = new InMemorySyncServer();

      const record = await enqueueMutation(db, {
        entityType: "progress",
        entityId: "plan-1:1",
        operation: "create",
        payload: { dayNumber: 1 },
        baseRev: null,
      });

      const result = await sendPendingMutations(db, server, {
        isOnline: () => true,
        hasSession: () => true,
      });

      expect(result.attempted).toBe(1);
      expect(result.sent).toEqual([record.id]);
      expect((await db.outbox.get(record.id))?.status).toBe("sent");
    });

    it("nenhuma mutação pendente: não chama o servidor e não falha", async () => {
      const db = open(`estudobiblico-sync-gate-empty-${crypto.randomUUID()}`);
      await db.open();
      const server = new InMemorySyncServer();
      const sendBatchSpy = vi.spyOn(server, "sendBatch");

      const result = await sendPendingMutations(db, server, {
        isOnline: () => true,
        hasSession: () => true,
      });

      expect(result).toEqual({ attempted: 0, sent: [], rejected: [] });
      expect(sendBatchSpy).not.toHaveBeenCalled();
    });
  });

  describe("mutação recusada pelo servidor", () => {
    it("mantém 'pending' e incrementa retryCount quando o servidor devolve 'rejected'", async () => {
      const db = open(`estudobiblico-sync-rejected-${crypto.randomUUID()}`);
      await db.open();

      const record = await enqueueMutation(db, {
        entityType: "progress",
        entityId: "plan-1:1",
        operation: "create",
        payload: { dayNumber: 1 },
        baseRev: null,
      });

      const rejectingClient = {
        sendBatch: vi.fn().mockResolvedValue({
          outcomes: new Map([[record.id, "rejected" as const]]),
        }),
      };

      const result = await sendPendingMutations(db, rejectingClient, {
        isOnline: () => true,
        hasSession: () => true,
      });

      expect(result.sent).toEqual([]);
      expect(result.rejected).toEqual([record.id]);

      const stored = await db.outbox.get(record.id);
      expect(stored?.status).toBe("pending");
      expect(stored?.retryCount).toBe(1);
    });
  });
});

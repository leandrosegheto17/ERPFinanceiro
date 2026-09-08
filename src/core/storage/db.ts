import Dexie, { type EntityTable } from "dexie";

import type {
  AnonProgressRecord,
  ContentBundleRecord,
  CorpusBundleRecord,
  CorpusChapterRecord,
  EventQueueRecord,
  OutboxRecord,
  PreferenceRecord,
  ProgressRecord,
} from "./types";

/**
 * Schema Dexie versionado das stores locais de Fase 1 (TASK-016, SDD.md §5.2).
 *
 * Fora de escopo aqui (Módulo 2 / Fase 2, ADR-012, DI-13, TASK-070):
 * `outlines`, `outlineSnapshots`, `presentationState`.
 *
 * DI-10: esta é a única camada de armazenamento local de conteúdo do
 * usuário — `localStorage` nunca é usado para isso.
 *
 * Histórico de versões:
 * - v1: stores de Fase 1 conforme SDD.md §5.2, sem `retryCount` em `outbox`.
 * - v2: adiciona `retryCount` a `outbox` (contagem de tentativas de envio,
 *   necessária para o envio em lote idempotente de TASK-019) e o índice
 *   composto `[status+createdAt]` (consulta eficiente "próximos pendentes,
 *   por ordem de criação"). A função `upgrade` back-preenche `retryCount: 0`
 *   em toda mutação já existente na outbox, para que a migração nunca perca
 *   nem invalide dado pendente de sincronização real do usuário.
 */
export class AppDatabase extends Dexie {
  corpusChapters!: EntityTable<CorpusChapterRecord, "bookId" | "chapter">;
  corpusBundle!: EntityTable<CorpusBundleRecord, "id">;
  contentBundle!: EntityTable<ContentBundleRecord, "id">;
  anonProgress!: EntityTable<AnonProgressRecord, "id">;
  progress!: EntityTable<ProgressRecord, "id">;
  preferences!: EntityTable<PreferenceRecord, "key">;
  outbox!: EntityTable<OutboxRecord, "id">;
  eventQueue!: EntityTable<EventQueueRecord, "id">;

  constructor(name = "estudobiblico") {
    super(name);

    this.version(1).stores({
      corpusChapters: "[bookId+chapter], lastAccessedAt",
      corpusBundle: "id",
      contentBundle: "id",
      anonProgress: "++id, [planId+dayNumber]",
      progress: "++id, [planId+dayNumber], syncedAt",
      preferences: "key, updatedAt",
      outbox: "id, createdAt, status",
      eventQueue: "++id, occurredAt, sentAt",
    });

    this.version(2)
      .stores({
        outbox: "id, createdAt, status, [status+createdAt]",
      })
      .upgrade(async (tx) => {
        type OutboxRecordV1 = Omit<OutboxRecord, "retryCount"> & { retryCount?: number };
        await tx
          .table("outbox")
          .toCollection()
          .modify((record: OutboxRecordV1) => {
            if (record.retryCount === undefined) {
              record.retryCount = 0;
            }
          });
      });
  }
}

/** Instância singleton do banco local, para uso direto pelo app. */
export function createAppDatabase(name?: string): AppDatabase {
  return new AppDatabase(name);
}

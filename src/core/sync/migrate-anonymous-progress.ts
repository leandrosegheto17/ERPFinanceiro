import type { AppDatabase } from "../storage/db";
import type { AnonProgressRecord, ProgressRecord } from "../storage/types";
import { enqueueMutation } from "./enqueue-mutation";

/**
 * TASK-022 — `core/sync`: migração de progresso anônimo ao criar conta
 * (RN-07, SDD.md §5.2/§5.3, PRD-TECNICO.md).
 *
 * O usuário pode usar o plano sem conta: cada dia concluído fica em
 * `anonProgress` (`types.ts`: "dias concluídos sem conta; migra uma vez ao
 * criar conta"). Ao criar conta, RN-07 exige que esse progresso "apareça em
 * `plan_progress`" — a tabela do servidor (SDD.md §5.3). Esta função cobre só
 * a etapa **local** dessa migração, que é tudo que existe até o backend real
 * chegar (Lote 5/6): copia cada `AnonProgressRecord` para a store `progress`
 * (do titular, mesmo `planId`/`dayNumber`/`completedAt`) e enfileira a
 * mutação correspondente via `enqueueMutation` (TASK-018) para que o envio
 * real ao servidor ocorra quando `sendPendingMutations` (TASK-019) rodar
 * contra um backend existente — sem chamada de rede aqui (DI-01/DI-02: essa
 * é a única forma de progresso chegar ao servidor, e ela já existe).
 *
 * **Idempotência ("migra uma vez", RN-07):** se `anonProgress` já estiver
 * vazio (migração anterior já rodou, ou nunca houve progresso anônimo), a
 * função não faz nada — nunca duplica progresso já migrado. Ao final de uma
 * migração bem-sucedida, `anonProgress` é limpo, o que é o próprio mecanismo
 * de "uma vez": uma segunda chamada encontra a store vazia e é um no-op.
 */
export interface MigrateAnonymousProgressResult {
  /** Registros migrados de `anonProgress` para `progress` nesta chamada (vazio se já migrado). */
  readonly migrated: ReadonlyArray<Omit<ProgressRecord, "id">>;
}

/** Chave de entidade da mutação de progresso enfileirada — mesmo padrão usado pelos testes de TASK-018. */
function progressEntityId(record: Pick<AnonProgressRecord, "planId" | "dayNumber">): string {
  return `${record.planId}:${record.dayNumber}`;
}

/**
 * Migra todo progresso anônimo (`anonProgress`) para a store do titular
 * (`progress`), enfileirando uma mutação `create` por dia migrado. Recebe
 * `db` já aberto (mesmo padrão de `enqueueMutation`/`sendPendingMutations`) —
 * este módulo não gerencia o ciclo de vida da conexão Dexie.
 */
export async function migrateAnonymousProgress(db: AppDatabase): Promise<MigrateAnonymousProgressResult> {
  const anonRecords = await db.anonProgress.toArray();

  if (anonRecords.length === 0) {
    // Já migrado (ou nunca houve progresso anônimo): no-op, RN-07 "uma vez".
    return { migrated: [] };
  }

  const migrated: Array<Omit<ProgressRecord, "id">> = anonRecords.map((record) => ({
    planId: record.planId,
    dayNumber: record.dayNumber,
    completedAt: record.completedAt,
  }));

  await db.progress.bulkAdd(migrated);

  await Promise.all(
    migrated.map((record) =>
      enqueueMutation(db, {
        entityType: "progress",
        entityId: progressEntityId(record),
        operation: "create",
        payload: record,
        baseRev: null,
      }),
    ),
  );

  // "Migra uma vez": limpa `anonProgress` só depois de gravar em `progress` e
  // enfileirar as mutações — se algo acima falhar, o progresso anônimo
  // permanece intacto para uma nova tentativa, em vez de ser perdido.
  await db.anonProgress.clear();

  return { migrated };
}

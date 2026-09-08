import type { AppDatabase } from "../storage/db";
import type { OutboxRecord } from "../storage/types";

/**
 * TASK-018 — `core/sync`: outbox local (ADR-007, SDD.md §5.2/§5.4).
 *
 * "Escreve local primeiro; enfileira mutação com id próprio" (diagrama de
 * sequência de SDD.md, linhas ~240-247): a UI grava a mutação do usuário na
 * store local correspondente e, **no mesmo passo, sem depender de rede nem de
 * sessão**, enfileira essa mutação na store `outbox` (TASK-016) para
 * conciliação posterior (TASK-019 envia; TASK-020 concilia por entidade).
 *
 * `enqueueMutation` cobre só a etapa de enfileiramento: gera um `id` próprio
 * para a mutação (não reaproveita nenhum id de entidade — o mesmo registro de
 * uma entidade pode gerar várias mutações em sequência, cada uma com seu
 * próprio id de outbox) e grava com o `baseRev` conhecido no momento da
 * mutação (revisão-base para detecção de conflito em TASK-020; `null` quando
 * a entidade ainda não tem revisão conhecida, ex.: criação).
 *
 * Não há chamada de rede aqui — a função só escreve na store local `outbox`
 * via Dexie/IndexedDB (`AppDatabase`, TASK-016). Essa ausência estrutural de
 * I/O de rede é o que torna a mutação persistível "sem rede" (critério de
 * aceite desta tarefa); o envio de fato (gate de rede/sessão) é TASK-019.
 */
export interface EnqueueMutationInput {
  readonly entityType: string;
  readonly entityId: string;
  readonly operation: string;
  readonly payload: unknown;
  /** Revisão-base conhecida no momento da mutação; `null` se desconhecida (ex.: criação). */
  readonly baseRev: number | null;
}

/**
 * Gera um `id` próprio para a mutação (`crypto.randomUUID()`, disponível em
 * todo runtime alvo deste projeto — navegador e Node ≥ 19 usado pelos testes)
 * e grava o registro completo na store `outbox` do banco informado.
 *
 * Recebe `db` já aberto (`AppDatabase`, TASK-016) em vez de gerenciar uma
 * instância própria — este módulo não é dono do ciclo de vida da conexão
 * Dexie, só da regra de como uma mutação é enfileirada.
 */
export async function enqueueMutation(
  db: AppDatabase,
  input: EnqueueMutationInput,
): Promise<OutboxRecord> {
  const record: OutboxRecord = {
    id: crypto.randomUUID(),
    entityType: input.entityType,
    entityId: input.entityId,
    operation: input.operation,
    payload: input.payload,
    baseRev: input.baseRev,
    createdAt: new Date().toISOString(),
    status: "pending",
    retryCount: 0,
  };

  await db.outbox.add(record);

  return record;
}

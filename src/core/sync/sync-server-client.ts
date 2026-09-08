import type { OutboxRecord } from "../storage/types";

/**
 * TASK-019 — `core/sync`: porta de envio em lote (ADR-007, SDD.md §5.4).
 *
 * `SyncServerClient` é a interface injetável que representa "o servidor" do
 * diagrama de sequência (SDD.md, linhas ~240-247, nó "Envia lote idempotente").
 * Não há backend real ainda — Supabase só chega no Lote 5/Lote 6 (autenticação
 * real, `core/auth`) — então esta é a única forma de implementar o envio de
 * forma testável hoje: `core/sync` depende só desta porta, nunca de um SDK/
 * cliente HTTP concreto. Quando o backend real existir, um adaptador concreto
 * implementa esta mesma interface sem `sendPendingMutations` mudar.
 *
 * A prova central do critério de aceite desta tarefa ("reenvio de lote não
 * duplica no servidor mock") depende do **contrato de idempotência por `id`**
 * descrito aqui: um servidor real, tal como o mock usado nos testes, precisa
 * reconhecer um `id` de mutação já recebido e devolver `"duplicate"` em vez de
 * processá-lo de novo — nunca reprocessar o efeito duas vezes.
 */

/** Resultado do envio de uma mutação individual dentro do lote. */
export type MutationOutcome =
  /** Servidor recebeu e processou o efeito da mutação agora, pela primeira vez. */
  | "accepted"
  /** Servidor já tinha recebido este `id` antes; nenhum efeito novo aplicado (idempotência). */
  | "duplicate"
  /** Servidor recusou a mutação (ex.: payload inválido); elegível a nova tentativa futura. */
  | "rejected";

/** Resultado do envio de um lote inteiro, um outcome por `id` de mutação enviado. */
export interface SendBatchResult {
  readonly outcomes: ReadonlyMap<string, MutationOutcome>;
}

/**
 * Porta de envio de lote. Implementações reais (Lote 5/6) fazem a chamada de
 * rede de fato; `InMemorySyncServer` (`in-memory-sync-server.ts`) é o dublê
 * usado em teste, mas qualquer implementação desta interface serve.
 */
export interface SyncServerClient {
  sendBatch(mutations: readonly OutboxRecord[]): Promise<SendBatchResult>;
}

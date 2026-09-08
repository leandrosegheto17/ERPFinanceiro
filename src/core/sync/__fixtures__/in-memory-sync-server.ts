import type { OutboxRecord } from "../../storage/types";
import type { MutationOutcome, SendBatchResult, SyncServerClient } from "../sync-server-client";

/**
 * TASK-019 — dublê de servidor em memória, usado só em teste
 * (`send-pending-mutations.test.ts`). Não é código de produção: como não há
 * backend real ainda (Supabase chega no Lote 5/6), este é o "servidor mock"
 * citado literalmente no critério de aceite da tarefa ("Reenvio de lote não
 * duplica no servidor mock").
 *
 * Simula exatamente a garantia que um servidor real precisa oferecer sob
 * ADR-007 ("Idempotência por chave de mutação"): registra cada mutação pelo
 * seu `id` próprio (gerado no cliente por TASK-018, nunca pelo servidor) e,
 * ao receber um `id` já conhecido, devolve `"duplicate"` sem aplicar o efeito
 * de novo — nunca duplica o registro internamente.
 */
export class InMemorySyncServer implements SyncServerClient {
  private readonly receivedById = new Map<string, OutboxRecord>();

  sendBatch(mutations: readonly OutboxRecord[]): Promise<SendBatchResult> {
    const outcomes = new Map<string, MutationOutcome>();

    for (const mutation of mutations) {
      if (this.receivedById.has(mutation.id)) {
        outcomes.set(mutation.id, "duplicate");
        continue;
      }
      this.receivedById.set(mutation.id, mutation);
      outcomes.set(mutation.id, "accepted");
    }

    return Promise.resolve({ outcomes });
  }

  /** Quantas mutações **distintas** o servidor de fato registrou — nunca conta duplicata. */
  receivedCount(): number {
    return this.receivedById.size;
  }

  /** Mutações efetivamente registradas, para inspeção fina em teste. */
  receivedMutations(): readonly OutboxRecord[] {
    return [...this.receivedById.values()];
  }
}

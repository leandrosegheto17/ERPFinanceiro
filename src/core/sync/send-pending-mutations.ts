import type { AppDatabase } from "../storage/db";
import type { OutboxRecord } from "../storage/types";
import type { SyncServerClient } from "./sync-server-client";

/**
 * TASK-019 — `core/sync`: envio em lote idempotente com gate de rede/sessão
 * (ADR-007, SDD.md §5.4, diagrama de sequência linhas ~240-247, nó
 * `outbox --> gate{"Tem rede e sessao?"}`).
 *
 * Cobre a etapa "Envia lote idempotente" do diagrama: lê as mutações
 * `pending` da outbox local (TASK-016/018), só as envia se houver rede *e*
 * sessão, envia via `SyncServerClient` injetável (nenhuma chamada de rede
 * concreta aqui — `core/*` não conhece `fetch`/SDK, DI-02), e marca como
 * `"sent"` toda mutação que o servidor confirma ter recebido — seja agora
 * (`"accepted"`) seja antes, num reenvio (`"duplicate"`). É esse tratamento
 * de `"duplicate"` como sucesso local que resolve o critério de aceite: um
 * reenvio do mesmo lote (mesmos `id`s) nunca duplica o efeito no servidor
 * *nem* deixa a mutação presa como pendente para sempre, porque o servidor já
 * a tinha.
 *
 * **Gate de sessão (documentação da lacuna, DI-01):** não existe
 * autenticação real neste projeto ainda — `core/auth`/Supabase só chegam no
 * Lote 6. `hasSession` é, por isso, um parâmetro simples e explícito (função
 * síncrona devolvendo presença/ausência de sessão), não uma integração real.
 * Quando `core/auth` existir, o chamador passa `() => authStore.hasSession()`
 * (ou equivalente) sem este módulo mudar.
 */

/** Função injetável de status de rede, para tornar o gate testável sem navegador real. */
export type NetworkStatusReader = () => boolean;

/**
 * Leitor padrão de rede via `navigator.onLine`. Ambiente sem `navigator`
 * (Node/teste sem override, SSR) devolve `true` — mesma postura de
 * `estimateStorageUsage` (TASK-017): na ausência de informação real, nunca
 * assume o cenário mais restritivo "no escuro"; quem precisa testar o
 * caminho offline passa `isOnline` explicitamente.
 */
export const defaultIsOnline: NetworkStatusReader = () => {
  const globalNavigator = (globalThis as { navigator?: { onLine?: boolean } }).navigator;
  return globalNavigator?.onLine ?? true;
};

export interface SendPendingMutationsOptions {
  /** Leitor de rede; padrão `defaultIsOnline` (`navigator.onLine`, com fallback `true`). */
  readonly isOnline?: NetworkStatusReader;
  /**
   * Presença de sessão — sem `core/auth` real ainda (Lote 6), é responsabilidade
   * do chamador informar. Obrigatório (sem valor padrão): não há sessão "por
   * omissão" segura de assumir.
   */
  readonly hasSession: () => boolean;
}

export type SendPendingMutationsSkippedReason = "offline" | "no-session";

export interface SendPendingMutationsResult {
  /** Quantas mutações `pending` foram de fato submetidas ao `client.sendBatch`. */
  readonly attempted: number;
  /** `id`s marcados `"sent"` nesta chamada (aceitos ou já duplicados no servidor). */
  readonly sent: readonly string[];
  /** `id`s que o servidor recusou (`"rejected"`); permanecem `pending`, `retryCount` incrementado. */
  readonly rejected: readonly string[];
  /** Presente só quando o gate impediu o envio — nenhuma tentativa de rede ocorreu. */
  readonly skippedReason?: SendPendingMutationsSkippedReason;
}

/**
 * Envia em lote toda mutação `pending` da outbox, respeitando o gate de
 * rede/sessão. Recebe `db` já aberto (mesmo padrão de `enqueueMutation`,
 * TASK-018) — este módulo também não gerencia o ciclo de vida da conexão
 * Dexie.
 */
export async function sendPendingMutations(
  db: AppDatabase,
  client: SyncServerClient,
  options: SendPendingMutationsOptions,
): Promise<SendPendingMutationsResult> {
  const isOnline = options.isOnline ?? defaultIsOnline;

  if (!isOnline()) {
    return { attempted: 0, sent: [], rejected: [], skippedReason: "offline" };
  }
  if (!options.hasSession()) {
    return { attempted: 0, sent: [], rejected: [], skippedReason: "no-session" };
  }

  const pending = await db.outbox.where("status").equals("pending").sortBy("createdAt");
  if (pending.length === 0) {
    return { attempted: 0, sent: [], rejected: [] };
  }

  const { outcomes } = await client.sendBatch(pending);

  const sent: string[] = [];
  const rejected: string[] = [];

  await Promise.all(
    pending.map(async (mutation: OutboxRecord) => {
      const outcome = outcomes.get(mutation.id);
      if (outcome === "accepted" || outcome === "duplicate") {
        await db.outbox.update(mutation.id, { status: "sent" });
        sent.push(mutation.id);
        return;
      }
      // "rejected" ou ausente na resposta (servidor não se pronunciou sobre
      // este id): mantém `pending` para nova tentativa futura, incrementando
      // `retryCount` (campo introduzido em TASK-016 justamente para isto).
      await db.outbox.update(mutation.id, { retryCount: mutation.retryCount + 1 });
      rejected.push(mutation.id);
    }),
  );

  return { attempted: pending.length, sent, rejected };
}

import type { ErasureRequestGateway } from "./erasure-request-gateway";
import type { RequestAccountErasureInput, RequestAccountErasureResult } from "./types";

/**
 * TASK-040 — grava uma linha em `public.erasure_request` (migration de
 * TASK-032, RN-11: exclusão em até 15 dias). Só envia `user_id` no corpo do
 * INSERT — `due_at` é **sempre** calculado pelo trigger `before insert`
 * `set_erasure_request_due_at` no servidor (`requested_at + 15 dias`); o
 * cliente nunca tenta calcular/enviar `due_at`, mesmo que o SDK permitisse
 * — o trigger sobrescreveria de qualquer forma, mas o corpo do INSERT nem
 * chega a mandar o campo, para não sugerir (nem em código) que o cliente
 * tem alguma autoridade sobre essa data (RN-11 é regra do servidor, não do
 * cliente).
 *
 * `select("requested_at, due_at").single()` encadeado no mesmo INSERT
 * devolve a data-limite real gravada pelo servidor, para
 * `BlocoExcluirConta` exibir o estado "exclusão solicitada" com a data-
 * limite visível (UX-SPEC T-13) sem uma segunda ida ao banco.
 *
 * `unique(user_id)` na migration faz um segundo pedido para a mesma conta
 * falhar (violação de unicidade) — tratado aqui como o mesmo balde de erro
 * genérico; `BlocoExcluirConta` evita esse caminho na prática consultando
 * `getErasureRequestStatus` antes de mostrar o formulário.
 */
export async function requestAccountErasure(
  gateway: ErasureRequestGateway,
  input: RequestAccountErasureInput,
): Promise<RequestAccountErasureResult> {
  const { data, error } = await gateway
    .from("erasure_request")
    .insert({ user_id: input.userId })
    .select("requested_at, due_at")
    .single();

  if (error || !data) {
    return { status: "error", message: error?.message ?? "Resposta vazia do servidor." };
  }

  return {
    status: "requested",
    requestedAt: (data as { requested_at: string }).requested_at,
    dueAt: (data as { due_at: string }).due_at,
  };
}

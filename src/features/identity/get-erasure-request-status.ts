import type { ErasureRequestGateway } from "./erasure-request-gateway";
import type { GetErasureRequestStatusResult } from "./types";

/**
 * TASK-040 — lê o pedido de exclusão do titular autenticado, se existir
 * (política `erasure_request_select_own`, RLS de TASK-032: só a própria
 * linha é visível). Usado por `BlocoExcluirConta` ao montar, para o estado
 * "exclusão solicitada" continuar visível depois de um recarregamento de
 * página (UX-SPEC T-13: "Após confirmar, a conta entra em estado 'exclusão
 * solicitada' com a data-limite visível") — sem essa checagem, a tela
 * perderia a data-limite ao atualizar e só descobriria que o pedido já
 * existe ao tentar enviar de novo e colidir com `unique(user_id)`.
 */
export async function getErasureRequestStatus(
  gateway: ErasureRequestGateway,
  userId: string,
): Promise<GetErasureRequestStatusResult> {
  const { data, error } = await gateway
    .from("erasure_request")
    .select("requested_at, due_at")
    .eq("user_id", userId)
    .maybeSingle();

  if (error) {
    return { status: "error", message: error.message };
  }

  if (!data) {
    return { status: "none" };
  }

  return {
    status: "requested",
    requestedAt: (data as { requested_at: string }).requested_at,
    dueAt: (data as { due_at: string }).due_at,
  };
}

import type { AuthError } from "@supabase/supabase-js";

import type { SignInErrorReason } from "./sign-in-types";

/**
 * TASK-037 — classificação **deliberadamente grosseira** de erro do
 * Supabase Auth para os fluxos desta tela. Só reconhece dois motivos que
 * não têm nenhuma relação com a existência da conta — limite de
 * requisições (`rate-limited`, HTTP 429) e falha de rede/transporte
 * (`network-error`, erro sem `status` HTTP, típico de `fetch` falhando
 * antes de qualquer resposta do servidor) — e agrupa **todo o resto** em
 * `"invalid-credentials"`, sem inspecionar o texto da mensagem para tentar
 * diferenciar "senha incorreta" de "e-mail não encontrado".
 *
 * Isso vale mesmo que o Supabase Auth já devolva, na prática, a mesma
 * mensagem genérica ("Invalid login credentials") para os dois casos: não
 * confiamos nesse comportamento como contrato — se uma versão futura da
 * API passar a diferenciar a mensagem, esta função continua colapsando os
 * dois casos no mesmo balde, porque a normalização acontece aqui, no
 * cliente, e não depende do texto vindo do servidor (guardrail da
 * tarefa).
 */
export function classifyAuthError(error: AuthError): SignInErrorReason {
  if (error.status === 429) {
    return "rate-limited";
  }
  if (isNetworkError(error)) {
    return "network-error";
  }
  return "invalid-credentials";
}

function isNetworkError(error: AuthError): boolean {
  if (typeof error.status !== "number") {
    return true;
  }
  const message = error.message.toLowerCase();
  return message.includes("fetch") || message.includes("network");
}

import { classifyAuthError } from "./classify-auth-error";
import type { SignInGateway } from "./sign-in-gateway";
import type { PasswordlessRequestResult } from "./sign-in-types";

/**
 * TASK-037 — Envio de link mágico (alternativa a senha, UX-SPEC T-09).
 *
 * `shouldCreateUser: false` — link mágico é um método de **entrada**,
 * nunca de cadastro implícito; criar conta é o fluxo dedicado da T-08
 * (TASK-036). Isso faz o Supabase recusar o pedido com um erro quando o
 * e-mail não tem conta ainda — sinal que, sozinho, revelaria a existência
 * da conta. Por isso este wrapper colapsa deliberadamente esse erro (e
 * qualquer outro que não seja de transporte/limite) no mesmo
 * `"request-sent"` do caso de sucesso: a UI mostra a mesma confirmação
 * ("se esse e-mail existir, você recebe um link") nos dois casos,
 * satisfazendo o critério de aceite desta tarefa.
 */
export async function signInWithMagicLink(
  gateway: SignInGateway,
  email: string,
): Promise<PasswordlessRequestResult> {
  const { error } = await gateway.signInWithOtp({
    email,
    options: { shouldCreateUser: false },
  });

  if (!error) {
    return { status: "request-sent" };
  }

  const reason = classifyAuthError(error);
  if (reason === "network-error" || reason === "rate-limited") {
    return { status: "error", reason };
  }

  // `reason === "invalid-credentials"` aqui cobre, entre outras coisas, o
  // e-mail sem conta associada (com `shouldCreateUser: false`) — colapsado
  // deliberadamente em sucesso aparente, nunca em erro visível na UI.
  return { status: "request-sent" };
}

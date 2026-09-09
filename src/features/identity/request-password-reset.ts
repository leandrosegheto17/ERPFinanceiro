import { classifyAuthError } from "./classify-auth-error";
import type { SignInGateway } from "./sign-in-gateway";
import type { PasswordlessRequestResult } from "./sign-in-types";

/**
 * TASK-037 — Recuperação de senha (UX-SPEC T-09).
 *
 * O próprio Supabase Auth já é desenhado para não revelar, na resposta de
 * `resetPasswordForEmail`, se o e-mail tem conta (retorna sucesso nos dois
 * casos) — mas este wrapper não depende desse comportamento como
 * contrato: qualquer erro que não seja de transporte/limite é tratado
 * exatamente como sucesso (`"request-sent"`), a mesma normalização
 * defensiva de `sign-in-with-magic-link.ts`. Se uma versão futura da API
 * passar a diferenciar a resposta, o cliente continua colapsando os dois
 * casos.
 */
export async function requestPasswordReset(
  gateway: SignInGateway,
  email: string,
  options?: { readonly redirectTo?: string },
): Promise<PasswordlessRequestResult> {
  const { error } = await gateway.resetPasswordForEmail(email, {
    redirectTo: options?.redirectTo,
  });

  if (!error) {
    return { status: "request-sent" };
  }

  const reason = classifyAuthError(error);
  if (reason === "network-error" || reason === "rate-limited") {
    return { status: "error", reason };
  }

  return { status: "request-sent" };
}

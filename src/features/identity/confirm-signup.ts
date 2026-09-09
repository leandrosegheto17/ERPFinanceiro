import type { AuthGateway } from "./auth-gateway";
import type { ConfirmSignUpInput, ConfirmSignUpResult } from "./types";

/**
 * TASK-035 — Completa a verificação de e-mail do cadastro: troca o
 * `token_hash` extraído do link de confirmação (enviado por
 * `signUpWithEmailPassword`; em dev/CI capturado pelo Inbucket/Mailpit do
 * Supabase local — "verificação mock" do critério de aceite, nunca envio
 * real) por uma sessão válida.
 */
export async function confirmSignUp(
  gateway: AuthGateway,
  input: ConfirmSignUpInput,
): Promise<ConfirmSignUpResult> {
  const { data, error } = await gateway.verifyOtp({
    type: "signup",
    token_hash: input.tokenHash,
  });

  if (error || !data.session || !data.user) {
    return {
      status: "error",
      message: error?.message ?? "Verificação de e-mail não retornou sessão válida.",
    };
  }

  return {
    status: "confirmed",
    userId: data.user.id,
    accessToken: data.session.access_token,
    refreshToken: data.session.refresh_token,
  };
}

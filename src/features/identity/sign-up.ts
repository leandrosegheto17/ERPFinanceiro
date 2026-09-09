import type { AuthError } from "@supabase/supabase-js";

import type { AuthGateway } from "./auth-gateway";
import type { SignUpErrorReason, SignUpInput, SignUpResult } from "./types";

/**
 * TASK-035 — Cadastro e-mail+senha via Supabase Auth (SDD.md §7.1).
 *
 * Encapsula `auth.signUp` sem expor nenhum segredo de serviço: `gateway` é
 * sempre construído pelo chamador a partir da `anon key` pública
 * (DI-09/GUARDRAILS G-10 — ver `create-identity-supabase-client.ts`); esta
 * função nunca recebe nem usa credencial de `service_role`.
 *
 * `input.metadata`, quando informado, vira `options.data` do `signUp` —
 * repassado sem interpretação (esta função não sabe, nem precisa saber, o
 * que `features/identity` grava com isso; ver `SignUpInput.metadata` e
 * `sign-up-with-consent.ts`, TASK-036).
 */
export async function signUpWithEmailPassword(
  gateway: AuthGateway,
  input: SignUpInput,
): Promise<SignUpResult> {
  const { data, error } = await gateway.signUp({
    email: input.email,
    password: input.password,
    ...(input.metadata ? { options: { data: input.metadata } } : {}),
  });

  if (error) {
    return { status: "error", reason: classifySignUpError(error), message: error.message };
  }

  if (!data.user) {
    return {
      status: "error",
      reason: "unknown",
      message: "Supabase Auth não retornou usuário nem erro para o cadastro.",
    };
  }

  if (data.session) {
    return {
      status: "signed-in",
      userId: data.user.id,
      accessToken: data.session.access_token,
      refreshToken: data.session.refresh_token,
    };
  }

  return { status: "confirmation-required", userId: data.user.id };
}

function classifySignUpError(error: AuthError): SignUpErrorReason {
  const message = error.message.toLowerCase();
  if (message.includes("already registered") || message.includes("already exists")) {
    return "email-already-registered";
  }
  if (message.includes("password")) {
    return "weak-password";
  }
  if (message.includes("email")) {
    return "invalid-email";
  }
  if (error.status === 429) {
    return "rate-limited";
  }
  return "unknown";
}

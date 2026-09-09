import { classifyAuthError } from "./classify-auth-error";
import type { SignInGateway } from "./sign-in-gateway";
import type { SignInWithPasswordInput, SignInWithPasswordResult } from "./sign-in-types";

/**
 * TASK-037 — Login por e-mail+senha (T-09, UX-SPEC).
 *
 * Guardrail central desta tarefa: a mensagem de erro nunca revela se o
 * e-mail existe na base. Por isso `SignInWithPasswordResult` só tem o
 * balde genérico `SignInErrorReason` (`classify-auth-error.ts`) — nenhum
 * ramo aqui inspeciona `error.message` tentando separar "e-mail não
 * encontrado" de "senha incorreta"; os dois caem no mesmo
 * `"invalid-credentials"`, e é a UI (`TelaEntrar.tsx`) quem decide a
 * única mensagem exibida para esse balde.
 */
export async function signInWithPassword(
  gateway: SignInGateway,
  input: SignInWithPasswordInput,
): Promise<SignInWithPasswordResult> {
  const { data, error } = await gateway.signInWithPassword({
    email: input.email,
    password: input.password,
  });

  if (error || !data.session || !data.user) {
    return { status: "error", reason: error ? classifyAuthError(error) : "invalid-credentials" };
  }

  return {
    status: "signed-in",
    userId: data.user.id,
    accessToken: data.session.access_token,
    refreshToken: data.session.refresh_token,
  };
}

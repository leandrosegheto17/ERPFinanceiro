import { describe, expect, it, vi } from "vitest";

import type { AuthGateway } from "./auth-gateway";
import { signUpWithConsent } from "./sign-up-with-consent";

type SignUpResponse = Awaited<ReturnType<AuthGateway["signUp"]>>;

const baseInput = {
  email: "leitor@example.test",
  password: "senha-forte-9f8e7d6c!",
  consentVersion: "2026-09-08.v1",
  consentTextSha256: "hash-do-texto",
};

/**
 * TASK-036 (correção da reprovação crítica do Validador, 2026-09-08 — ver
 * `.md/BLOCKERS.md` Entrada 2): a ordem "`consent` antes de qualquer dado
 * pessoal" deixou de depender de uma segunda chamada HTTP separada
 * (`recordConsent`/`ConsentGateway`, removidos) e passou a ser garantida
 * pelo gatilho de banco `handle_new_auth_user`
 * (`supabase/migrations/20260908190000_profile_on_signup_trigger.sql`), que
 * lê `consent_version`/`consent_text_sha256` da metadata do próprio
 * `signUp` (`options.data`) e insere `profile`+`consent` na mesma
 * transação que cria `auth.users`. Estes testes de unidade só confirmam
 * que `signUpWithConsent` repassa exatamente essa metadata para
 * `authGateway.signUp` — a garantia de atomicidade em si é responsabilidade
 * do banco e é coberta por `tests/db/rls-profile-consent.test.mjs`
 * (nível de integração, contra o Supabase local real).
 */
describe("signUpWithConsent (TASK-036)", () => {
  it("chama signUp uma única vez, com a metadata de consentimento em options.data", async () => {
    const signUp = vi.fn(async (): Promise<SignUpResponse> => ({
      data: { user: { id: "user-1" }, session: null },
      error: null,
    } as SignUpResponse));
    const authGateway: AuthGateway = {
      signUp,
      verifyOtp: async () => {
        throw new Error("não deveria ser chamado por signUpWithConsent");
      },
    };

    const result = await signUpWithConsent(authGateway, baseInput);

    expect(result).toEqual({ status: "confirmation-required", userId: "user-1" });
    expect(signUp).toHaveBeenCalledTimes(1);
    expect(signUp).toHaveBeenCalledWith({
      email: "leitor@example.test",
      password: "senha-forte-9f8e7d6c!",
      options: {
        data: {
          consent_version: "2026-09-08.v1",
          consent_text_sha256: "hash-do-texto",
        },
      },
    });
  });

  it("propaga o erro classificado quando o signUp (com a metadata) falha", async () => {
    const authGateway: AuthGateway = {
      signUp: async () =>
        ({
          data: { user: null, session: null },
          error: { message: "User already registered", status: 422 },
        }) as SignUpResponse,
      verifyOtp: async () => {
        throw new Error("não deveria ser chamado");
      },
    };

    const result = await signUpWithConsent(authGateway, baseInput);

    expect(result).toEqual({
      status: "error",
      reason: "email-already-registered",
      message: "User already registered",
    });
  });

  it("propaga o resultado signed-in quando o signUp já devolve sessão (confirmação desligada)", async () => {
    const authGateway: AuthGateway = {
      signUp: async () =>
        ({
          data: {
            user: { id: "user-3" },
            session: { access_token: "at-3", refresh_token: "rt-3" },
          },
          error: null,
        }) as SignUpResponse,
      verifyOtp: async () => {
        throw new Error("não deveria ser chamado");
      },
    };

    const result = await signUpWithConsent(authGateway, baseInput);

    expect(result).toEqual({
      status: "signed-in",
      userId: "user-3",
      accessToken: "at-3",
      refreshToken: "rt-3",
    });
  });
});

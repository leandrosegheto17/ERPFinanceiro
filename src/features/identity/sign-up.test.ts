import { describe, expect, it, vi } from "vitest";

import type { AuthGateway } from "./auth-gateway";
import { signUpWithEmailPassword } from "./sign-up";

type SignUpResponse = Awaited<ReturnType<AuthGateway["signUp"]>>;

function gatewayReturning(response: SignUpResponse): AuthGateway {
  return {
    signUp: async () => response,
    // Não usado por signUpWithEmailPassword — presente só para satisfazer o
    // tipo `AuthGateway` (Pick<SupabaseClient["auth"], "signUp" | "verifyOtp">).
    verifyOtp: async () => {
      throw new Error("não deveria ser chamado por signUpWithEmailPassword");
    },
  };
}

describe("signUpWithEmailPassword (TASK-035)", () => {
  it("retorna confirmation-required quando o Supabase não devolve sessão (enable_confirmations = true)", async () => {
    const gateway = gatewayReturning({
      data: { user: { id: "user-1" }, session: null },
      error: null,
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "leitor@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(result).toEqual({ status: "confirmation-required", userId: "user-1" });
  });

  it("retorna signed-in quando o Supabase já devolve sessão (confirmação desligada)", async () => {
    const gateway = gatewayReturning({
      data: {
        user: { id: "user-2" },
        session: { access_token: "at-2", refresh_token: "rt-2" },
      },
      error: null,
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "leitor2@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(result).toEqual({
      status: "signed-in",
      userId: "user-2",
      accessToken: "at-2",
      refreshToken: "rt-2",
    });
  });

  it("classifica e-mail já cadastrado", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "User already registered", status: 422 },
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "duplicado@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(result).toEqual({
      status: "error",
      reason: "email-already-registered",
      message: "User already registered",
    });
  });

  it("classifica senha fraca", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Password should be at least 6 characters", status: 422 },
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "leitor3@example.test",
      password: "123",
    });

    expect(result).toEqual({
      status: "error",
      reason: "weak-password",
      message: "Password should be at least 6 characters",
    });
  });

  it("classifica e-mail inválido", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Unable to validate email address: invalid format", status: 400 },
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "nao-e-um-email",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(result).toEqual({
      status: "error",
      reason: "invalid-email",
      message: "Unable to validate email address: invalid format",
    });
  });

  it("classifica limite de tentativas (HTTP 429) como rate-limited", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Too many requests", status: 429 },
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "leitor4@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(result).toEqual({
      status: "error",
      reason: "rate-limited",
      message: "Too many requests",
    });
  });

  it("classifica erro não reconhecido como unknown, sem lançar exceção", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Something unexpected happened", status: 500 },
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "leitor5@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(result).toEqual({
      status: "error",
      reason: "unknown",
      message: "Something unexpected happened",
    });
  });

  it("repassa metadata como options.data ao signUp, quando informada (mecanismo usado por TASK-036 para consent)", async () => {
    const signUp = vi.fn(async () => ({
      data: { user: { id: "user-7" }, session: null },
      error: null,
    })) as unknown as AuthGateway["signUp"];
    const gateway: AuthGateway = {
      signUp,
      verifyOtp: async () => {
        throw new Error("não deveria ser chamado por signUpWithEmailPassword");
      },
    };

    await signUpWithEmailPassword(gateway, {
      email: "leitor7@example.test",
      password: "senha-forte-9f8e7d6c!",
      metadata: { consent_version: "2026-09-08.v1", consent_text_sha256: "hash" },
    });

    expect(signUp).toHaveBeenCalledWith({
      email: "leitor7@example.test",
      password: "senha-forte-9f8e7d6c!",
      options: { data: { consent_version: "2026-09-08.v1", consent_text_sha256: "hash" } },
    });
  });

  it("não envia options quando metadata não é informada (contrato inalterado para o caso comum)", async () => {
    const signUp = vi.fn(async () => ({
      data: { user: { id: "user-8" }, session: null },
      error: null,
    })) as unknown as AuthGateway["signUp"];
    const gateway: AuthGateway = {
      signUp,
      verifyOtp: async () => {
        throw new Error("não deveria ser chamado por signUpWithEmailPassword");
      },
    };

    await signUpWithEmailPassword(gateway, {
      email: "leitor8@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(signUp).toHaveBeenCalledWith({
      email: "leitor8@example.test",
      password: "senha-forte-9f8e7d6c!",
    });
  });

  it("retorna erro unknown quando o Supabase não devolve usuário nem erro (contrato inesperado)", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: null,
    } as SignUpResponse);

    const result = await signUpWithEmailPassword(gateway, {
      email: "leitor6@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(result.status).toBe("error");
    if (result.status === "error") {
      expect(result.reason).toBe("unknown");
    }
  });
});

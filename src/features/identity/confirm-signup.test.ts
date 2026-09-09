import { describe, expect, it } from "vitest";

import type { AuthGateway } from "./auth-gateway";
import { confirmSignUp } from "./confirm-signup";

type VerifyOtpResponse = Awaited<ReturnType<AuthGateway["verifyOtp"]>>;

function gatewayReturning(response: VerifyOtpResponse): AuthGateway {
  return {
    signUp: async () => {
      throw new Error("não deveria ser chamado por confirmSignUp");
    },
    verifyOtp: async () => response,
  };
}

describe("confirmSignUp (TASK-035)", () => {
  it("retorna confirmed com a sessão trocada a partir do token_hash do link de verificação", async () => {
    const gateway = gatewayReturning({
      data: {
        user: { id: "user-1" },
        session: { access_token: "at-1", refresh_token: "rt-1" },
      },
      error: null,
    } as VerifyOtpResponse);

    const result = await confirmSignUp(gateway, { tokenHash: "hash-valido" });

    expect(result).toEqual({
      status: "confirmed",
      userId: "user-1",
      accessToken: "at-1",
      refreshToken: "rt-1",
    });
  });

  it("retorna erro quando o token está expirado/inválido", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Email link is invalid or has expired", status: 403 },
    } as VerifyOtpResponse);

    const result = await confirmSignUp(gateway, { tokenHash: "hash-expirado" });

    expect(result).toEqual({
      status: "error",
      message: "Email link is invalid or has expired",
    });
  });

  it("retorna erro (sem lançar exceção) quando não há erro explícito mas também não há sessão", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: null,
    } as VerifyOtpResponse);

    const result = await confirmSignUp(gateway, { tokenHash: "hash-qualquer" });

    expect(result.status).toBe("error");
  });
});

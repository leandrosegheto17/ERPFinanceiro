import { describe, expect, it } from "vitest";

import type { SignInGateway } from "./sign-in-gateway";
import { requestPasswordReset } from "./request-password-reset";

type ResetResponse = Awaited<ReturnType<SignInGateway["resetPasswordForEmail"]>>;

function gatewayReturning(response: ResetResponse): SignInGateway {
  return {
    signInWithPassword: async () => {
      throw new Error("não deveria ser chamado por requestPasswordReset");
    },
    signInWithOtp: async () => {
      throw new Error("não deveria ser chamado por requestPasswordReset");
    },
    resetPasswordForEmail: async () => response,
  };
}

describe("requestPasswordReset (TASK-037)", () => {
  it("e-mail com conta existente: request-sent", async () => {
    const gateway = gatewayReturning({ data: {}, error: null } as unknown as ResetResponse);
    const result = await requestPasswordReset(gateway, "conta-existente@example.test");
    expect(result).toEqual({ status: "request-sent" });
  });

  it("critério de aceite — e-mail sem conta: mesmo request-sent que o caso de conta existente (Supabase não diferencia, e o cliente não confia nisso mesmo assim)", async () => {
    const gateway = gatewayReturning({
      data: {},
      error: { message: "Unable to find user", status: 400 },
    } as unknown as ResetResponse);
    const result = await requestPasswordReset(gateway, "sem-conta@example.test");
    expect(result).toEqual({ status: "request-sent" });
  });

  it("limite de tentativas continua visível como erro", async () => {
    const gateway = gatewayReturning({
      data: {},
      error: { message: "Too many requests", status: 429 },
    } as unknown as ResetResponse);
    const result = await requestPasswordReset(gateway, "leitor@example.test");
    expect(result).toEqual({ status: "error", reason: "rate-limited" });
  });

  it("falha de rede continua visível como erro", async () => {
    const gateway = gatewayReturning({
      data: {},
      error: { message: "Failed to fetch" },
    } as unknown as ResetResponse);
    const result = await requestPasswordReset(gateway, "leitor2@example.test");
    expect(result).toEqual({ status: "error", reason: "network-error" });
  });
});

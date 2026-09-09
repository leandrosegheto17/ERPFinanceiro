import { describe, expect, it } from "vitest";

import type { SignInGateway } from "./sign-in-gateway";
import { signInWithMagicLink } from "./sign-in-with-magic-link";

type OtpResponse = Awaited<ReturnType<SignInGateway["signInWithOtp"]>>;

function gatewayReturning(response: OtpResponse): SignInGateway {
  return {
    signInWithPassword: async () => {
      throw new Error("não deveria ser chamado por signInWithMagicLink");
    },
    signInWithOtp: async (credentials) => {
      // Confirma que shouldCreateUser: false é sempre enviado (link mágico
      // nunca cria conta implicitamente — isso é escopo de T-08).
      expect((credentials as { options?: { shouldCreateUser?: boolean } }).options).toEqual({
        shouldCreateUser: false,
      });
      return response;
    },
    resetPasswordForEmail: async () => {
      throw new Error("não deveria ser chamado por signInWithMagicLink");
    },
  };
}

describe("signInWithMagicLink (TASK-037)", () => {
  it("e-mail com conta existente: request-sent", async () => {
    const gateway = gatewayReturning({ data: {}, error: null } as OtpResponse);
    const result = await signInWithMagicLink(gateway, "conta-existente@example.test");
    expect(result).toEqual({ status: "request-sent" });
  });

  it("critério de aceite — e-mail sem conta (shouldCreateUser:false rejeita): mesmo request-sent, nunca erro visível", async () => {
    const gateway = gatewayReturning({
      data: {},
      error: { message: "Signups not allowed for otp", status: 400 },
    } as OtpResponse);
    const result = await signInWithMagicLink(gateway, "sem-conta@example.test");
    expect(result).toEqual({ status: "request-sent" });
  });

  it("limite de tentativas continua visível como erro (não é sobre existência de conta)", async () => {
    const gateway = gatewayReturning({
      data: {},
      error: { message: "Too many requests", status: 429 },
    } as OtpResponse);
    const result = await signInWithMagicLink(gateway, "leitor@example.test");
    expect(result).toEqual({ status: "error", reason: "rate-limited" });
  });

  it("falha de rede continua visível como erro", async () => {
    const gateway = gatewayReturning({
      data: {},
      error: { message: "Failed to fetch" },
    } as OtpResponse);
    const result = await signInWithMagicLink(gateway, "leitor2@example.test");
    expect(result).toEqual({ status: "error", reason: "network-error" });
  });
});

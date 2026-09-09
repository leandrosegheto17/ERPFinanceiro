import { describe, expect, it } from "vitest";

import type { SignInGateway } from "./sign-in-gateway";
import { signInWithPassword } from "./sign-in-with-password";

type SignInResponse = Awaited<ReturnType<SignInGateway["signInWithPassword"]>>;

function gatewayReturning(response: SignInResponse): SignInGateway {
  return {
    signInWithPassword: async () => response,
    signInWithOtp: async () => {
      throw new Error("não deveria ser chamado por signInWithPassword");
    },
    resetPasswordForEmail: async () => {
      throw new Error("não deveria ser chamado por signInWithPassword");
    },
  };
}

describe("signInWithPassword (TASK-037)", () => {
  it("retorna signed-in com sessão válida", async () => {
    const gateway = gatewayReturning({
      data: {
        user: { id: "user-1" },
        session: { access_token: "at-1", refresh_token: "rt-1" },
      },
      error: null,
    } as unknown as SignInResponse);

    const result = await signInWithPassword(gateway, {
      email: "leitor@example.test",
      password: "senha-correta",
    });

    expect(result).toEqual({
      status: "signed-in",
      userId: "user-1",
      accessToken: "at-1",
      refreshToken: "rt-1",
    });
  });

  it("critério de aceite — senha errada de conta existente: error genérico invalid-credentials", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Invalid login credentials", status: 400 },
    } as unknown as SignInResponse);

    const result = await signInWithPassword(gateway, {
      email: "conta-existente@example.test",
      password: "senha-errada",
    });

    expect(result).toEqual({ status: "error", reason: "invalid-credentials" });
  });

  it("critério de aceite — e-mail inexistente: mesmíssimo resultado do caso de senha errada", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Invalid login credentials", status: 400 },
    } as unknown as SignInResponse);

    const result = await signInWithPassword(gateway, {
      email: "nao-cadastrado@example.test",
      password: "qualquer-coisa",
    });

    expect(result).toEqual({ status: "error", reason: "invalid-credentials" });
  });

  it("classifica limite de tentativas (HTTP 429) como rate-limited", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Too many requests", status: 429 },
    } as unknown as SignInResponse);

    const result = await signInWithPassword(gateway, {
      email: "leitor2@example.test",
      password: "senha-qualquer",
    });

    expect(result).toEqual({ status: "error", reason: "rate-limited" });
  });

  it("classifica falha de rede como network-error", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: { message: "Failed to fetch" },
    } as unknown as SignInResponse);

    const result = await signInWithPassword(gateway, {
      email: "leitor3@example.test",
      password: "senha-qualquer",
    });

    expect(result).toEqual({ status: "error", reason: "network-error" });
  });

  it("contrato inesperado (sem erro nem sessão) também vira invalid-credentials, nunca lança exceção", async () => {
    const gateway = gatewayReturning({
      data: { user: null, session: null },
      error: null,
    } as unknown as SignInResponse);

    const result = await signInWithPassword(gateway, {
      email: "leitor4@example.test",
      password: "senha-qualquer",
    });

    expect(result).toEqual({ status: "error", reason: "invalid-credentials" });
  });
});

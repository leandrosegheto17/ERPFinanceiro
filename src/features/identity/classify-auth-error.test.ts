import { describe, expect, it } from "vitest";

import { classifyAuthError } from "./classify-auth-error";

describe("classifyAuthError (TASK-037)", () => {
  it("classifica HTTP 429 como rate-limited", () => {
    expect(
      classifyAuthError({ message: "Too many requests", status: 429 } as never),
    ).toBe("rate-limited");
  });

  it("classifica erro sem status HTTP como network-error", () => {
    expect(classifyAuthError({ message: "Failed to fetch" } as never)).toBe("network-error");
  });

  it("classifica mensagem contendo 'network' como network-error mesmo com status presente", () => {
    expect(
      classifyAuthError({ message: "Network request failed", status: 0 } as never),
    ).toBe("network-error");
  });

  it("critério de aceite: 'Invalid login credentials' (senha errada) vira invalid-credentials", () => {
    expect(
      classifyAuthError({ message: "Invalid login credentials", status: 400 } as never),
    ).toBe("invalid-credentials");
  });

  it("critério de aceite: mensagem que citaria e-mail inexistente também colapsa em invalid-credentials, nunca um balde próprio", () => {
    expect(
      classifyAuthError({
        message: "User not found for the provided email address",
        status: 400,
      } as never),
    ).toBe("invalid-credentials");
  });

  it("qualquer outro erro com status HTTP cai em invalid-credentials (balde genérico, nunca inspeciona o texto)", () => {
    expect(
      classifyAuthError({ message: "Something completely different", status: 422 } as never),
    ).toBe("invalid-credentials");
  });
});

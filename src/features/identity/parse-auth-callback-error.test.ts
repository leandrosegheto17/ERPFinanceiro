import { describe, expect, it } from "vitest";

import { parseAuthCallbackError } from "./parse-auth-callback-error";

describe("parseAuthCallbackError (TASK-037)", () => {
  it("retorna undefined quando não há parâmetro de erro", () => {
    expect(parseAuthCallbackError("")).toBeUndefined();
    expect(parseAuthCallbackError("#access_token=abc&token_type=bearer")).toBeUndefined();
  });

  it("reconhece link mágico/recuperação expirado (otp_expired)", () => {
    const fragmento =
      "#error=access_denied&error_code=otp_expired&error_description=Email+link+is+invalid+or+has+expired";
    expect(parseAuthCallbackError(fragmento)).toBe("link-expirado");
  });

  it("também funciona a partir de query string (?...)", () => {
    expect(parseAuthCallbackError("?error=access_denied&error_code=otp_expired")).toBe(
      "link-expirado",
    );
  });

  it("qualquer outro erro reconhecido vira 'desconhecido', nunca lança exceção", () => {
    expect(parseAuthCallbackError("#error=server_error&error_code=unexpected_failure")).toBe(
      "desconhecido",
    );
  });
});

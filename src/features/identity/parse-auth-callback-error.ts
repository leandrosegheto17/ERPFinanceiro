import type { AuthCallbackErrorReason } from "./sign-in-types";

/**
 * TASK-037 — Detecta se a tela T-09 foi aberta a partir de um redirect de
 * link mágico/recuperação de senha que expirou ou já foi usado. O
 * Supabase Auth anexa esses parâmetros ao fragmento da URL de retorno
 * (`#error=access_denied&error_code=otp_expired&error_description=...`),
 * nunca ao corpo de uma resposta — por isso é lido da URL, não de uma
 * chamada de API.
 *
 * Não revela nada sobre a existência do e-mail: o link expirado é uma
 * propriedade do próprio link (validade temporal / uso único), não uma
 * resposta a uma tentativa de autenticação — por isso é um motivo
 * separado de `SignInErrorReason` (UX-SPEC T-09 trata "link expirado" como
 * estado de erro distinto de "credencial inválida").
 */
export function parseAuthCallbackError(
  urlFragmentOrSearch: string,
): AuthCallbackErrorReason | undefined {
  const raw = urlFragmentOrSearch.startsWith("#") || urlFragmentOrSearch.startsWith("?")
    ? urlFragmentOrSearch.slice(1)
    : urlFragmentOrSearch;
  if (!raw) {
    return undefined;
  }
  const params = new URLSearchParams(raw);
  const errorCode = params.get("error_code");
  const error = params.get("error");
  if (!error && !errorCode) {
    return undefined;
  }
  if (errorCode === "otp_expired") {
    return "link-expirado";
  }
  return "desconhecido";
}

/**
 * TASK-037 (Lote 6 — Identidade): tipos da tela T-09 (Entrar/recuperar
 * acesso) — login por senha, link mágico e recuperação de senha.
 *
 * Contrato central desta tarefa (critério de aceite): nenhum destes tipos
 * carrega um motivo que diferencie "e-mail não existe" de qualquer outra
 * causa de falha de autenticação — só existe o balde genérico
 * `"invalid-credentials"` para login por senha, e link
 * mágico/recuperação sempre chegam a `"request-sent"` independentemente de
 * o e-mail existir ou não na base. Ver `sign-in-with-password.ts`,
 * `sign-in-with-magic-link.ts` e `request-password-reset.ts`.
 */

export interface SignInWithPasswordInput {
  readonly email: string;
  readonly password: string;
}

/**
 * Balde único e deliberadamente genérico: nunca existe um valor separado
 * para "e-mail não encontrado" — isso é o que impede o vazamento de
 * existência de e-mail via mensagem de erro (UX-SPEC T-09, critério de
 * aceite de TASK-037). `"network-error"`/`"rate-limited"` são estados de
 * transporte/limite, não revelam nada sobre a conta.
 */
export type SignInErrorReason = "invalid-credentials" | "network-error" | "rate-limited";

export type SignInWithPasswordResult =
  | {
      readonly status: "signed-in";
      readonly userId: string;
      readonly accessToken: string;
      readonly refreshToken: string;
    }
  | { readonly status: "error"; readonly reason: SignInErrorReason };

/**
 * Só dois estados possíveis chegam à UI: o pedido foi aceito para
 * processamento (`"request-sent"` — usado tanto para link mágico quanto
 * para recuperação de senha, e mostrado **igual** exista ou não o e-mail
 * na base) ou falhou por motivo de transporte/limite, nunca por motivo
 * ligado à existência da conta.
 */
export type PasswordlessRequestResult =
  | { readonly status: "request-sent" }
  | { readonly status: "error"; readonly reason: "network-error" | "rate-limited" };

/**
 * Estado de erro ao chegar nesta tela via redirecionamento de um link de
 * e-mail (link mágico ou recuperação de senha) que expirou ou já foi
 * usado — Supabase Auth expõe isso como parâmetros de erro na URL de
 * retorno (`#error=access_denied&error_code=otp_expired&...`). Distinto de
 * `SignInErrorReason`: não é sobre credencial nem revela existência de
 * e-mail, é sobre a validade temporal do link em si (UX-SPEC T-09, "link
 * expirado").
 */
export type AuthCallbackErrorReason = "link-expirado" | "desconhecido";

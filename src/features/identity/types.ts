/**
 * TASK-035 (Lote 6 — Identidade): tipos do mecanismo de cadastro
 * e-mail+senha com verificação, via Supabase Auth (SDD.md §7.1).
 *
 * Escopo desta tarefa: só o mecanismo de cadastro/verificação. A tela T-08
 * (com o `BlocoConsentimento`) é TASK-036 e consome estes tipos.
 */

export interface SignUpInput {
  readonly email: string;
  readonly password: string;
  /**
   * Metadata opcional enviada como `options.data` ao `auth.signUp` do
   * Supabase Auth. O GoTrue persiste isso em `auth.users.raw_user_meta_data`
   * antes do gatilho `handle_new_auth_user`
   * (`supabase/migrations/20260908190000_profile_on_signup_trigger.sql`)
   * disparar — mecanismo genérico usado por TASK-036 para levar
   * `consent_version`/`consent_text_sha256` até o banco na mesma transação
   * que cria o usuário (SDD.md §7.6: ordem/atomicidade garantida por
   * política de banco, nunca por sequência de chamadas do cliente). Esta
   * função (TASK-035) não interpreta o conteúdo — só repassa.
   */
  readonly metadata?: Readonly<Record<string, string>>;
}

/** Motivo de falha de cadastro, classificado a partir do erro do Supabase Auth. */
export type SignUpErrorReason =
  | "email-already-registered"
  | "weak-password"
  | "invalid-email"
  | "rate-limited"
  | "unknown";

export type SignUpResult =
  /**
   * Caminho normal deste projeto: `enable_confirmations = true`
   * (`supabase/config.toml`) faz `signUp` não devolver sessão até o e-mail
   * ser confirmado (SDD.md §7.1 — "e-mail + senha com verificação de
   * e-mail"). O chamador aguarda `confirmSignUp` (após o usuário abrir o
   * link recebido) antes de tratar o cadastro como concluído.
   */
  | { readonly status: "confirmation-required"; readonly userId: string }
  /**
   * Suportado pelo próprio Supabase Auth quando a confirmação de e-mail
   * está desligada — não é o caminho usado por este projeto, mas o tipo
   * cobre o retorno real possível da API em vez de assumir silenciosamente
   * que sessão nunca vem no `signUp`.
   */
  | {
      readonly status: "signed-in";
      readonly userId: string;
      readonly accessToken: string;
      readonly refreshToken: string;
    }
  | { readonly status: "error"; readonly reason: SignUpErrorReason; readonly message: string };

export interface ConfirmSignUpInput {
  /**
   * Valor do parâmetro `token` do link de verificação recebido por e-mail
   * (`.../auth/v1/verify?token=...&type=signup&...`) — a API de
   * `verifyOtp` do Supabase Auth trata esse valor como `token_hash`.
   */
  readonly tokenHash: string;
}

export type ConfirmSignUpResult =
  | {
      readonly status: "confirmed";
      readonly userId: string;
      readonly accessToken: string;
      readonly refreshToken: string;
    }
  | { readonly status: "error"; readonly message: string };

/**
 * TASK-036 — tipos da orquestração de cadastro que garante a ordem
 * "`consent` antes de qualquer dado pessoal" (CA-10.2).
 *
 * Correção da reprovação crítica do Validador (2026-09-08, ver
 * `.md/BLOCKERS.md` Entrada 2): não existe mais uma etapa separada de
 * "gravar consent depois do signUp" no cliente — `consentVersion`/
 * `consentTextSha256` viajam como metadata do próprio `signUp`
 * (`SignUpInput.metadata`) e o gatilho de banco
 * (`handle_new_auth_user`) insere `profile`+`consent` na mesma transação
 * que cria `auth.users`. Por isso `SignUpWithConsentResult` é exatamente
 * `SignUpResult`: não há mais um desfecho "consent-failed" distinto de
 * "signUp falhou" — se a gravação de `consent` falhar dentro do gatilho, a
 * transação inteira (incluindo a criação do usuário) é desfeita pelo
 * Postgres, e o `signUp` em si retorna erro (nunca sucesso parcial).
 */
export interface SignUpWithConsentInput {
  readonly email: string;
  readonly password: string;
  /** Versão do texto de consentimento aceito — igual a `consent.version`. */
  readonly consentVersion: string;
  readonly consentTextSha256: string;
}

export type SignUpWithConsentResult = SignUpResult;

/**
 * TASK-040 (Lote 6 — Identidade): tipos do fluxo de exclusão de conta
 * (tela T-13, `public.erasure_request`, migration de TASK-032, RN-11:
 * exclusão em até 15 dias). `dueAt`/`requestedAt` vêm sempre do servidor
 * (trigger `set_erasure_request_due_at`) — nunca calculados no cliente.
 */
export interface RequestAccountErasureInput {
  readonly userId: string;
}

export type RequestAccountErasureResult =
  | { readonly status: "requested"; readonly requestedAt: string; readonly dueAt: string }
  | { readonly status: "error"; readonly message: string };

export type GetErasureRequestStatusResult =
  | { readonly status: "none" }
  | { readonly status: "requested"; readonly requestedAt: string; readonly dueAt: string }
  | { readonly status: "error"; readonly message: string };

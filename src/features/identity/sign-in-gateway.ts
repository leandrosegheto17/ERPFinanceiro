import type { SupabaseClient } from "@supabase/supabase-js";

/**
 * TASK-037 (Lote 6 — Identidade) — porta mínima sobre o cliente de
 * autenticação do Supabase para a tela T-09 (Entrar/recuperar acesso):
 * `signInWithPassword` (login por senha), `signInWithOtp` (link mágico) e
 * `resetPasswordForEmail` (recuperação de senha).
 *
 * Deliberadamente um tipo próprio, separado de `AuthGateway`
 * (`auth-gateway.ts`, TASK-035 — cadastro/verificação): mesmo padrão de
 * porta explícita e mínima já usado no módulo (`core/sync`,
 * `SyncServerClient`), evitando que esta tarefa precise editar o tipo
 * compartilhado usado por TASK-035/036/038 em paralelo no mesmo lote.
 */
export type SignInGateway = Pick<
  SupabaseClient["auth"],
  "signInWithPassword" | "signInWithOtp" | "resetPasswordForEmail"
>;

import type { SupabaseClient } from "@supabase/supabase-js";

/**
 * Porta mínima sobre o cliente de autenticação do Supabase — só os dois
 * métodos que `features/identity` usa nesta tarefa (`signUp`/`verifyOtp`).
 * Mesmo padrão de porta explícita já usado por `core/sync`
 * (`SyncServerClient`, `src/core/sync/sync-server-client.ts`): a lógica de
 * cadastro depende só desta porta, nunca do SDK inteiro, para que os testes
 * de unidade não precisem simular a superfície completa do `GoTrueClient`
 * real.
 */
export type AuthGateway = Pick<SupabaseClient["auth"], "signUp" | "verifyOtp">;

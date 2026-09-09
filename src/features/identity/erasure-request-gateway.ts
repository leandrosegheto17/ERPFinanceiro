import type { SupabaseClient } from "@supabase/supabase-js";

/**
 * TASK-040 (Lote 6 — Identidade): porta mínima sobre o cliente Supabase
 * usada por `requestAccountErasure`/`getErasureRequestStatus` para
 * ler/gravar `public.erasure_request` (migration de TASK-032, RN-11) — só
 * `from`, mesmo padrão de porta explícita já usado por `AuthGateway`
 * (`auth-gateway.ts`): a lógica depende só desta porta, nunca do SDK
 * inteiro, para os testes de unidade não simularem a superfície completa do
 * `SupabaseClient` real.
 */
export type ErasureRequestGateway = Pick<SupabaseClient, "from">;

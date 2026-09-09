import { createClient } from "@supabase/supabase-js";

import { createAppDatabase, type AppDatabase } from "../../core/storage";

import type { AuthGateway } from "./auth-gateway";
import { createDexieAuthStorage } from "./dexie-auth-storage";

export interface SupabaseClientEnvConfig {
  readonly url: string;
  readonly anonKey: string;
}

/**
 * Banco local (`core/storage`, Dexie) usado por padrão pelo adaptador de
 * storage do cliente Supabase Auth (TASK-038) quando o chamador não injeta o
 * seu próprio `db` — ver `createIdentityAuthGateway`. Lazy e memoizado: só é
 * instanciado (e só abre a conexão IndexedDB) na primeira chamada real, e
 * reaproveitado nas chamadas seguintes em vez de abrir uma conexão nova a
 * cada `createIdentityAuthGateway` sem argumento explícito.
 */
let defaultAppDatabase: AppDatabase | undefined;
function getDefaultAppDatabase(): AppDatabase {
  defaultAppDatabase ??= createAppDatabase();
  return defaultAppDatabase;
}

export interface CreateIdentityAuthGatewayOptions {
  /**
   * Banco local (`core/storage`) onde a sessão é persistida (DI-10/G-11).
   * Padrão: singleton lazy de `getDefaultAppDatabase()`. Testes injetam sua
   * própria instância (nome único, fechada/apagada ao final do teste).
   */
  readonly db?: AppDatabase;
  /**
   * `fetch` usado pelo cliente Supabase Auth para as chamadas HTTP reais.
   * Padrão: `fetch` global do runtime. Testes injetam um fake para simular
   * login e refresh de token sem rede real (ver
   * `create-identity-supabase-client.test.ts`).
   */
  readonly fetch?: typeof fetch;
}

/**
 * Lê a configuração pública do cliente Supabase Auth das variáveis
 * injetadas pelo Vite (`VITE_SUPABASE_URL`/`VITE_SUPABASE_ANON_KEY`, ver
 * `.env.example`/`docs/ci-secrets.md`). Nunca lê nem aceita variável de
 * `service_role` (DI-09/GUARDRAILS G-10) — só a `anon key` pública, segura
 * de aparecer no bundle publicado porque a autorização real é RLS
 * (server-side), não o sigilo desta string (ver `.env.example`).
 */
export function resolveSupabaseClientEnvConfig(): SupabaseClientEnvConfig {
  const url = import.meta.env.VITE_SUPABASE_URL;
  const anonKey = import.meta.env.VITE_SUPABASE_ANON_KEY;
  if (!url || !anonKey) {
    throw new Error(
      "VITE_SUPABASE_URL/VITE_SUPABASE_ANON_KEY ausentes — copie .env.example para " +
        ".env e preencha com os valores impressos por `supabase start` (docs/ci-secrets.md §3).",
    );
  }
  return { url, anonKey };
}

/**
 * Cria o `AuthGateway` (porta usada por `signUpWithEmailPassword`/
 * `confirmSignUp`) a partir da configuração pública do cliente — nunca a
 * partir de segredo de serviço. Usado pela camada de UI real (TASK-036 em
 * diante); testes de unidade/e2e injetam um `AuthGateway` próprio (fake ou
 * cliente apontado explicitamente para o Supabase local), sem depender de
 * `import.meta.env`.
 *
 * TASK-038 (DI-10/G-11, gestão de sessão): a sessão (`access_token`/
 * `refresh_token`) nunca é persistida em `localStorage` — o cliente é
 * configurado com `storage: createDexieAuthStorage(db)`, que grava em
 * IndexedDB (Dexie, `core/storage`, store `authSession`) em vez do
 * `localStorage` que o SDK usaria por padrão no navegador.
 * `autoRefreshToken: true` (já é o padrão do SDK, explicitado aqui para
 * deixar a diretriz de DI-10 auditável neste único ponto) mantém o refresh
 * rotativo automático em segundo plano — ver `GoTrueClient#_autoRefreshTokenTick`
 * — sem que este projeto reimplemente esse mecanismo.
 */
export function createIdentityAuthGateway(
  config: SupabaseClientEnvConfig,
  options: CreateIdentityAuthGatewayOptions = {},
): AuthGateway {
  const db = options.db ?? getDefaultAppDatabase();
  return createClient(config.url, config.anonKey, {
    auth: {
      storage: createDexieAuthStorage(db),
      persistSession: true,
      autoRefreshToken: true,
      detectSessionInUrl: true,
    },
    ...(options.fetch ? { global: { fetch: options.fetch } } : {}),
  }).auth;
}

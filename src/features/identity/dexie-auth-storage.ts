import type { AppDatabase } from "../../core/storage";

/**
 * Adaptador de storage do cliente Supabase Auth (TASK-038, Lote 6 —
 * Identidade), backeado por `core/storage` (IndexedDB via Dexie) em vez do
 * `localStorage` que o SDK usa por padrão no navegador.
 *
 * Contrato mínimo esperado pelo `GoTrueClientOptions.storage`
 * (`SupportedStorage` de `@supabase/auth-js`): `getItem`/`setItem`/
 * `removeItem`, por chave string, valor string, síncrono ou assíncrono —
 * deliberadamente não importamos o tipo `SupportedStorage` do SDK aqui, para
 * que `features/identity` continue descrevendo sua própria porta mínima
 * (mesmo padrão já usado por `AuthGateway`, `auth-gateway.ts`) em vez de
 * acoplar este módulo à superfície inteira do SDK.
 *
 * DI-10/G-11: esta é a peça que resolve, para o token de sessão
 * (`access_token`/`refresh_token`), a mesma regra já aplicada a todo o resto
 * do conteúdo do usuário — nunca `localStorage`, sempre IndexedDB (Dexie).
 * Sem este adaptador, `createClient` do Supabase gravaria a sessão em
 * `globalThis.localStorage` por padrão.
 */
export interface DexieAuthStorage {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
  removeItem(key: string): Promise<void>;
}

/**
 * Cria o adaptador sobre a store `authSession` (`core/storage`, v3, TASK-038).
 * `key`/`value` são tratados como opacos — o formato interno de cada valor é
 * definido pelo Supabase Auth (JSON serializado da sessão), não por este
 * módulo.
 */
export function createDexieAuthStorage(db: AppDatabase): DexieAuthStorage {
  return {
    async getItem(key) {
      const record = await db.authSession.get(key);
      return record?.value ?? null;
    },
    async setItem(key, value) {
      await db.authSession.put({ key, value, updatedAt: new Date().toISOString() });
    },
    async removeItem(key) {
      await db.authSession.delete(key);
    },
  };
}

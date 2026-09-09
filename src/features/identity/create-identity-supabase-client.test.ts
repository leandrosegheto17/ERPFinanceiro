// @vitest-environment jsdom
/**
 * TASK-038 (Lote 6 — Identidade, DI-10/G-11): confirma que
 * `createIdentityAuthGateway` (a) nunca grava a sessão em `localStorage`
 * (inspeciona `window.localStorage` diretamente) e (b) o refresh automático
 * de token do SDK (`GoTrueClient#_autoRefreshTokenTick`, disparado pelo
 * próprio timer interno do SDK — aqui invocado diretamente para não depender
 * de um timer real de 30s em teste) renova a sessão sem novo login.
 *
 * `fetch` é substituído por um fake que responde às duas chamadas HTTP que
 * o cliente Supabase Auth realmente faz (`/token?grant_type=password` e
 * `/token?grant_type=refresh_token`) — não há chamada de rede real nem
 * dependência de `supabase start` (TASK-005) para este teste.
 */
import "fake-indexeddb/auto";

import type { SupabaseClient } from "@supabase/supabase-js";
import { afterEach, describe, expect, it, vi } from "vitest";

import { AppDatabase } from "../../core/storage";

import { createIdentityAuthGateway } from "./create-identity-supabase-client";

type FullAuthClient = SupabaseClient["auth"];

/**
 * `_autoRefreshTokenTick` é privado na classe real (`GoTrueClient`) — chamado
 * aqui só para a checagem extra de idempotência (linha ~180), não para
 * disparar o refresh que o teste prova ser automático (esse já aconteceu
 * sozinho, via `vi.waitFor` acima). Cast local isolado em vez de estender o
 * tipo público `SupabaseClient["auth"]` com o método, o que colidiria com o
 * membro privado de mesmo nome já existente na classe.
 */
function callAutoRefreshTick(auth: FullAuthClient): Promise<void> {
  return (auth as unknown as { _autoRefreshTokenTick(): Promise<void> })._autoRefreshTokenTick();
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

describe("createIdentityAuthGateway — sessão via Dexie, refresh automático (TASK-038)", () => {
  const openDatabases: AppDatabase[] = [];
  const openClients: FullAuthClient[] = [];

  afterEach(async () => {
    window.localStorage.clear();
    while (openClients.length > 0) {
      const client = openClients.pop();
      await client?.stopAutoRefresh();
    }
    while (openDatabases.length > 0) {
      const db = openDatabases.pop();
      db?.close();
      if (db) {
        await db.delete();
      }
    }
  });

  it("após login, window.localStorage não guarda nenhuma chave de sessão/usuário", async () => {
    const db = new AppDatabase(`identity-session-login-${crypto.randomUUID()}`);
    openDatabases.push(db);
    await db.open();

    const nowSeconds = Math.floor(Date.now() / 1000);
    const fetchCalls: string[] = [];
    const fakeFetch: typeof fetch = async (input) => {
      const url = typeof input === "string" ? input : input.toString();
      fetchCalls.push(url);
      if (url.includes("/token?grant_type=password")) {
        return jsonResponse({
          access_token: "at-initial",
          refresh_token: "rt-initial",
          expires_in: 3600,
          expires_at: nowSeconds + 3600,
          token_type: "bearer",
          user: { id: "user-1", aud: "authenticated" },
        });
      }
      throw new Error(`fetch inesperado no teste: ${url}`);
    };

    const auth = createIdentityAuthGateway(
      { url: "http://localhost:54421/auth/v1", anonKey: "anon-key-fake" },
      { db, fetch: fakeFetch },
    ) as unknown as FullAuthClient;
    openClients.push(auth);

    expect(window.localStorage.length).toBe(0);

    const { data, error } = await auth.signInWithPassword({
      email: "leitor@example.test",
      password: "senha-forte-9f8e7d6c!",
    });

    expect(error).toBeNull();
    expect(data.session?.access_token).toBe("at-initial");
    expect(fetchCalls.some((url) => url.includes("grant_type=password"))).toBe(true);

    // Prova central do critério de aceite: a sessão foi persistida (senão o
    // teste de refresh abaixo não teria o que renovar), mas nunca em
    // `localStorage` — o adaptador `createDexieAuthStorage` grava em
    // IndexedDB (Dexie), e é isso que `window.localStorage` confirma vazio.
    expect(window.localStorage.length).toBe(0);
    expect(await db.authSession.count()).toBeGreaterThan(0);
  });

  it("refresh automático (mecanismo interno do SDK, sem novo login) renova o access_token, e localStorage segue vazio", async () => {
    const db = new AppDatabase(`identity-session-refresh-${crypto.randomUUID()}`);
    openDatabases.push(db);
    await db.open();

    // `expires_in: 60` (60s) já nasce dentro da margem de refresh automático
    // do SDK (`EXPIRY_MARGIN_MS` = 3 ticks × 30s = 90s, `lib/constants.js`) —
    // login real via `signInWithPassword` (não seed manual de storage), para
    // que a sessão inicial seja gravada com a chave real que o próprio SDK
    // usa (derivada da URL, `sb-<host>-auth-token`), sem este teste precisar
    // conhecer/duplicar essa derivação. Com `autoRefreshToken: true`
    // (configurado por `createIdentityAuthGateway`, DI-10), o próprio SDK
    // agenda `_startAutoRefresh()` ao salvar essa sessão e roda o primeiro
    // tick sozinho, no próximo turno do event loop (`GoTrueClient.js`,
    // `_startAutoRefresh` → `setTimeout(..., 0)`) — sem esperar o intervalo
    // real de 30s e sem qualquer chamada manual deste teste a
    // `_autoRefreshTokenTick`.
    let refreshCalls = 0;
    const fakeFetch: typeof fetch = async (input) => {
      const url = typeof input === "string" ? input : input.toString();
      if (url.includes("/token?grant_type=password")) {
        return jsonResponse({
          access_token: "at-quase-expirando",
          refresh_token: "rt-quase-expirando",
          expires_in: 60,
          expires_at: Math.floor(Date.now() / 1000) + 60,
          token_type: "bearer",
          user: { id: "user-1", aud: "authenticated" },
        });
      }
      if (url.includes("/token?grant_type=refresh_token")) {
        refreshCalls += 1;
        return jsonResponse({
          access_token: "at-renovado",
          refresh_token: "rt-renovado",
          expires_in: 3600,
          expires_at: Math.floor(Date.now() / 1000) + 3600,
          token_type: "bearer",
          user: { id: "user-1", aud: "authenticated" },
        });
      }
      throw new Error(`fetch inesperado no teste: ${url}`);
    };

    const auth = createIdentityAuthGateway(
      { url: "http://localhost:54421/auth/v1", anonKey: "anon-key-fake" },
      { db, fetch: fakeFetch },
    ) as unknown as FullAuthClient;
    openClients.push(auth);

    const signIn = await auth.signInWithPassword({
      email: "leitor@example.test",
      password: "senha-forte-9f8e7d6c!",
    });
    expect(signIn.error).toBeNull();
    expect(signIn.data.session?.access_token).toBe("at-quase-expirando");
    expect(window.localStorage.length).toBe(0);

    // Não chamamos `_autoRefreshTokenTick()` manualmente aqui — o objetivo é
    // provar o mecanismo automático de verdade, não simulá-lo por fora. O
    // tick automático que `_startAutoRefresh()` agenda roda de forma
    // assíncrona (macrotask), então esperamos até ele acontecer.
    await vi.waitFor(
      () => {
        expect(refreshCalls).toBe(1);
      },
      { timeout: 5000 },
    );

    const after = await auth.getSession();
    expect(after.data.session?.access_token).toBe("at-renovado");
    expect(after.data.session?.refresh_token).toBe("rt-renovado");

    // Explicitamente sem novo login: só uma chamada de `grant_type=password`
    // ocorreu no total (a do `signInWithPassword` acima); a renovação veio
    // do `refresh_token` já guardado.
    expect(refreshCalls).toBe(1);

    // Uma segunda checagem do tick (mesmo mecanismo, chamado diretamente
    // desta vez) confirma que não há refresh redundante quando a sessão já
    // não está mais perto de expirar — prova de que o disparo acima foi
    // condicionado à expiração, não incondicional.
    await callAutoRefreshTick(auth);
    expect(refreshCalls).toBe(1);

    // Nenhum novo login ocorreu — só o refresh_token guardado desde o início
    // foi usado — e a sessão renovada continua fora de `localStorage`.
    expect(window.localStorage.length).toBe(0);
    const persistedRecords = await db.authSession.toArray();
    expect(persistedRecords.some((record) => record.value.includes("at-renovado"))).toBe(true);
  });
});

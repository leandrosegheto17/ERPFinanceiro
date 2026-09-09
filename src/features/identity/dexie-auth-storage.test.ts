// @vitest-environment jsdom
/**
 * TASK-038 (Lote 6 — Identidade, DI-10/G-11).
 *
 * Precisa de `jsdom` (não do `environment: "node"` padrão de `vitest.config.ts`)
 * porque o critério de aceite exige inspecionar `window.localStorage`
 * diretamente — indisponível em Node puro. `fake-indexeddb/auto` (mesmo
 * padrão de `src/core/storage/db.test.ts`) provê `indexedDB`/`IDBKeyRange`
 * para o Dexie funcionar fora do navegador real.
 */
import "fake-indexeddb/auto";

import { afterEach, describe, expect, it } from "vitest";

import { AppDatabase } from "../../core/storage";

import { createDexieAuthStorage } from "./dexie-auth-storage";

describe("createDexieAuthStorage (TASK-038, DI-10/G-11)", () => {
  const openDatabases: AppDatabase[] = [];

  function open(): AppDatabase {
    const db = new AppDatabase(`dexie-auth-storage-${crypto.randomUUID()}`);
    openDatabases.push(db);
    return db;
  }

  afterEach(async () => {
    window.localStorage.clear();
    while (openDatabases.length > 0) {
      const db = openDatabases.pop();
      db?.close();
      if (db) {
        await db.delete();
      }
    }
  });

  it("getItem/setItem/removeItem persistem em IndexedDB (Dexie), nunca em localStorage", async () => {
    const db = open();
    await db.open();
    const storage = createDexieAuthStorage(db);

    expect(window.localStorage.length).toBe(0);
    expect(await storage.getItem("supabase.auth.token")).toBeNull();

    const sessionValue = JSON.stringify({ access_token: "at-1", refresh_token: "rt-1" });
    await storage.setItem("supabase.auth.token", sessionValue);

    // Prova central do critério de aceite (DI-10/G-11): o valor foi
    // persistido de fato — mas `window.localStorage` continua vazio.
    expect(await storage.getItem("supabase.auth.token")).toBe(sessionValue);
    const stored = await db.authSession.get("supabase.auth.token");
    expect(stored?.value).toBe(sessionValue);
    expect(window.localStorage.length).toBe(0);

    await storage.removeItem("supabase.auth.token");
    expect(await storage.getItem("supabase.auth.token")).toBeNull();
    expect(await db.authSession.get("supabase.auth.token")).toBeUndefined();
    expect(window.localStorage.length).toBe(0);
  });

  it("isola chaves diferentes sem colisão (mesma store, chaves distintas)", async () => {
    const db = open();
    await db.open();
    const storage = createDexieAuthStorage(db);

    await storage.setItem("supabase.auth.token", "sessao-principal");
    await storage.setItem("supabase.auth.token-code-verifier", "pkce-verifier");

    expect(await storage.getItem("supabase.auth.token")).toBe("sessao-principal");
    expect(await storage.getItem("supabase.auth.token-code-verifier")).toBe("pkce-verifier");

    await storage.removeItem("supabase.auth.token");
    expect(await storage.getItem("supabase.auth.token")).toBeNull();
    expect(await storage.getItem("supabase.auth.token-code-verifier")).toBe("pkce-verifier");
    expect(window.localStorage.length).toBe(0);
  });
});

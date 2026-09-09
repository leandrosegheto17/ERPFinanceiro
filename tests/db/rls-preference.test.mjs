// tests/db/rls-preference.test.mjs
//
// TASK-029 (Lote 5 — Backend: Schema e Segurança): teste automatizado que
// comprova que a política de RLS de `preference`
// (supabase/migrations/20260908181000_preference_rls.sql) funciona de fato —
// não apenas que RLS está "habilitada", mas que um usuário autenticado não
// consegue ler/gravar/apagar a preferência de outro usuário, nem via SELECT
// nem via INSERT/UPDATE/DELETE forjando `user_id`. Também confirma que
// `received_at` (RT-06) é sempre determinado pelo servidor, mesmo quando o
// cliente tenta forjar o valor no corpo da requisição. Roda contra o
// Supabase local real (Postgres + PostgREST + GoTrue via `supabase start`),
// nunca simulado/mockado — mesmo padrão de
// `tests/db/rls-profile-consent.test.mjs` (TASK-027).
//
// Pré-requisito: `supabase start` rodando localmente (Docker) e a migration
// desta tarefa aplicada (`supabase db reset` ou `supabase db push` local).
//
// Rodar com: node --test tests/db/*.test.mjs
//
// Credenciais: mesmas chaves de demonstração fixas e públicas do Supabase
// CLI local, documentadas em `docs/ci-secrets.md` §3 — não são segredo.

import test from 'node:test';
import assert from 'node:assert/strict';

const SUPABASE_URL = process.env.SUPABASE_URL ?? 'http://127.0.0.1:54421';
const ANON_KEY =
  process.env.SUPABASE_ANON_KEY ??
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0';
const SERVICE_ROLE_KEY =
  process.env.SUPABASE_SERVICE_ROLE_KEY ??
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU';

const AUTH_URL = `${SUPABASE_URL}/auth/v1`;
const REST_URL = `${SUPABASE_URL}/rest/v1`;

/** Confirma que o Supabase local está de pé antes de rodar a suíte real. */
async function isLocalSupabaseUp() {
  try {
    const res = await fetch(`${SUPABASE_URL}/auth/v1/health`, {
      headers: { apikey: ANON_KEY },
    });
    return res.ok;
  } catch {
    return false;
  }
}

let localSupabaseUp = await isLocalSupabaseUp();

function uniqueEmail(label) {
  return `rls-pref-${label}-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
}

async function createConfirmedUser(email, password) {
  const res = await fetch(`${AUTH_URL}/admin/users`, {
    method: 'POST',
    headers: {
      apikey: SERVICE_ROLE_KEY,
      Authorization: `Bearer ${SERVICE_ROLE_KEY}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email, password, email_confirm: true }),
  });
  if (!res.ok) {
    throw new Error(`Falha ao criar usuário de teste (${res.status}): ${await res.text()}`);
  }
  return res.json();
}

async function signIn(email, password) {
  const res = await fetch(`${AUTH_URL}/token?grant_type=password`, {
    method: 'POST',
    headers: {
      apikey: ANON_KEY,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) {
    throw new Error(`Falha ao autenticar usuário de teste (${res.status}): ${await res.text()}`);
  }
  return res.json();
}

async function deleteUser(userId) {
  await fetch(`${AUTH_URL}/admin/users/${userId}`, {
    method: 'DELETE',
    headers: {
      apikey: SERVICE_ROLE_KEY,
      Authorization: `Bearer ${SERVICE_ROLE_KEY}`,
    },
  });
}

/**
 * Chama PostgREST autenticado como um usuário final (papel `authenticated`,
 * sob RLS) — nunca com a service role, que ignora RLS por definição.
 */
async function restAsUser({ accessToken, method, table, query = '', body }) {
  const res = await fetch(`${REST_URL}/${table}${query}`, {
    method,
    headers: {
      apikey: ANON_KEY,
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
      Prefer: 'return=representation',
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  const json = text ? JSON.parse(text) : null;
  return { status: res.status, ok: res.ok, body: json };
}

test('RLS de preference isola cada titular das linhas de outro titular', { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte de RLS pulada; ver README/docs/ci-secrets.md §3' }, async (t) => {
  const passwordA = 'senha-teste-A-9f8e7d6c!';
  const passwordB = 'senha-teste-B-1a2b3c4d!';
  const emailA = uniqueEmail('a');
  const emailB = uniqueEmail('b');

  const userA = await createConfirmedUser(emailA, passwordA);
  const userB = await createConfirmedUser(emailB, passwordB);

  t.after(async () => {
    await deleteUser(userA.id);
    await deleteUser(userB.id);
  });

  const sessionA = await signIn(emailA, passwordA);
  const sessionB = await signIn(emailB, passwordB);

  const beforeInsert = new Date();

  await t.test('preference: usuário só grava a própria linha', async () => {
    const ownInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'preference',
      body: {
        user_id: userA.id,
        key: 'theme',
        value: 'dark',
        updated_at: new Date().toISOString(),
      },
    });
    assert.equal(ownInsert.status, 201, `insert da própria linha deveria ter sucesso: ${JSON.stringify(ownInsert.body)}`);

    const forgedInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'preference',
      body: {
        user_id: userB.id,
        key: 'theme',
        value: 'light',
        updated_at: new Date().toISOString(),
      },
    });
    assert.ok(
      !forgedInsert.ok,
      'insert com user_id de outro titular deveria ser rejeitado pela política RLS (with check)',
    );

    // Segundo titular precisa da própria linha para os testes de leitura/escrita abaixo.
    const otherOwnInsert = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'POST',
      table: 'preference',
      body: {
        user_id: userB.id,
        key: 'theme',
        value: 'light',
        updated_at: new Date().toISOString(),
      },
    });
    assert.equal(otherOwnInsert.status, 201);
  });

  await t.test('preference: received_at é definido pelo servidor, não aceito do cliente (RT-06)', async () => {
    const forgedFuture = new Date(Date.now() + 1000 * 60 * 60 * 24 * 365).toISOString();
    const insertWithForgedReceivedAt = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'preference',
      body: {
        user_id: userA.id,
        key: 'reminder_time',
        value: '07:00',
        updated_at: new Date().toISOString(),
        received_at: forgedFuture,
      },
    });
    assert.equal(insertWithForgedReceivedAt.status, 201, `insert deveria ter sucesso: ${JSON.stringify(insertWithForgedReceivedAt.body)}`);
    const receivedAt = insertWithForgedReceivedAt.body[0].received_at;
    assert.notEqual(receivedAt, forgedFuture, 'received_at forjado pelo cliente não deveria ter sido aceito');
    assert.ok(
      new Date(receivedAt) >= beforeInsert && new Date(receivedAt) <= new Date(),
      'received_at deveria refletir o relógio do servidor, próximo do instante real do insert',
    );
  });

  await t.test('preference: usuário não lê linha de outro titular', async () => {
    const readAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'GET',
      table: 'preference',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readAsB.status, 200);
    assert.deepEqual(readAsB.body, [], 'B não deveria enxergar as preferências de A');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
    });
    assert.equal(readOwnAsA.body.length, 1, 'A deveria enxergar a própria preferência');
  });

  await t.test('preference: usuário não atualiza (LWW) linha de outro titular', async () => {
    const updateAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'PATCH',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
      body: { value: 'hacked', updated_at: new Date().toISOString() },
    });
    assert.equal(updateAsB.status, 200);
    assert.deepEqual(updateAsB.body, [], 'update de B sobre a preferência de A não deveria afetar nenhuma linha');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
    });
    assert.equal(readOwnAsA.body[0].value, 'dark', 'valor de A não deveria ter sido alterado por B');
  });

  await t.test('preference: usuário atualiza a própria linha (LWW substitui o valor)', async () => {
    const updateOwn = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'PATCH',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
      body: { value: 'light', updated_at: new Date().toISOString() },
    });
    assert.equal(updateOwn.status, 200);
    assert.equal(updateOwn.body[0].value, 'light', 'A deveria conseguir atualizar a própria preferência');
  });

  await t.test('preference: usuário não apaga linha de outro titular', async () => {
    const deleteAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'DELETE',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
    });
    assert.equal(deleteAsB.status, 200);
    assert.deepEqual(deleteAsB.body, [], 'delete de B sobre a preferência de A não deveria afetar nenhuma linha');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
    });
    assert.equal(readOwnAsA.body.length, 1, 'preferência de A não deveria ter sido apagada por B');
  });

  await t.test('preference: usuário apaga a própria linha', async () => {
    const deleteOwn = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'DELETE',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
    });
    assert.equal(deleteOwn.status, 200);
    assert.equal(deleteOwn.body.length, 1, 'A deveria conseguir apagar a própria preferência');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'preference',
      query: `?user_id=eq.${userA.id}&key=eq.theme`,
    });
    assert.deepEqual(readOwnAsA.body, [], 'preferência apagada não deveria mais existir');
  });

  await t.test('anônimo (sem sessão de usuário) não lê nenhuma linha de preference', async () => {
    // Sem sessão de usuário, o papel efetivo é `anon`, que não tem GRANT de
    // tabela em `preference` (SDD.md §7.2 — exige sessão autenticada). O
    // bloqueio acontece na camada de privilégio do Postgres, antes mesmo de
    // a política de RLS ser avaliada — PostgREST reporta isso como 401
    // (permission denied), não como uma lista vazia.
    const anonPreference = await restAsUser({
      accessToken: ANON_KEY,
      method: 'GET',
      table: 'preference',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.ok(!anonPreference.ok, 'papel anon não deveria conseguir ler public.preference');
  });
});

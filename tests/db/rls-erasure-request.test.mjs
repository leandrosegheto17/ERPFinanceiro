// tests/db/rls-erasure-request.test.mjs
//
// TASK-032 (Lote 5 — Backend: Schema e Segurança): teste automatizado que
// comprova que a política de RLS de `erasure_request`
// (supabase/migrations/20260908180432_erasure_request_rls.sql) funciona de
// fato — um usuário autenticado não vê nem cria pedido de exclusão de outro
// usuário, e não consegue gravar/alterar `due_at`/`completed_at`
// arbitrariamente (coluna gerada + ausência de política de update). Roda
// contra o Supabase local real (Postgres + PostgREST + GoTrue via
// `supabase start`), nunca simulado/mockado — mesmo padrão de
// `tests/db/rls-profile-consent.test.mjs` (TASK-027).
//
// Pré-requisito: `supabase start` rodando localmente (Docker) e a migration
// desta tarefa aplicada (`supabase db reset` ou `supabase db push` local).
//
// Rodar com: node --test tests/db/*.test.mjs (ou node --test
// tests/db/rls-erasure-request.test.mjs para só esta suíte)

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
  return `rls-erasure-${label}-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
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

/** Chama PostgREST com a service role — o único papel que pode preencher
 * `completed_at` (jobs/erasure, TASK-041), já que RLS não se aplica a ela. */
async function restAsService({ method, table, query = '', body }) {
  const res = await fetch(`${REST_URL}/${table}${query}`, {
    method,
    headers: {
      apikey: SERVICE_ROLE_KEY,
      Authorization: `Bearer ${SERVICE_ROLE_KEY}`,
      'Content-Type': 'application/json',
      Prefer: 'return=representation',
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  const json = text ? JSON.parse(text) : null;
  return { status: res.status, ok: res.ok, body: json };
}

test('RLS de erasure_request isola cada titular das linhas de outro titular', { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte de RLS pulada; ver README/docs/ci-secrets.md §3' }, async (t) => {
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

  let requestIdA;

  await t.test('usuário cria o próprio pedido de exclusão e due_at é calculado (+15 dias)', async () => {
    const before = Date.now();
    const ownInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'erasure_request',
      body: { user_id: userA.id },
    });
    assert.equal(ownInsert.status, 201, `insert do próprio pedido deveria ter sucesso: ${JSON.stringify(ownInsert.body)}`);
    requestIdA = ownInsert.body[0].id;

    const requestedAt = new Date(ownInsert.body[0].requested_at).getTime();
    const dueAt = new Date(ownInsert.body[0].due_at).getTime();
    const fifteenDaysMs = 15 * 24 * 60 * 60 * 1000;
    assert.ok(requestedAt >= before, 'requested_at deveria ser gravado no momento do insert');
    assert.equal(dueAt - requestedAt, fifteenDaysMs, 'due_at deveria ser exatamente requested_at + 15 dias');
    assert.equal(ownInsert.body[0].completed_at, null, 'completed_at deveria começar nulo (pedido em andamento)');
  });

  await t.test('usuário não consegue gravar due_at arbitrário (trigger recalcula sempre)', async () => {
    const forgedDueAt = new Date(Date.now() + 1000 * 60 * 60).toISOString(); // daqui a 1h, não +15 dias
    const insertWithDueAt = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'POST',
      table: 'erasure_request',
      body: { user_id: userB.id, due_at: forgedDueAt },
    });
    assert.equal(insertWithDueAt.status, 201, `insert deveria ter sucesso (o trigger sobrescreve due_at, não rejeita): ${JSON.stringify(insertWithDueAt.body)}`);

    const requestedAt = new Date(insertWithDueAt.body[0].requested_at).getTime();
    const storedDueAt = new Date(insertWithDueAt.body[0].due_at).getTime();
    const fifteenDaysMs = 15 * 24 * 60 * 60 * 1000;
    assert.notEqual(insertWithDueAt.body[0].due_at, forgedDueAt, 'due_at gravado não deveria ser o valor forjado pelo cliente');
    assert.equal(storedDueAt - requestedAt, fifteenDaysMs, 'trigger deveria recalcular due_at como requested_at + 15 dias, ignorando o valor enviado');
  });

  await t.test('erasure_request: usuário não cria pedido em nome de outro titular', async () => {
    const forgedInsert = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'POST',
      table: 'erasure_request',
      body: { user_id: userA.id },
    });
    assert.ok(
      !forgedInsert.ok,
      'B criar um pedido de exclusão em nome de A deveria ser rejeitado pela política RLS (with check)',
    );
  });

  await t.test('erasure_request: usuário não lê pedido de outro titular', async () => {
    const readAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'GET',
      table: 'erasure_request',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readAsB.status, 200);
    assert.deepEqual(readAsB.body, [], 'B não deveria enxergar o pedido de exclusão de A');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'erasure_request',
      query: `?id=eq.${requestIdA}`,
    });
    assert.equal(readOwnAsA.body.length, 1, 'A deveria enxergar o próprio pedido de exclusão');
  });

  await t.test('erasure_request: usuário não altera status/completed_at, nem o próprio nem o de outro titular', async () => {
    const updateOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'PATCH',
      table: 'erasure_request',
      query: `?id=eq.${requestIdA}`,
      body: { completed_at: new Date().toISOString() },
    });
    // Diferente de profile/consent (que têm GRANT de update para
    // `authenticated`, bloqueado só pela ausência de política de RLS —
    // 200 com corpo vazio), `erasure_request` não concede `update` a
    // `authenticated` nem no privilégio de tabela: o bloqueio acontece uma
    // camada antes, na permissão do Postgres, e o PostgREST responde 403.
    assert.equal(updateOwnAsA.status, 403, `nem o próprio titular deveria conseguir alterar completed_at via API: ${JSON.stringify(updateOwnAsA.body)}`);

    const updateAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'PATCH',
      table: 'erasure_request',
      query: `?id=eq.${requestIdA}`,
      body: { completed_at: new Date().toISOString() },
    });
    assert.equal(updateAsB.status, 403, `B não deveria conseguir alterar o pedido de exclusão de A: ${JSON.stringify(updateAsB.body)}`);

    const confirmUnchanged = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'erasure_request',
      query: `?id=eq.${requestIdA}`,
    });
    assert.equal(confirmUnchanged.body[0].completed_at, null, 'completed_at deveria permanecer nulo após as tentativas de update do cliente');
  });

  await t.test('service_role (jobs/erasure) consegue preencher completed_at, ignorando RLS', async () => {
    const completeAsService = await restAsService({
      method: 'PATCH',
      table: 'erasure_request',
      query: `?id=eq.${requestIdA}`,
      body: { completed_at: new Date().toISOString() },
    });
    assert.equal(completeAsService.status, 200);
    assert.equal(completeAsService.body.length, 1, 'service_role deveria conseguir atualizar a linha, ignorando RLS');
    assert.ok(completeAsService.body[0].completed_at, 'completed_at deveria estar preenchido após a atualização de serviço');
  });

  await t.test('anônimo (sem sessão de usuário) não lê nenhuma linha de erasure_request', async () => {
    const anonRead = await restAsUser({
      accessToken: ANON_KEY,
      method: 'GET',
      table: 'erasure_request',
      query: `?id=eq.${requestIdA}`,
    });
    assert.ok(!anonRead.ok, 'papel anon não deveria conseguir ler public.erasure_request');
  });
});

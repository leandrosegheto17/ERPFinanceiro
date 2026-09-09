// tests/db/rls-plan-progress.test.mjs
//
// TASK-028 (Lote 5 — Backend: Schema e Segurança): teste automatizado que
// comprova as duas metades do critério de aceite contra o Supabase local
// real (Postgres + PostgREST + GoTrue via `supabase start`), nunca
// simulado/mockado:
//
//   (a) RLS isola usuários — um titular não lê nem grava linha de
//       `plan_progress` de outro titular (nem via SELECT, nem via INSERT
//       forjando `user_id`);
//   (b) insert duplicado (mesmo user_id + plan_id + day_number) falha por
//       violação da constraint UNIQUE — prova a propriedade append-only /
//       união monotônica do schema (ADR-007).
//
// Pré-requisito: `supabase start` rodando localmente (Docker) e a migration
// desta tarefa aplicada (`supabase db reset` ou `supabase db push` local).
//
// Rodar com: node --test tests/db/*.test.mjs
//
// Credenciais: mesmo padrão de tests/db/rls-profile-consent.test.mjs
// (TASK-027) — chaves de demonstração fixas e públicas do Supabase CLI para
// ambiente local (docs/ci-secrets.md), não segredo.

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
  return `rls-plan-progress-${label}-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
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

test('RLS de plan_progress isola titulares e a constraint impede duplicidade', { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte de RLS pulada; ver README/docs/ci-secrets.md §3' }, async (t) => {
  const passwordA = 'senha-teste-A-plan-9f8e7d6c!';
  const passwordB = 'senha-teste-B-plan-1a2b3c4d!';
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

  const planId = 'plano-90-dias';
  let progressIdA;

  await t.test('plan_progress: usuário só grava a própria linha', async () => {
    const ownInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'plan_progress',
      body: { user_id: userA.id, plan_id: planId, day_number: 1 },
    });
    assert.equal(ownInsert.status, 201, `insert da própria linha deveria ter sucesso: ${JSON.stringify(ownInsert.body)}`);
    progressIdA = ownInsert.body[0].id;

    const forgedInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'plan_progress',
      body: { user_id: userB.id, plan_id: planId, day_number: 1 },
    });
    assert.ok(
      !forgedInsert.ok,
      'insert com user_id de outro titular deveria ser rejeitado pela política RLS (with check)',
    );
  });

  await t.test('plan_progress: usuário não lê linha de outro titular', async () => {
    const readAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'GET',
      table: 'plan_progress',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readAsB.status, 200);
    assert.deepEqual(readAsB.body, [], 'B não deveria enxergar a linha de plan_progress de A');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'plan_progress',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readOwnAsA.body.length, 1, 'A deveria enxergar a própria linha de plan_progress');
  });

  await t.test('plan_progress: insert duplicado (mesmo user_id+plan_id+day_number) falha por constraint', async () => {
    const duplicateInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'plan_progress',
      body: { user_id: userA.id, plan_id: planId, day_number: 1 },
    });
    assert.equal(
      duplicateInsert.status,
      409,
      `insert duplicado deveria falhar com 409 (unique_violation): ${JSON.stringify(duplicateInsert.body)}`,
    );
    assert.equal(duplicateInsert.body?.code, '23505', 'código de erro Postgres esperado para unique_violation');

    // Mesmo dia em plano diferente, ou dia diferente no mesmo plano, não
    // colide com a constraint composta — prova que a UNIQUE é exatamente
    // (user_id, plan_id, day_number), não um subconjunto mais amplo.
    const differentPlanInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'plan_progress',
      body: { user_id: userA.id, plan_id: 'outro-plano', day_number: 1 },
    });
    assert.equal(differentPlanInsert.status, 201);

    const differentDayInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'plan_progress',
      body: { user_id: userA.id, plan_id: planId, day_number: 2 },
    });
    assert.equal(differentDayInsert.status, 201);
  });

  await t.test('plan_progress: tabela é append-only — sem GRANT/política de UPDATE/DELETE para authenticated', async () => {
    const updateAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'PATCH',
      table: 'plan_progress',
      query: `?id=eq.${progressIdA}`,
      body: { day_number: 99 },
    });
    assert.ok(
      !updateAsA.ok,
      'UPDATE não deveria ser permitido nem sobre a própria linha (append-only, sem GRANT de UPDATE)',
    );

    const deleteAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'DELETE',
      table: 'plan_progress',
      query: `?id=eq.${progressIdA}`,
    });
    assert.ok(
      !deleteAsA.ok,
      'DELETE não deveria ser permitido nem sobre a própria linha (append-only, sem GRANT de DELETE)',
    );

    const readAfter = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'plan_progress',
      query: `?id=eq.${progressIdA}`,
    });
    assert.equal(readAfter.body.length, 1, 'a linha original deveria continuar existindo (DELETE não teve efeito real)');
    assert.equal(readAfter.body[0].day_number, 1, 'day_number não deveria ter sido alterado (UPDATE não teve efeito real)');
  });

  await t.test('anônimo (sem sessão de usuário) não lê nem grava nenhuma linha de plan_progress', async () => {
    const anonRead = await restAsUser({
      accessToken: ANON_KEY,
      method: 'GET',
      table: 'plan_progress',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.ok(!anonRead.ok, 'papel anon não deveria conseguir ler public.plan_progress');

    const anonInsert = await restAsUser({
      accessToken: ANON_KEY,
      method: 'POST',
      table: 'plan_progress',
      body: { user_id: userA.id, plan_id: 'anon-probe', day_number: 1 },
    });
    assert.ok(
      !anonInsert.ok,
      'papel anon não deveria conseguir inserir em public.plan_progress (sem GRANT/política para anon)',
    );
  });
});

// tests/db/rls-push-subscription-reminder-log.test.mjs
//
// TASK-030 (Lote 5 — Backend: Schema e Segurança): teste automatizado que
// comprova que a política de RLS de `push_subscription` e `reminder_log`
// (supabase/migrations/20260908180500_push_subscription_reminder_log_rls.sql)
// funciona de fato — não apenas que RLS está "habilitada", mas que um
// usuário autenticado não consegue ler/gravar linha de outro usuário, nem
// via SELECT nem via INSERT/UPDATE/DELETE forjando `user_id`. Confirma
// também que `reminder_log` é somente leitura para o cliente (escrita é
// exclusiva de `service_role`, ver comentário na migration). Roda contra o
// Supabase local real (Postgres + PostgREST + GoTrue via `supabase start`),
// nunca simulado/mockado — mesmo padrão de
// `tests/db/rls-profile-consent.test.mjs` (TASK-027).
//
// Pré-requisito: `supabase start` rodando localmente (Docker) e a migration
// desta tarefa aplicada (`supabase db reset` ou `supabase db push` local).
//
// Rodar com: node --test tests/db/*.test.mjs (ou `npm run test:db`)
//
// Credenciais: mesmo fallback documentado em `rls-profile-consent.test.mjs`
// — chaves de demonstração fixas e públicas do Supabase CLI para ambiente
// local (docs/ci-secrets.md §3), não segredo real.

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
  return `rls-${label}-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
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

/** Chama PostgREST com `service_role` — usado só para semear `reminder_log`,
 * que o cliente nunca grava diretamente (ver migration). */
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

test(
  'RLS de push_subscription e reminder_log isola cada titular das linhas de outro titular',
  { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte de RLS pulada; ver README/docs/ci-secrets.md §3' },
  async (t) => {
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

    let subscriptionIdA;

    await t.test('push_subscription: usuário só grava a própria linha', async () => {
      const ownInsert = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'POST',
        table: 'push_subscription',
        body: {
          user_id: userA.id,
          endpoint: 'https://push.example.test/A',
          p256dh_key: 'p256dh-A',
          auth_key: 'auth-A',
          platform: 'ios',
        },
      });
      assert.equal(ownInsert.status, 201, `insert da própria linha deveria ter sucesso: ${JSON.stringify(ownInsert.body)}`);
      subscriptionIdA = ownInsert.body[0].id;

      const forgedInsert = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'POST',
        table: 'push_subscription',
        body: {
          user_id: userB.id,
          endpoint: 'https://push.example.test/forged',
          p256dh_key: 'p256dh-forged',
          auth_key: 'auth-forged',
          platform: 'android',
        },
      });
      assert.ok(
        !forgedInsert.ok,
        'insert com user_id de outro titular deveria ser rejeitado pela política RLS (with check)',
      );

      const otherOwnInsert = await restAsUser({
        accessToken: sessionB.access_token,
        method: 'POST',
        table: 'push_subscription',
        body: {
          user_id: userB.id,
          endpoint: 'https://push.example.test/B',
          p256dh_key: 'p256dh-B',
          auth_key: 'auth-B',
          platform: 'android',
        },
      });
      assert.equal(otherOwnInsert.status, 201);
    });

    await t.test('push_subscription: usuário não lê nem atualiza linha de outro titular', async () => {
      const readAsB = await restAsUser({
        accessToken: sessionB.access_token,
        method: 'GET',
        table: 'push_subscription',
        query: `?user_id=eq.${userA.id}`,
      });
      assert.equal(readAsB.status, 200);
      assert.deepEqual(readAsB.body, [], 'B não deveria enxergar a assinatura de push de A');

      const readOwnAsA = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'GET',
        table: 'push_subscription',
        query: `?user_id=eq.${userA.id}`,
      });
      assert.equal(readOwnAsA.body.length, 1, 'A deveria enxergar a própria assinatura de push');

      const updateAsB = await restAsUser({
        accessToken: sessionB.access_token,
        method: 'PATCH',
        table: 'push_subscription',
        query: `?id=eq.${subscriptionIdA}`,
        body: { last_seen_at: new Date().toISOString() },
      });
      assert.equal(updateAsB.status, 200);
      assert.deepEqual(updateAsB.body, [], 'update de B sobre a assinatura de A não deveria afetar nenhuma linha');
    });

    await t.test('push_subscription: usuário não apaga linha de outro titular, mas apaga a própria', async () => {
      const deleteAsB = await restAsUser({
        accessToken: sessionB.access_token,
        method: 'DELETE',
        table: 'push_subscription',
        query: `?id=eq.${subscriptionIdA}`,
      });
      assert.equal(deleteAsB.status, 200);
      assert.deepEqual(deleteAsB.body, [], 'delete de B sobre a assinatura de A não deveria afetar nenhuma linha');

      const stillThere = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'GET',
        table: 'push_subscription',
        query: `?id=eq.${subscriptionIdA}`,
      });
      assert.equal(stillThere.body.length, 1, 'assinatura de A não deveria ter sido apagada por B');

      const deleteAsA = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'DELETE',
        table: 'push_subscription',
        query: `?id=eq.${subscriptionIdA}`,
      });
      assert.equal(deleteAsA.status, 200);
      assert.equal(deleteAsA.body.length, 1, 'A deveria conseguir desinscrever a própria assinatura de push');
    });

    let reminderLogIdA;
    let reminderLogIdB;

    await t.test('reminder_log: seed via service_role (cliente não grava o próprio log)', async () => {
      const scheduledForA = new Date(Date.now() + 60_000).toISOString();
      const scheduledForB = new Date(Date.now() + 120_000).toISOString();

      const seedA = await restAsService({
        method: 'POST',
        table: 'reminder_log',
        body: {
          user_id: userA.id,
          scheduled_for: scheduledForA,
          sent_at: scheduledForA,
          outcome: 'sent',
          channel: 'push',
        },
      });
      assert.equal(seedA.status, 201, `seed via service_role deveria ter sucesso: ${JSON.stringify(seedA.body)}`);
      reminderLogIdA = seedA.body[0].id;

      const seedB = await restAsService({
        method: 'POST',
        table: 'reminder_log',
        body: {
          user_id: userB.id,
          scheduled_for: scheduledForB,
          sent_at: null,
          outcome: 'failed',
          channel: 'push',
        },
      });
      assert.equal(seedB.status, 201);
      reminderLogIdB = seedB.body[0].id;

      // Idempotência por dia (RT-07): reagendar o mesmo titular para o
      // mesmo instante-alvo falha na constraint `unique (user_id,
      // scheduled_for)`, mesmo via service_role (constraint de dado, não de
      // RLS/privilégio).
      const duplicateSeedA = await restAsService({
        method: 'POST',
        table: 'reminder_log',
        body: {
          user_id: userA.id,
          scheduled_for: scheduledForA,
          sent_at: scheduledForA,
          outcome: 'sent',
          channel: 'push',
        },
      });
      assert.ok(
        !duplicateSeedA.ok,
        'segundo agendamento com o mesmo (user_id, scheduled_for) deveria violar a constraint unique (envio idempotente por dia)',
      );

      // Cliente autenticado nunca grava o próprio log de lembrete — só
      // service_role, conforme a migration (sem policy de insert para
      // `authenticated`, e sem GRANT insert de tabela).
      const clientInsertAttempt = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'POST',
        table: 'reminder_log',
        body: {
          user_id: userA.id,
          scheduled_for: new Date(Date.now() + 180_000).toISOString(),
          outcome: 'sent',
          channel: 'push',
        },
      });
      assert.ok(
        !clientInsertAttempt.ok,
        'cliente autenticado não deveria conseguir gravar diretamente em reminder_log (somente leitura)',
      );
    });

    await t.test('reminder_log: usuário só lê o próprio log, nunca de outro titular', async () => {
      const readOwnAsA = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'GET',
        table: 'reminder_log',
        query: `?id=eq.${reminderLogIdA}`,
      });
      assert.equal(readOwnAsA.status, 200);
      assert.equal(readOwnAsA.body.length, 1, 'A deveria enxergar o próprio log de lembrete');
      assert.equal(readOwnAsA.body[0].outcome, 'sent');

      const readOtherAsA = await restAsUser({
        accessToken: sessionA.access_token,
        method: 'GET',
        table: 'reminder_log',
        query: `?id=eq.${reminderLogIdB}`,
      });
      assert.equal(readOtherAsA.status, 200);
      assert.deepEqual(readOtherAsA.body, [], 'A não deveria enxergar o log de lembrete de B');

      const readOwnAsB = await restAsUser({
        accessToken: sessionB.access_token,
        method: 'GET',
        table: 'reminder_log',
        query: `?id=eq.${reminderLogIdB}`,
      });
      assert.equal(readOwnAsB.body.length, 1, 'B deveria enxergar o próprio log de lembrete');
    });

    await t.test('anônimo (sem sessão de usuário) não lê nenhuma linha de nenhuma tabela', async () => {
      // Sem sessão de usuário, o papel efetivo é `anon`, que não tem GRANT
      // de tabela em `push_subscription`/`reminder_log` (SDD.md §7.2 —
      // exigem sessão autenticada). Bloqueio na camada de privilégio do
      // Postgres, antes mesmo da RLS — PostgREST reporta 401/403, não uma
      // lista vazia.
      const anonPushSubscription = await restAsUser({
        accessToken: ANON_KEY,
        method: 'GET',
        table: 'push_subscription',
        query: `?user_id=eq.${userA.id}`,
      });
      assert.ok(!anonPushSubscription.ok, 'papel anon não deveria conseguir ler public.push_subscription');

      const anonReminderLog = await restAsUser({
        accessToken: ANON_KEY,
        method: 'GET',
        table: 'reminder_log',
        query: `?id=eq.${reminderLogIdA}`,
      });
      assert.ok(!anonReminderLog.ok, 'papel anon não deveria conseguir ler public.reminder_log');
    });
  },
);

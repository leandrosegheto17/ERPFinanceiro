// tests/db/rls-analytics.test.mjs
//
// TASK-031 (Lote 5 — Backend: Schema e Segurança) [insep.]: teste automatizado
// que comprova, contra o Supabase local real (Postgres + PostgREST + GoTrue
// via `supabase start`), que:
//
//   1. `analytics_daily_aggregate` (agregado anônimo, ADR-009 nível 1) NÃO é
//      legível diretamente por `authenticated` nem por `anon` — nem via
//      REST direto na tabela, confirmando o critério de aceite literal
//      "cliente não lê o agregado" (SDD.md §7.2).
//   2. A função de incremento `rpc.bump_metric` funciona de fato: chamável
//      por `anon` e por `authenticated`, incrementa o contador do dia/métrica
//      corrente a cada chamada (1 -> 2 na segunda chamada da mesma métrica no
//      mesmo dia), e rejeita métrica fora da lista fechada do CHECK.
//   3. `analytics_event` (nível 2, identificado) segue o mesmo isolamento por
//      titular já confirmado para `profile`/`consent`/`plan_progress`
//      (TASK-027/028): usuário só lê/grava a própria linha.
//
// Como a tabela do agregado não é legível por NENHUM papel de API (nem
// mesmo o titular do próprio evento que disparou o incremento — não há
// titular, é anônimo por desenho), a confirmação de que o contador de fato
// incrementou é feita via `service_role` (que ignora RLS por definição do
// Postgres/Supabase) — é o único papel que consegue inspecionar o resultado
// da função para fins de teste, sem que isso implique que o CLIENTE consiga
// o mesmo (ver seção 1 acima, que prova o oposto para `authenticated`/`anon`).
//
// Pré-requisito: `supabase start` rodando localmente (Docker) e a migration
// desta tarefa aplicada (`supabase db reset` ou `supabase db push` local).
//
// Rodar com: node --test tests/db/*.test.mjs

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
  return `analytics-${label}-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
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

/** Chama PostgREST com uma credencial arbitrária (anon key ou access token de usuário). */
async function rest({ accessToken, method, path, body }) {
  const res = await fetch(`${REST_URL}${path}`, {
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

/** Lê o agregado via service_role — único papel usado para CONFIRMAR o resultado do teste, nunca para provar que o cliente consegue. */
async function readAggregateAsService(metric, date) {
  return rest({
    accessToken: SERVICE_ROLE_KEY,
    method: 'GET',
    path: `/analytics_daily_aggregate?metric=eq.${metric}&date=eq.${date}`,
  });
}

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

test(
  'analytics_daily_aggregate: cliente não lê o agregado; analytics_event isola por titular',
  { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte de RLS pulada; ver README/docs/ci-secrets.md §3' },
  async (t) => {
    const password = 'senha-teste-analytics-9f8e7d6c!';
    const email = uniqueEmail('a');
    const user = await createConfirmedUser(email, password);

    t.after(async () => {
      await deleteUser(user.id);
    });

    const session = await signIn(email, password);

    await t.test('anon não lê analytics_daily_aggregate diretamente', async () => {
      const res = await rest({
        accessToken: ANON_KEY,
        method: 'GET',
        path: '/analytics_daily_aggregate?select=*',
      });
      assert.ok(
        !res.ok,
        `papel anon não deveria conseguir ler public.analytics_daily_aggregate diretamente (status ${res.status})`,
      );
    });

    await t.test('authenticated não lê analytics_daily_aggregate diretamente', async () => {
      const res = await rest({
        accessToken: session.access_token,
        method: 'GET',
        path: '/analytics_daily_aggregate?select=*',
      });
      assert.ok(
        !res.ok,
        `papel authenticated não deveria conseguir ler public.analytics_daily_aggregate diretamente (status ${res.status})`,
      );
    });

    await t.test('anon não grava direto na tabela do agregado (só via função)', async () => {
      const res = await rest({
        accessToken: ANON_KEY,
        method: 'POST',
        path: '/analytics_daily_aggregate',
        body: { date: todayIso(), metric: 'vitrine_aberta', count: 999 },
      });
      assert.ok(
        !res.ok,
        `insert direto de anon em analytics_daily_aggregate deveria ser rejeitado (status ${res.status})`,
      );
    });

    await t.test('bump_metric via RPC (anon) incrementa o agregado sem sujeito', async () => {
      const metric = `vitrine_aberta`;
      const date = todayIso();

      // Lê o valor ANTES de chamar a função e compara o incremento relativo
      // (não um valor absoluto): a chave do agregado é (date, metric), então
      // reexecuções da suíte no mesmo dia-calendário acumulam no mesmo
      // contador — asserção por incremento relativo é o que torna o teste
      // idempotente entre execuções, sem exigir reset do banco a cada rodada.
      const before = await readAggregateAsService(metric, date);
      const baseline = before.body[0]?.count ?? 0;

      // `bump_metric` retorna `void`: PostgREST responde 204 No Content (sem
      // corpo), não 200 — `res.ok` cobre as duas (200–299), mas a asserção
      // é explícita para documentar por que 204 é o resultado esperado, não
      // um sinal de falha.
      const first = await rest({
        accessToken: ANON_KEY,
        method: 'POST',
        path: '/rpc/bump_metric',
        body: { p_metric: metric },
      });
      assert.equal(first.status, 204, `primeira chamada de bump_metric (anon) deveria ter sucesso (204, função void): ${JSON.stringify(first.body)}`);

      const second = await rest({
        accessToken: ANON_KEY,
        method: 'POST',
        path: '/rpc/bump_metric',
        body: { p_metric: metric },
      });
      assert.equal(second.status, 204, `segunda chamada de bump_metric (anon) deveria ter sucesso (204, função void): ${JSON.stringify(second.body)}`);

      const readBack = await readAggregateAsService(metric, date);
      assert.equal(readBack.status, 200);
      assert.equal(readBack.body.length, 1, 'deveria existir exatamente uma linha (date, metric) para o dia corrente');
      assert.equal(readBack.body[0].count, baseline + 2, 'duas chamadas de bump_metric no mesmo dia/métrica deveriam somar +2 ao contador existente');
      assert.equal(Object.hasOwn(readBack.body[0], 'user_id'), false, 'linha do agregado não deveria ter coluna de sujeito');
    });

    await t.test('bump_metric via RPC (authenticated) também incrementa', async () => {
      const metric = 'falha_offline_apresentacao';
      const date = todayIso();

      const before = await readAggregateAsService(metric, date);
      const baseline = before.body[0]?.count ?? 0;

      const call = await rest({
        accessToken: session.access_token,
        method: 'POST',
        path: '/rpc/bump_metric',
        body: { p_metric: metric },
      });
      assert.equal(call.status, 204, `bump_metric (authenticated) deveria ter sucesso (204, função void): ${JSON.stringify(call.body)}`);

      const readBack = await readAggregateAsService(metric, date);
      assert.equal(readBack.status, 200);
      assert.equal(readBack.body.length, 1);
      assert.equal(readBack.body[0].count, baseline + 1, 'uma chamada de bump_metric deveria somar +1 ao contador existente');
    });

    await t.test('bump_metric rejeita métrica fora da lista fechada (CHECK)', async () => {
      const call = await rest({
        accessToken: ANON_KEY,
        method: 'POST',
        path: '/rpc/bump_metric',
        body: { p_metric: 'metrica-inventada-fora-da-lista' },
      });
      assert.ok(!call.ok, 'bump_metric deveria rejeitar métrica fora da lista fechada do CHECK');
    });

    let eventIdOwn;

    await t.test('analytics_event: usuário só grava a própria linha', async () => {
      const ownInsert = await rest({
        accessToken: session.access_token,
        method: 'POST',
        path: '/analytics_event',
        body: {
          user_id: user.id,
          type: 'dia_concluido',
          occurred_at: new Date().toISOString(),
          payload: { dayNumber: 1 },
        },
      });
      assert.equal(ownInsert.status, 201, `insert do próprio evento deveria ter sucesso: ${JSON.stringify(ownInsert.body)}`);
      eventIdOwn = ownInsert.body[0].id;

      const forgedInsert = await rest({
        accessToken: session.access_token,
        method: 'POST',
        path: '/analytics_event',
        body: {
          user_id: '00000000-0000-0000-0000-000000000000',
          type: 'dia_concluido',
          occurred_at: new Date().toISOString(),
          payload: {},
        },
      });
      assert.ok(
        !forgedInsert.ok,
        'insert de evento com user_id de outro titular deveria ser rejeitado pela política RLS (with check)',
      );
    });

    await t.test('analytics_event: anon não lê nem grava evento', async () => {
      const readAsAnon = await rest({
        accessToken: ANON_KEY,
        method: 'GET',
        path: `/analytics_event?id=eq.${eventIdOwn}`,
      });
      assert.ok(!readAsAnon.ok, 'papel anon não deveria conseguir ler public.analytics_event');

      const insertAsAnon = await rest({
        accessToken: ANON_KEY,
        method: 'POST',
        path: '/analytics_event',
        body: {
          user_id: user.id,
          type: 'dia_concluido',
          occurred_at: new Date().toISOString(),
          payload: {},
        },
      });
      assert.ok(!insertAsAnon.ok, 'papel anon não deveria conseguir inserir em public.analytics_event');
    });

    await t.test('analytics_event: usuário lê a própria linha', async () => {
      const readOwn = await rest({
        accessToken: session.access_token,
        method: 'GET',
        path: `/analytics_event?id=eq.${eventIdOwn}`,
      });
      assert.equal(readOwn.status, 200);
      assert.equal(readOwn.body.length, 1);
      assert.equal(readOwn.body[0].user_id, user.id);
    });
  },
);

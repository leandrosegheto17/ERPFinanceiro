// tests/db/rls-profile-consent.test.mjs
//
// TASK-027 (Lote 5 — Backend: Schema e Segurança): teste automatizado que
// comprova que a política de RLS de `profile` e `consent`
// (supabase/migrations/20260908175414_profile_consent_rls.sql) funciona de
// fato — não apenas que RLS está "habilitada", mas que um usuário
// autenticado não consegue ler/gravar linha de outro usuário, nem via
// SELECT nem via INSERT/UPDATE forjando `user_id`. Roda contra o Supabase
// local real (Postgres + PostgREST + GoTrue via `supabase start`), nunca
// simulado/mockado.
//
// Pré-requisito: `supabase start` rodando localmente (Docker) e a migration
// desta tarefa aplicada (`supabase db reset` ou `supabase db push` local).
//
// Rodar com: node --test tests/db/*.test.mjs
//
// Credenciais: `SUPABASE_URL`/`SUPABASE_ANON_KEY`/`SUPABASE_SERVICE_ROLE_KEY`
// são lidas de variáveis de ambiente, com fallback para os valores padrão
// impressos por `supabase start`/`supabase status` neste projeto (ver
// `docs/ci-secrets.md` §3 e `supabase/config.toml`). Esses valores default
// são as chaves de demonstração fixas e públicas do Supabase CLI para
// ambiente **local** — não são segredo (docs/ci-secrets.md, "Local dev
// security notice": "API keys and JWT secrets are shared defaults. Do not
// use in production"). Em CI, o mesmo padrão local é usado, então nenhum
// valor de produção é necessário aqui.

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

async function createConfirmedUser(email, password, userMetadata) {
  const res = await fetch(`${AUTH_URL}/admin/users`, {
    method: 'POST',
    headers: {
      apikey: SERVICE_ROLE_KEY,
      Authorization: `Bearer ${SERVICE_ROLE_KEY}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      email,
      password,
      email_confirm: true,
      ...(userMetadata ? { user_metadata: userMetadata } : {}),
    }),
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

test('RLS de profile e consent isola cada titular das linhas de outro titular', { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte de RLS pulada; ver README/docs/ci-secrets.md §3' }, async (t) => {
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

  // TASK-035/TASK-036 (correção da reprovação crítica do Validador,
  // 2026-09-08 — ver `.md/BLOCKERS.md` Entrada 2): o gatilho de banco
  // `handle_new_auth_user`
  // (`supabase/migrations/20260908190000_profile_on_signup_trigger.sql`)
  // agora cria `profile` automaticamente para todo `auth.users` novo — logo
  // a linha de A/B já existe assim que `createConfirmedUser` (via
  // `/auth/v1/admin/users`) responde, antes de qualquer INSERT pelo próprio
  // usuário. Esta suíte passou a comprovar isso (em vez de repetir um
  // INSERT que agora colide com a linha já criada pelo gatilho) e a manter
  // a mesma cobertura de RLS de sempre: cada titular só grava/lê/atualiza a
  // própria linha, nunca a de outro.
  await t.test('profile: a linha já existe (criada pelo gatilho on_auth_user_created) e só é visível/gravável pelo próprio titular', async () => {
    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'profile',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readOwnAsA.status, 200);
    assert.equal(readOwnAsA.body.length, 1, 'o gatilho deveria ter criado a linha de profile de A no signup/admin-create');

    const readOwnAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'GET',
      table: 'profile',
      query: `?user_id=eq.${userB.id}`,
    });
    assert.equal(readOwnAsB.status, 200);
    assert.equal(readOwnAsB.body.length, 1, 'o gatilho deveria ter criado a linha de profile de B no signup/admin-create');

    // Duplicar a própria linha (já criada pelo gatilho) é rejeitado — não
    // pela política RLS, e sim pela chave primária (`user_id`) —, o que já
    // demonstra que não há como um titular ter mais de uma linha, nem criar
    // a de outro por engano.
    const duplicateOwnInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'profile',
      body: { user_id: userA.id },
    });
    assert.ok(
      !duplicateOwnInsert.ok,
      'insert duplicado da própria linha (já criada pelo gatilho) deveria falhar',
    );

    const forgedInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'profile',
      body: { user_id: userB.id },
    });
    assert.ok(
      !forgedInsert.ok,
      'insert com user_id de outro titular deveria ser rejeitado pela política RLS (with check)',
    );
  });

  await t.test('profile: usuário não lê linha de outro titular', async () => {
    const readAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'GET',
      table: 'profile',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readAsB.status, 200);
    assert.deepEqual(readAsB.body, [], 'B não deveria enxergar a linha de profile de A');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'profile',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readOwnAsA.body.length, 1, 'A deveria enxergar a própria linha de profile');
  });

  await t.test('profile: usuário não atualiza linha de outro titular', async () => {
    const updateAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'PATCH',
      table: 'profile',
      query: `?user_id=eq.${userA.id}`,
      body: { created_at: new Date().toISOString() },
    });
    assert.equal(updateAsB.status, 200);
    assert.deepEqual(updateAsB.body, [], 'update de B sobre a linha de A não deveria afetar nenhuma linha');
  });

  let consentIdA;

  await t.test('consent: usuário só grava a própria linha, com versão/hash do texto aceito', async () => {
    const ownInsert = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'POST',
      table: 'consent',
      body: {
        user_id: userA.id,
        version: '2026-09-08',
        text_sha256: 'a'.repeat(64),
      },
    });
    assert.equal(ownInsert.status, 201, `insert do próprio consentimento deveria ter sucesso: ${JSON.stringify(ownInsert.body)}`);
    consentIdA = ownInsert.body[0].id;

    const forgedInsert = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'POST',
      table: 'consent',
      body: {
        user_id: userA.id,
        version: '2026-09-08',
        text_sha256: 'b'.repeat(64),
      },
    });
    assert.ok(
      !forgedInsert.ok,
      'B inserir um consentimento em nome de A deveria ser rejeitado pela política RLS (with check)',
    );
  });

  await t.test('consent: usuário não lê nem revoga consentimento de outro titular', async () => {
    const readAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'GET',
      table: 'consent',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.equal(readAsB.status, 200);
    assert.deepEqual(readAsB.body, [], 'B não deveria enxergar o consentimento de A');

    const revokeAsB = await restAsUser({
      accessToken: sessionB.access_token,
      method: 'PATCH',
      table: 'consent',
      query: `?id=eq.${consentIdA}`,
      body: { revoked_at: new Date().toISOString() },
    });
    assert.equal(revokeAsB.status, 200);
    assert.deepEqual(revokeAsB.body, [], 'B não deveria conseguir revogar o consentimento de A');

    const readOwnAsA = await restAsUser({
      accessToken: sessionA.access_token,
      method: 'GET',
      table: 'consent',
      query: `?id=eq.${consentIdA}`,
    });
    assert.equal(readOwnAsA.body.length, 1);
    assert.equal(readOwnAsA.body[0].revoked_at, null, 'consentimento de A não deveria ter sido revogado por B');
  });

  await t.test('anônimo (sem sessão de usuário) não lê nenhuma linha de nenhuma tabela', async () => {
    // Sem sessão de usuário, o papel efetivo é `anon`, que não tem GRANT de
    // tabela em `profile`/`consent` (SDD.md §7.2 — estas duas tabelas exigem
    // sessão autenticada). O bloqueio acontece na camada de privilégio do
    // Postgres, antes mesmo de a política de RLS ser avaliada — PostgREST
    // reporta isso como 401 (permission denied), não como uma lista vazia.
    const anonProfile = await restAsUser({
      accessToken: ANON_KEY,
      method: 'GET',
      table: 'profile',
      query: `?user_id=eq.${userA.id}`,
    });
    assert.ok(!anonProfile.ok, 'papel anon não deveria conseguir ler public.profile');

    const anonConsent = await restAsUser({
      accessToken: ANON_KEY,
      method: 'GET',
      table: 'consent',
      query: `?id=eq.${consentIdA}`,
    });
    assert.ok(!anonConsent.ok, 'papel anon não deveria conseguir ler public.consent');
  });
});

/**
 * TASK-035 + TASK-036 — correção da reprovação crítica do Validador
 * (2026-09-08, `.md/BLOCKERS.md` Entrada 2, achado 1): comprova que
 * `profile`+`consent` nascem atomicamente, dentro da mesma transação de
 * banco que cria `auth.users`, a partir da metadata `consent_version`/
 * `consent_text_sha256` enviada no próprio cadastro — nunca por uma
 * segunda chamada HTTP separada do cliente. Este é o mesmo caminho usado
 * por `signUpWithConsent` (`src/features/identity/sign-up-with-consent.ts`,
 * via `options.data` do `auth.signUp`); aqui simulado com
 * `/admin/users` + `user_metadata` (equivalente, do ponto de vista do
 * gatilho `handle_new_auth_user`, que lê `raw_user_meta_data` sem saber
 * como o usuário foi criado).
 */
test(
  'gatilho on_auth_user_created cria profile+consent atomicamente a partir da metadata do signUp (TASK-035/TASK-036)',
  { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte pulada; ver README/docs/ci-secrets.md §3' },
  async (t) => {
    const email = uniqueEmail('consent-metadata');
    const password = 'senha-teste-consent-9f8e7d6c!';
    const consentVersion = '2026-09-08.v1';
    const consentTextSha256 = 'c'.repeat(64);

    const user = await createConfirmedUser(email, password, {
      consent_version: consentVersion,
      consent_text_sha256: consentTextSha256,
    });
    t.after(async () => {
      await deleteUser(user.id);
    });

    const session = await signIn(email, password);

    const profile = await restAsUser({
      accessToken: session.access_token,
      method: 'GET',
      table: 'profile',
      query: `?user_id=eq.${user.id}`,
    });
    assert.equal(profile.status, 200);
    assert.equal(profile.body.length, 1, 'profile deveria ter sido criado pelo gatilho, junto com consent');

    const consent = await restAsUser({
      accessToken: session.access_token,
      method: 'GET',
      table: 'consent',
      query: `?user_id=eq.${user.id}`,
    });
    assert.equal(consent.status, 200);
    assert.equal(
      consent.body.length,
      1,
      'consent deveria ter sido criado pelo gatilho a partir da metadata do cadastro, sem nenhum INSERT separado do cliente',
    );
    assert.equal(consent.body[0].version, consentVersion);
    assert.equal(consent.body[0].text_sha256, consentTextSha256);
  },
);

/**
 * Contraprova do teste acima: sem a metadata de consentimento (mesmo
 * caminho usado pelos usuários criados só para os testes de RLS, sem
 * relação com o fluxo real de cadastro), o gatilho continua criando
 * `profile` (comportamento original de TASK-035), mas não inventa uma
 * linha de `consent` que o titular nunca aceitou.
 */
test(
  'gatilho on_auth_user_created não cria consent quando a metadata de consentimento não foi enviada',
  { skip: !localSupabaseUp && 'Supabase local (supabase start) não está respondendo — suíte pulada; ver README/docs/ci-secrets.md §3' },
  async (t) => {
    const email = uniqueEmail('sem-consent-metadata');
    const password = 'senha-teste-sem-consent-9f8e7d6c!';

    const user = await createConfirmedUser(email, password);
    t.after(async () => {
      await deleteUser(user.id);
    });

    const session = await signIn(email, password);

    const profile = await restAsUser({
      accessToken: session.access_token,
      method: 'GET',
      table: 'profile',
      query: `?user_id=eq.${user.id}`,
    });
    assert.equal(profile.status, 200);
    assert.equal(profile.body.length, 1, 'profile deveria continuar sendo criado pelo gatilho, com ou sem metadata de consent');

    const consent = await restAsUser({
      accessToken: session.access_token,
      method: 'GET',
      table: 'consent',
      query: `?user_id=eq.${user.id}`,
    });
    assert.equal(consent.status, 200);
    assert.equal(consent.body.length, 0, 'sem metadata de consentimento, o gatilho não deveria inventar uma linha de consent');
  },
);

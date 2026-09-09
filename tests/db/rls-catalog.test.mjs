// tests/db/rls-catalog.test.mjs
//
// TASK-033 (Lote 5 — Backend: Schema e Segurança): teste de catálogo (RT-04 /
// GUARDRAILS.md G-09 / SDD.md §7.2, DI-12) que confirma MECANICAMENTE, contra
// o catálogo real do Postgres local (`pg_class.relrowsecurity` +
// `information_schema.columns`), que:
//
//   TODA tabela do schema `public` que tem uma coluna `user_id` tem RLS
//   habilitada (`relrowsecurity = true`).
//
// Decisão de design (por que consulta o catálogo em vez de listar as 9
// tabelas conhecidas): o objetivo do gate (DI-12) é pegar uma tabela FUTURA
// que alguém adicione com `user_id` e esqueça de habilitar RLS, sem que
// ninguém precise lembrar de atualizar este teste a cada nova migration. Uma
// lista hardcoded das tabelas de hoje (`profile`, `consent`,
// `plan_progress`, `preference`, `push_subscription`, `reminder_log`,
// `analytics_event`, `erasure_request` — nota: `analytics_daily_aggregate`
// fica de fora por desenho, é agregado anônimo sem `user_id`, ver SDD §7.2/
// ADR-009) teria exatamente o defeito que RT-04 pede para evitar: alguém
// adiciona uma tabela nova sem RLS e o teste continua passando porque nunca
// nem olhou para ela. A consulta abaixo é genérica: varre TODAS as tabelas
// de `public` a cada execução, sem hardcode de nome de tabela.
//
// Por que conexão direta ao Postgres (`pg`) em vez de PostgREST: o catálogo
// (`pg_class`, `information_schema`) não é exposto via API REST — é
// metadado de schema, não dado de aplicação. A conexão usa a credencial
// padrão do Supabase local (`postgres:postgres@127.0.0.1:<db.port>`, a mesma
// que `supabase start` imprime), nunca uma credencial de produção — mesmo
// espírito de `docs/ci-secrets.md` §3 (valores locais, não de produção).
//
// Prova de que o teste falha de verdade quando deveria (não é uma asserção
// vazia): o segundo teste deste arquivo ("autoteste") cria, dentro da própria
// suíte, uma tabela temporária com `user_id` e SEM RLS habilitada, roda a
// MESMA função de verificação usada no primeiro teste, confirma que ela é
// detectada como ofensora, e só então remove a tabela (em `t.after`, mesmo
// em caso de falha do teste). Isso mantém a prova empírica como parte
// permanente e executável da suíte, em vez de um passo manual que alguém
// rodou uma vez e documentou em prosa — se a lógica de detecção regredir
// (por exemplo, alguém "arruma" a query e ela para de pegar tabela sem RLS),
// este autoteste quebra imediatamente.
//
// Pré-requisito: `supabase start` rodando localmente (Docker). Sem isso, a
// suíte é pulada (mesmo padrão dos demais arquivos de tests/db/*.test.mjs),
// exceto em CI, onde `supabase start` roda como passo anterior do workflow
// (ver .github/workflows/ci.yml) — lá a suíte não pode ser pulada sem falhar
// o job, é o próprio gate do critério de aceite "CI falha ao adicionar
// tabela sem RLS de propósito".
//
// Rodar com: node --test tests/db/*.test.mjs (ou npm run test:db)

import test, { after } from 'node:test';
import assert from 'node:assert/strict';
import pg from 'pg';

const { Client } = pg;

const DB_URL =
  process.env.SUPABASE_DB_URL ?? 'postgresql://postgres:postgres@127.0.0.1:54422/postgres';

async function connectOrNull() {
  const client = new Client({ connectionString: DB_URL, connectionTimeoutMillis: 2000 });
  try {
    await client.connect();
    return client;
  } catch {
    return null;
  }
}

let sharedClient = await connectOrNull();
const localPostgresUp = sharedClient !== null;

/**
 * Consulta genérica do catálogo: toda tabela de `public` (`relkind = 'r'`,
 * tabela comum — exclui view/matview/tabela particionada-pai etc.) que tem
 * uma coluna chamada `user_id`, junto com o flag de RLS habilitada.
 *
 * Não hardcoda nenhum nome de tabela — varre o catálogo inteiro a cada
 * chamada, então uma tabela criada por uma migration futura entra
 * automaticamente na varredura sem precisar tocar este arquivo.
 */
async function tablesWithUserIdColumn(client) {
  const { rows } = await client.query(`
    select
      c.relname as table_name,
      c.relrowsecurity as rls_enabled
    from pg_catalog.pg_class c
    join pg_catalog.pg_namespace n on n.oid = c.relnamespace
    where n.nspname = 'public'
      and c.relkind = 'r'
      and exists (
        select 1
        from information_schema.columns col
        where col.table_schema = 'public'
          and col.table_name = c.relname
          and col.column_name = 'user_id'
      )
    order by c.relname;
  `);
  return rows;
}

async function offendersWithoutRls(client) {
  const rows = await tablesWithUserIdColumn(client);
  return rows.filter((row) => row.rls_enabled !== true).map((row) => row.table_name);
}

test(
  'catálogo: toda tabela de public com coluna user_id tem RLS habilitada (RT-04)',
  { skip: !localPostgresUp && 'Postgres local (supabase start) não está respondendo — suíte de RLS pulada; ver docs/ci-secrets.md §3' },
  async () => {
    const client = sharedClient;

    const rows = await tablesWithUserIdColumn(client);

    // Sanidade do próprio teste: se a varredura não encontrou NENHUMA
    // tabela com user_id, algo está errado com a conexão/schema (as 8
    // tabelas do Lote 5 com user_id deveriam aparecer) — falha alto e claro
    // em vez de passar vazio por omissão silenciosa.
    assert.ok(
      rows.length > 0,
      'esperava encontrar ao menos uma tabela em public com coluna user_id (schema vazio ou migrations não aplicadas?)',
    );

    const offenders = rows.filter((row) => row.rls_enabled !== true);
    assert.deepEqual(
      offenders,
      [],
      `tabela(s) com user_id e RLS desabilitada (viola RT-04/GUARDRAILS.md G-09): ${JSON.stringify(offenders)}`,
    );
  },
);

test(
  'autoteste: detecta de fato uma tabela sem RLS de propósito (prova empírica do gate)',
  { skip: !localPostgresUp && 'Postgres local (supabase start) não está respondendo — suíte de RLS pulada; ver docs/ci-secrets.md §3' },
  async (t) => {
    const client = sharedClient;
    const offenderTable = `__rls_catalog_test_offender_${Date.now()}`;

    t.after(async () => {
      await client.query(`drop table if exists public."${offenderTable}";`);
    });

    // Cria, só para este teste, uma tabela com user_id e SEM RLS habilitada
    // — de propósito, para provar que a função de detecção acima pega o
    // caso que deveria pegar, e não é uma asserção vazia.
    await client.query(`
      create table public."${offenderTable}" (
        id uuid primary key default gen_random_uuid(),
        user_id uuid not null
      );
    `);

    const offendersBefore = await offendersWithoutRls(client);
    assert.ok(
      offendersBefore.includes(offenderTable),
      `a tabela de teste "${offenderTable}" (user_id, sem RLS) deveria ter sido detectada como ofensora, mas não foi — a lógica de detecção não está funcionando`,
    );

    // Habilita RLS na tabela de teste e confirma que ela some da lista de
    // ofensoras — fecha o círculo: a mesma verificação reage a `relrowsecurity`
    // mudando de false para true, não é um efeito de outra coisa.
    await client.query(`alter table public."${offenderTable}" enable row level security;`);

    const offendersAfter = await offendersWithoutRls(client);
    assert.ok(
      !offendersAfter.includes(offenderTable),
      `depois de habilitar RLS, a tabela de teste "${offenderTable}" não deveria mais aparecer como ofensora`,
    );
  },
);

after(async () => {
  if (sharedClient) {
    await sharedClient.end();
  }
});

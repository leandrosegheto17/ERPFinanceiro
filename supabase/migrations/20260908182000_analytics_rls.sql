-- TASK-031 (Lote 5 — Backend: Schema e Segurança) [insep.]
--
-- Cria `analytics_event` + `analytics_daily_aggregate` + função de incremento
-- sem identificador, conforme o modelo de dados do SDD.md §5.3/§7.2 e
-- ADR-009 (telemetria first-party em dois níveis). RLS habilitada nas DUAS
-- tabelas na MESMA migration que as cria (DI-03 / GUARDRAILS.md G-09 /
-- RT-04). Inseparável (marcada `[insep.]` no TASK.md) porque a função de
-- incremento do nível 1 só faz sentido junto do schema das duas tabelas que
-- ela liga (evento identificado vs. agregado anônimo) — dividir a migration
-- deixaria uma das duas metades sem RLS por uma migration inteira, violando
-- DI-03/RT-04 no meio do caminho.
--
-- Nível 2 — `analytics_event`: evento identificado, só depois do
-- consentimento (ADR-009). Mesmo modelo de propriedade das demais tabelas do
-- titular (SDD.md §7.2, linha `analytics_event`): RLS por
-- `auth.uid() = user_id`. Deliberadamente só SELECT/INSERT (sem
-- UPDATE/DELETE) expostos a `authenticated`: um evento telemétrico é
-- imutável uma vez recebido (mesmo raciocínio de `plan_progress`,
-- TASK-028) — o `occurred_at` é o relógio do dispositivo no momento do
-- evento e não deve ser reescrito depois; correção de relógio incorreto é
-- tratada na apuração (RT-06 do SDD.md, via `received_at`), nunca por
-- UPDATE do cliente. A exclusão em cascata do titular (RN-11) segue o fluxo
-- de `erasure_request` (TASK-032) via `service_role`, não DELETE direto do
-- cliente.
--
-- Nível 1 — `analytics_daily_aggregate`: agregado anônimo, sem sujeito
-- (`date`, `metric`, `count` — SDD.md §5.3, linha
-- `analytics_daily_aggregate`). **Sem NENHUMA política de SELECT nem GRANT
-- de tabela para `authenticated`/`anon`** — o cliente nunca lê o agregado
-- diretamente (SDD.md §7.2: "Sem leitura pelo cliente"), critério de
-- aceite desta tarefa é literal nesse ponto. O único ponto de escrita é a
-- função `public.bump_metric`, `SECURITY DEFINER` (roda com o privilégio de
-- quem a criou — nesta migration, o papel de migração do Supabase, que não
-- está sujeito a RLS de `authenticated`/`anon` — e ignora completamente a
-- ausência de GRANT de tabela do chamador, porque não é o chamador quem
-- toca a tabela: é a função). `metric` é restrito por CHECK à lista fechada
-- de métricas do nível 1 definida em ADR-009 (`soft_gate_exibido`,
-- `soft_gate_aceito`, `soft_gate_recusado`, `vitrine_aberta`,
-- `falha_offline_apresentacao`, CA-09.5/M-04/M-06) — evita que um chamador
-- da RPC grave métrica arbitrária na tabela através da função privilegiada.

create table if not exists public.analytics_event (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  type text not null,
  occurred_at timestamptz not null,
  received_at timestamptz not null default now(),
  payload jsonb not null default '{}'::jsonb
);

comment on table public.analytics_event is
  'Nível 2 (ADR-009): evento identificado, só após consentimento (RF-18, '
  'CA-18.1 a CA-18.4). `occurred_at` é o relógio do dispositivo; '
  '`received_at` é o relógio do servidor, usado para descartar divergência '
  'absurda na apuração (RT-06). Imutável após inserido: sem UPDATE/DELETE '
  'para `authenticated` — exclusão só via cascata de `erasure_request` '
  '(RN-11, TASK-032) por `service_role`.';

create index if not exists analytics_event_user_id_idx
  on public.analytics_event (user_id);

alter table public.analytics_event enable row level security;

create policy "analytics_event_select_own"
  on public.analytics_event
  for select
  to authenticated
  using (auth.uid() = user_id);

create policy "analytics_event_insert_own"
  on public.analytics_event
  for insert
  to authenticated
  with check (auth.uid() = user_id);

-- GRANT explícito (mesmo racional documentado em TASK-027/TASK-028: este
-- Supabase local não tem `auto_expose_new_tables`). Só SELECT/INSERT, sem
-- UPDATE/DELETE, coerente com a ausência de política para essas operações
-- acima. Nenhum GRANT para `anon`: nível 2 só existe pós-consentimento, que
-- exige sessão autenticada.
grant usage on schema public to authenticated;
grant select, insert on public.analytics_event to authenticated;

create table if not exists public.analytics_daily_aggregate (
  date date not null,
  metric text not null check (
    metric in (
      'soft_gate_exibido',
      'soft_gate_aceito',
      'soft_gate_recusado',
      'vitrine_aberta',
      'falha_offline_apresentacao'
    )
  ),
  count bigint not null default 0 check (count >= 0),
  primary key (date, metric)
);

comment on table public.analytics_daily_aggregate is
  'Nível 1 (ADR-009): agregado anônimo, sem sujeito — sem coluna de '
  'usuário, sessão, dispositivo, IP ou user-agent (CA-09.5, M-06). Sem '
  'nenhuma política de SELECT nem GRANT de tabela para '
  '`authenticated`/`anon` (SDD.md §7.2): o cliente NUNCA lê o agregado '
  'diretamente. Único ponto de escrita: `public.bump_metric` '
  '(SECURITY DEFINER).';

alter table public.analytics_daily_aggregate enable row level security;

-- Deliberadamente NENHUMA política de RLS criada para esta tabela — nem
-- select, nem insert, nem update, nem delete, para nenhum papel. RLS
-- habilitada sem política nenhuma equivale a "ninguém acessa por essa via",
-- que é exatamente o requisito (SDD.md §7.2: "Sem leitura pelo cliente").
-- A escrita acontece exclusivamente dentro de `bump_metric`, que roda como
-- o dono da função (bypassa RLS do chamador) — nunca através de uma
-- política concedida a `authenticated`/`anon`.
--
-- Nenhum GRANT de tabela para `authenticated`/`anon` nesta tabela: mesmo
-- sem GRANT, uma política eventual não adiantaria nada — mas a ausência de
-- GRANT reforça a intenção na camada de privilégio também, mesmo padrão já
-- usado para "sem operação exposta" em `plan_progress` (TASK-028).
--
-- GRANT de SELECT para `service_role`: achado real ao validar esta
-- migration — o padrão "always-revoked" deste projeto Supabase local
-- (`auto_expose_new_tables` não habilitado, `supabase/config.toml`) revoga
-- privilégio de tabela por padrão para TODO papel de API em tabela nova,
-- **inclusive `service_role`** (confirmado empiricamente: `\dp` no Postgres
-- mostrava `service_role=Dxtm/postgres`, sem `r` de SELECT, antes deste
-- GRANT) — não é específico de `anon`/`authenticated` como em
-- `profile`/`consent`/`plan_progress`. Isso não compromete "cliente não lê
-- o agregado" (SDD.md §7.2): `service_role` nunca é a chave usada pelo
-- cliente (GUARDRAILS G-10 proíbe expor credencial de serviço no bundle) —
-- é o papel que a apuração de métricas via SQL versionado (ADR-009,
-- "Apuração") e o teste automatizado desta tarefa usam para inspecionar o
-- resultado de `bump_metric` no servidor, nunca a partir do navegador.
grant select on public.analytics_daily_aggregate to service_role;

create or replace function public.bump_metric(p_metric text)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
  insert into public.analytics_daily_aggregate (date, metric, count)
  values (current_date, p_metric, 1)
  on conflict (date, metric)
  do update set count = public.analytics_daily_aggregate.count + 1;
end;
$$;

comment on function public.bump_metric(text) is
  'Nível 1 (ADR-009): único ponto de escrita do agregado anônimo. '
  'SECURITY DEFINER: roda com o privilégio de quem criou a função (não do '
  'chamador), bypassando RLS/GRANT de `analytics_daily_aggregate` do lado '
  'do chamador — por isso funciona tanto para `anon` (soft gate '
  'pré-consentimento) quanto para `authenticated`. `p_metric` fora da lista '
  'fechada do CHECK da tabela falha com violação de constraint, não grava '
  'métrica arbitrária. Sem parâmetro de identidade: a função não aceita '
  '`user_id`/sessão/dispositivo, então não há como reconstituir "quem" a '
  'partir do agregado (CA-09.5, M-06).';

-- `EXECUTE` concedido a `anon` E `authenticated`: o gate de consentimento
-- (CA-09.5, M-06 — `soft_gate_exibido`/`aceito`/`recusado`) e a vitrine
-- (`vitrine_aberta`) acontecem ANTES de qualquer conta existir, e
-- `falha_offline_apresentacao` (M-04) pode ocorrer em qualquer sessão,
-- autenticada ou não. `grant usage on schema public to anon` é necessário
-- para o papel `anon` sequer localizar a função via `rpc.bump_metric` do
-- PostgREST — mesmo racional de GRANT explícito já documentado em
-- TASK-027/TASK-028 para tabela, aqui aplicado a função.
grant usage on schema public to anon;
grant execute on function public.bump_metric(text) to anon, authenticated;

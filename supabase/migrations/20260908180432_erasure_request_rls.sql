-- TASK-032 (Lote 5 — Backend: Schema e Segurança)
--
-- Cria `erasure_request`, conforme o modelo de dados do SDD.md §5.3, com RLS
-- habilitada na MESMA migration que cria a tabela (DI-03 / GUARDRAILS.md G-09
-- / RT-04). Modelo de autorização: propriedade simples por titular
-- (SDD.md §7.2) — `auth.uid() = user_id`.
--
-- `erasure_request` registra o pedido de exclusão de conta do titular
-- (RN-11, SDD.md §7.6: exclusão em até 15 dias) — a base sobre a qual
-- `jobs/erasure` (TASK-041, Lote 7) executa a cascata de exclusão dentro do
-- SLA, e que a tela T-13 de exclusão (TASK-040, Lote 6) usa para mostrar o
-- estado do pedido ao usuário.
--
-- Cardinalidade do ER do SDD.md §5.3 (`PROFILE ||--o| ERASURE_REQUEST`): no
-- máximo um pedido de exclusão por titular — `unique (user_id)` abaixo torna
-- essa cardinalidade uma invariante de banco, não só um desenho de diagrama;
-- não há caso de negócio de reabrir/repetir um pedido para a mesma conta
-- viva (o pedido é terminal: ou completa a exclusão em cascata, ou a conta
-- deixa de existir e a linha é arrastada pelo `on delete cascade`).
--
-- `due_at = requested_at + 15 dias` (RN-11) é calculado por um trigger
-- `before insert` (`set_erasure_request_due_at`) que **sempre** sobrescreve
-- `NEW.due_at`, ignorando qualquer valor enviado pelo cliente no corpo do
-- INSERT — não foi possível usar coluna gerada (`generated always as ...
-- stored`) porque aritmética de `timestamptz` não é IMMUTABLE aos olhos do
-- Postgres (depende do timezone da sessão em `+ interval`), e uma expressão
-- de coluna gerada precisa ser IMMUTABLE. O trigger é o equivalente
-- funcional: nenhum cliente autenticado consegue gravar um `due_at`
-- arbitrário — nem por INSERT (o trigger recalcula e substitui o valor
-- antes da gravação), nem por UPDATE (não existe política de update para
-- `authenticated`, ver abaixo).
--
-- `completed_at` só é preenchido por `jobs/erasure` (TASK-041), que roda com
-- `service_role` — papel que ignora RLS por definição do Postgres/Supabase e,
-- portanto, não precisa (nem deve) de política própria aqui. O cliente nunca
-- tem caminho de UPDATE sobre esta tabela: apenas `select`/`insert` da
-- própria linha. Alterar `due_at`/`completed_at`/status do pedido é
-- exclusivamente responsabilidade da rotina de serviço, nunca do titular.

create table if not exists public.erasure_request (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  requested_at timestamptz not null default now(),
  due_at timestamptz not null,
  completed_at timestamptz,
  unique (user_id)
);

create or replace function public.set_erasure_request_due_at()
returns trigger
language plpgsql
as $$
begin
  -- Sempre recalcula a partir de requested_at (RN-11: +15 dias), ignorando
  -- qualquer due_at enviado no corpo da requisição — trigger roda como
  -- dono da função (não SECURITY DEFINER, papel do chamador é irrelevante
  -- aqui: a garantia vem de sempre sobrescrever, não de checar permissão).
  new.due_at := new.requested_at + interval '15 days';
  return new;
end;
$$;

create trigger erasure_request_set_due_at
  before insert on public.erasure_request
  for each row
  execute function public.set_erasure_request_due_at();

comment on table public.erasure_request is
  'Pedido de exclusão de conta do titular (RN-11, SDD.md §7.6). '
  '`due_at` é gerado a partir de `requested_at` (+15 dias) e não é gravável '
  'pelo cliente; `completed_at` só é preenchido por `jobs/erasure` '
  '(service_role, TASK-041). No máximo um pedido por titular (unique '
  'user_id) — o pedido é terminal.';

create index if not exists erasure_request_due_at_idx
  on public.erasure_request (due_at)
  where completed_at is null;

alter table public.erasure_request enable row level security;

create policy "erasure_request_select_own"
  on public.erasure_request
  for select
  to authenticated
  using (auth.uid() = user_id);

create policy "erasure_request_insert_own"
  on public.erasure_request
  for insert
  to authenticated
  with check (auth.uid() = user_id);

-- Sem política de update/delete para `authenticated`: uma vez criado, o
-- pedido só é alterado (preenchimento de `completed_at`) ou removido (via
-- cascata de exclusão do próprio usuário em `auth.users`) pela rotina de
-- serviço (`service_role`, `jobs/erasure`), nunca pelo titular diretamente —
-- é exatamente essa a garantia pedida pela tarefa ("usuário não pode alterar
-- due_at/status arbitrariamente").
--
-- GRANT explícito: mesmo racional já documentado em
-- `20260908175414_profile_consent_rls.sql` (TASK-027) — este Supabase local
-- roda sem `auto_expose_new_tables`, então falta o privilégio de tabela do
-- Postgres sem o GRANT abaixo, mesmo com RLS/política corretas. `usage on
-- schema public` é reconcedido aqui de forma idempotente (já concedido pela
-- migration de TASK-027) só para esta migration não depender silenciosamente
-- da ordem de aplicação de outra tarefa do mesmo lote. Apenas
-- `select`/`insert` para `authenticated` — nunca `update`/`delete`, coerente
-- com a ausência de política para essas operações acima. Nenhum GRANT para
-- `anon`: pedido de exclusão exige sessão autenticada (SDD.md §7.2).
grant usage on schema public to authenticated;
grant select, insert on public.erasure_request to authenticated;

-- `service_role` (jobs/erasure, TASK-041) ignora RLS por definição, mas
-- ainda depende do privilégio de tabela do Postgres para acessar via
-- PostgREST — e o privilégio padrão de `pg_default_acl` para tabela criada
-- pela role de migração (`postgres`) só concede `service_role`
-- delete/truncate/trigger/references, não select/update (confirmado por
-- consulta a `pg_default_acl` neste ambiente). Sem este GRANT, a rotina de
-- exclusão não conseguiria ler o pedido pendente nem gravar
-- `completed_at`.
grant select, update on public.erasure_request to service_role;

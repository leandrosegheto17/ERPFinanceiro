-- TASK-028 (Lote 5 — Backend: Schema e Segurança)
--
-- Cria `plan_progress`, conforme o modelo de dados do SDD.md §5.3, com RLS
-- habilitada na MESMA migration que cria a tabela (DI-03 / GUARDRAILS.md G-09
-- / RT-04). Modelo de autorização: propriedade simples por titular (SDD.md
-- §7.2) — cada linha só é visível/gravável por quem tem
-- `auth.uid() = user_id`.
--
-- `plan_progress`: registra a conclusão de um dia do plano pelo titular
-- (RF-03/RF-04). É **append-only, com união monotônica** (ADR-007, "Progresso
-- do plano"): a chave `UNIQUE(user_id, plan_id, day_number)` é o mecanismo
-- que torna a conciliação entre dispositivos uma propriedade do schema — o
-- cliente sempre envia `INSERT ... ON CONFLICT (user_id, plan_id,
-- day_number) DO NOTHING`, preservando o `completed_at` da primeira escrita
-- em vez de sobrescrever. Não existe `UPDATE`/`DELETE` exposto a
-- `authenticated`: uma vez concluído, um dia nunca "desconclui" pelo próprio
-- titular (RN-08, CA-03.5, CA-11.3) — a única forma de remover linhas de
-- `plan_progress` é a cascata de `erasure_request` (RN-11, TASK-032),
-- executada por rotina de serviço com `service_role`.
--
-- `plan_id` é `text`, não FK para tabela alguma: `Plan` é conteúdo estático
-- gerado em build (SDD.md §5.1 — "Não existem aqui: dia-calendário, usuário,
-- progresso"), não uma tabela do Postgres; `plan_id` referencia o `Plan.id`
-- do bundle de conteúdo editorial publicado, fora do banco (mesmo raciocínio
-- de `core/storage` no cliente, onde `planId`/`dayNumber` já são
-- `string`/`number` sem chave estrangeira local — `src/core/storage/types.ts`).

create table if not exists public.plan_progress (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  plan_id text not null,
  day_number integer not null check (day_number > 0),
  completed_at timestamptz not null default now(),
  unique (user_id, plan_id, day_number)
);

comment on table public.plan_progress is
  'Conclusão de dia do plano por titular (RF-03/RF-04). Append-only: união '
  'monotônica via UNIQUE(user_id, plan_id, day_number) + INSERT ... ON '
  'CONFLICT DO NOTHING no cliente (ADR-007). Sem UPDATE/DELETE expostos a '
  '`authenticated` — RN-08, CA-03.5, CA-11.3.';

create index if not exists plan_progress_user_id_idx
  on public.plan_progress (user_id);

alter table public.plan_progress enable row level security;

create policy "plan_progress_select_own"
  on public.plan_progress
  for select
  to authenticated
  using (auth.uid() = user_id);

create policy "plan_progress_insert_own"
  on public.plan_progress
  for insert
  to authenticated
  with check (auth.uid() = user_id);

-- Deliberadamente NENHUMA política de update/delete para `authenticated`: a
-- tabela é append-only por design (RN-08) — ver comentário acima e ADR-007.
-- A exclusão de conta (RN-11) segue o fluxo de `erasure_request` (TASK-032),
-- executado por `service_role`, que ignora RLS por definição do
-- Postgres/Supabase, e não pelo cliente autenticado.
--
-- GRANT explícito: este projeto Supabase local roda com
-- `auto_expose_new_tables` não habilitado (`supabase/config.toml`), mesmo
-- padrão já documentado e confirmado empiricamente em
-- 20260908175414_profile_consent_rls.sql (TASK-027) e reconfirmado aqui via
-- PostgREST real (`tests/db/rls-plan-progress.test.mjs`, requisição HTTP
-- direta contra o Supabase local, não só leitura de código): sem o GRANT,
-- toda chamada de `authenticated`/`anon` falha com "permission denied for
-- table" (42501/HTTP 401) já na camada de privilégio do Postgres, antes de a
-- RLS ser avaliada. Só SELECT/INSERT concedidos a `authenticated` (sem
-- UPDATE/DELETE), coerente com a ausência de política para essas duas
-- operações acima — quando um cliente autenticado tenta UPDATE/DELETE, a
-- rejeição já acontece na camada de privilégio de tabela (mesmo resultado
-- prático de "0 linhas afetadas/erro de permissão" que a RLS também daria se
-- o GRANT existisse sem política equivalente); manter os dois reforços
-- (sem GRANT de UPDATE/DELETE, sem política de UPDATE/DELETE) documenta a
-- intenção "append-only" nas duas camadas, não só numa. Nenhum GRANT para
-- `anon`: leitura/escrita desta tabela exige sessão autenticada (SDD.md
-- §7.2) — visitante sem conta recebe "permission denied" na camada de
-- privilégio de tabela, não uma lista vazia.
grant usage on schema public to authenticated;
grant select, insert on public.plan_progress to authenticated;

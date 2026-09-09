-- TASK-030 (Lote 5 — Backend: Schema e Segurança)
--
-- [insep.] Cria `push_subscription` e `reminder_log` na MESMA migration,
-- conforme o modelo de dados do SDD.md §5.3, com RLS habilitada nas duas
-- tabelas nesta mesma migration (DI-03 / GUARDRAILS.md G-09 / RT-04).
-- Inseparabilidade: as duas tabelas formam o mesmo par funcional (assinatura
-- de push + desfecho de lembrete, RF-05/CA-05.7/M-08) e nenhuma das duas tem
-- sentido isolado nesta fase — TASK-056/057 (Lote 11) consomem as duas
-- juntas. Modelo de autorização: propriedade simples por titular (SDD.md
-- §7.2) — cada linha só é visível/gravável por quem tem
-- `auth.uid() = user_id`.
--
-- `push_subscription`: assinatura de Web Push (VAPID) do dispositivo do
-- titular (RF-05). Campos essenciais do SDD.md §5.3: `user_id`, endpoint,
-- chaves, `platform`, `last_seen_at`. Modelado com `endpoint` como chave
-- natural de deduplicação por dispositivo/navegador (a API padrão de Web
-- Push nunca reaproveita o mesmo endpoint para assinaturas distintas) via
-- `unique (user_id, endpoint)` — reassinar o mesmo endpoint atualiza a
-- linha em vez de duplicar. `p256dh`/`auth_key` são as duas chaves exigidas
-- pelo protocolo Web Push (RFC 8291/8292) para cifrar o payload — nomeadas
-- por fora do termo genérico "chaves" do SDD.md para não colidir com
-- `public.consent` nem com nenhuma outra tabela. `platform` é texto livre
-- curto (ex.: "ios", "android", "desktop") — o SDD.md §5.3 não fecha um
-- enum, e o texto de UX-SPEC/ADR-010 sobre estado do canal por plataforma é
-- fora do escopo desta migration (é leitura/exibição, não schema).
-- `last_seen_at` é atualizado pelo cliente a cada resposta bem-sucedida do
-- Service Worker/`pushManager`, dando ao backend (TASK-057) um sinal de
-- assinatura ainda viva sem depender só do retorno de erro do provedor de
-- push no momento do envio.
--
-- `reminder_log`: log de desfecho de um lembrete agendado (CA-05.7, M-08,
-- RT-07). Campos essenciais do SDD.md §5.3: `user_id`, `scheduled_for`,
-- `sent_at`, `outcome`, `channel`. `outcome` é um enum fechado
-- (`sent`/`failed`/`skipped`) — deliberadamente sem "delivered"/"opened":
-- Web Push não garante confirmação de entrega/abertura ao servidor, então
-- modelar esses estados criaria um campo que nunca seria preenchido de
-- verdade (SPIKE-01, RT-02). `channel` é texto livre curto (ex.: "push",
-- "in-app") — mesma decisão de não fechar enum nesta migration que
-- `platform` acima, pelo mesmo motivo (SDD.md §5.3 não fecha um). Índice
-- único por `(user_id, scheduled_for)` implementa o "envio idempotente por
-- dia" exigido pela mitigação de RT-07 diretamente na constraint do banco,
-- não só na lógica de aplicação — uma segunda tentativa de agendar o mesmo
-- lembrete para o mesmo titular no mesmo instante-alvo falha na escrita em
-- vez de duplicar o log.

create table if not exists public.push_subscription (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  endpoint text not null,
  p256dh_key text not null,
  auth_key text not null,
  platform text not null,
  created_at timestamptz not null default now(),
  last_seen_at timestamptz not null default now(),
  unique (user_id, endpoint)
);

comment on table public.push_subscription is
  'Assinatura de Web Push (VAPID) do dispositivo do titular (RF-05). '
  'Consumida por TASK-056 (assinatura) e TASK-057 (envio).';

create index if not exists push_subscription_user_id_idx
  on public.push_subscription (user_id);

alter table public.push_subscription enable row level security;

create policy "push_subscription_select_own"
  on public.push_subscription
  for select
  to authenticated
  using (auth.uid() = user_id);

create policy "push_subscription_insert_own"
  on public.push_subscription
  for insert
  to authenticated
  with check (auth.uid() = user_id);

create policy "push_subscription_update_own"
  on public.push_subscription
  for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

create policy "push_subscription_delete_own"
  on public.push_subscription
  for delete
  to authenticated
  using (auth.uid() = user_id);

-- Delete permitido aqui (diferente de `profile`/`consent`, TASK-027):
-- desinscrever um dispositivo do Web Push é uma ação legítima e reversível
-- do próprio titular (ex.: trocar de aparelho, revogar permissão do
-- navegador), sem relação com a cascata de exclusão de conta de RN-11.
--
-- GRANT explícito: este projeto Supabase local roda com
-- `auto_expose_new_tables` não habilitado (mesmo racional documentado em
-- TASK-027/20260908175414_profile_consent_rls.sql) — sem o GRANT, toda
-- chamada de `authenticated` falha com "permission denied for table"
-- (42501/HTTP 401), mesmo com a política correta. Nenhum GRANT para
-- `anon`: leitura/escrita exige sessão autenticada (SDD.md §7.2).
grant usage on schema public to authenticated;
grant select, insert, update, delete on public.push_subscription to authenticated;

create table if not exists public.reminder_log (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  scheduled_for timestamptz not null,
  sent_at timestamptz,
  outcome text not null check (outcome in ('sent', 'failed', 'skipped')),
  channel text not null,
  created_at timestamptz not null default now(),
  unique (user_id, scheduled_for)
);

comment on table public.reminder_log is
  'Log de desfecho de lembrete agendado (CA-05.7, M-08). Envio idempotente '
  'por dia via unique (user_id, scheduled_for) — mitigação de RT-07.';

create index if not exists reminder_log_user_id_idx
  on public.reminder_log (user_id);

alter table public.reminder_log enable row level security;

create policy "reminder_log_select_own"
  on public.reminder_log
  for select
  to authenticated
  using (auth.uid() = user_id);

-- Sem política de insert/update/delete para `authenticated`: `reminder_log`
-- é escrito exclusivamente pela rotina de agendamento/envio de lembrete
-- (TASK-057, `service_role`, que ignora RLS por definição do
-- Postgres/Supabase) — o cliente nunca grava o próprio desfecho de envio,
-- só o lê (mesmo racional de `analytics_daily_aggregate`, SDD.md §7.2:
-- sinal do sistema, não dado editável pelo titular). Modelar insert/update
-- de cliente aqui abriria a mesma falsificação de métrica descrita em
-- RT-10.
--
-- GRANT explícito, mesmo racional documentado acima em `push_subscription`
-- — mas só de leitura, coerente com a ausência de política de escrita.
-- Nenhum GRANT para `anon`.
grant select on public.reminder_log to authenticated;

-- `service_role` ignora RLS por definição do Postgres/Supabase (atributo
-- BYPASSRLS do papel), mas **não** ignora o GRANT de privilégio de tabela —
-- as duas camadas são independentes. Sem este GRANT, a rotina de
-- agendamento/envio de lembrete (TASK-057, que roda com `service_role`)
-- falharia com "permission denied for table reminder_log" (42501) ao
-- tentar gravar o desfecho do envio, mesmo sendo o papel de serviço —
-- confirmado empiricamente ao rodar
-- `tests/db/rls-push-subscription-reminder-log.test.mjs` contra o Supabase
-- local antes deste GRANT existir.
grant select, insert, update on public.reminder_log to service_role;

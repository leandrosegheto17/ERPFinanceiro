-- TASK-029 (Lote 5 — Backend: Schema e Segurança)
--
-- Cria `preference`, conforme o modelo de dados do SDD.md §5.3, com RLS
-- habilitada na MESMA migration que cria a tabela (DI-03 / GUARDRAILS.md
-- G-09 / RT-04). Modelo de autorização: propriedade simples por titular
-- (SDD.md §7.2) — cada linha só é visível/gravável por quem tem
-- `auth.uid() = user_id`. Mesmo padrão de RLS + GRANT explícito já
-- estabelecido em `20260908175414_profile_consent_rls.sql` (TASK-027).
--
-- `preference` é o análogo servidor de `PreferenceRecord`
-- (`src/core/storage/types.ts`, TASK-016/020/021, Lote 3) e sincroniza por
-- LWW (RN-08), não por união monotônica como `plan_progress`. Campos
-- coerentes com o cliente:
--   - `key`/`value`  -> `PreferenceRecord.key`/`.value` (mesmo par chave/valor
--     por preferência; `value` é `jsonb` no servidor porque o cliente já
--     trata `value: unknown`, e o mesmo par pode carregar tipos distintos
--     por chave — horário de lembrete, tema, etc.).
--   - `updated_at`   -> `PreferenceRecord.updatedAt` (relógio do cliente que
--     escreveu; é o carimbo usado na comparação LWW em `core/sync`).
--   - `received_at`  -> `PreferenceRecord.receivedAt` (RT-06, TASK-021):
--     equivalente servidor do campo já implementado no cliente. Aqui é
--     **sempre** definido pelo relógio do servidor via trigger (nunca aceita
--     o valor que o cliente eventualmente enviar no corpo da requisição) —
--     é exatamente esse "relógio confiável, independente do dispositivo que
--     escreveu" que RT-06 pede; se o valor viesse do cliente, deixaria de
--     mitigar o risco de relógio de dispositivo divergente que motivou o
--     campo.
--
-- Chave primária composta `(user_id, key)`: uma preferência por titular por
-- chave (não um histórico) — LWW substitui a linha inteira em vez de
-- acumular versões, ao contrário de `plan_progress` (append-only, UNIQUE de
-- 3 colunas).

create table if not exists public.preference (
  user_id uuid not null references auth.users (id) on delete cascade,
  key text not null,
  value jsonb not null,
  updated_at timestamptz not null,
  received_at timestamptz not null default now(),
  primary key (user_id, key)
);

comment on table public.preference is
  'Preferências do titular (horário de lembrete, tema, ...), sincronizadas '
  'por LWW (RN-08). Análogo servidor de PreferenceRecord '
  '(src/core/storage/types.ts). `received_at` é sempre definido pelo '
  'servidor (trigger), nunca aceito do cliente — RT-06.';

create index if not exists preference_user_id_idx on public.preference (user_id);

-- `received_at` reflete o instante em que o servidor recebeu a mutação
-- (RT-06) — nunca o relógio do cliente. Definido por trigger em vez de só
-- `default now()` porque `default` só se aplica em INSERT: sem o trigger,
-- um UPDATE (o caso comum de LWW — a mesma chave é regravada) manteria o
-- `received_at` antigo, ou pior, aceitaria um valor forjado pelo cliente no
-- corpo do PATCH. `NEW.received_at := now()` roda antes de INSERT/UPDATE e
-- sobrescreve incondicionalmente qualquer valor recebido.
create or replace function public.preference_set_received_at()
returns trigger
language plpgsql
as $$
begin
  new.received_at := now();
  return new;
end;
$$;

create trigger preference_set_received_at
  before insert or update on public.preference
  for each row
  execute function public.preference_set_received_at();

alter table public.preference enable row level security;

create policy "preference_select_own"
  on public.preference
  for select
  to authenticated
  using (auth.uid() = user_id);

create policy "preference_insert_own"
  on public.preference
  for insert
  to authenticated
  with check (auth.uid() = user_id);

create policy "preference_update_own"
  on public.preference
  for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

-- Ao contrário de `profile`/`consent` (TASK-027), `preference` **tem**
-- política de delete para `authenticated`: SDD.md §7.2 lista
-- `SELECT/INSERT/UPDATE/DELETE` como o conjunto de operações do titular
-- sobre `preference` (mesma linha da tabela de autorização que cobre
-- `plan_progress`/`push_subscription`/`reminder_log`/`analytics_event`/
-- `outline`/`outline_version`/`erasure_request`) — diferente de
-- `profile`/`consent`, cuja exclusão é reservada à rotina de `service_role`
-- de RN-11. Uma preferência é um par chave/valor local que o titular pode
-- legitimamente remover (ex.: desativar um lembrete) sem que isso seja
-- exclusão de conta.
create policy "preference_delete_own"
  on public.preference
  for delete
  to authenticated
  using (auth.uid() = user_id);

-- GRANT explícito: este projeto Supabase local roda com
-- `auto_expose_new_tables` não habilitado (`supabase/config.toml`) — mesmo
-- racional e mesma evidência empírica documentados em
-- `20260908175414_profile_consent_rls.sql` (TASK-027). `grant usage on
-- schema public to authenticated` já foi concedido por essa migration
-- anterior (idempotente, mas não repetido aqui). Nenhum GRANT para `anon`:
-- leitura/escrita de `preference` exige sessão autenticada (SDD.md §7.2).
grant select, insert, update, delete on public.preference to authenticated;

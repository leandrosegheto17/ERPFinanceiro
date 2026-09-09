-- TASK-027 (Lote 5 — Backend: Schema e Segurança)
--
-- Cria `profile` e `consent`, conforme o modelo de dados do SDD.md §5.3, com
-- RLS habilitada na MESMA migration que cria as tabelas (DI-03 / GUARDRAILS.md
-- G-09 / RT-04). Modelo de autorização: propriedade simples por titular
-- (SDD.md §7.2) — cada linha só é visível/gravável por quem tem
-- `auth.uid() = user_id`.
--
-- `profile`: marcador mínimo de titular (RF-10). `user_id` referencia
-- `auth.users` do Supabase Auth (identidade), com `on delete cascade` —
-- exclusão do usuário no serviço de identidade arrasta a linha de perfil,
-- coerente com a cascata de exclusão de RN-11 (a cascata completa das
-- demais tabelas do titular é implementada pelas migrations dos Lotes
-- seguintes / `jobs/erasure`, TASK-032).
--
-- `consent`: registra a versão do texto de consentimento aceito (RNF-05,
-- CA-10.2) — consumida por TASK-036 (Bloco de Consentimento) e por
-- `features/identity` no cadastro. Uma linha por aceite: revogação é
-- modelada por `revoked_at` (não por exclusão de linha), preservando o
-- histórico de consentimento como evidência de conformidade LGPD.

create table if not exists public.profile (
  user_id uuid primary key references auth.users (id) on delete cascade,
  created_at timestamptz not null default now()
);

comment on table public.profile is
  'Marcador de titular (RF-10). Um registro por usuário autenticado.';

alter table public.profile enable row level security;

create policy "profile_select_own"
  on public.profile
  for select
  to authenticated
  using (auth.uid() = user_id);

create policy "profile_insert_own"
  on public.profile
  for insert
  to authenticated
  with check (auth.uid() = user_id);

create policy "profile_update_own"
  on public.profile
  for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

-- Sem política de delete para `authenticated`: a exclusão de conta segue o
-- fluxo de `erasure_request` (RN-11, TASK-032), executado por rotina de
-- serviço que opera com `service_role` (que ignora RLS por definição do
-- Postgres/Supabase) — nunca por delete direto do cliente.
--
-- GRANT explícito: este projeto Supabase local roda com
-- `auto_expose_new_tables` não habilitado (`supabase/config.toml`, padrão
-- atual do CLI) — uma tabela nova não fica acessível à API só por existir e
-- ter RLS habilitada; falta o privilégio de tabela do Postgres (camada
-- anterior à RLS). Sem o GRANT abaixo, toda chamada de `authenticated`
-- falharia com "permission denied for table" (42501/HTTP 401), mesmo com
-- a política correta — confirmado empiricamente ao rodar
-- `tests/db/rls-profile-consent.test.mjs` contra o Supabase local antes
-- deste GRANT existir. Nenhum GRANT para `anon`: leitura/escrita destas
-- duas tabelas exige sessão autenticada (SDD.md §7.2) — visitante sem
-- conta recebe "permission denied" (bloqueio na camada de privilégio de
-- tabela, antes mesmo da RLS entrar em jogo), não uma lista vazia.
grant usage on schema public to authenticated;
grant select, insert, update on public.profile to authenticated;

create table if not exists public.consent (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  version text not null,
  text_sha256 text not null,
  accepted_at timestamptz not null default now(),
  revoked_at timestamptz
);

comment on table public.consent is
  'Registro do consentimento específico e destacado para tratamento de dado '
  'pessoal sensível (RNF-05, CA-10.2). `version`/`text_sha256` identificam o '
  'texto exato aceito; revogação é `revoked_at`, nunca exclusão de linha.';

create index if not exists consent_user_id_idx on public.consent (user_id);

alter table public.consent enable row level security;

create policy "consent_select_own"
  on public.consent
  for select
  to authenticated
  using (auth.uid() = user_id);

create policy "consent_insert_own"
  on public.consent
  for insert
  to authenticated
  with check (auth.uid() = user_id);

create policy "consent_update_own"
  on public.consent
  for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

-- Sem política de delete para `authenticated`, pelo mesmo motivo de
-- `profile` — e porque o histórico de consentimento é evidência de
-- conformidade que não deve ser apagável pelo próprio titular a qualquer
-- momento (só pela cascata de exclusão de conta, via `service_role`).
--
-- GRANT explícito, mesmo racional documentado acima em `profile`. Sem
-- GRANT para `anon` pelo mesmo motivo.
grant select, insert, update on public.consent to authenticated;

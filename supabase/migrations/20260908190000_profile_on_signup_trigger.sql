-- TASK-035 + TASK-036 (Lote 6 — Identidade)
--
-- Cria, na MESMA transação que insere a linha em `auth.users` (Supabase
-- Auth, cadastro e-mail+senha, `features/identity`), a linha correspondente
-- em `public.profile` e, quando o cadastro veio acompanhado de aceite de
-- consentimento, a linha em `public.consent` — nunca dependendo de o
-- cliente fazer duas chamadas HTTP separadas em sequência.
--
-- ## Correção de reprovação crítica do Validador (2026-09-08)
--
-- Versão anterior desta migration criava só `profile`, incondicionalmente,
-- e o cadastro com consentimento (TASK-036, `sign-up-with-consent.ts`)
-- garantia a ordem "`consent` antes de qualquer dado pessoal" só por
-- sequência de chamadas no cliente (`auth.signUp` seguido de um INSERT
-- separado em `consent`) — exatamente o mecanismo que `SDD.md` §7.6 proíbe
-- textualmente: "Nenhuma escrita de dado pessoal no servidor ocorre antes
-- de existir a linha de consentimento — garantido por política no banco,
-- não por ordem de chamadas no cliente." Se o INSERT de `consent` falhasse
-- depois do `signUp` já ter tido sucesso (queda de rede, aba fechada, crash
-- entre as duas chamadas), ficava uma conta persistida no servidor sem
-- nenhuma linha de `consent`, e nada no banco impedia ou reparava isso.
-- Ver `.md/QA-REPORT.md` "Lote 6" (Seções 2-3), `.md/SECURITY-REVIEW.md`
-- "Lote 6" (DEVSEC-L6-01/02) e `.md/BLOCKERS.md`, Entrada 2.
--
-- Correção: o aceite do consentimento (`version`/`text_sha256`) passa a
-- viajar como metadata do próprio `auth.signUp`
-- (`options.data`, suportado nativamente pelo Supabase Auth — ver
-- `src/features/identity/sign-up-with-consent.ts`), persistida pelo GoTrue
-- em `auth.users.raw_user_meta_data` **antes** deste gatilho disparar. Este
-- gatilho lê essa metadata e insere `profile` e `consent` na mesma
-- transação que cria `auth.users`. Se qualquer um dos dois INSERTs falhar
-- (exceção dentro deste `plpgsql`), o Postgres desfaz a transação inteira —
-- inclusive o INSERT em `auth.users` que disparou o gatilho — portanto não
-- existe mais estado intermediário possível ("usuário existe, consentimento
-- não"): ou a conta nasce com `profile`+`consent` completos, ou não nasce.
-- Isso é garantia estrutural de banco (atomicidade de transação), não
-- ordem de chamadas de cliente — o requisito não negociável de SDD.md §7.6.
--
-- `consent` só é inserido quando as duas chaves de metadata
-- (`consent_version`/`consent_text_sha256`) estão presentes — cobre os
-- usuários criados sem consentimento no momento do `signUp`, como os
-- criados via `/auth/v1/admin/users` pelos testes de RLS de outras tarefas
-- (`tests/db/rls-profile-consent.test.mjs`, TASK-027/Lote 5): esses
-- usuários continuam recebendo `profile` automaticamente (mesmo
-- comportamento de sempre), sem `consent`, e o próprio teste grava
-- `consent` depois, como um INSERT autenticado comum sob RLS — cenário que
-- este gatilho não precisa nem deve cobrir, já que não representa o fluxo
-- real de cadastro da aplicação.
--
-- `security definer` continua necessário pelo mesmo motivo original: o
-- gatilho roda a partir de um evento em `auth.users` (fora do controle do
-- cliente) e precisa gravar em `public.profile`/`public.consent`
-- independentemente de qualquer sessão de `authenticated` estar ativa — a
-- função roda com o privilégio de quem a criou (dono das duas tabelas),
-- então a política de RLS (`profile_insert_own`/`consent_insert_own`,
-- migration de TASK-027) é contornada da mesma forma que já é para
-- `service_role` (Postgres: dono de tabela ignora RLS, salvo
-- `force row level security`, não usado aqui) — nunca para o cliente, que
-- continua exigindo `auth.uid() = user_id` em toda leitura/escrita própria.
-- `set search_path = public` evita hijack de schema (mesma prática de
-- segurança recomendada para função `security definer` no Postgres/Supabase).
create or replace function public.handle_new_auth_user()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_consent_version text := new.raw_user_meta_data ->> 'consent_version';
  v_consent_text_sha256 text := new.raw_user_meta_data ->> 'consent_text_sha256';
begin
  insert into public.profile (user_id)
  values (new.id)
  on conflict (user_id) do nothing;

  if v_consent_version is not null and v_consent_text_sha256 is not null then
    insert into public.consent (user_id, version, text_sha256)
    values (new.id, v_consent_version, v_consent_text_sha256);
  end if;

  return new;
end;
$$;

-- `after insert` (não `before`): as linhas de `profile`/`consent`
-- referenciam `auth.users(id)` via chave estrangeira (TASK-027); inserir
-- antes da linha de `auth.users` existir de fato violaria a FK.
drop trigger if exists on_auth_user_created on auth.users;

create trigger on_auth_user_created
  after insert on auth.users
  for each row
  execute function public.handle_new_auth_user();

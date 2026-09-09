# BLOCKERS.md — App de Leitura Bíblica Guiada + Preparo de Estudo

## Entrada 1 — TASK-010 aguarda SPIKE-03

- **Status**: Resolvido — 2026-09-07
- **Resolução**: SPIKE-03 executado. Tabela de abreviações dos 66 livros e
  lista de casos "não resolvido" fechadas em
  `.md/spikes/SPIKE-03-resultado.md`. TASK-010 voltou a `Pendente` no
  `.md/TASK.md` (Lote 1) — elegível para `/executar` novamente.
- **Desde**: 2026-09-07
- **Afeta**: TASK-010 (Lote 1 — Núcleo: Corpus e Importação); em cascata, todo o
  Módulo 2 (Lotes 14–15 dependem de TASK-010, ver `.md/TASK.md` Seção 5, RP-01).
- **O quê**: SPIKE-03 (gramática de reconhecimento de referência bíblica em
  pt-BR: abreviações ambíguas, intervalos entre capítulos, variações de
  digitação reais — `.md/TASK.md` Seção 2) ainda não foi executado nesta
  sessão. TASK-010 (`core/reference`) depende explicitamente da conclusão do
  spike antes de começar (Seção 3, coluna "Depende de").
- **Escala para**: usuário (orquestrador) — decidir quando/como rodar o
  spike (não é uma tarefa de implementação do Executor no formato do
  TASK.md; é investigação sem estimativa forçada, conforme Seção 2). Conforme
  RP-01 (Seção 5 do TASK.md): "se ultrapassar 2 dias sem convergir, escalar ao
  Coordenador via BLOCKERS.md antes de estimar TASK-010 com confiança" — ainda
  dentro da janela, registrado aqui como bloqueio ativo para visibilidade.
- **Estado do Lote 1**: TASK-006, TASK-007, TASK-008, TASK-009 e TASK-011
  `Concluída`. TASK-010 é a única tarefa pendente do lote — o lote não fecha
  (não fica elegível para `/validar` como um todo) enquanto TASK-010 estiver
  bloqueada.

## Entrada 2 — Lote 6 (Identidade): reprovação crítica de TASK-035/TASK-036

- **Status**: Aberto — 2026-09-08
- **Desde**: 2026-09-08
- **Afeta**: TASK-035 e TASK-036 (Lote 6 — Identidade), revertidas de
  `Concluída` para `Em andamento` no `.md/TASK.md`. Bloqueia o deploy do
  Lote 6 (chapéu DevOps não deve implantar estas 6 tarefas). Não afeta os
  Lotes 0-5, já com dupla aprovação e implantáveis independentemente.
- **O quê**: 2 achados críticos, correlatos, ambos com raiz no gatilho de
  banco `handle_new_auth_user` (TASK-035,
  `supabase/migrations/20260908190000_profile_on_signup_trigger.sql`):
  1. **Compliance obrigatório (LGPD art. 5º II)**: `SDD.md` §7.6 exige que
     "nenhuma escrita de dado pessoal no servidor ocorre antes de existir a
     linha de consentimento" seja "garantido por política no banco, não por
     ordem de chamadas no cliente". A implementação atual (TASK-036,
     `sign-up-with-consent.ts`) garante essa ordem só por sequência de
     chamadas no cliente (`signUp` seguido de `recordConsent`); o gatilho de
     TASK-035 cria `profile` sem nenhuma checagem de `consent`. Falha de
     rede/JS entre as duas chamadas deixa uma conta persistida no servidor
     sem consentimento, sem nenhum mecanismo de banco que impeça ou repare.
  2. **Regressão determinística no gate de CI**: `npm run test:db`
     (`tests/db/rls-profile-consent.test.mjs`, gate de CI real desde
     TASK-033/Lote 5, já `Validado`) falha de forma determinística — o
     gatilho de TASK-035 cria `profile` para todo `auth.users` novo
     (inclusive usuários de teste criados via `/admin/users`), e o teste
     (escrito antes do gatilho existir) ainda tenta inserir a linha pelo
     lado do cliente, recebendo `409` em vez do `201` esperado. Isolamento
     de RLS por titular continua intacto (as demais asserções do mesmo
     bloco de teste seguem verdes) — não é uma falha de segurança de RLS em
     si, é uma asserção obsoleta que quebra o gate compartilhado do
     projeto.
  Detalhamento completo: `.md/QA-REPORT.md`, seção "Lote 6", Seções 2-3;
  `.md/SECURITY-REVIEW.md`, seção "Lote 6", Seção 1 (`DEVSEC-L6-01`,
  `DEVSEC-L6-02`).
- **Direção de correção sugerida (não imposta ao `executor`)**: enviar a
  aceitação do consentimento (`version`, `text_sha256`) como metadata do
  próprio `signUp` (`options.data`) e estender o mesmo gatilho `security
  definer` (`handle_new_auth_user`) para inserir `profile` **e** `consent`
  na mesma transação que cria a linha em `auth.users` — elimina a
  dependência de duas chamadas HTTP separadas do cliente e, de quebra,
  exige reescrever a asserção obsoleta de `rls-profile-consent.test.mjs`
  para o novo mecanismo de criação de `profile`, resolvendo os dois
  achados juntos.
- **Escala para**: `executor` (correção de TASK-035/TASK-036, revalidação
  focada no que foi reprovado e no que depende disso — TASK-037/038 usam
  `AuthGateway`/sessão mas não o gatilho de `profile`/`consent` em si, não
  precisam ser retestadas do zero). `gestor`, em paralelo, não como
  pré-requisito do bloqueio — achado de compliance (LGPD) com relevância
  estratégica (ver `.md/SECURITY-REVIEW.md`, seção "Lote 6", Seção 6). Não
  escalado ao `coordenador`: correção é de implementação (gatilho de banco +
  orquestração cliente-servidor), não redesenho de dependência/decomposição
  do Lote 6 — a decomposição em 6 tarefas e suas dependências permanecem
  válidas.

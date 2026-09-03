---
description: Orquestra a fase de execução por lote (Backend/Frontend/Mobile em paralelo dentro de cada lote + QA/DevSecOps por lote fechado + DevOps) a partir de um TASK.md aprovado, até o deploy em produção. Por padrão processa um lote por chamada e para; --continuar encadeia vários. Só pausa em reprovação, achado crítico, teto de fix-loop, bloqueio ou deploy em produção.
argument-hint: [vazio = 1 lote e para | nome do lote = processa esse lote e para | --continuar = encadeia sem limite | --continuar N = encadeia até N lotes]
---

# Orquestrador da Fase de Execução

Você está entrando no **modo Orquestrador de Execução**, que persiste pelo resto desta
conversa até o fluxo terminar (ou você decidir interrompê-lo). A lógica deste fluxo
está definida em `.claude/EXECUTION-FLOW.md` — leia esse arquivo agora, antes de fazer
qualquer outra coisa, se ainda não o tiver em contexto. Ele por sua vez assume o que já
está declarado nos 12 agentes (`.claude/agents/`) e em `PIPELINE-CONVENTIONS.md`.

A unidade de trabalho é o **lote** (grupo de tarefas do `TASK.md`, coluna `Lote` da
Seção 3) — nunca a tarefa individual isolada, nunca o backlog inteiro.

Argumento recebido (pode estar vazio): $ARGUMENTS

## Modo de execução

Antes de qualquer outra coisa, interprete `$ARGUMENTS`:

- **Vazio, ou nome de um lote**: **modo padrão** — processa um único lote (o
  nomeado, ou o próximo elegível se vazio) e para ao final dele, mesmo que tenha
  fechado limpo, sem erro nenhum.
- **`--continuar`, sem número**: **modo encadeado sem limite** — processa lote após
  lote, sem parar entre um lote limpo e o próximo, até não sobrar lote pendente no
  `TASK.md` ou até bater uma pausa obrigatória (Seção 7 e as demais já existentes).
- **`--continuar N`** (N inteiro positivo): **modo encadeado com teto** — mesma
  lógica do item anterior, mas para sozinho depois de fechar N lotes nesta chamada.
  Inicialize um contador `lotes_fechados_nesta_chamada = 0` antes da Seção 1.

As pausas obrigatórias já existentes (reprovação do QA, achado crítico do
DevSecOps, teto de fix-loop, deploy em produção, bloqueio não resolvido) valem
igual em qualquer modo — nenhuma delas muda com isto.

## 0. Pré-requisitos bloqueantes

Antes de disparar qualquer agente, confirme os dois pré-requisitos que
`EXECUTION-FLOW.md` marca como bloqueantes:

1. **Repositório git**: se `.git` não existir, pare e avise o usuário — a camada de
   revisão pós-tarefa depende de `git diff`. Não inicialize o repo sem confirmação.
2. **Planejamento aprovado**: leia `.md/CTO-REVIEW.md` e confirme que o **Gate 3** foi
   Aprovado (ou Aprovado com ressalvas) e que `.md/GUARDRAILS.md` está aprovado
   (`guardrails-governance`). Se não estiver, pare — este fluxo não roda sobre um
   `TASK.md` que ainda não fechou o planejamento (use `/planejar` primeiro).
3. **Coluna `Lote` presente**: confirme que a Seção 3 do `.md/TASK.md` tem a coluna
   `Lote` preenchida para toda tarefa. Se não tiver (TASK.md gerado antes desta
   convenção), pare e avise o usuário — não invente agrupamento por conta própria;
   é o Tech Lead (`tech-lead`) quem atribui lote, alinhado aos clusters de
   dependência da Seção 4.

## 1. Determinar o ponto de retomada

Nunca presuma que está começando do zero:

1. Leia a Seção 3 do `.md/TASK.md` e agrupe as tarefas por `Lote`, na ordem em que
   aparecem no documento. Classifique cada lote: `Fechado` (QA + DevSecOps + Tech
   Lead aprovados para o lote, com ou sem deploy já feito), `Em andamento` (alguma
   tarefa `Concluída` mas não todas, ou todas `Concluída` porém o fechamento QA/
   DevSecOps/Tech Lead ainda pendente), `Não iniciado` (nenhuma tarefa `Concluída`
   nem `Em andamento`). Apresente essa visão de conjunto ao usuário antes de
   prosseguir — é a mesma varredura que o `/listar` usa para relatório, aqui serve
   para orientar a retomada.
2. Identifique o **lote-alvo** desta chamada: o nomeado em `$ARGUMENTS`, ou, se
   vazio, o primeiro lote `Em andamento`, ou — se nenhum estiver em andamento — o
   primeiro `Não iniciado` cujas dependências externas (Seção 4) já estejam
   `Concluída` fora do lote.
3. Dentro do lote-alvo, monte três filas (Backend/Frontend/Mobile) só com tarefas
   `Pendente`/`Em andamento` elegíveis (dependências internas ao lote já resolvidas).
4. Leia `.md/BLOCKERS.md` (se existir) e trate qualquer entrada `Aberto` relativa a
   este lote antes de avançar (ver Seção 7).
5. Leia `.md/QA-REPORT.md` e `.md/SECURITY-REVIEW.md` (se existirem) para saber se já
   há veredito pendente de reação sobre este lote.
6. Se o lote-alvo não tem tarefa elegível e nada está bloqueado, ele já está pronto
   para a Seção 4 (fim de lote) ou já foi concluído — vá direto para lá. Se não há
   nenhum lote elegível no `TASK.md` inteiro, o `TASK.md` está com a execução
   terminada — vá para a Seção 6 (encerramento geral).

## 2. Início da execução (uma vez por projeto, não por lote)

Só na primeira vez que este comando roda para o `TASK.md` corrente (nada em
`.md/DEPLOY.md` ainda, ou nenhum lote `Concluído`):

- Dispare, em background (`run_in_background: true`), o `devsecops` para
  `static-security-analysis` contínuo e o `devops` para
  `infrastructure-as-code-provisioning` + `cicd-pipeline-configuration` — os dois
  partem do `SDD.md`/`GUARDRAILS.md` já aprovados, nível de projeto, sem esperar
  nenhum lote.
- Dispare o `qa` para `test-strategy-planning` (produz `TEST-PLAN.md`), também nível
  de projeto.
- Anuncie que os três foram iniciados e siga direto para a Seção 3 — não pausa aqui.

## 3. Ciclo do lote-alvo

Repita este ciclo até as três filas do lote-alvo (Seção 1) esvaziarem (ou até um
bloqueio parar o fluxo):

1. **Anuncie** quais tarefas (uma por trilha elegível — Backend/Frontend/Mobile, só
   as que têm tarefa na fila do lote) vão rodar nesta rodada.
2. **Dispare em paralelo, num único bloco de chamadas** (`Agent`, `subagent_type`
   `backend`/`frontend`/`mobile` conforme a trilha, `run_in_background: false` — a
   revisão seguinte depende do resultado), uma tarefa por trilha. O prompt de cada
   dispatch: aponte a tarefa específica do `TASK.md` (não o arquivo inteiro) e seu
   critério de aceite — o agente já sabe implementar em ciclo TDD via sua própria
   `automated-testing`.
3. **Para cada tarefa que voltar**: dispare a revisão de spec-compliance + qualidade
   de código contra o `git diff` daquela tarefa.
   - Achado: devolve para o mesmo agente corrigir e revisa de novo (fix-loop
     interno, sem pausar) — **máximo 2 tentativas**.
   - **3ª falha consecutiva na mesma tarefa**: pare o ciclo, marque a tarefa
     `Bloqueada`, registre `BLOCKERS.md` e escale para `tech-lead` (Seção 7) — não
     insista indefinidamente.
4. **Marque a tarefa `Concluída`** no `TASK.md`. **Não dispare QA aqui** — QA só roda
   quando o lote inteiro fechar (Seção 4).
5. Se o achado contínuo do DevSecOps (Seção 2) sinalizar severidade alta/crítica a
   qualquer momento (checar `.md/BLOCKERS.md`/relatório do DevSecOps antes de cada
   nova rodada): **pare o ciclo**, trate como bloqueio (Seção 7).
6. Volte ao passo 1 com as filas atualizadas (recalcule dependências internas ao
   lote — uma tarefa `Concluída` pode ter liberado outra do mesmo lote).

**Dependência de contrato de API**: não orquestre isso manualmente — `frontend.md`/
`mobile.md` já resolvem sozinhos (mock se o endpoint já está em `API-CONTRACT.yaml`,
aguarda se não está). Uma tarefa `Em andamento` com nota de mock não conta como
`Concluída` — o lote não fecha enquanto ela não trocar para a API real.

## 4. Fim de lote — QA, auditoria e deploy

Quando **todas** as tarefas do lote-alvo estiverem `Concluída`:

1. Dispare o `qa` para as 5 skills de validação sobre o **lote inteiro** (não tarefa
   a tarefa) → atualiza `QA-REPORT.md`.
   - Aprovação (Aprovado ou Aprovado com ressalvas) do lote: siga.
   - **Reprovação de alguma tarefa do lote: pare.** Volte a(s) tarefa(s) reprovada(s)
     — e o que depende delas dentro do lote — para `Em andamento`, explique ao
     usuário, redisparar a trilha responsável no próximo giro da Seção 3. Ao
     retomar, revalide só o que foi reprovado, não o lote inteiro.
2. Com o lote aprovado pelo QA: dispare o `devsecops` para a auditoria completa (as 5
   skills além do SAST contínuo) → `SECURITY-REVIEW.md`.
   - Achado que bloqueia: pare, explique, escale para a trilha responsável (campo
     "Escala para" já definido em `devsecops.md`), redisparar a trilha, retomar a
     auditoria depois da correção.
3. Com `QA-REPORT.md` e `SECURITY-REVIEW.md` do lote aprovados: dispare o
   `tech-lead` para a checagem estrutural de fechamento do lote (tarefas
   `Concluída`, dependências da Seção 4 resolvidas, nada `Bloqueada` sem resolução).
4. Lote fechado (QA + DevSecOps + Tech Lead aprovados): dispare o `devops` para
   deploy em **staging** desse lote — sem pausa.
5. **Deploy em produção: pare sempre aqui**, mesmo com tudo limpo, e peça confirmação
   explícita do usuário antes de disparar o `devops` para produção — é por lote, não
   espera os demais lotes do TASK.md fecharem, e não é condicional a haver problema.

## 5. Reset de contexto — fechamento do lote

Ao concluir o passo 4 (com ou sem deploy em produção autorizado), monte um **resumo
compacto** do lote a partir do que já está nos artefatos — não crie arquivo novo:

- notas de status das tarefas do lote no `TASK.md`;
- veredito e débitos do lote no `QA-REPORT.md`;
- veredito e débitos do lote no `SECURITY-REVIEW.md`;
- resultado do deploy do lote no `DEPLOY.md`, quando houve.

Apresente esse resumo ao usuário. Esse resumo — não o histórico detalhado de
dispatches, revisões e fix-loops deste lote — é o único contexto que você carrega ao
processar o próximo lote (nova chamada deste comando).

**Na prática**: ao montar os prompts de dispatch do próximo lote (se houver, ver
abaixo), não referencie dispatches, idas-e-voltas de revisão/fix-loop, nem
justificativas específicas do lote que acabou de fechar — baseie-se só neste resumo
e no estado atual dos artefatos em disco (`TASK.md`, `GUARDRAILS.md`,
`API-CONTRACT.yaml`). Se precisar justificar uma decisão sobre o novo lote com algo
do passado, cite o resumo, não a conversa anterior.

**O que fazer a seguir depende do modo de execução (ver seção "Modo de execução")**:

1. Se não sobrar nenhum lote pendente no `TASK.md` (este era o último): avance para
   a Seção 6 (encerramento geral) — independente do modo, é o fim real do projeto.
2. Senão, se estiver em **modo encadeado** (`--continuar` sem número, ou
   `--continuar N` com `lotes_fechados_nesta_chamada < N` depois de incrementar):
   incremente `lotes_fechados_nesta_chamada` e volte à Seção 1 automaticamente, sem
   parar.
3. Senão (**modo padrão**, ou `--continuar N` com o teto já atingido): **pare aqui**.
   Apresente o resumo do lote (já produzido acima) e uma linha avisando que o
   projeto ainda tem lote(s) pendente(s) — sem listar quais nem detalhar (isso é
   papel do `/listar`, não duplique aqui). Termine a resposta.

## 6. Encerramento geral

Quando não houver mais lote com tarefa pendente no `TASK.md`:

1. Confirme que todo lote deployado em produção tem uma entrada de Gate 4 em
   `.md/CTO-REVIEW.md` (uma por lote, sem poder de veto — só registro).
2. **Monte a lista consolidada a partir dos resumos da Seção 5** de cada lote
   fechado ao longo desta execução — não da memória da conversa (o detalhe
   intermediário de lotes anteriores já foi descartado por design). Se o resumo de
   algum lote não estiver mais disponível, reconstrua-o relendo os artefatos
   (`TASK.md`, `QA-REPORT.md`, `SECURITY-REVIEW.md`, `DEPLOY.md`) em vez de tentar
   recordar da conversa.
3. **Apresente a lista consolidada** ao usuário: todos os lotes processados nesta
   sessão, com status final de cada um (implementado → QA → segurança → Tech Lead →
   deploy).

## 7. Bloqueio e escalonamento

Sempre que um agente sinalizar bloqueio (relatório próprio ou nova entrada `Aberto`
em `.md/BLOCKERS.md`):

1. **Pare** e explique ao usuário: quem reportou, o que está bloqueado, tempo parado
   (calculado a partir da data já registrada na entrada de `BLOCKERS.md` — nunca
   deixe travado em silêncio) e para qual agente foi escalado (campo "Escala para").
2. **Dispare o agente de destino** com o conteúdo da entrada de `BLOCKERS.md` como
   contexto.
3. Apresente a resolução e aguarde validação do usuário.
4. **Retome o ciclo da Seção 3** a partir da trilha que estava bloqueada, dentro do
   mesmo lote — nunca do início da execução nem de outro lote.

Nunca decida a resolução de um bloqueio por conta própria — quem resolve é sempre o
agente de destino definido no campo "Escala para" do agente que escalou.

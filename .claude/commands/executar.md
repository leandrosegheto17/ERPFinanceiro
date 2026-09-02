---
description: Orquestra a fase de execução (Backend/Frontend/Mobile em paralelo + QA/DevSecOps contínuos + DevOps) a partir de um TASK.md aprovado, até o deploy em produção. Só pausa em reprovação, achado crítico, bloqueio ou deploy em produção.
argument-hint: [opcional — foco num lote específico de tarefas; vazio processa tudo que estiver pronto no TASK.md]
---

# Orquestrador da Fase de Execução

Você está entrando no **modo Orquestrador de Execução**, que persiste pelo resto desta
conversa até o fluxo terminar (ou você decidir interrompê-lo). A lógica deste fluxo
está definida em `.claude/EXECUTION-FLOW.md` — leia esse arquivo agora, antes de fazer
qualquer outra coisa, se ainda não o tiver em contexto. Ele por sua vez assume o que já
está declarado nos 12 agentes (`.claude/agents/`) e em `PIPELINE-CONVENTIONS.md`.

Foco opcional recebido (pode estar vazio, processa tudo que estiver pronto): $ARGUMENTS

## 0. Pré-requisitos bloqueantes

Antes de disparar qualquer agente, confirme os dois pré-requisitos que
`EXECUTION-FLOW.md` marca como bloqueantes:

1. **Repositório git**: se `.git` não existir, pare e avise o usuário — a camada de
   revisão pós-tarefa depende de `git diff`. Não inicialize o repo sem confirmação.
2. **Planejamento aprovado**: leia `.md/CTO-REVIEW.md` e confirme que o **Gate 3** foi
   Aprovado (ou Aprovado com ressalvas) e que `.md/GUARDRAILS.md` está aprovado
   (`guardrails-governance`). Se não estiver, pare — este fluxo não roda sobre um
   `TASK.md` que ainda não fechou o planejamento (use `/planejar` primeiro).

## 1. Determinar o ponto de retomada

Nunca presuma que está começando do zero:

1. Leia a Seção 3 do `.md/TASK.md` (tabela de tarefas com dono/status) — monte três
   filas, uma por trilha (Backend/Frontend/Mobile), só com tarefas `Pendente` ou
   `Em andamento` cujas dependências (Seção 4 do TASK.md) já estejam `Concluída`.
2. Leia `.md/BLOCKERS.md` (se existir) e trate qualquer entrada `Aberto` antes de
   avançar (ver Seção 6).
3. Leia `.md/QA-REPORT.md` e `.md/SECURITY-REVIEW.md` (se existirem) para saber se já
   há veredito pendente de reação (reprovação não resolvida = tarefa correspondente
   não deveria estar `Concluída`; se estiver, é uma inconsistência — trate como
   bloqueio, Seção 6).
4. Se nenhuma trilha tem tarefa elegível e nada está bloqueado, o lote atual está
   terminado — vá para a Seção 5 (fechamento) ou, se ainda houver tarefas `Pendente`
   com dependência não resolvida, reporte como bloqueio silencioso (Seção 6).

## 2. Início da execução (uma vez por sessão de execução)

Só na primeira vez que este comando roda para o `TASK.md` corrente (nada em
`.md/DEPLOY.md` ainda, ou nenhuma tarefa `Concluída`):

- Dispare, em background (`run_in_background: true`), o `devsecops` para
  `static-security-analysis` contínuo e o `devops` para
  `infrastructure-as-code-provisioning` + `cicd-pipeline-configuration` — os dois
  partem do `SDD.md`/`GUARDRAILS.md` já aprovados, sem esperar nada da implementação.
- Dispare o `qa` para `test-strategy-planning` (produz `TEST-PLAN.md`), também sem
  esperar tarefa concluída.
- Anuncie que os três foram iniciados e siga direto para a Seção 3 — não pausa aqui.

## 3. Ciclo por lote de tarefas

Repita este ciclo até as três filas da Seção 1 esvaziarem (ou até um bloqueio parar o
fluxo):

1. **Anuncie** quais tarefas (uma por trilha elegível — Backend/Frontend/Mobile, só as
   que têm tarefa na fila) vão rodar nesta rodada.
2. **Dispare em paralelo, num único bloco de chamadas** (`Agent`, `subagent_type`
   `backend`/`frontend`/`mobile` conforme a trilha, `run_in_background: false` — a
   revisão seguinte depende do resultado), uma tarefa por trilha. O prompt de cada
   dispatch: aponte a tarefa específica do `TASK.md` (não o arquivo inteiro) e seu
   critério de aceite — o agente já sabe implementar em ciclo TDD via sua própria
   `automated-testing`.
3. **Para cada tarefa que voltar**: dispare a revisão de spec-compliance + qualidade
   de código contra o `git diff` daquela tarefa. Achado: devolve para o mesmo agente
   corrigir e revisa de novo (fix-loop interno, sem pausar e sem consumir uma rodada
   do ciclo).
4. **Marque a tarefa `Concluída`** no `TASK.md` e, imediatamente — não em lote —
   dispare o `qa` para as 5 skills de validação daquela tarefa específica.
   - Aprovação (Aprovado ou Aprovado com ressalvas): siga.
   - **Reprovação: pare o ciclo.** Volte a tarefa para `Em andamento`, explique o
     motivo ao usuário, redisparar a trilha responsável com o relatório do QA no
     próximo giro do ciclo.
5. Se o achado contínuo do DevSecOps (Seção 2) sinalizar severidade alta/crítica a
   qualquer momento (checar `.md/BLOCKERS.md`/relatório do DevSecOps antes de cada
   nova rodada): **pare o ciclo**, trate como bloqueio (Seção 6).
6. Volte ao passo 1 com as filas atualizadas (recalcule dependências — uma tarefa
   `Concluída` pode ter liberado outra).

**Dependência de contrato de API**: não orquestre isso manualmente — `frontend.md`/
`mobile.md` já resolvem sozinhos (mock se o endpoint já está em `API-CONTRACT.yaml`,
aguarda se não está). Uma tarefa `Em andamento` com nota de mock não conta como pronta
para QA ainda.

## 4. Fim de lote — auditoria e deploy em staging

Quando o QA tiver aprovado (Aprovado ou Aprovado com ressalvas) **todas** as tarefas do
lote em execução:

1. Dispare o `devsecops` para a auditoria completa (as 5 skills além do SAST
   contínuo) → `SECURITY-REVIEW.md`.
   - Achado que bloqueia: pare, explique, escale para a trilha responsável (campo
     "Escala para" já definido em `devsecops.md`), redisparar a trilha, retomar a
     auditoria depois da correção.
2. Com `QA-REPORT.md` (Aprovado/Aprovado com ressalvas) **e** `SECURITY-REVIEW.md`
   (Aprovado/Aprovado com débito registrado) do mesmo build: dispare o `devops` para
   deploy em **staging** — sem pausa.
3. **Deploy em produção: pare sempre aqui**, mesmo com tudo limpo, e peça confirmação
   explícita do usuário antes de disparar o `devops` para produção — não é
   condicional a haver problema, é uma regra fixa deste fluxo.

## 5. Encerramento

Depois do deploy em produção:

1. Dispare o `devops` para registrar o resultado em `.md/DEPLOY.md` (sucesso,
   rollback ou incidente).
2. Dispare o `cto` para fechar o **Gate 4** em `.md/CTO-REVIEW.md` — só registro, sem
   poder de veto aqui.
3. **Apresente a lista consolidada**: tudo que foi implementado, testado, auditado e
   deployado nesta execução, com status final de cada peça (tarefa → QA → segurança →
   deploy).

Se ainda houver tarefas `Pendente` no `TASK.md` fora deste lote, informe que ficaram
para uma próxima chamada deste comando — não force tudo num único lote.

## 6. Bloqueio e escalonamento

Sempre que um agente sinalizar bloqueio (relatório próprio ou nova entrada `Aberto` em
`.md/BLOCKERS.md`):

1. **Pare** e explique ao usuário: quem reportou, o que está bloqueado, tempo parado
   (calculado a partir da data já registrada na entrada de `BLOCKERS.md` — nunca
   deixe travado em silêncio) e para qual agente foi escalado (campo "Escala para").
2. **Dispare o agente de destino** com o conteúdo da entrada de `BLOCKERS.md` como
   contexto.
3. Apresente a resolução e aguarde validação do usuário.
4. **Retome o ciclo da Seção 3** a partir da trilha que estava bloqueada — nunca do
   início da execução.

Nunca decida a resolução de um bloqueio por conta própria — quem resolve é sempre o
agente de destino definido no campo "Escala para" do agente que escalou.

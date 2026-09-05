# EXECUTION-FLOW.md

Sequência lógica da **fase de execução** — parte de onde o planejamento termina
(`SDD.md`/`UX-SPEC.md`/`TASK.md` aprovados pelo usuário + `GUARDRAILS.md` aprovado
pelo Gestor, ver `PLANNING-FLOW.md`) e vai até o deploy em produção, fechando o
ciclo de volta ao Gestor.

Este documento cobre a lógica dos **três comandos** da fase de execução —
`/executar` (Executor implementa), `/validar` (Validador audita um lote fechado) e
`/deploy` (Validador publica) — e do comando somente-leitura `/listar`. Nenhum dos
três dispara o próximo automaticamente: **o usuário é o orquestrador**, decide
quando rodar cada um. Este documento não redefine os agentes consolidados
(`.claude/agents/gestor.md`, `coordenador.md`, `executor.md`, `validador.md`) nem a
convenção de artefatos (`PIPELINE-CONVENTIONS.md`) — só ordena o que cada um já
declara, em nível de comando.

> Modelo anterior (12 agentes, um único `/executar` que fazia implementação + QA +
> DevSecOps + DevOps encadeados por lote) descontinuado — ver nota no topo de
> `PIPELINE-CONVENTIONS.md`. Os agentes `backend`, `frontend`, `mobile`, `qa`,
> `devsecops`, `devops`, `tech-lead` continuam existindo como arquivos, mas não são
> mais acionados por este fluxo.

**Pré-requisito bloqueante**: este projeto precisa ser um repositório git antes de
`/executar` rodar de verdade — a revisão inline pós-tarefa (ver comando 1) depende
de `git diff`.

---

## Unidade de trabalho: o lote

A unidade de trabalho continua sendo o **lote** — um conjunto de tarefas do
`TASK.md` que formam uma funcionalidade/módulo com sentido próprio (ex.: "cadastro
de paciente"), atribuído pelo Coordenador durante a decomposição
(`/definir_organizar`).

- **Onde vive**: coluna `Lote` na Seção 3 do `TASK.md`.
- **Tamanho do lote**: ~5-6 tarefas, mesmo critério de antes — mas agora cada
  **tarefa** é bem menor (granularidade fina, ver `coordenador.md`), porque o
  paralelismo real acontece dentro do lote, tarefa a tarefa, não mais só entre 3
  trilhas fixas por papel.
- **Paralelismo dentro do lote**: a Seção 4 do `TASK.md` marca explicitamente quais
  tarefas de um lote são independentes entre si (podem rodar em paralelo) e quais
  têm dependência direta. O `/executar` usa esse mapeamento para decidir quantas
  instâncias do Executor disparar em paralelo a cada rodada — não há mais um teto
  fixo de "3 trilhas" (Backend/Frontend/Mobile); o teto real é "toda tarefa
  elegível da rodada", respeitando o tamanho do lote (~5-6) como limite prático de
  paralelismo simultâneo.
- **O que opera em nível de lote**: `/executar` (implementação), `/validar`
  (auditoria QA + DevSecOps + checagem estrutural do Coordenador).
- **O que opera em nível de projeto** (não de lote): `/deploy` pode processar um
  lote específico ou o conjunto de lotes já validados e ainda não publicados — ver
  comando 3.

---

## Comando 1: `/executar` — Executor (Backend/Frontend/Mobile), em paralelo por
tarefa

| Dispara quando | Agente | Ação | Pausa obrigatória |
|---|---|---|---|
| Usuário roda `/executar` sobre um `TASK.md` com tarefas pendentes | `executor` (múltiplas instâncias em paralelo, por tarefa elegível) | Implementa, testa (TDD), atualiza Status no TASK.md | Reprovação da revisão inline após 2 tentativas; desvio grande de escopo sinalizado pelo Executor; fim do lote-alvo |

### 1. Determinar o lote-alvo

1. Leia a Seção 3 do `TASK.md`, agrupe por `Lote`. Classifique cada um: `Fechado`
   (já validado por `/validar` e sem tarefa pendente), `Em andamento` (alguma
   tarefa `Concluída`/`Em andamento`, mas não todas), `Não iniciado`.
2. Lote-alvo: o nomeado em `$ARGUMENTS`, ou o primeiro `Em andamento`, ou — se
   nenhum — o primeiro `Não iniciado` cujas dependências externas (Seção 4) já
   estejam satisfeitas.
3. Monte a fila de tarefas elegíveis do lote-alvo: `Pendente`/`Em andamento` com
   dependências internas ao lote já resolvidas (conforme marcação da Seção 4).
4. Leia `.md/BLOCKERS.md` — se houver entrada `Aberto` afetando este lote, **pare
   aqui** e apresente ao usuário (ver Seção 4 deste documento) antes de disparar
   qualquer agente.

### 2. Rodada paralela

Repita até a fila esvaziar ou um bloqueio parar o fluxo:

1. **Anuncie** todas as tarefas elegíveis desta rodada (uma instância do Executor
   por tarefa, independente de qual chapéu — Backend/Frontend/Mobile — cada uma
   exige).
2. **Dispare em paralelo, num único bloco de chamadas** (`Agent`, `subagent_type:
   executor`, `run_in_background: false` — a revisão seguinte depende do
   resultado), uma por tarefa. O prompt de cada dispatch: a tarefa específica do
   `TASK.md` (não o arquivo inteiro) e seu critério de aceite.
3. **Para cada tarefa que voltar**, rode a revisão inline (você, o executor do
   comando — não um agente separado) contra o `git diff` daquela tarefa
   especificamente: spec-compliance (bate com o critério de aceite e as
   diretrizes de implementação) + qualidade de código (skill `code-review`).
   - **Achado**: devolva para a mesma instância do Executor corrigir, e revise de
     novo — fix-loop, **máximo 2 tentativas**, sem pausar entre elas.
   - **3ª falha consecutiva na mesma tarefa**: **pare** — marque a tarefa
     `Bloqueada`, registre `BLOCKERS.md`, e **encerre o comando aqui**, explicando
     ao usuário o que falhou nas 2 tentativas e perguntando como seguir. Não
     insista numa 4ª tentativa por conta própria.
4. **Marque a tarefa `Concluída`** no `TASK.md` quando a revisão passar.
5. Se o Executor sinalizar um desvio grande de escopo/estimativa, ou uma
   lacuna/inconsistência no `UX-SPEC.md`/`SDD.md`: **pare o comando aqui** (ver
   Seção 4) — não decida por conta própria, não redisparur o Coordenador
   sozinho.
6. Recalcule a fila (uma tarefa `Concluída` pode ter liberado outra do mesmo
   lote) e volte ao passo 1.

**Dependência de contrato de API**: não orquestre isso manualmente — `executor.md`
já resolve sozinho (mock se o endpoint já está em `API-CONTRACT.yaml`, aguarda se
não está). Uma tarefa `Em andamento` com nota de mock não conta como `Concluída`.

### 3. Fim do lote-alvo

Quando a fila esvaziar (todas as tarefas `Concluída` ou `Bloqueada` com bloqueio já
reportado):

1. Apresente um resumo: tarefas concluídas, tarefas bloqueadas (se houver), e o
   estado do lote.
2. Informe que o lote está pronto para `/validar` — **não dispare o Validador
   automaticamente**, isso é decisão do usuário.
3. **Pare aqui.** Se `$ARGUMENTS` não pediu processamento de outro lote em
   sequência, termine a resposta. Rodar `/executar` de novo (sem argumento, ou
   nomeando o próximo lote) é decisão do usuário.

**Argumento opcional `--continuar [N]`**: se o usuário passar esse argumento,
encadeie lote após lote (até N, ou sem limite se N omitido) sem pausar entre um
lote fechado e o próximo — as pausas obrigatórias (fix-loop esgotado, desvio de
escopo, bloqueio) continuam valendo igual em qualquer modo.

---

## Comando 2: `/validar` — Validador (QA + DevSecOps) + checagem estrutural do
Coordenador

| Dispara quando | Agente(s) | Ação | Pausa obrigatória |
|---|---|---|---|
| Usuário roda `/validar` sobre um lote com todas as tarefas `Concluída` | `validador` (chapéu QA → chapéu DevSecOps) → `coordenador` (checagem estrutural) | Valida funcionalmente, audita segurança, confirma consistência do TASK.md | Reprovação do QA; achado de severidade alta/crítica do DevSecOps; inconsistência estrutural do Coordenador |

### 1. Determinar o lote-alvo

1. Lote-alvo: o nomeado em `$ARGUMENTS`, ou o primeiro lote com **todas** as
   tarefas `Concluída` e ainda sem veredito de `QA-REPORT.md`/`SECURITY-REVIEW.md`
   para o estado atual das tarefas.
2. Se nenhum lote atende ao critério (nenhum fechado o suficiente para validar),
   informe isso ao usuário e pare — não há o que validar ainda.

### 2. Validação funcional (chapéu QA)

1. Dispare `validador` (chapéu QA: `acceptance-criteria-validation`,
   `cross-platform-integration-testing`, `bug-documentation`,
   `non-functional-validation`, `qa-report-drafting`) sobre o lote-alvo inteiro →
   atualiza `QA-REPORT.md`.
2. **Aprovado ou Aprovado com ressalvas**: siga para a Seção 3.
3. **Reprovação de alguma tarefa**: **pare**. Volte a(s) tarefa(s) reprovada(s) — e
   o que depende delas dentro do lote — para `Em andamento` no `TASK.md`, explique
   ao usuário o motivo (do `QA-REPORT.md`), e informe que a próxima ação é rodar
   `/executar` de novo sobre esse lote para corrigir. Não dispare o Executor
   automaticamente.

### 3. Auditoria de segurança (chapéu DevSecOps)

1. Dispare `validador` (chapéu DevSecOps: `static-security-analysis`,
   `security-requirement-validation`, `compliance-validation`,
   `sensitive-data-exposure-check`, `finding-severity-classification`,
   `security-report-drafting`) sobre o lote-alvo → atualiza `SECURITY-REVIEW.md`.
2. **Sem achado bloqueante** (ou só débito de baixa/média severidade registrado):
   siga para a Seção 4.
3. **Achado de severidade alta/crítica, ou compliance obrigatório não atendido**:
   **pare**. Explique ao usuário o achado e o campo "Escala para" (normalmente
   `executor`, para correção de código). Informe que a próxima ação é rodar
   `/executar` de novo sobre a tarefa afetada.

### 4. Checagem estrutural (Coordenador)

1. Dispare `coordenador` para confirmar: toda tarefa do lote `Concluída`, nenhuma
   dependência da Seção 4 órfã/inconsistente, nenhuma tarefa `Bloqueada` sem
   resolução.
2. **Consistente**: o lote está `Validado` — pronto para `/deploy`.
3. **Inconsistência encontrada**: **pare**, explique, e informe se é o `TASK.md`
   que precisa de correção direta ou se é pendência de implementação (nesse caso,
   volta para `/executar`).

### 5. Encerramento

Apresente o resumo do lote validado (veredito QA, veredito DevSecOps, checagem
estrutural) e informe que está pronto para `/deploy`. **Não dispare o deploy
automaticamente.**

---

## Comando 3: `/deploy` — Validador (QA + DevSecOps de confirmação → DevOps)

| Dispara quando | Agente | Ação | Pausa obrigatória |
|---|---|---|---|
| Usuário roda `/deploy` | `validador` (confirmação final + chapéu DevOps) | Provisiona infra/CI-CD (1ª vez), confirma validação, publica | Sempre antes de produção; achado bloqueante na confirmação final |

### 1. Preparação de infraestrutura (só na primeira chamada do projeto)

Se `.md/DEPLOY.md` ainda não existir (nenhum deploy anterior): dispare `validador`
(chapéu DevOps: `infrastructure-as-code-provisioning`,
`cicd-pipeline-configuration`) a partir do `SDD.md`/`GUARDRAILS.md` já aprovados.
Isso não depende de nenhum lote específico — é preparação de projeto.

### 2. Determinar o que vai ser publicado

1. Lote-alvo: o nomeado em `$ARGUMENTS`, ou — se vazio — **todos** os lotes com
   status `Validado` (Seção 4 do comando 2) que ainda não aparecem como
   publicados em `.md/DEPLOY.md`.
2. Se não houver nenhum lote `Validado` pendente de publicação, informe isso ao
   usuário e pare — rode `/validar` primeiro.

### 3. Validação final (chapéu QA + DevSecOps, de confirmação)

Para cada lote a publicar: dispare `validador` (chapéus QA + DevSecOps) de novo,
desta vez focado em **confirmar que nada mudou** desde o veredito registrado em
`QA-REPORT.md`/`SECURITY-REVIEW.md` e em checar integração **entre lotes** que vão
ser publicados juntos (regressão cruzada que uma validação por lote isolado não
cobre). Se o lote já foi validado recentemente e nada mudou, esta etapa pode ser
mais leve (confirmação), mas nunca é pulada.

- **Achado bloqueante nesta confirmação**: **pare**, explique, informe que a
  correção volta para `/executar` (e o lote precisará passar por `/validar` de
  novo antes de tentar `/deploy` outra vez).

### 4. Deploy em staging

Com a validação final limpa: dispare `validador` (chapéu DevOps:
`deployment-execution`, `observability-setup`,
`non-functional-requirement-validation`) para staging, para o conjunto de lotes
desta chamada — **sem pausa**.

### 5. Deploy em produção

**Pare sempre aqui**, mesmo com tudo limpo, e peça confirmação explícita do
usuário antes de disparar `validador` (chapéu DevOps) para produção. Isso vale
para o conjunto publicado nesta chamada de `/deploy` — não é condicional a haver
problema.

### 6. Fechamento (Gate 4 do Gestor)

Após deploy em produção confirmado: dispare `gestor` (registro de fechamento,
Gate 4, sem poder de veto) para registrar em `.md/CTO-REVIEW.md` o resultado —
sucesso, versão, lotes incluídos. Apresente o `deploy-report-drafting`
(`.md/DEPLOY.md`) atualizado e a confirmação do Gate 4.

---

## Comando 4: `/listar` — somente leitura

Sem mudança de mecânica em relação à versão anterior: lê `TASK.md` (Seção 3,
agrupando por `Lote`), `QA-REPORT.md`, `SECURITY-REVIEW.md`, `DEPLOY.md` e
`BLOCKERS.md`, classifica cada lote (`Concluído`/`Validado`/`Bloqueado`/`Em
andamento`/`Não iniciado`/`Indeterminado`) e apresenta o relatório. Único ajuste:
"Concluído" (critério de fechamento antigo: QA + DevSecOps + Tech Lead aprovados)
passa a ser "Validado" (critério do comando 2 acima: QA + DevSecOps do Validador +
checagem estrutural do Coordenador), e "publicado" passa a ser rastreado via
`DEPLOY.md` produzido pelo comando 3. Não dispara nenhum agente, não avança
tarefa, não sugere próximo comando.

---

## Bloqueio e escalonamento (comum aos três comandos)

Sempre que um agente sinalizar bloqueio (relatório próprio ou nova entrada
`Aberto` em `.md/BLOCKERS.md`):

1. **Pare** e explique ao usuário: quem reportou, o que está bloqueado, e para
   qual agente foi escalado (campo "Escala para" do agente que reportou).
2. **Não dispare o agente de destino automaticamente** — o usuário decide o
   próximo passo (rodar `/executar`/`/definir_organizar`/`/planejar` de novo sobre
   o ponto afetado, pedir um parecer ad hoc ao Gestor, ou ajustar manualmente).
3. Quando o usuário indicar que quer resolver, rode o comando correspondente —
   nunca decida a resolução por conta própria.

## Reset de contexto entre lotes/comandos

Ao final de cada comando (fim de lote no `/executar`, lote validado no `/validar`,
publicação no `/deploy`), monte um **resumo compacto** a partir do que já existe
nos artefatos — não crie arquivo novo. Esse resumo é o contexto que carrega para a
próxima chamada do mesmo ou de outro comando; não recarregue o histórico detalhado
de dispatches, revisões e fix-loops já fechados — releia os artefatos em disco
quando precisar de detalhe específico do passado.

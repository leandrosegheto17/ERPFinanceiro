# EXECUTION-FLOW.md

Sequência lógica da **fase de execução** — parte de onde o planejamento termina
(`SDD.md`/`UX-SPEC.md`/`TASK.md` aprovados pelo usuário + `GUARDRAILS.md` aprovado
pelo Gestor, ver `PLANNING-FLOW.md`) e vai até o deploy em produção, fechando o
ciclo de volta ao Gestor.

Este documento cobre a lógica dos **comandos** da fase de execução —
`/executar` (Executor implementa por lote), `/executar_tarefa` (versão de escopo
mínimo: uma única tarefa por vez, com validação leve), `/validar` (Validador
audita um lote fechado) e `/deploy` (Validador publica) — e do comando
somente-leitura `/listar`. Nenhum deles dispara o próximo automaticamente: **o
usuário é o orquestrador**, decide quando rodar cada um. **Exceção documentada**:
`/executar --continuar` passa a rodar a validação (Comando 2) de cada lote
automaticamente assim que ele fecha, antes de seguir para o próximo — ver Comando
1, Seção 3. `/executar_tarefa` nunca encadeia (ver Comando 1b) — mesmo assim,
não substitui o `/validar` completo por lote, só uma checagem de critério de
aceite da tarefa isolada. Fora disso, o comando seguinte continua sendo decisão do
usuário. Este documento não redefine os agentes consolidados
(`.claude/agents/gestor.md`, `coordenador.md`, `executor.md`, `validador.md`) nem
a convenção de artefatos (`PIPELINE-CONVENTIONS.md`) — só ordena o que cada um já
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
- **O que opera em nível de lote**: `/executar` (implementação, e com
  `--continuar` também a validação de cada lote assim que fecha), `/validar`
  (auditoria QA + DevSecOps + checagem estrutural — hoje resolvida pelo próprio
  Validador, sem reabrir o Coordenador, salvo escalação real — ver Comando 2).
- **O que opera em nível de projeto** (não de lote): `/deploy` pode processar um
  lote específico ou o conjunto de lotes já validados e ainda não publicados — ver
  comando 3.

---

## Comando 1: `/executar` — Executor (Backend/Frontend/Mobile), em paralelo por
tarefa

| Dispara quando | Agente | Ação | Pausa obrigatória |
|---|---|---|---|
| Usuário roda `/executar` sobre um `TASK.md` com tarefas pendentes (`--continuar [N]` encadeia lotes e já valida cada um ao fechar) | `executor` (múltiplas instâncias em paralelo, por tarefa elegível); em `--continuar`, também `validador` (validação do lote que acabou de fechar) | Implementa, testa (TDD), atualiza Status no TASK.md; em `--continuar`, valida o lote (Comando 2) antes de seguir | Reprovação da revisão inline após 2 tentativas; desvio grande de escopo sinalizado pelo Executor; reprovação **crítica** de QA ou achado alto/crítico de DevSecOps na validação automática; fim do lote-alvo (modo padrão) |

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
3. **Assim que cada instância voltar, confira o canário de contexto antes de
   qualquer outra coisa**: olhe o campo `subagent_tokens` no resultado do próprio
   dispatch (`Agent`) daquela tarefa. Isso é mecânico — não depende do Executor
   perceber ou reportar nada. Se passar de ~300 mil tokens para uma única tarefa:
   trate como o item 6 abaixo (desvio grande de escopo) **imediatamente**, mesmo
   que a revisão inline do item 4 ainda não tenha rodado — não vale a pena gastar
   fix-loop numa tarefa que já provou estar mal decomposta.
4. **Para cada tarefa que passou pelo item 3 sem estourar o canário**, rode a
   revisão inline (você, o executor do comando — não um agente separado) contra o
   `git diff` daquela tarefa especificamente: spec-compliance (bate com o
   critério de aceite e as diretrizes de implementação) + qualidade de código
   (skill `code-review`).
   - **Achado**: devolva para a mesma instância do Executor corrigir, e revise de
     novo — fix-loop, **máximo 2 tentativas**, sem pausar entre elas.
   - **3ª falha consecutiva na mesma tarefa**: **pare** — marque a tarefa
     `Bloqueada`, registre `BLOCKERS.md`, e **encerre o comando aqui**, explicando
     ao usuário o que falhou nas 2 tentativas e perguntando como seguir. Não
     insista numa 4ª tentativa por conta própria.
5. **Marque a tarefa `Concluída`** no `TASK.md` quando a revisão passar.
6. Se o Executor sinalizar um desvio grande de escopo/estimativa, se o canário de
   contexto do item 3 disparar, ou se houver uma lacuna/inconsistência no
   `UX-SPEC.md`/`SDD.md`: **pare o comando aqui** (ver Seção 4) — não decida por
   conta própria, não redisparur o Coordenador sozinho. Tratamento igual nos três
   casos: pausa, `Bloqueada`, `BLOCKERS.md`, nunca empurrado com mais fix-loop.
7. Recalcule a fila (uma tarefa `Concluída` pode ter liberado outra do mesmo
   lote) e volte ao passo 1.

**Dependência de contrato de API**: não orquestre isso manualmente — `executor.md`
já resolve sozinho (mock se o endpoint já está em `API-CONTRACT.yaml`, aguarda se
não está). Uma tarefa `Em andamento` com nota de mock não conta como `Concluída`.

### 3. Fim do lote-alvo

Quando a fila esvaziar (todas as tarefas `Concluída` ou `Bloqueada` com bloqueio já
reportado):

1. Apresente um resumo: tarefas concluídas, tarefas bloqueadas (se houver), e o
   estado do lote.
2. **Modo padrão**: informe que o lote está pronto para `/validar` — **não
   dispare o Validador automaticamente**, isso é decisão do usuário. **Pare
   aqui.** Rodar `/executar` de novo (sem argumento, ou nomeando o próximo lote) é
   decisão do usuário.
3. **`--continuar [N]`**: não pare — valide este lote agora, seguindo exatamente
   as Seções 2-5 do Comando 2 (`/validar`) mais abaixo (pule a Seção 1 de lá: o
   lote-alvo já é conhecido, é o que acabou de fechar). Trate as pausas
   obrigatórias da validação igual às deste comando:
   - **Reprovação crítica de QA, achado alto/crítico de DevSecOps, ou
     inconsistência estrutural que exige redesenho** (Comando 2, Seção 4):
     **pare o `/executar` aqui também** — não siga para o próximo lote.
   - **Reprovação simples/débito baixo-médio**: não para — vira tarefa em
     `Refatoração Lote-X` (Comando 2, Seção 4), e o fluxo segue.
   - Validação limpa: o lote fecha `Validado` (ou `Validado com ressalvas`) e,
     se não atingiu o teto `N` (ou não há teto) e ainda há lote pendente, volte à
     Seção 1 deste comando para o próximo lote, sem pausar.
   - Se não sobrar nenhum lote pendente no `TASK.md`: informe que a execução (com
     validação) está completa.

**Argumento opcional `--continuar [N]`**: encadeia lote após lote (até N, ou sem
limite se N omitido), **incluindo a validação automática de cada lote** assim que
ele fecha (item 3 acima) — o usuário não precisa rodar `/validar` separado depois
de cada lote para manter o encadeamento. As pausas obrigatórias de qualquer uma
das duas etapas (implementação ou validação) valem igual em qualquer modo — só
param por achado crítico/bloqueio real, nunca por achado simples/débito
baixo-médio. Rodar `/validar --continuar` separadamente continua útil para
alcançar lotes que fecharam fora de uma sessão de `/executar --continuar` (ex.:
implementados manualmente, ou numa chamada anterior sem `--continuar`).

**Infra em paralelo (oportunista)**: se `.md/DEPLOY.md` ainda não existir e o
`SDD.md` já estiver aprovado, este é um bom momento para disparar em paralelo o
mesmo dispatch de preparação de infraestrutura do chapéu DevOps (Comando 3, Seção
1) — sem esperar a primeira chamada de `/deploy`. Isso não pausa nem bloqueia o
`/executar`, é só aproveitar a sessão longa para adiantar trabalho que já é
declarado como paralelo à implementação em `validador.md`.

---

## Comando 1b: `/executar_tarefa` — Executor, uma única tarefa (+ validação leve)

| Dispara quando | Agente | Ação | Pausa obrigatória |
|---|---|---|---|
| Usuário roda `/executar_tarefa` (sem argumento) | `executor` (uma única instância, para a primeira tarefa elegível de todo o `TASK.md`); `validador` (validação leve, só `acceptance-criteria-validation`, escopada à tarefa) | Implementa a tarefa, revisão inline, valida só o critério de aceite dela, atualiza Status | Sempre, ao fim da tarefa (execução + validação leve) — nunca encadeia; bloqueio Aberto com prioridade; reprovação da revisão inline após 2 tentativas; reprovação da validação leve |

Versão de **escopo mínimo** do Comando 1, pensada para não poluir o contexto:
processa **uma tarefa por chamada**, nunca um lote inteiro, e sempre para ao
final — não tem modo `--continuar`. Útil quando o usuário quer avançar a fila aos
poucos, sob controle manual, em vez de disparar todas as tarefas elegíveis de um
lote em paralelo.

### 1. Determinar o item-alvo (tarefa ou bloqueio)

1. Leia `.md/BLOCKERS.md` primeiro. Uma entrada `Aberto` que afete a primeira
   tarefa elegível **tem prioridade sobre executar**: pare e apresente a entrada —
   nunca pule para outra tarefa não afetada nesta chamada.
2. Sem bloqueio com prioridade: ache a **primeira** tarefa `Pendente`/`Em
   andamento` de toda a Seção 3 do `TASK.md` (não só de um lote) cujas
   dependências internas (Seção 4) já estejam resolvidas. Essa é a única
   tarefa-alvo da chamada.
3. Nenhuma tarefa elegível em todo o `TASK.md`: informe e pare.

### 2. Execução + revisão inline

Igual ao Comando 1, Seção "2. Rodada paralela", itens 3-4, mas para uma única
tarefa (sem rodada, sem recalcular fila): dispare `executor`
(`run_in_background: false`), confira o canário de contexto
(`subagent_tokens` > ~300 mil trata como desvio grande de escopo, pausa
imediata), rode a revisão inline (spec-compliance + `code-review`) contra o
`git diff` da tarefa, fix-loop de até 2 tentativas. 3ª falha, desvio de escopo, ou
lacuna no `UX-SPEC.md`/`SDD.md`: **pare** (`Bloqueada` + `BLOCKERS.md`). Passou
limpo: marque `Concluída`.

### 3. Validação leve (não é o `/validar` de lote)

Dispare `validador` rodando **só** `acceptance-criteria-validation` contra o
critério de aceite desta tarefa — sem `cross-platform-integration-testing`, sem
chapéu DevSecOps, sem checagem estrutural de lote (essas continuam exclusivas do
Comando 2, quando o lote inteiro fechar). Reprovação (crítica ou simples): pare e
reporte, sem criar `Refatoração Lote-X` aqui — isso é decisão de lote, do Comando
2. Aprovado: tarefa confirmada.

### 4. Encerramento

Sempre pare aqui, mesmo limpo — não dispara `/validar` de lote nem `/deploy`, não
processa outra tarefa. Quando todas as tarefas do lote fecharem `Concluída`, o
`/validar` completo (Comando 2) segue sendo o gate de fechamento de lote.

---

## Comando 2: `/validar` — Validador (QA + DevSecOps + checagem estrutural)

| Dispara quando | Agente(s) | Ação | Pausa obrigatória |
|---|---|---|---|
| Usuário roda `/validar` sobre um lote com todas as tarefas `Concluída` (`--continuar [N]` encadeia lotes prontos, sem esperar; `/executar --continuar` também aciona isso automaticamente por lote) | `validador` (chapéu QA → chapéu DevSecOps → checagem estrutural, tudo na mesma chamada) | Valida funcionalmente, audita segurança, confirma consistência do TASK.md sozinho; achado simples/débito baixo-médio vira tarefa em `Refatoração Lote-X` sem parar | Reprovação **crítica** do QA; achado de severidade alta/crítica do DevSecOps; inconsistência estrutural que exige redesenho (só aí escala ao `coordenador`) |

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
3. **Reprovação de alguma tarefa**: o `validador` classifica cada reprovação no
   próprio `QA-REPORT.md` (ver `validador.md`):
   - **Crítica** (compromete o critério de aceite central da tarefa, exige mudança
     de escopo/arquitetura, ou quebra algo de que outra tarefa do lote depende):
     **pare aqui**. Volte a(s) tarefa(s) para `Em andamento` no `TASK.md`, explique
     o motivo ao usuário (do `QA-REPORT.md`) e informe que a próxima ação é rodar
     `/executar` de novo sobre esse lote. Não dispare o Executor automaticamente.
   - **Simples** (ajuste pontual e de baixo esforço — mensagem de erro, edge case
     secundário, validação de campo — que não compromete o critério de aceite
     central nem bloqueia outra tarefa do lote): **não pare**. A tarefa continua
     `Concluída`; siga para a Seção 3 normalmente — a correção é agendada na
     Seção 4, como tarefa em `Refatoração Lote-X`, não como retorno ao Executor.

### 3. Auditoria de segurança (chapéu DevSecOps)

1. Dispare `validador` (chapéu DevSecOps: `static-security-analysis`,
   `security-requirement-validation`, `compliance-validation`,
   `sensitive-data-exposure-check`, `finding-severity-classification`,
   `security-report-drafting`) sobre o lote-alvo → atualiza `SECURITY-REVIEW.md`.
2. **Sem achado bloqueante** (ou só débito de baixa/média severidade registrado):
   siga para a Seção 4 — o débito vira tarefa em `Refatoração Lote-X` lá, não fica
   só como nota solta no relatório.
3. **Achado de severidade alta/crítica, ou compliance obrigatório não atendido**:
   **pare**. Explique ao usuário o achado e o campo "Escala para" (normalmente
   `executor`, para correção de código). Informe que a próxima ação é rodar
   `/executar` de novo sobre a tarefa afetada.

### 4. Checagem estrutural (o próprio Validador — sem reabrir o Coordenador)

Esta etapa **não dispara outro agente**: o `validador`, ainda na mesma chamada,
com o que já tem em mãos (`TASK.md`, `QA-REPORT.md`, `SECURITY-REVIEW.md` que ele
mesmo acabou de produzir), confirma o fechamento do lote sozinho. Voltar ao
Coordenador para essa confirmação de rotina só reabre um agente com escopo limpo
(ver "Reset de contexto" neste documento) para reafirmar o que o Validador já
sabe — por isso essa etapa deixou de ser um dispatch separado.

1. Confirme: toda tarefa do lote `Concluída`, nenhuma dependência da Seção 4 do
   `TASK.md` órfã/inconsistente **relativa a este lote**, nenhuma tarefa
   `Bloqueada` sem resolução.
2. **Se a Seção 2 ou 3 acima produziu reprovação simples/débito não bloqueante**:
   crie (ou adicione tarefa a) o lote `Refatoração Lote-X` na Seção 3 do
   `TASK.md` (X = identificador do lote-alvo atual), posicionado depois de todos
   os lotes já existentes na ordem de execução (Seção 4 do `TASK.md`) — uma
   tarefa por achado, referenciando a entrada do `QA-REPORT.md`/
   `SECURITY-REVIEW.md` que a originou. O lote-alvo **não** é reaberto por causa
   disso. Uso restrito das skills de decomposição do Coordenador
   (`task-decomposition`, `dependency-sequencing`, `task-md-drafting` — ver
   `validador.md`): só para esta tarefa pontual, nunca para redecompor um lote
   inteiro.
3. **Consistente** (mecânico — nenhuma constatação exige redesenho): siga para a
   Seção 5. Se o item 2 se aplicou, o lote fecha como `Validado (com ressalvas)`
   — a correção já está agendada em `Refatoração Lote-X`.
4. **Inconsistência que exige redesenho de dependência/decomposição real** (não é
   mera constatação de rotina — ex.: dependência genuinamente quebrada, lacuna de
   decomposição que o Validador não tem autoridade para decidir sozinho): **só
   aqui** volte a depender de outro agente — **pare**, registre em
   `BLOCKERS.md` escalando para `coordenador` (PIPELINE-CONVENTIONS.md §4), e
   informe ao usuário se é o `TASK.md` que precisa de correção direta ou se é
   pendência de implementação (nesse caso, volta para `/executar` depois que o
   Coordenador resolver).

### 5. Encerramento

Apresente o resumo do lote validado (veredito QA, veredito DevSecOps, checagem
estrutural, e as tarefas criadas em `Refatoração Lote-X`, se houver) e informe que
está pronto para `/deploy`. **Não dispare o deploy automaticamente.**

- Modo padrão: **pare aqui**, mesmo com o lote fechado limpo.
- `--continuar [N]`: se não atingiu o teto N (ou não há teto) e a Seção 1 encontra
  outro lote pronto agora, volte à Seção 1 para ele, sem pausar. Se não sobrar
  nenhum lote pronto no momento: **isso não é erro** — informe e encerre a
  resposta normalmente, é o estado esperado entre uma liberação de lote e outra.

**Argumento opcional `--continuar [N]`**: encadeia a validação lote após lote (até
N, ou sem limite se N omitido) — ao terminar um lote (Seção 5), volta à Seção 1
para o próximo lote já pronto, sem pausar entre eles. Os pontos de parada
obrigatórios (reprovação **crítica** de QA, achado alto/crítico de DevSecOps,
inconsistência estrutural, bloqueio) valem igual em qualquer modo — só reprovação
**simples**/débito baixo-médio não para, e vira tarefa em `Refatoração Lote-X`
(Seção 4, item 2). Este comando nunca espera lote ficar pronto sozinho: se nenhum
lote atende ao critério da Seção 1, ele encerra normalmente na hora, mesmo em
`--continuar`. Para checar "tem lote novo pronto?" de tempos em tempos sem o
usuário rodar o comando de novo manualmente, combine com o skill `/loop` — ex.:
`/loop 5m /validar --continuar` — que reagenda a próxima chamada sozinho; uma
validação em andamento nunca é interrompida no meio por causa do intervalo.

---

## Comando 3: `/deploy` — Validador (QA + DevSecOps de confirmação → DevOps)

| Dispara quando | Agente | Ação | Pausa obrigatória |
|---|---|---|---|
| Usuário roda `/deploy` | `validador` (confirmação final + chapéu DevOps) | Provisiona infra/CI-CD (1ª vez), confirma validação, publica | Sempre antes de produção; achado bloqueante na confirmação final |

### 1. Preparação de infraestrutura (normalmente já feita antes da primeira chamada)

Se `.md/DEPLOY.md` ainda não existir (nenhum deploy anterior): dispare `validador`
(chapéu DevOps: `infrastructure-as-code-provisioning`,
`cicd-pipeline-configuration`) a partir do `SDD.md`/`GUARDRAILS.md` já aprovados.
Isso não depende de nenhum lote específico — é preparação de projeto.

`validador.md` já declara que o chapéu DevOps prepara infra/CI-CD "em paralelo à
implementação", assim que o `SDD.md` é aprovado pelo usuário — não precisa esperar
o primeiro `/deploy`. Na prática, o orquestrador pode (e deve, quando fizer
sentido) disparar esse mesmo dispatch de forma proativa durante uma sessão longa
de `/executar --continuar`, assim que o `SDD.md` for aprovado (ver Comando 1,
fim da Seção 3) — puramente oportunista, não bloqueia nem pausa nada. Esta Seção 1
continua existindo como rede de segurança: se por algum motivo isso não aconteceu
antes, `/deploy` garante a preparação na primeira chamada, do jeito que já
funcionava.

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
passa a ser "Validado" (critério do comando 2 acima: QA + DevSecOps + checagem
estrutural, tudo resolvido pelo próprio Validador salvo escalação real ao
Coordenador), e "publicado" passa a ser rastreado via
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

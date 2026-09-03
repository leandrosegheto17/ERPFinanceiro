# EXECUTION-FLOW.md

Sequência lógica da **fase de execução** — parte de onde o planejamento termina
(`TASK.md` aprovado no Gate 3 + `GUARDRAILS.md` aprovado, ver `PLANNING-FLOW.md`) e
vai até o deploy em produção, fechando o ciclo de volta ao CTO.

Este documento não redefine nenhum dos 12 agentes nem os critérios internos de cada
um além do necessário para operar em lote (ver seção seguinte) — só ordena o que cada
agente já declara, com pontos de paralelismo, pausa e escalonamento.

**Técnica de implementação**: cada trilha usa ciclo TDD (teste falha → implementação
mínima → teste passa → refatora) dentro da própria skill `automated-testing` do
agente — é técnica interna de cada dispatch, não um passo separado do orquestrador.
Uma camada de revisão (spec-compliance + qualidade de código) roda depois de cada
tarefa implementada, antes do QA entrar — mecanismo próprio deste fluxo, não do
Superpowers (ver histórico de decisão: Superpowers foi avaliado e descartado como
motor deste fluxo por ser dimensionado para feature isolada em manutenção, não para
um pipeline de 12 papéis com gate de CTO — revisitar quando a fase de manutenção
pós-v1 for desenhada).

**Pré-requisito bloqueante**: este projeto precisa ser um repositório git antes deste
fluxo rodar de verdade — a camada de revisão depende de diff (`git diff`) entre o
estado antes e depois de cada tarefa.

---

## Unidade de trabalho: o lote

Até a revisão que originou esta seção, a unidade de trabalho era a tarefa individual
— o que gerava dispatch demais (bateria completa de QA a cada tarefa) e nenhum ponto
de poda de contexto ao longo do projeto. O extremo oposto (Backend/Frontend/Mobile
executando o `TASK.md` inteiro antes de qualquer QA) foi descartado por criar efeito
cascata de retrabalho quando um erro nasce cedo e só é detectado no fim. A unidade
adotada é o **lote**: um conjunto de tarefas do `TASK.md` que formam uma
funcionalidade/módulo com sentido próprio (ex.: "cadastro de paciente").

- **Onde vive**: coluna `Lote` na Seção 3 do `TASK.md` (`task-decomposition`,
  `tech-lead.md`), atribuída pelo Tech Lead durante a decomposição, alinhada aos
  clusters de dependência que ele já mapeia na Seção 4 — um lote é, sempre que
  possível, um componente conectado no grafo de dependências, não um corte
  arbitrário.
- **O que opera em nível de lote**: as 3 trilhas de implementação (dentro do lote,
  tarefas em sequência), a revisão spec-compliance + qualidade pós-tarefa, a bateria
  completa de QA, a auditoria completa do DevSecOps, o fechamento estrutural do Tech
  Lead e o deploy em staging/produção.
- **O que continua em nível de projeto** (não de lote): `test-strategy-planning` do
  QA, `static-security-analysis` contínuo do DevSecOps, e a preparação de
  infraestrutura/CI-CD do DevOps — os três já rodam desde o início da execução,
  independente de qualquer lote fechar.
- **Teto de fix-loop**: a revisão pós-tarefa (spec-compliance + qualidade) tem no
  máximo **2 tentativas de correção**. Na 3ª falha consecutiva na mesma tarefa: para
  o ciclo, marca a tarefa `Bloqueada`, registra `BLOCKERS.md` e escala para
  `tech-lead` — falha repetida de revisão é sinal de tarefa mal decomposta/ambígua,
  mesma lógica já usada quando o QA identifica um padrão recorrente de bug.

---

## As 3 trilhas paralelas (Backend / Frontend / Mobile), dentro de um lote

Cada trilha processa, em paralelo com as outras duas, as tarefas **do lote corrente**
atribuídas a ela (coluna "dono/time responsável", Seção 3). **Dentro de uma mesma
trilha, as tarefas rodam em sequência** (uma por vez, respeitando as dependências já
mapeadas na Seção 4 do TASK.md) — só o paralelismo *entre* trilhas é real.

Por tarefa, dentro da trilha:

1. Dispara o agente da trilha (`backend`/`frontend`/`mobile`) para a tarefa
   específica — ele implementa em ciclo TDD via sua própria `automated-testing`.
2. Ao concluir, dispara uma revisão de spec-compliance (contra o critério de aceite
   da própria tarefa) + qualidade de código.
3. Achado da revisão: corrige e revisa de novo (fix-loop) — **sem pausar**, até o
   teto de 2 tentativas definido acima; na 3ª falha, pausa e escala (ver seção
   anterior).
4. Marca a tarefa `Concluída` no `TASK.md` (mecanismo já definido nos três agentes).

**Dependência de contrato de API (Frontend/Mobile ↔ Backend)**: não é orquestrada
aqui — `frontend.md`/`mobile.md` já resolvem sozinhos ("mock se o endpoint já está
em `API-CONTRACT.yaml`, aguarda se não está"). O orquestrador só precisa saber que
uma tarefa `Em andamento` com nota de mock ainda não está de fato pronta — e,
portanto, o lote ainda não fechou.

---

## QA — uma vez por lote fechado

`test-strategy-planning` continua rodando desde o Gate 3 (fim do planejamento), em
paralelo, nível de projeto — não é acionado de novo aqui e não muda com esta revisão.

As 5 skills de validação (`acceptance-criteria-validation`,
`cross-platform-integration-testing`, `bug-documentation`,
`non-functional-validation`, `qa-report-drafting`) disparam **uma vez, quando todas
as tarefas do lote corrente estiverem `Concluída`** por Backend/Frontend/Mobile —
não mais por tarefa individual, nem em lote com o projeto inteiro. Isso muda o
"Ponto de Sincronização" e os "Critérios de Pronto" declarados em `qa.md`
(atualizados junto com esta revisão) de granularidade "por tarefa" para "por lote".

- Aprovação (Aprovado ou Aprovado com ressalvas) do lote inteiro: segue sem pausa.
- **Reprovação de qualquer tarefa do lote: pausa obrigatória.** Explica o motivo, a(s)
  tarefa(s) reprovada(s) — e o que depende delas dentro do mesmo lote — volta(m) pra
  trilha responsável (`Em andamento`, conforme já definido em `qa.md`). O QA
  **revalida só o que foi reprovado e suas dependências**, não o lote inteiro do
  zero, quando retomar.

---

## DevSecOps — dois ritmos, auditoria por lote

- `static-security-analysis` (SAST, dependências) roda **contínuo desde o início**
  da execução, nível de projeto, em paralelo às 3 trilhas — não espera nenhum lote
  fechar. Mantido assim deliberadamente: é varredura automática de baixo custo de
  dispatch (não é a bateria de 5 skills isoladas que gerava o problema original), e
  detectar segredo vazado ou dependência vulnerável cedo vale mais do que esperar o
  lote fechar.
- **Achado de severidade alta/crítica, a qualquer momento** (inclusive durante a
  varredura contínua): **pausa obrigatória**, como já era.
- A auditoria completa (as outras 5 skills) dispara quando o QA tiver aprovado
  (Aprovado ou Aprovado com ressalvas) **todas as tarefas do lote** — mesmo gatilho
  "QA aprovou o build" que `devsecops.md` já define; não precisou mudar nada nesse
  agente, ele já operava em nível de build/lote.
- Achado que bloqueia: pausa, explica, escala para a trilha de implementação
  responsável (campo "Escala para" já definido em `devsecops.md`), retoma a trilha
  original após a correção — e a auditoria do lote é refeita sobre o que mudou.

---

## Tech Lead — fecha o lote

Quando QA e DevSecOps já aprovaram (Aprovado ou Aprovado com ressalvas/débito) o
mesmo lote: o Tech Lead faz uma checagem **estrutural**, não uma reavaliação de
qualidade ou segurança (isso já foi feito) — confirma que:

- toda tarefa do lote está `Concluída` no `TASK.md`;
- nenhuma dependência da Seção 4 relativa ao lote ficou órfã ou inconsistente;
- nenhuma tarefa do lote segue `Bloqueada` sem resolução registrada.

Essa confirmação é o que libera o lote para deploy (seção seguinte) — coerente com o
papel do Tech Lead como dono do `TASK.md`, sem duplicar a autoridade de QA/DevSecOps.
Se algo não bate, o Tech Lead corrige o `TASK.md` (ou devolve para a trilha
responsável, se for pendência de implementação) antes de liberar.

---

## DevOps — prepara desde o início do projeto, deploy por lote

- `infrastructure-as-code-provisioning` e `cicd-pipeline-configuration` disparam
  assim que o `SDD.md` está aprovado (Gate 2, já aconteceu no planejamento) — nível
  de projeto, não de lote — em paralelo com as 3 trilhas, sem pausa.
- O deploy de um lote em si só dispara com a **dupla aprovação** (`QA-REPORT.md` e
  `SECURITY-REVIEW.md` do mesmo lote, Aprovado ou Aprovado com ressalvas/débito) **e**
  a checagem estrutural do Tech Lead (seção anterior).
- **Deploy em staging: sem pausa**, assim que o lote fecha — cada lote fechado já é
  um incremento deployável.
- **Deploy em produção: pausa obrigatória sempre**, por lote — mesmo com tudo limpo.
  O usuário decide, lote a lote, se aquele incremento vai para produção agora ou
  espera acumular com o próximo; não é condicional a haver problema, e não espera o
  `TASK.md` inteiro fechar.

---

## Reset de contexto entre lotes

Ao fechar um lote (QA aprovado + DevSecOps aprovado + Tech Lead aprovado
estruturalmente), o orquestrador monta um **resumo compacto** a partir do que já
existe nos artefatos — não cria um arquivo novo:

- notas de status das tarefas do lote no `TASK.md`;
- veredito e débitos registrados do lote no `QA-REPORT.md`;
- veredito e débitos registrados do lote no `SECURITY-REVIEW.md`;
- resultado do deploy do lote (staging/produção) no `DEPLOY.md`, quando aplicável.

Esse resumo é apresentado ao usuário no fechamento do lote e é o **único contexto
carregado** para o próximo — os dispatches do próximo lote se baseiam nos artefatos
em disco + esse resumo, não no histórico detalhado de dispatches, revisões e
fix-loops do lote anterior (esse histórico já cumpriu seu papel e não precisa ser
re-carregado).

---

## Bloqueio silencioso

Se uma tarefa (ou o lote inteiro) ficar travado por dependência não resolvida (de
outra tarefa, de um endpoint que não existe, de um achado ainda não corrigido), o
reporte inclui **há quanto tempo está parado** — calculado a partir da data já
registrada na entrada correspondente de `BLOCKERS.md`, nunca deixado travado em
silêncio.

---

## Resumo: quando pausa e quando não pausa

**Pausa obrigatória**:
- Qualquer reprovação do QA numa tarefa do lote.
- Qualquer achado de severidade alta/crítica do DevSecOps, a qualquer momento.
- 3ª falha consecutiva do fix-loop numa mesma tarefa (teto de 2 tentativas).
- Sempre antes do deploy em produção, por lote.
- Tarefa ou lote bloqueado por dependência não resolvida (reporta com tempo parado).

**Progride sem pausa**:
- Execução paralela normal das 3 trilhas, dentro do lote.
- Preparação de infraestrutura e pipeline do DevOps (nível de projeto).
- SAST contínuo do DevSecOps (nível de projeto).
- Aprovações limpas do QA e do DevSecOps sobre o lote fechado.
- Checagem estrutural limpa do Tech Lead.
- Fix-loop interno de revisão pós-implementação, até o teto de 2 tentativas.
- Deploy em staging, por lote.

---

## Onde o fluxo termina

Cada deploy em produção gera uma entrada em `DEPLOY.md` (sucesso, rollback,
incidente) e um registro de fechamento do **Gate 4** em `CTO-REVIEW.md` pelo CTO —
um registro por lote deployado em produção, sem poder de veto (o deploy já
aconteceu), coerente com o formato "relatório de cada deploy" que `devops.md` já
declarava.

A sessão de execução (`/executar`) termina quando não houver mais lote com tarefa
pendente no escopo corrente do `TASK.md`. Se ainda houver lotes não iniciados,
informa que ficaram para uma próxima chamada — não força todos os lotes numa única
execução.

Ao final, apresentar a lista consolidada de tudo que foi implementado, testado,
auditado e deployado nesta execução, com status de cada lote.

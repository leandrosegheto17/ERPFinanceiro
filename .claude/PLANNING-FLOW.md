# PLANNING-FLOW.md

Sequência lógica da **fase de planejamento** do pipeline — da ideia inicial até um
`TASK.md` + `GUARDRAILS.md` prontos para a fase de execução (`EXECUTION-FLOW.md`).

Este documento não redefine nenhum dos 4 agentes consolidados
(`.claude/agents/gestor.md`, `coordenador.md`, `executor.md`, `validador.md`) nem a
convenção de artefatos (`PIPELINE-CONVENTIONS.md`) — só ordena o que cada um já
declara, na forma de **dois comandos que o usuário aciona manualmente**: `/planejar`
e `/definir_organizar`. Não existe mais um único fluxo encadeado com pausa a cada
sub-etapa — **o usuário é o orquestrador**: cada comando roda um ou mais **loops
de refinamento** (ver seção abaixo) até o usuário aprovar explicitamente cada
artefato ou pacote de artefatos, e então para. O usuário decide quando pedir
ajuste dentro do loop aberto, quando aprovar e seguir, ou quando descartar e
recomeçar.

> Modelo anterior (12 agentes, 8 etapas com pausa a cada uma) descontinuado — ver
> nota no topo de `PIPELINE-CONVENTIONS.md`. Os agentes `cto`, `pm`,
> `business-analyst`, `software-architect`, `ux-ui`, `tech-lead` continuam existindo
> como arquivos, mas não são mais acionados por este fluxo.

---

## Mecânica de loop de refinamento

Os dois comandos abaixo compartilham o mesmo mecanismo de refinamento — definido
uma vez aqui para não repetir em cada um.

- **Rodada**: um dispatch do agente que produz/atualiza um rascunho, seguido da
  reação do usuário (aprova / pede ajuste, descrevendo o quê / descarta e
  recomeça).
- **Loop**: uma sequência de rodadas sobre **a mesma instância viva** de um
  agente. A rodada 1 de um loop é sempre um dispatch novo (`Agent`,
  `subagent_type` correspondente, `run_in_background: false`). A rodada 2 em
  diante do **mesmo loop** é sempre `SendMessage` para essa mesma instância —
  nunca um dispatch novo — para o agente não precisar reexplicar o rascunho a
  cada rodada.
- **O artefato é escrito a cada rodada**, não só na aprovação final — consistente
  com a convenção já existente de que artefatos são versionados pelo git do
  projeto, não por sufixo de nome de arquivo (PIPELINE-CONVENTIONS.md §2).
  Aprovação é um marco conceitual (o usuário decidiu que está pronto), não um
  lock técnico.
- **Um loop fecha** só quando o usuário aprova explicitamente o rascunho atual —
  aí sim o comando segue para o próximo loop/etapa. **Não há teto de rodadas**:
  ao contrário do fix-loop de 2 tentativas da fase de execução (limitado porque é
  sobre corretude verificável contra um `git diff`), este loop é sobre
  completude/alinhamento com o usuário, então roda quantas rodadas forem
  necessárias.
- **"Descarta e recomeça"**: se o usuário rejeitar o rascunho inteiro em vez de
  pedir ajuste pontual, a próxima rodada é um dispatch novo (não `SendMessage`
  para a instância descartada).
- **Bloqueio no meio de um loop**: uma entrada nova `Aberto` em `BLOCKERS.md`
  **suspende** o loop (não o descarta). Para retomar: `SendMessage` para a mesma
  instância, se ela ainda existir na sessão atual; se não (ver limitação técnica
  abaixo), um dispatch novo lendo o artefato + `BLOCKERS.md` do disco, equivalente
  a uma reabertura pontual restrita ao ponto que ficou em aberto.
- **Limitação técnica do `SendMessage`**: só funciona enquanto o processo da
  instância da rodada 1 ainda existe (mesma sessão de trabalho). Se o usuário
  encerrar e voltar numa sessão nova no meio de um loop aberto, essa instância se
  perde — o comando cai para um dispatch novo lendo o artefato do disco no estado
  em que ficou. Não existe (nem está previsto) nenhum mecanismo de persistência
  para contornar isso — é uma limitação aceita, não um bug a corrigir.
- **Entre loops/estágios distintos é sempre dispatch novo**, lendo o artefato já
  fechado do disco — nunca `SendMessage` continuando a instância do loop
  anterior. Isso preserva a convenção já existente de handoff por artefato entre
  fases (PIPELINE-CONVENTIONS.md §2), diferente de rodadas dentro do **mesmo**
  loop, que continuam a mesma instância deliberadamente.
- **"Reabertura pontual" é sempre depois de um loop já fechado** (artefato já
  aprovado pelo usuário): sempre um dispatch novo lendo o artefato do disco, nunca
  `SendMessage` para uma instância de um loop já encerrado.

---

## Comando 1: `/planejar` — Gestor (CTO + PM + BA), Loop A

| Dispara quando | Agente | Produz | O que o usuário faz depois |
|---|---|---|---|
| Ideia inicial (briefing) ou retomada de um `PRD.md`/`PRD-TECNICO.md` já existente | `gestor` (uma instância viva ao longo do Loop A) | Gate 1 em `CTO-REVIEW.md` + `PRD.md` + `PRD-TECNICO.md` | Aprova (segue para `/definir_organizar`), pede ajuste (mais uma rodada do mesmo loop), ou descarta e recomeça |

**Gate 1 é a "rodada 0" do Loop A**: o chapéu CTO valida o alinhamento estratégico
sobre o briefing antes de qualquer rascunho de PRD existir. Se reprovar dentro do
próprio dispatch, não há rascunho para iterar — o Gestor devolve só o veredito e o
motivo, sem PRD.md/PRD-TECNICO.md; o usuário ajusta o briefing e a próxima chamada
é sempre um dispatch novo (não há loop para continuar ainda).

Se o Gate 1 aprovar (com ou sem ressalvas), o mesmo dispatch já produz a rodada 1
do Loop A: os chapéus PM → BA em sequência interna, produzindo um **rascunho** de
`PRD.md` + `PRD-TECNICO.md` — não um resultado final. Da rodada 2 em diante, o
Loop A continua a **mesma instância** do Gestor via `SendMessage` (nunca um
dispatch novo) com o feedback do usuário, até ele aprovar explicitamente os dois
documentos juntos. Só então o Loop A fecha.

**Retomada**: se `PRD.md`/`PRD-TECNICO.md` já existirem, distinga três casos: (a)
o Loop A desta mesma sessão ainda está aberto (nenhuma aprovação registrada) —
continue via `SendMessage`; (b) o Loop A já fechou (aprovado) e o usuário está
pedindo algo novo — trate como reabertura pontual (sempre dispatch novo, lendo o
que existe do disco); (c) execução anterior ficou incompleta numa sessão que não
existe mais (ver limitação técnica do `SendMessage` acima) — dispatch novo lendo
`PRD.md`/`PRD-TECNICO.md`/`CTO-REVIEW.md` do disco, retomando do ponto em que
ficou.

## Comando 2: `/definir_organizar` — Coordenador, dois loops sequenciais (Loop B +
Loop C) + aprovação de GUARDRAILS.md pelo Gestor

| Dispara quando | Agente(s) | Produz | O que o usuário faz depois |
|---|---|---|---|
| `/planejar` liberou `PRD-TECNICO.md` (aprovado pelo usuário) | `coordenador` (uma instância viva por loop — Loop B, depois Loop C, dispatches distintos) → `gestor` (dispatch curto, só `guardrails-governance`) | `SDD.md` + ADRs + `UX-SPEC.md` (Loop B) → `TASK.md` (Loop C) → `GUARDRAILS.md` (com veredito do Gestor) | Aprova cada loop conforme fecha; ao final, aprova o pacote técnico completo e decide quando rodar `/executar` |

Um único comando, duas etapas internas sequenciais — cada uma seu próprio loop de
refinamento (ver "Mecânica de loop de refinamento" acima), a segunda só começa
depois que a primeira fecha:

### Loop B — Coordenador, chapéus Software Architect + UX/UI

Rodada 1 (dispatch novo): chapéu Software Architect → `SDD.md` + ADRs; na mesma
instância, chapéu UX/UI → `UX-SPEC.md`, já respeitando os limites técnicos do
SDD.md que acabou de produzir. Os dois juntos são o rascunho da rodada 1 — não um
resultado final. Rodada 2 em diante: feedback do usuário vai para a mesma
instância via `SendMessage`, até ele aprovar `SDD.md` + `UX-SPEC.md` juntos. Esse
é o **checkpoint**: o Loop C só começa depois do Loop B fechar.

### Loop C — Coordenador, chapéu Tech Lead (decomposição)

Rodada 1 é **sempre um dispatch novo** (nunca continua a instância do Loop B) —
lê `SDD.md`/`UX-SPEC.md` já fechados do disco, não carrega memória de conversa do
Loop B. Antes de apresentar a rodada 1 ao usuário, o Coordenador roda um
**autocheck mecânico** contra as regras de granularidade de `coordenador.md`
("Como Tech Lead / decomposição": tamanho-alvo ~1 dia-pessoa, não-mistura de
tela/endpoint/regra de negócio/SQL, canário de ~300 mil tokens) e já re-divide em
tarefas menores o que violar — apresentando o rascunho **já corrigido**, com uma
nota breve do que foi dividido e por quê (nunca em silêncio, para o usuário não
estranhar uma contagem de tarefas maior que o esperado). Produz `TASK.md` + o
rascunho de `GUARDRAILS.md`. Rodada 2 em diante: feedback do usuário via
`SendMessage`, até aprovar.

### Aprovação de GUARDRAILS.md (Gestor)

Depois que o Loop C fecha: `gestor`, só a skill `guardrails-governance`, sobre o
rascunho de `.md/GUARDRAILS.md` — dispatch único, não é um loop (é checagem
mecânica de governança, não negociação de gosto). Não é um gate técnico sobre
SDD/TASK.

**Não existe mais aprovação do Gestor sobre o SDD.md/TASK.md em si** (os antigos
Gates 2 e 3 do CTO) — o usuário aprova cada loop diretamente. Se o usuário quiser
um parecer de risco/trade-off antes de aprovar (as skills
`architecture-decision-review`, `build-vs-buy-analysis`, `risk-and-compliance-check`,
`capacity-and-timeline-validation` continuam existindo no Gestor, agora só como
parecer ad hoc — ver `gestor.md`), pode pedir explicitamente — não é automático.

**Granularidade das tarefas**: o autocheck do Loop C garante que toda tarefa do
`TASK.md` é pequena o bastante para caber num único ciclo de implementação do
Executor (~1 dia-pessoa, sem misturar tela/endpoint/regra de negócio/SQL, sem
exigir mais de ~300k tokens de contexto de trabalho previstos), e a Seção 4 marca
explicitamente quais tarefas de cada lote podem rodar em paralelo.

**Fechamento**: junto do resumo de sempre (Seção 4 abaixo), relate explicitamente
a contagem — **N tarefas, M lotes, e por lote quantas tarefas são
paralelizáveis** (= quantas instâncias do Executor o `/executar` pode disparar por
rodada nesse lote). O dado já existe nas Seções 3-4 do `TASK.md`; isso é só
relatar o número com todas as letras no fechamento.

**Reabertura pontual**: só depois que **ambos** os loops já fecharam (pacote
aprovado). Se o usuário pedir ajuste em só um ponto (ex.: "revê a Seção 3 do
SDD.md"), o comando redispara o Coordenador com **dispatch novo** (nunca
`SendMessage` para uma instância de loop já encerrado) só para aquele ponto —
nunca refaz o pacote inteiro, a menos que a mudança tenha efeito cascata (o
próprio Coordenador sinaliza se for o caso, e se afetar o SDD.md/UX-SPEC.md depois
que o TASK.md já foi decomposto em cima deles, o Coordenador aponta quais tarefas
do Loop C podem precisar de revisão).

---

## Onde as "specs por chapéu de execução" já existem

- **Backend, Frontend, Mobile** (chapéus do Executor): rotulados na Seção 3 do
  `TASK.md` — cada tarefa tem coluna de dono/chapéu, granularidade pequena, e
  marcação de paralelismo (Seção 4). Essa tabela é a spec individual de
  implementação; não existe arquivo separado por chapéu.
- **UX/UI**: incorporado ao Coordenador — o `UX-SPEC.md` é produzido antes do
  `TASK.md`, na mesma chamada de `/definir_organizar`, não incrementalmente ao
  longo da execução como no pipeline de 12 agentes.
- **DevSecOps e DevOps** (chapéus do Validador): não recebem tarefa individual no
  `TASK.md` — o chapéu DevSecOps consome o `SDD.md` inteiro (Seção 7) dentro do
  `/validar`; o chapéu DevOps consome as Seções 3 e 6 dentro do `/deploy`. Ver
  `EXECUTION-FLOW.md`.

---

## CLAUDE.md e GUARDRAILS.md

- **CLAUDE.md**: não existe como arquivo separado — consolidado na Seção 1 do
  `TASK.md` ("Diretrizes de Implementação"), produzida pelo Coordenador dentro de
  `/definir_organizar`.
- **GUARDRAILS.md**: o Coordenador propõe o rascunho no mesmo `/definir_organizar`
  que produz o `TASK.md`; o Gestor aprova (skill `guardrails-governance`), como
  último passo do mesmo comando, automaticamente — o usuário não precisa acionar
  nada à parte para isso.

---

## Onde o fluxo de planejamento termina

`SDD.md` + `UX-SPEC.md` + `TASK.md` aprovados pelo usuário **+** `GUARDRAILS.md`
aprovado pelo Gestor — os dois juntos marcam planejamento completo. A partir daqui,
o usuário aciona `/executar` para começar a fase de execução (`EXECUTION-FLOW.md`).

---

## Pausa e escalonamento

- Cada rodada de cada loop para ao final, apresentando um resumo objetivo (pontos
  principais do rascunho atual, não o documento inteiro, e o checklist "Critérios
  de Pronto" do agente) e as três opções (aprovar / pedir ajuste / descartar e
  recomeçar) — não só uma vez ao final do comando inteiro, mas a cada rodada de
  cada loop aberto.
- Se um agente gerar uma entrada em `BLOCKERS.md` durante o dispatch (ex.: o
  Coordenador acha o `PRD-TECNICO.md` inviável tecnicamente): o loop aberto
  **suspende** (não descarta) e o comando **pára imediatamente**, explica o
  bloqueio (quem reportou, o quê, e o campo "Escala para" do agente que reportou)
  e devolve a decisão ao usuário — nenhum agente é disparado automaticamente para
  resolver. O usuário decide se pede ajuste no ponto afetado (retomando o loop
  suspenso via `SendMessage`, ou dispatch novo se a sessão não existir mais — ver
  "Mecânica de loop de refinamento"), pede ao Gestor um parecer ad hoc, ou segue de
  outra forma.
- Se o usuário pedir ajuste dentro de um loop aberto: mais uma rodada do mesmo
  loop, via `SendMessage`, nunca um dispatch novo. Se pedir para descartar tudo e
  recomeçar: a próxima rodada é sempre um dispatch novo.
- Um loop só fecha (e o comando avança para o próximo loop/etapa) na aprovação
  explícita do usuário sobre aquele rascunho.

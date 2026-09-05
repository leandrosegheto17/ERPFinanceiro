# PLANNING-FLOW.md

Sequência lógica da **fase de planejamento** do pipeline — da ideia inicial até um
`TASK.md` + `GUARDRAILS.md` prontos para a fase de execução (`EXECUTION-FLOW.md`).

Este documento não redefine nenhum dos 4 agentes consolidados
(`.claude/agents/gestor.md`, `coordenador.md`, `executor.md`, `validador.md`) nem a
convenção de artefatos (`PIPELINE-CONVENTIONS.md`) — só ordena o que cada um já
declara, na forma de **dois comandos que o usuário aciona manualmente**: `/planejar`
e `/definir_organizar`. Não existe mais um único fluxo encadeado com pausa a cada
sub-etapa — **o usuário é o orquestrador**: cada comando roda até completar seu
próprio artefato ou pacote de artefatos, apresenta o resultado, e para. O usuário
decide quando rodar o próximo comando, quando pedir ajuste, ou quando reprovar e
mandar refazer.

> Modelo anterior (12 agentes, 8 etapas com pausa a cada uma) descontinuado — ver
> nota no topo de `PIPELINE-CONVENTIONS.md`. Os agentes `cto`, `pm`,
> `business-analyst`, `software-architect`, `ux-ui`, `tech-lead` continuam existindo
> como arquivos, mas não são mais acionados por este fluxo.

---

## Comando 1: `/planejar` — Gestor (CTO + PM + BA)

| Dispara quando | Agente | Produz | O que o usuário faz depois |
|---|---|---|---|
| Ideia inicial (briefing) ou retomada de um `PRD.md`/`PRD-TECNICO.md` já existente | `gestor` (dispatch único) | Gate 1 em `CTO-REVIEW.md` + `PRD.md` + `PRD-TECNICO.md` | Aprova, pede ajuste, ou reprova — decide quando rodar `/definir_organizar` |

Uma única chamada do agente `gestor` cobre os três chapéus em sequência interna
(CTO → PM → BA, exatamente como `gestor.md` já descreve) e devolve os três
resultados de uma vez — não há pausa entre o chapéu CTO e o chapéu PM, por exemplo;
a única pausa é no final do comando, para o usuário.

**Se o Gate 1 (chapéu CTO) reprovar dentro do próprio dispatch**: o Gestor não seue
para os chapéus PM/BA — devolve só o veredito do Gate 1 e o motivo, sem PRD.md nem
PRD-TECNICO.md. O usuário ajusta o briefing e roda `/planejar` de novo.

**Retomada**: se `PRD.md`/`PRD-TECNICO.md` já existirem (execução anterior
incompleta, ou usuário pedindo ajuste), `/planejar` não recomeça do zero — lê o que
já existe, identifica o que falta ou o que o usuário quer ajustar, e redispara o
Gestor só para a parte necessária.

## Comando 2: `/definir_organizar` — Coordenador (Software Architect + Tech Lead +
UX/UI) + aprovação de GUARDRAILS.md pelo Gestor

| Dispara quando | Agente(s) | Produz | O que o usuário faz depois |
|---|---|---|---|
| `/planejar` liberou `PRD-TECNICO.md` (aprovado pelo usuário) | `coordenador` (dispatch único, 3 chapéus internos) → `gestor` (dispatch curto, só `guardrails-governance`) | `SDD.md` + ADRs + `UX-SPEC.md` + `TASK.md` + `GUARDRAILS.md` (com veredito do Gestor) | Aprova o pacote técnico (SDD/UX-SPEC/TASK) diretamente, pede ajuste pontual, ou reprova — decide quando rodar `/executar` |

Sequência interna de uma única chamada:

1. `coordenador`, chapéu Software Architect → `SDD.md` + ADRs.
2. `coordenador` (mesma instância/contexto), chapéu UX/UI → `UX-SPEC.md`, já
   respeitando os limites técnicos do SDD.md que acabou de produzir.
3. `coordenador`, chapéu Tech Lead → `TASK.md` (tarefas pequenas, com coluna de
   paralelismo dentro do lote — ver `coordenador.md`) + rascunho de
   `GUARDRAILS.md`.
4. `gestor`, só a skill `guardrails-governance`, sobre o rascunho de
   `GUARDRAILS.md` — aprovação rápida, não é um gate técnico sobre SDD/TASK, é a
   checagem de governança que só o Gestor faz (PIPELINE-CONVENTIONS.md §5).

**Não existe mais aprovação do Gestor sobre o SDD.md/TASK.md em si** (os antigos
Gates 2 e 3 do CTO) — o usuário revisa e aprova esse par diretamente. Se o usuário
quiser um parecer de risco/trade-off antes de aprovar (as skills
`architecture-decision-review`, `build-vs-buy-analysis`, `risk-and-compliance-check`,
`capacity-and-timeline-validation` continuam existindo no Gestor), pode pedir
explicitamente — não é automático.

**Granularidade das tarefas**: `/definir_organizar` só está completo quando toda
tarefa do `TASK.md` é pequena o bastante para caber num único ciclo de
implementação do Executor, e a Seção 4 marca explicitamente quais tarefas de cada
lote podem rodar em paralelo — isso é o que permite ao `/executar` disparar
múltiplas instâncias do Executor em thread depois.

**Reabertura pontual**: se o usuário pedir ajuste em só um ponto (ex.: "revê a
Seção 3 do SDD.md"), o comando redispara o Coordenador só para aquele ponto — nunca
refaz o pacote inteiro, a menos que a mudança tenha efeito cascata (o próprio
Coordenador sinaliza se for o caso).

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

- Cada um dos dois comandos roda até entregar seu pacote completo, e para uma única
  vez ao final, apresentando um resumo objetivo (pontos principais dos artefatos,
  não o documento inteiro, e o checklist "Critérios de Pronto" do agente).
- Se um agente gerar uma entrada em `BLOCKERS.md` durante o dispatch (ex.: o
  Coordenador acha o `PRD-TECNICO.md` inviável tecnicamente): o comando **pára
  imediatamente**, explica o bloqueio (quem reportou, o quê, e o campo "Escala
  para" do agente que reportou) e devolve a decisão ao usuário — nenhum agente é
  disparado automaticamente para resolver. O usuário decide se roda `/planejar` de
  novo (ajuste no PRD-TECNICO.md), pede ao Gestor um parecer ad hoc, ou segue de
  outra forma.
- Se o usuário reprovar um pacote (Gate 1 do `/planejar`, ou o pacote técnico do
  `/definir_organizar`): o comando não avança — o usuário roda o mesmo comando de
  novo, com o ajuste pedido, quando estiver pronto.

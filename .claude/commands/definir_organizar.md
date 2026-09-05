---
description: Aciona o agente Coordenador em dois loops de refinamento sequenciais — Loop B (Software Architect + UX/UI, produz SDD.md+ADRs+UX-SPEC.md) e, só depois de fechado, Loop C (Tech Lead, produz TASK.md com autocheck automático de granularidade — 1 dia-pessoa por tarefa, sem misturar tela/endpoint/regra/SQL, canário de ~300k tokens) — seguido de uma aprovação rápida do Gestor sobre o rascunho de GUARDRAILS.md. Fecha relatando N tarefas/N lotes/paralelismo por lote. Você aprova o pacote técnico diretamente — não existe mais gate do CTO sobre SDD.md/TASK.md.
argument-hint: [vazio = continua o loop aberto ou roda a partir do PRD-TECNICO.md aprovado | texto = ajuste pontual sobre um artefato já aprovado]
---

# Comando `/definir_organizar` — Coordenador, Loop B + Loop C

A lógica deste comando está definida em `.claude/PLANNING-FLOW.md` (Comando 2,
inclusive a seção "Mecânica de loop de refinamento") — leia esse arquivo agora,
antes de fazer qualquer outra coisa, se ainda não o tiver em contexto. Ele por sua
vez assume o que está declarado em `.claude/agents/coordenador.md`,
`.claude/agents/gestor.md` e em `PIPELINE-CONVENTIONS.md`.

**O usuário é o orquestrador.** Este comando roda dois loops de refinamento
sequenciais com o Coordenador — Loop B (Software Architect + UX/UI) e, só depois
de fechado, Loop C (Tech Lead/decomposição) — seguidos de uma checagem de
governança do Gestor sobre `GUARDRAILS.md`. Não existe mais um agente CTO
aprovando SDD.md/TASK.md no meio do caminho — quem aprova cada loop é você.

Argumento recebido (pode estar vazio ou ser um pedido de ajuste pontual): $ARGUMENTS

## 0. Pré-requisito

Confirme que `.md/PRD-TECNICO.md` existe e foi aprovado pelo usuário (Loop A de
`/planejar` fechado). Se não existir, pare e informe que `/planejar` precisa
rodar primeiro.

## 1. Determinar o ponto de retomada

Identifique em qual estado o planejamento técnico está:

1. **Loop B desta mesma sessão ainda aberto** (instância do Coordenador viva, sem
   aprovação de SDD.md+UX-SPEC.md registrada): continue via `SendMessage` — vá
   para a Seção 2, rodada seguinte.
2. **Loop B fechado, Loop C ainda não começou ou está aberto nesta mesma sessão**:
   se ainda não começou, dispatch novo (Seção 3, rodada inicial); se já está
   aberto, continue via `SendMessage` (Seção 3, rodada seguinte).
3. **Ambos os loops fechados, `GUARDRAILS.md` ainda não aprovado**: vá para a
   Seção 4.
4. **Tudo já aprovado e `$ARGUMENTS` pede um ajuste pontual** (ex.: "revê a Seção
   3 do SDD.md", "adiciona um estado de erro no fluxo X do UX-SPEC.md"): trate
   como reabertura pontual (Seção 6) — sempre dispatch novo, nunca `SendMessage`
   para uma instância de loop já encerrado; não refaça o pacote inteiro.
5. **Nada existe ainda**: siga para a Seção 2, do zero (rodada inicial do Loop B).

## 2. Loop B — Coordenador (Software Architect + UX/UI)

### 2a. Rodada inicial (dispatch novo)

1. **Anuncie** que vai acionar o Coordenador para produzir um rascunho de SDD.md
   + UX-SPEC.md.
2. **Dispare o agente** via `Agent` (`subagent_type: coordenador`,
   `run_in_background: false`). O prompt de dispatch: aponte o `PRD-TECNICO.md`
   já disponível — o próprio agente cobre Architect → UX/UI internamente (não
   dispare duas vezes).
3. Se o Coordenador sinalizar bloqueio em qualquer chapéu: trate como bloqueio
   (Seção 7).

### 2b. Rodadas seguintes (mesma instância, via `SendMessage`)

Enquanto o usuário pedir ajuste: continue a **mesma instância** via `SendMessage`
(nunca dispatch novo), sem teto de rodadas. Cada rodada reescreve `SDD.md`/ADRs/
`UX-SPEC.md` no disco. "Descartar e recomeçar" → próxima rodada é dispatch novo
(volte para 2a).

### 2c. Fechamento do Loop B

Só fecha quando o usuário aprova `SDD.md` + `UX-SPEC.md` **juntos** — é o
checkpoint antes do Loop C poder começar.

## 3. Loop C — Coordenador (Tech Lead / decomposição)

### 3a. Rodada inicial (sempre dispatch novo — nunca continua o Loop B)

1. **Anuncie** que vai acionar o Coordenador para decompor o `TASK.md` a partir do
   `SDD.md`/`UX-SPEC.md` já aprovados.
2. **Dispare um agente novo** via `Agent` (`subagent_type: coordenador`,
   `run_in_background: false`) — não continue a instância do Loop B; o prompt
   aponta os artefatos já fechados no disco.
3. **Autocheck mecânico antes de apresentar**: o Coordenador confere cada tarefa
   contra as regras de granularidade de `coordenador.md` ("Como Tech Lead /
   decomposição" — tamanho-alvo ~1 dia-pessoa, não misturar tela/endpoint/regra de
   negócio/SQL na mesma tarefa, canário de ~300 mil tokens de contexto de
   trabalho previsto) e já re-divide o que violar **antes** de mostrar o
   resultado. Apresente o rascunho já corrigido junto com uma nota breve do que
   foi dividido e por quê — nunca em silêncio.
4. Se o Coordenador sinalizar bloqueio: trate como bloqueio (Seção 7).

### 3b. Rodadas seguintes (mesma instância, via `SendMessage`)

Igual ao Loop B: feedback do usuário continua a mesma instância via
`SendMessage`, sem teto, até aprovação. "Descartar e recomeçar" → dispatch novo
(volte para 3a).

### 3c. Fechamento do Loop C

Só fecha quando o usuário aprova o `TASK.md` (com o rascunho de `GUARDRAILS.md`
junto).

## 4. Aprovação de GUARDRAILS.md (Gestor)

1. **Dispare o agente** via `Agent` (`subagent_type: gestor`,
   `run_in_background: false`), só para a skill `guardrails-governance` sobre o
   rascunho de `.md/GUARDRAILS.md` que o Loop C acabou de produzir.
2. Isto NÃO é um gate técnico sobre SDD.md/TASK.md — é só a checagem de
   governança que `PIPELINE-CONVENTIONS.md` §5 reserva ao Gestor. Dispatch único,
   não é loop.

## 5. Apresentar o resultado (a cada rodada, e no fechamento final)

A cada rodada de cada loop, apresente um resumo objetivo do rascunho atual (não o
documento inteiro) e as três opções (aprovar / ajustar / descartar e recomeçar).

No **fechamento final** (depois da Seção 4), apresente:

- Pontos principais do `SDD.md` (arquitetura, stack, principais ADRs, riscos).
- Pontos principais do `UX-SPEC.md` (fluxos mapeados, componentes novos,
  restrições técnicas aplicadas).
- Pontos principais do `TASK.md`, incluindo o que foi auto-dividido pelo
  autocheck do Loop C.
- **A contagem explícita: N tarefas, M lotes, e por lote quantas tarefas são
  paralelizáveis** (= quantas instâncias do Executor o `/executar` pode disparar
  por rodada nesse lote).
- Veredito do Gestor sobre `GUARDRAILS.md`.
- O checklist "Critérios de Pronto" do `coordenador.md` para cada artefato.

**Pare aqui.** O usuário aprova o pacote técnico (SDD/UX-SPEC/TASK) diretamente —
sem gate de agente. Se aprovar: informe que o próximo passo disponível é
`/executar`. Se pedir ajuste pontual depois disso: trate na próxima chamada deste
comando (Seção 1.4). Se reprovar: pergunte o que precisa mudar antes de rodar de
novo.

Se o usuário quiser um parecer de risco/trade-off adicional antes de aprovar
(build-vs-buy, risco estratégico, capacidade/prazo), informe que pode pedir
explicitamente um parecer ad hoc ao `gestor` — isso não roda por padrão.

## 6. Reabertura pontual

Sempre um **dispatch novo** (nunca `SendMessage` para uma instância de loop já
encerrado), lendo o artefato afetado do disco, só sobre o ponto pedido — a menos
que o Coordenador sinalize efeito cascata (ex.: mudança no SDD.md/UX-SPEC.md que
afeta tarefas já decompostas no Loop C, caso em que aponta quais tarefas do
`TASK.md` podem precisar de revisão).

## 7. Bloqueio

Se o dispatch gerar uma entrada em `.md/BLOCKERS.md` (`Aberto`): o loop aberto no
momento **suspende** (não descarta) e o comando pára — explique quem reportou, o
quê, e o campo "Escala para" — **não dispare nenhum outro agente
automaticamente**. Informe ao usuário que a decisão de como seguir é dele. Para
retomar depois de resolvido: `SendMessage` para a mesma instância, se ainda
existir nesta sessão; senão, dispatch novo lendo o artefato afetado + `BLOCKERS.md`
do disco.

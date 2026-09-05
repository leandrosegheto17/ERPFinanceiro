---
description: Aciona o agente Coordenador (Software Architect + Tech Lead + UX/UI) para produzir SDD.md + ADRs + UX-SPEC.md + TASK.md numa única sequência interna, seguido de uma aprovação rápida do Gestor sobre o rascunho de GUARDRAILS.md. Você aprova o pacote técnico diretamente — não existe mais gate do CTO sobre SDD.md/TASK.md.
argument-hint: [vazio = roda a partir do PRD-TECNICO.md aprovado | texto = ajuste pontual sobre um artefato já produzido]
---

# Comando `/definir_organizar` — Coordenador

A lógica deste comando está definida em `.claude/PLANNING-FLOW.md` (Comando 2) —
leia esse arquivo agora, antes de fazer qualquer outra coisa, se ainda não o tiver
em contexto. Ele por sua vez assume o que está declarado em
`.claude/agents/coordenador.md`, `.claude/agents/gestor.md` e em
`PIPELINE-CONVENTIONS.md`.

**O usuário é o orquestrador.** Este comando faz uma sequência de chamadas
(Coordenador → Gestor, só para GUARDRAILS.md) e para, apresentando o pacote
completo para aprovação direta do usuário — não existe mais um agente CTO
aprovando SDD.md/TASK.md no meio do caminho.

Argumento recebido (pode estar vazio ou ser um pedido de ajuste pontual): $ARGUMENTS

## 0. Pré-requisito

Confirme que `.md/PRD-TECNICO.md` existe e foi aprovado pelo usuário (última
resposta de `/planejar`). Se não existir, pare e informe que `/planejar` precisa
rodar primeiro.

## 1. Determinar o ponto de retomada

1. Verifique o que já existe em `.md/`: `SDD.md`, ADRs (`.md/adr/`), `UX-SPEC.md`,
   `TASK.md`, `GUARDRAILS.md`.
2. Se `$ARGUMENTS` pede um ajuste pontual (ex.: "revê a Seção 3 do SDD.md",
   "adiciona um estado de erro no fluxo X do UX-SPEC.md"): trate como reabertura
   pontual (Seção 4) — não refaça o pacote inteiro.
3. Se nada existe ainda: siga para a Seção 2, do zero.

## 2. Disparar o Coordenador (sequência interna: Architect → UX/UI → Tech Lead)

1. **Anuncie** que vai acionar o Coordenador para produzir SDD.md, UX-SPEC.md e
   TASK.md em sequência.
2. **Dispare o agente** via `Agent` (`subagent_type: coordenador`,
   `run_in_background: false`). O prompt de dispatch: aponte o `PRD-TECNICO.md` já
   disponível — o próprio agente executa a sequência dos 3 chapéus internamente
   (não dispare 3 vezes; é uma única chamada que cobre Architect, UX/UI e Tech
   Lead, conforme `coordenador.md`).
3. Confirme que o resultado inclui os 4 artefatos (SDD.md + ADRs, UX-SPEC.md,
   TASK.md com rascunho de GUARDRAILS.md) antes de seguir — se o Coordenador
   sinalizar bloqueio em qualquer chapéu (ex.: requisito inviável, achado só no
   chapéu Tech Lead), pare e trate como bloqueio (Seção 5), sem descartar o que
   já foi produzido nos chapéus anteriores.

## 3. Aprovação de GUARDRAILS.md (Gestor)

1. **Dispare o agente** via `Agent` (`subagent_type: gestor`,
   `run_in_background: false`), só para a skill `guardrails-governance` sobre o
   rascunho de `.md/GUARDRAILS.md` que o Coordenador acabou de produzir.
2. Isto NÃO é um gate técnico sobre SDD.md/TASK.md — é só a checagem de
   governança que `PIPELINE-CONVENTIONS.md` §5 reserva ao Gestor.

## 4. Apresentar o resultado

Apresente um resumo objetivo (não os documentos inteiros):

- Pontos principais do `SDD.md` (arquitetura, stack, principais ADRs, riscos).
- Pontos principais do `UX-SPEC.md` (fluxos mapeados, componentes novos,
  restrições técnicas aplicadas).
- Pontos principais do `TASK.md` (número de lotes, número de tarefas por lote,
  quais tarefas são paralelizáveis dentro de cada lote, spikes identificados).
- Veredito do Gestor sobre `GUARDRAILS.md`.
- O checklist "Critérios de Pronto" do `coordenador.md` para cada artefato.

**Pare aqui.** O usuário aprova o pacote técnico (SDD/UX-SPEC/TASK) diretamente —
sem gate de agente. Se aprovar: informe que o próximo passo disponível é
`/executar`. Se pedir ajuste pontual: trate na próxima chamada deste comando
(Seção 1.2). Se reprovar: pergunte o que precisa mudar antes de rodar de novo.

Se o usuário quiser um parecer de risco/trade-off adicional antes de aprovar
(build-vs-buy, risco estratégico, capacidade/prazo), informe que pode pedir
explicitamente um parecer ad hoc ao `gestor` — isso não roda por padrão.

## 5. Bloqueio

Se o dispatch gerar uma entrada em `.md/BLOCKERS.md` (`Aberto`): pare, explique
quem reportou, o quê, e o campo "Escala para" — **não dispare nenhum outro agente
automaticamente**. Informe ao usuário que a decisão de como seguir é dele.

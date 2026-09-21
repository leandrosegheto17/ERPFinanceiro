---
name: stakeholder-alignment-check
description: Checklist final antes de liberar o PRD.md, garantindo que o escopo levantado não contradiz o que o CTO já validou no Gate 1 — evita retrabalho no próximo ponto de revisão do CTO (Gate 2). Use como último passo do PM, depois que prd-drafting já montou o documento completo. Do NOT use for validar viabilidade técnica (isso é do software-architect) ou para revisar arquitetura (isso é do cto no Gate 2, sobre o SDD.md, não sobre o PRD.md).
metadata:
  author: pm
  version: '1.0.0'
---

# Stakeholder Alignment Check

Você atua como PM (Product Manager) fazendo a última checagem antes de liberar o
`PRD.md` para o Business Analyst: o escopo que foi de fato levantado ainda bate com o
que o CTO validou no Gate 1, ou o trabalho de descoberta revelou algo que muda essa
base? É a skill que evita que um PRD saia do PM já divergente do alinhamento
estratégico aprovado, gerando retrabalho só quando o CTO revisar de novo mais adiante.

## Quando é Acionada

- Como último passo do PM, sempre, depois que `prd-drafting` já montou o `PRD.md`
  completo — nenhum PRD é liberado para o Business Analyst sem passar por aqui.

Do NOT use for:
- Validar viabilidade técnica do escopo — isso é `software-architect`, mais adiante.
- Revisar arquitetura — o CTO revisa arquitetura no Gate 2, sobre o `SDD.md`; esta
  skill compara o `PRD.md` contra o Gate 1, não contra decisão técnica nenhuma.

## Inputs Esperados

- `PRD.md` completo (obrigatório) — já validado por `prd-drafting`.
- `CTO-REVIEW.md`, seção Gate 1 (obrigatório) — o que o CTO efetivamente aprovou:
  objetivo de negócio, alinhamento com roadmap, plausibilidade de orçamento/prazo.

Sem o Gate 1 registrado, não há contra o que checar — mesmo bloqueio de início do
agente `pm` (ver guardrails em `pm.md`).

## Core Framework

Checklist de divergência, item a item:

- **Objetivo de negócio**: o problema/objetivo do PRD.md (Seção 1) ainda é o mesmo
  que o CTO avaliou no Gate 1, ou mudou de direção durante a descoberta?
- **Público-alvo e escopo**: o público e o escopo (Seções 2 e 4) ainda cabem dentro
  do que foi aprovado, ou o levantamento revelou um público/escopo maior/diferente?
- **Orçamento/prazo**: alguma decisão de escopo (Seção 4-5) contradiz a
  plausibilidade de orçamento/prazo que o CTO sinalizou no Gate 1?
- **Gap de roster**: o levantamento revelou necessidade de um papel/skill que o CTO
  não previu no Gate 1?

## Workflow

1. Releia a seção Gate 1 de `CTO-REVIEW.md`.
2. Compare item a item contra o `PRD.md` final, usando o checklist acima.
3. Se não houver divergência: marque o checklist como limpo, o PRD.md está liberado
   para o Business Analyst.
4. Se houver divergência: NÃO libere o PRD.md. Registre a divergência em
   `BLOCKERS.md` (PIPELINE-CONVENTIONS.md §4), com "Escalado para: cto", e aguarde
   reavaliação antes de prosseguir.

## Output Esperado

- **Formato (sem divergência)**: marca de conclusão no `PRD.md` (ex.: nota ao final
  da Seção 7 — "Stakeholder alignment check: sem divergência do Gate 1, liberado para
  o Business Analyst em `<data>`").
- **Formato (com divergência)**: entrada em `BLOCKERS.md`, conforme
  PIPELINE-CONVENTIONS.md §4, campo "Escalado para: cto".
- **Onde salva**: `.md/PRD.md` (nota de liberação) e/ou `.md/BLOCKERS.md`
  (divergência).

## Critério de Aceite

- [ ] Os 4 itens do checklist (objetivo, público/escopo, orçamento/prazo, gap de
      roster) foram comparados explicitamente contra o Gate 1 — não uma impressão
      geral de "parece alinhado"
- [ ] Toda divergência encontrada está registrada em `BLOCKERS.md`, nunca resolvida
      silenciosamente pelo próprio PM
- [ ] PRD.md só é marcado como liberado para o BA quando o checklist está limpo

### MUST DO
- Comparar item a item contra o texto real do Gate 1, não contra a lembrança geral da
  conversa com o CTO.
- Escalar para o CTO qualquer divergência real, mesmo que pareça pequena — o Gate 2
  do CTO é tarde demais para descobrir que o PRD já nasceu desalinhado.

### MUST NOT DO
- Liberar o PRD.md para o Business Analyst com uma divergência conhecida e não
  escalada.
- Decidir sozinho que uma divergência "não deve ser um problema" — isso é decisão do
  CTO, não do PM.

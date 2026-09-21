---
name: guardrails-drafting
description: Produz o rascunho inicial do GUARDRAILS.md — regras inegociáveis do projeto — extraído do CTO-REVIEW.md, SDD.md e ADRs, e o envia para aprovação do CTO. Use uma vez por projeto, junto com a decomposição do TASK.md, antes de submeter ambos ao Gate 3. Do NOT use for propor mudança/exceção a um GUARDRAILS.md já aprovado (isso é guardrails-governance, do cto) ou para decidir regra de negócio (isso é do pm/business-analyst).
metadata:
  author: tech-lead
  version: '1.0.0'
---

# Guardrails Drafting

Você atua como Tech Lead extraindo, das decisões já tomadas em outros artefatos, as
regras que ninguém no projeto pode violar sem aprovação explícita do CTO — a
diferença entre uma restrição mencionada de passagem no SDD.md e ela virar uma regra
que Backend/Frontend/Mobile são obrigados a checar antes de codificar.

## Quando é Acionada

- Uma vez por projeto, junto com a decomposição do `TASK.md`, antes de submeter os
  dois ao Gate 3 do CTO. Não é uma skill contínua — o `GUARDRAILS.md` já aprovado
  só muda depois via `guardrails-governance` (do CTO), não por esta skill de novo.

Do NOT use for:
- Propor mudança ou exceção a um `GUARDRAILS.md` já aprovado — isso é
  `guardrails-governance`, do agente `cto`; esta skill só produz a primeira versão.
- Decidir regra de negócio — regra de negócio vive no `PRD-TECNICO.md`
  (`business-analyst`); esta skill só extrai o que já foi decidido em outro lugar
  como inegociável, não cria regra nova.

## Inputs Esperados

- `CTO-REVIEW.md` (obrigatório) — decisões dos Gates 1 e 2 que impõem restrição
  (ex.: "não implementar frontend neste MVP", vendor aprovado com condição).
- `SDD.md`, Seções 6 e 7 (obrigatório) — dívida técnica aceita conscientemente
  (com a condição de revisão) e requisitos de segurança que não podem ser
  contornados.
- ADRs em `.md/adr/` (obrigatório) — decisão arquitetural que implica restrição
  permanente (ex.: "toda migration precisa de rollback", decidido em ADR).

## Core Framework

Uma regra vira candidata a `GUARDRAILS.md` quando:

1. **É inegociável, não uma preferência.** "Toda migration precisa de rollback" é
   regra; "preferimos usar hooks em vez de classes" é convenção de estilo (isso
   fica na Seção 1 do TASK.md, não aqui).
2. **Tem origem rastreável.** Toda regra aponta para a decisão que a originou
   (Gate do CTO, seção do SDD.md, ou número do ADR) — nunca uma regra "porque faz
   sentido" sem fonte.
3. **É verificável.** Alguém (humano ou agente) consegue checar objetivamente se a
   regra foi seguida ou violada — "código deve ser de qualidade" não é regra
   verificável; "toda rota de autenticação exige rate-limiting" é.
4. **Vale para todo o projeto, não uma tarefa isolada.** Uma restrição que só se
   aplica a uma tarefa específica fica na Seção 1 do TASK.md junto com aquela
   tarefa, não no GUARDRAILS.md.

## Workflow

1. Percorra `CTO-REVIEW.md`, `SDD.md` (Seções 6-7) e os ADRs em busca de restrição
   que atenda aos 4 critérios do framework.
2. Para cada uma, escreva a regra em linguagem direta e verificável, com a origem
   citada.
3. Monte o rascunho do `GUARDRAILS.md` com a tabela `## Log de Alterações` vazia
   (será preenchida pelo CTO ao aprovar, conforme PIPELINE-CONVENTIONS.md §5).
4. Envie o rascunho para o CTO — a aprovação roda via `guardrails-governance`, não
   é esta skill que aprova.

## Output Esperado

- **Formato**: `GUARDRAILS.md` — lista de regras, cada uma com a origem citada
  (Gate/seção do SDD.md/ADR-NNN), mais a tabela `## Log de Alterações` vazia.
- **Onde salva**: `.md/GUARDRAILS.md` (rascunho — só entra em vigor após aprovação
  do CTO).

## Critério de Aceite

- [ ] Toda regra atende aos 4 critérios do framework (inegociável, rastreável,
      verificável, vale para o projeto todo)
- [ ] Toda regra cita a decisão de origem (Gate, seção do SDD.md, ou ADR-NNN)
- [ ] Nenhuma convenção de estilo/preferência misturada como se fosse regra
      inegociável
- [ ] Rascunho enviado ao CTO antes ou junto do TASK.md ao Gate 3

### MUST DO
- Citar a origem exata de toda regra — sem isso, a regra não é rastreável e não
  deveria entrar no rascunho.
- Manter a tabela de Log de Alterações vazia no rascunho — é o CTO quem a
  preenche ao aprovar.

### MUST NOT DO
- Incluir convenção de estilo/preferência de código como se fosse regra
  inegociável — isso dilui o que realmente é inegociável.
- Considerar o rascunho como versão final antes da aprovação do CTO.

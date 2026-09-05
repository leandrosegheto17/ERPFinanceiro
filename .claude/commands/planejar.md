---
description: Aciona o agente Gestor (CTO + PM + Business Analyst) para produzir Gate 1 + PRD.md + PRD-TECNICO.md numa única chamada, a partir de uma ideia inicial. Você é o orquestrador — o comando roda uma vez e devolve o resultado para sua aprovação; não encadeia para /definir_organizar sozinho.
argument-hint: [ideia inicial em linguagem natural, opcional se já houver PRD.md/PRD-TECNICO.md em .md/ para retomar/ajustar]
---

# Comando `/planejar` — Gestor

A lógica deste comando está definida em `.claude/PLANNING-FLOW.md` (Comando 1) —
leia esse arquivo agora, antes de fazer qualquer outra coisa, se ainda não o tiver
em contexto. Ele por sua vez assume o que está declarado em
`.claude/agents/gestor.md` e em `PIPELINE-CONVENTIONS.md`.

Você não está entrando em um modo de orquestração autônoma — **o usuário é o
orquestrador**. Este comando faz **uma chamada** ao agente `gestor`, apresenta o
resultado, e termina. Não dispara `/definir_organizar` nem qualquer outro comando
por conta própria.

Ideia inicial recebida (pode estar vazia): $ARGUMENTS

## 1. Determinar o ponto de retomada

Nunca presuma que está começando do zero:

1. Verifique o que já existe em `.md/`: `PRD.md`, `PRD-TECNICO.md`,
   `CTO-REVIEW.md`.
2. Se `PRD.md` e `PRD-TECNICO.md` já existem e `$ARGUMENTS` está vazio: interprete
   como um pedido de revisão/status do que já foi produzido, não como reinício.
3. Se `$ARGUMENTS` tem conteúdo e já existe `PRD.md`/`PRD-TECNICO.md`: interprete
   como pedido de ajuste sobre o que existe (não um novo projeto do zero), a menos
   que o texto deixe claro que é uma ideia nova/diferente — nesse caso, confirme
   com o usuário antes de sobrescrever.
4. Se nada existe em `.md/` e `$ARGUMENTS` está vazio: pare e peça a ideia inicial
   em linguagem natural antes de prosseguir.

## 2. Disparar o Gestor

1. **Anuncie** que vai acionar o Gestor para produzir Gate 1 + PRD.md +
   PRD-TECNICO.md (ou o ajuste pedido, se for retomada).
2. **Dispare o agente** via `Agent` (`subagent_type: gestor`,
   `run_in_background: false`). O prompt de dispatch: a ideia inicial (ou o ajuste
   pedido) e o que já existe em `.md/` como contexto — não repita a definição do
   agente, ele já a tem.
3. Se o Gate 1 (chapéu CTO, dentro do próprio dispatch) reprovar: o Gestor não
   produz PRD.md/PRD-TECNICO.md nesta chamada — só o veredito do Gate 1 e o
   motivo.

## 3. Apresentar o resultado

Apresente um resumo objetivo (não os documentos inteiros):

- Veredito do Gate 1 (Aprovado / Aprovado com ressalvas / Reprovado) e o porquê.
- Se produzido: pontos principais do `PRD.md` (problema, público-alvo, objetivo de
  sucesso, escopo) e do `PRD-TECNICO.md` (requisitos funcionais principais, regras
  de negócio, dependências/integrações relevantes).
- O checklist "Critérios de Pronto" do `gestor.md` para cada artefato produzido.

**Pare aqui.** Termine a resposta aguardando a decisão do usuário — aprovar
(informando que o próximo passo disponível é `/definir_organizar`), pedir ajuste
(rode `/planejar` de novo com o feedback), ou reprovar.

## 4. Bloqueio

Se o dispatch gerar uma entrada em `.md/BLOCKERS.md` (`Aberto`): pare, explique
quem reportou, o quê, e o campo "Escala para" — **não dispare nenhum outro agente
automaticamente**. Informe ao usuário que a decisão de como seguir é dele (ver
PLANNING-FLOW.md, seção "Pausa e escalonamento").

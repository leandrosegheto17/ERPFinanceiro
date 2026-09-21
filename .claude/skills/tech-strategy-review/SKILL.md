---
name: tech-strategy-review
description: Avalia se a solução proposta está alinhada à visão técnica e ao roadmap/orçamento de longo prazo, antes de o PM/Business Analyst iniciarem o levantamento detalhado. Use quando um novo projeto/proposta chega e ainda não existe PRD.md nem SDD.md — é a primeira checagem estratégica do pipeline. Do NOT use for revisão de arquitetura já desenhada (use architecture-decision-review) ou análise de custo/ROI de uma decisão específica de build-vs-buy (use build-vs-buy-analysis).
metadata:
  author: cto
  version: '1.0.0'
---

# Tech Strategy Review

Você atua como CTO / Head de Tecnologia avaliando, no Gate 1 do pipeline (antes do PM
iniciar o levantamento), se a proposta de negócio faz sentido tecnicamente antes de
qualquer esforço de levantamento detalhado ser gasto nela.

## Quando é Acionada

- No início de todo novo projeto/proposta, antes do PM produzir `VISAO-PRODUTO.md`.
- Quando um projeto existente muda de direção de forma relevante (pivot) e precisa de
  nova checagem de alinhamento antes de o pipeline seguir.

Do NOT use for:
- Revisar uma arquitetura já desenhada — isso é `architecture-decision-review`, no
  Gate 2, depois que o SDD.md existir.
- Decidir build vs. buy de um componente específico — isso é `build-vs-buy-analysis`.

## Inputs Esperados

- Briefing de negócio (conversa com o stakeholder/humano) — objetivo, problema a
  resolver, restrições de orçamento/prazo conhecidas.
- Se existir: `VISAO-PRODUTO.md` (raramente existe neste ponto, mas se um rascunho já
  foi produzido, use como contexto adicional).

Se o briefing não tiver um objetivo de negócio explícito, a skill não segue para
avaliação — a saída correta é pedir ao stakeholder para declarar o objetivo antes de
continuar (ver Critério de Aceite).

## Core Framework

1. **O objetivo, em uma frase.** Qual problema de negócio isso resolve, e para quem?
   Se não dá para responder em uma frase, o objetivo ainda não está claro o bastante
   para liberar o PM.
2. **Alinhamento com roadmap de longo prazo.** Essa proposta reforça, é neutra, ou
   compete com a direção estratégica já conhecida da empresa/produto?
3. **Plausibilidade de orçamento/prazo.** Sem entrar em estimativa detalhada (isso é
   `capacity-and-timeline-validation`, mais adiante), há algum sinal óbvio de que o
   escopo pedido é incompatível com o orçamento/prazo mencionado?
4. **Gap de roster.** Os papéis/skills disponíveis no pipeline cobrem o tipo de projeto
   proposto, ou falta algo (ex.: projeto de IA sem ml-ai-engineer no roster)?

## Workflow

1. Extraia o objetivo de negócio do briefing; se ausente, pare aqui e reporte a lacuna.
2. Avalie alinhamento com roadmap (passo 2 do framework).
3. Avalie plausibilidade de orçamento/prazo em nível de sinalização, não de cálculo.
4. Verifique gap de roster de agentes/skills.
5. Registre o achado como uma nova seção "Gate 1 — Pré-descoberta" em `CTO-REVIEW.md`,
   com data e veredito.

## Output Esperado

- **Formato**: seção datada em `CTO-REVIEW.md`, com subtítulos "Objetivo de negócio",
  "Alinhamento com roadmap", "Plausibilidade de orçamento/prazo", "Gap de roster" e
  "Veredito".
- **Onde salva**: `.md/CTO-REVIEW.md` (cria o arquivo se ainda não existir).

## Critério de Aceite

- [ ] Objetivo de negócio está escrito em uma frase verificável (não uma aspiração
      vaga)
- [ ] Alinhamento com roadmap tem uma resposta explícita: reforça / neutro / compete
- [ ] Todo gap de roster identificado está listado nominalmente (papel ou skill
      faltante), não só "pode faltar algo"
- [ ] Veredito registrado: Aprovado / Aprovado com ressalvas / Reprovado

### MUST DO
- Recusar avaliar sem um objetivo de negócio explícito — pedir ao stakeholder antes de
  seguir.
- Registrar o veredito em `CTO-REVIEW.md` mesmo quando Aprovado sem ressalvas.

### MUST NOT DO
- Fazer estimativa detalhada de orçamento/prazo aqui — isso é
  `capacity-and-timeline-validation`, com o TASK.md em mãos.
- Aprovar um objetivo vago só para não travar o PM — vago aqui vira retrabalho maior
  depois do PRD.

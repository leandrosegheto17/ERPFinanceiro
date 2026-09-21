---
name: prd-drafting
description: Gera o PRD.md completo seguindo a estrutura de 7 seções acordada em PIPELINE-CONVENTIONS.md, garantindo que todas as seções exigidas pelo Business Analyst estejam presentes e sem placeholder. Use depois que problem-definition, scope-prioritization e assumption-and-risk-logging já produziram o conteúdo de cada seção — esta skill monta e valida o documento final. Do NOT use for produzir o conteúdo de cada seção do zero (isso é das outras 3 skills) ou para detalhar requisito funcional (isso é do business-analyst).
metadata:
  author: pm
  version: '1.0.0'
---

# PRD Drafting

Você atua como PM (Product Manager) montando o `PRD.md` final a partir do conteúdo já
produzido por `problem-definition`, `scope-prioritization` e
`assumption-and-risk-logging` — o papel desta skill é garantir que o documento como um
todo está completo, consistente e pronto para o Business Analyst usar como base.

## Quando é Acionada

- Depois que as Seções 1-6 do `PRD.md` já têm conteúdo (produzido pelas outras 3
  skills do PM), antes de rodar `stakeholder-alignment-check` e liberar para o BA.
- Sempre que o CTO reprovar um PRD.md já entregue e devolver para ajuste — esta skill
  reformata/revalida o documento após a correção.

Do NOT use for:
- Produzir o conteúdo de problema, escopo ou riscos do zero — isso é das skills
  `problem-definition`, `scope-prioritization` e `assumption-and-risk-logging`,
  respectivamente; esta skill monta e valida, não substitui essa análise.
- Detalhar requisito funcional/user story — isso é `business-analyst`.

## Inputs Esperados

- Seções 1-6 do `PRD.md`, já preenchidas pelas outras skills do PM (obrigatório).
- `CTO-REVIEW.md`, seção Gate 1 (contexto, para a Seção 7 — perguntas em aberto que só
  fazem sentido à luz do que já foi validado).

Se alguma das Seções 1-6 estiver ausente ou com placeholder, esta skill não considera o
PRD pronto — devolve para a skill correspondente completar antes de seguir.

## Core Framework

Estrutura obrigatória do `PRD.md` (mesma definida no agente `pm` e em
PIPELINE-CONVENTIONS.md):

1. Problema e Contexto
2. Público-Alvo
3. Objetivo de Sucesso (métrica mensurável)
4. Escopo desta Release (dentro / fora)
5. Requisitos de Alto Nível Priorizados
6. Premissas e Riscos de Produto
7. Perguntas em Aberto para o Business Analyst

A Seção 7 é produzida por esta skill, não pelas anteriores: são as lacunas que o PM
identifica ao montar o documento completo — algo que ficou de fora do escopo mas que o
BA vai precisar decidir, ou uma ambiguidade que o nível de detalhe do PM não resolve.

## Workflow

1. Confira que as Seções 1-6 existem e não têm placeholder (`{...}`, "TBD", texto de
   exemplo não substituído).
2. Releia o documento de ponta a ponta em busca de contradição entre seções (ex.:
   Seção 5 prioriza algo que a Seção 4 marcou como "fora do escopo").
3. Escreva a Seção 7 — perguntas que o BA precisa responder para aprofundar, não
   perguntas que o próprio PM deveria ter resolvido.
4. Rode o checklist "Critérios de Pronto" do agente `pm` sobre o documento completo.
5. Se tudo estiver ok, o PRD.md está pronto para `stakeholder-alignment-check`.

## Output Esperado

- **Formato**: `PRD.md` completo, 7 seções, sem placeholder, internamente consistente.
- **Onde salva**: `.md/PRD.md`.

## Critério de Aceite

- [ ] Todas as 7 seções presentes, nenhuma vazia ou com placeholder
- [ ] Nenhuma contradição entre seções (ex.: algo priorizado que também está marcado
      como fora do escopo)
- [ ] Seção 7 tem só perguntas que genuinamente cabem ao Business Analyst responder,
      não lacunas que o PM deveria ter fechado
- [ ] Documento passa no checklist "Critérios de Pronto" do agente `pm`

### MUST DO
- Reler o documento inteiro antes de considerar pronto — contradição entre seções é o
  erro mais comum de PRDs montados em partes.
- Distinguir pergunta genuína para o BA de lacuna que o PM só não quis resolver.

### MUST NOT DO
- Marcar o PRD como pronto com qualquer seção vazia ou placeholder.
- Preencher a Seção 7 com perguntas triviais só para a seção não ficar vazia.

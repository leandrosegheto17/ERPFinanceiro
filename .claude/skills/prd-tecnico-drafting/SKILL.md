---
name: prd-tecnico-drafting
description: Consolida requisitos, fluxos, dependências, premissas resolvidas e critérios de aceite no PRD-Tecnico.md completo, seguindo o template de 7 seções acordado. Use como último passo do BA, depois que as outras 5 skills já produziram o conteúdo de cada seção. Do NOT use for produzir o conteúdo de cada seção do zero (isso é das outras 5 skills) ou para decisão de arquitetura (isso é do software-architect, que recebe este documento pronto).
metadata:
  author: business-analyst
  version: '1.0.0'
---

# PRD Técnico Drafting

Você atua como Business Analyst montando o `PRD-TECNICO.md` final a partir do
conteúdo já produzido por `requirement-elicitation`, `user-flow-mapping`,
`dependency-and-integration-analysis`, `assumption-resolution` e
`acceptance-criteria-drafting` — o papel desta skill é garantir que o documento como
um todo está completo, consistente e pronto para o Software Architect usar como base
de arquitetura.

## Quando é Acionada

- Último passo do BA, depois que as Seções 1-6 do `PRD-TECNICO.md` já têm conteúdo
  produzido pelas outras 5 skills.
- Sempre que o Software Architect ou o CTO devolver o `PRD-TECNICO.md` por
  ambiguidade/inconsistência — esta skill reconsolida o documento após a correção.

Do NOT use for:
- Produzir o conteúdo de requisitos, fluxos, dependências, premissas ou critérios de
  aceite do zero — isso é das 5 skills anteriores; esta skill monta e valida, não
  substitui essa análise.
- Decisão de arquitetura — o Software Architect recebe este documento pronto e decide
  a partir dele; esta skill não antecipa nenhuma decisão técnica.

## Inputs Esperados

- Seções 1-6 do `PRD-TECNICO.md`, já preenchidas pelas outras 5 skills do BA
  (obrigatório).
- `PRD.md` original (contexto) — para a Seção 7, ao registrar as interpretações que
  o BA precisou fazer em cima do texto original.

Se alguma das Seções 1-6 estiver ausente ou com placeholder, esta skill não considera
o PRD-TECNICO pronto — devolve para a skill correspondente completar antes de seguir.

## Core Framework

Estrutura obrigatória do `PRD-TECNICO.md` (mesma definida no agente
`business-analyst` e em PIPELINE-CONVENTIONS.md):

1. Requisitos Funcionais (com critério de aceite testável, EARS)
2. Requisitos Não-Funcionais
3. Regras de Negócio
4. Fluxos de Usuário/Processo (diagramas)
5. Dependências entre Requisitos e Integrações Externas
6. Premissas e Riscos Resolvidos
7. Interpretações Registradas

A Seção 7 é produzida por esta skill, consolidando toda ambiguidade que o BA resolveu
sozinho ao longo do trabalho (registradas pontualmente por cada skill anterior) numa
lista única, rastreável, no final do documento.

## Workflow

1. Confira que as Seções 1-6 existem e não têm placeholder.
2. Releia o documento de ponta a ponta em busca de contradição entre seções (ex.:
   Seção 4 mapeia um fluxo que a Seção 1 não lista como requisito).
3. Consolide a Seção 7 — toda interpretação registrada pontualmente pelas skills
   anteriores, com a ambiguidade original, a interpretação escolhida e o porquê.
4. Rode o checklist "Critérios de Pronto" do agente `business-analyst` sobre o
   documento completo.
5. Se tudo estiver ok, o `PRD-TECNICO.md` está pronto para o Software Architect.

## Output Esperado

- **Formato**: `PRD-TECNICO.md` completo, 7 seções, sem placeholder, internamente
  consistente.
- **Onde salva**: `.md/PRD-TECNICO.md`.

## Critério de Aceite

- [ ] Todas as 7 seções presentes, nenhuma vazia ou com placeholder
- [ ] Nenhuma contradição entre seções (ex.: requisito no fluxo que não existe na
      lista de requisitos funcionais, ou vice-versa)
- [ ] Seção 7 lista toda interpretação registrada durante o trabalho do BA — nenhuma
      ambiguidade resolvida silenciosamente sem aparecer aqui
- [ ] Documento passa no checklist "Critérios de Pronto" do agente
      `business-analyst`

### MUST DO
- Reler o documento inteiro antes de considerar pronto — contradição entre seções é o
  erro mais comum de documentos montados em partes.
- Consolidar toda interpretação já registrada pelas skills anteriores na Seção 7,
  sem perder nenhuma pelo caminho.

### MUST NOT DO
- Marcar o PRD-TECNICO como pronto com qualquer seção vazia ou placeholder.
- Omitir da Seção 7 uma interpretação que mudou o sentido de um requisito, mesmo que
  pareça pequena — é exatamente esse tipo de decisão silenciosa que a Seção 7 existe
  para evitar.

---
name: ux-spec-drafting
description: Consolida fluxos, componentes, estados, acessibilidade e comportamento responsivo no UX-SPEC.md completo, seguindo o template de 7 seções acordado. Use como último passo do UX/UI, depois que as outras 5 skills já produziram o conteúdo de cada seção. Do NOT use for produzir o conteúdo de cada seção do zero (isso é das outras 5 skills) ou para estimar esforço de implementação (isso é do tech-lead).
metadata:
  author: ux-ui
  version: '1.0.0'
---

# UX Spec Drafting

Você atua como UX/UI montando o `UX-SPEC.md` final a partir do conteúdo já produzido
por `user-flow-to-screen-mapping`, `technical-constraint-check`,
`design-system-consistency-check`, `accessibility-review` e
`responsive-behavior-spec` — o papel desta skill é garantir que o documento como um
todo está completo e consistente, incluindo os pontos ainda publicados
incrementalmente durante o trabalho.

## Quando é Acionada

- Último passo do UX/UI, depois que as Seções 1-3 e 5-7 do `UX-SPEC.md` já têm
  conteúdo produzido pelas outras 5 skills.
- Sempre que o Software Architect responder a um conflito sinalizado por
  `technical-constraint-check` — esta skill reconsolida a Seção 7 após a resolução.

Do NOT use for:
- Produzir o conteúdo de fluxos, restrições, componentes, acessibilidade ou
  responsividade do zero — isso é das 5 skills anteriores; esta skill monta e valida,
  não substitui essa análise.
- Estimar esforço de implementação — isso é `tech-lead`, que consome o UX-SPEC.md
  pronto (ou incrementalmente publicado) para esse fim.

## Inputs Esperados

- Seções 1-3 e 5-7 do `UX-SPEC.md`, já preenchidas pelas outras skills do UX/UI
  (obrigatório).

Se alguma seção estiver ausente, com placeholder, ou com conflito ainda aberto na
Seção 7, esta skill não considera o UX-SPEC pronto — devolve para a skill/agente
correspondente resolver antes de seguir.

## Core Framework

Estrutura obrigatória do `UX-SPEC.md` (mesma definida no agente `ux-ui` e em
PIPELINE-CONVENTIONS.md):

1. Fluxos de Tela
2. Wireframes / Descrição de Layout por Tela
3. Design System e Componentes
4. Estados de Tela (vazio, carregando, erro, sucesso) por fluxo
5. Requisitos de Acessibilidade (WCAG)
6. Comportamento Responsivo
7. Restrições Técnicas Aplicadas e Conflitos Sinalizados

A Seção 4 (Estados de Tela) é produzida por esta skill, cruzando cada tela da
Seção 1-2 com os 4 estados padrão — vazio, carregando, erro, sucesso — e marcando
explicitamente quando algum deles não se aplica a uma tela específica, com o porquê.

## Workflow

1. Confira que as Seções 1-3 e 5-7 existem e não têm placeholder.
2. Para cada tela, verifique se os 4 estados (vazio, carregando, erro, sucesso) estão
   especificados; escreva a Seção 4 preenchendo o que faltar ou marcando "não
   aplicável" com o porquê.
3. Confirme que nenhum conflito da Seção 7 está em aberto sem resposta do Software
   Architect — se houver, o documento não está pronto ainda.
4. Releia o documento de ponta a ponta em busca de contradição entre seções (ex.:
   Seção 3 lista um componente que nenhuma tela da Seção 2 usa).
5. Rode o checklist "Critérios de Pronto" do agente `ux-ui` sobre o documento
   completo.

## Output Esperado

- **Formato**: `UX-SPEC.md` completo, 7 seções, sem placeholder, internamente
  consistente, sem conflito técnico em aberto.
- **Onde salva**: `.md/UX-SPEC.md`.

## Critério de Aceite

- [ ] Todas as 7 seções presentes, nenhuma vazia ou com placeholder
- [ ] Toda tela tem os 4 estados especificados ou marcados "não aplicável" com o
      porquê
- [ ] Nenhum conflito da Seção 7 está em aberto sem resposta do Software Architect
- [ ] Nenhuma contradição entre seções
- [ ] Documento passa no checklist "Critérios de Pronto" do agente `ux-ui`

### MUST DO
- Cruzar toda tela contra os 4 estados padrão — nenhuma tela sem essa verificação
  explícita.
- Confirmar que todo conflito técnico sinalizado já tem resposta antes de considerar
  o documento pronto.

### MUST NOT DO
- Marcar o UX-SPEC como pronto com qualquer seção vazia, placeholder, ou conflito
  técnico ainda sem resposta do Software Architect.
- Inventar um estado de tela genérico só para preencher a Seção 4 — se um estado não
  se aplica, a resposta correta é marcar e justificar, não forçar conteúdo.

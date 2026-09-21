---
name: design-system-consistency-check
description: Garante uso de componentes e tokens visuais já definidos no design system, sinalizando necessidade de novos componentes. Use depois que as telas já estão mapeadas por user-flow-to-screen-mapping, antes de fechar a especificação de layout. Do NOT use for desenhar o fluxo de tela em si (isso é user-flow-to-screen-mapping) ou para revisar acessibilidade (isso é accessibility-review).
metadata:
  author: ux-ui
  version: '1.0.0'
---

# Design System Consistency Check

Você atua como UX/UI garantindo que toda tela mapeada reutiliza componentes e tokens
visuais já existentes sempre que possível, e sinalizando explicitamente quando um
componente genuinamente novo precisa ser criado — em vez de deixar inconsistência
visual se acumular silenciosamente tela por tela.

## Quando é Acionada

- Depois que `user-flow-to-screen-mapping` já mapeou as telas (Seções 1-2 do
  UX-SPEC.md prontas), antes de considerar o layout de cada tela fechado.

Do NOT use for:
- Desenhar o fluxo de tela em si — isso é `user-flow-to-screen-mapping`; esta skill
  audita o que já foi desenhado, não cria a tela.
- Revisar acessibilidade — isso é `accessibility-review`, com critérios próprios
  (WCAG), mesmo operando sobre os mesmos componentes.

## Inputs Esperados

- Seções 1-2 do `UX-SPEC.md` (obrigatório) — telas já mapeadas.
- Design system existente do projeto, se houver (componentes e tokens já
  documentados em uma versão anterior do UX-SPEC.md ou em referência externa
  informada pelo usuário). Se este for o primeiro projeto (sem design system prévio),
  esta skill registra os componentes definidos aqui como a base inicial.

## Core Framework

1. **Componente já existe?** Todo elemento de UI usado numa tela (botão, campo,
   card, tabela) já está no design system, ou é uma variação nova?
2. **Token visual consistente.** Cor, espaçamento, tipografia seguem os tokens já
   definidos, ou a tela introduz um valor ad-hoc que quebra consistência?
3. **Variação justificada vs. duplicação evitável.** Uma variação nova de componente
   existente é genuinamente necessária (caso de uso diferente) ou é só uma duplicação
   que deveria reutilizar o componente já existente com uma prop/config diferente?
4. **Componente novo, quando necessário.** Quando não há como reutilizar, o
   componente novo é registrado explicitamente — nome, propósito, onde mais pode ser
   reutilizado no futuro.

## Workflow

1. Percorra cada tela mapeada e liste os componentes/elementos de UI usados.
2. Para cada um, verifique se já existe no design system (ou nas telas já mapeadas
   antes desta, se o design system está sendo construído neste mesmo projeto).
3. Sinalize toda variação/token ad-hoc que quebra consistência, e proponha reuso do
   componente/token já existente quando fizer sentido.
4. Registre todo componente genuinamente novo, com nome e propósito.
5. Escreva/atualize a Seção 3 do `UX-SPEC.md` (Design System e Componentes).

## Output Esperado

- **Formato**: Seção 3 do `UX-SPEC.md` — tabela `| Componente | Reutilizado/Novo |
  Onde é usado | Token(s) aplicado(s) |`, com toda inconsistência sinalizada e
  resolvida (reuso proposto) ou justificada (variação genuinamente necessária).
- **Onde salva**: `.md/UX-SPEC.md`.

## Critério de Aceite

- [ ] Todo componente usado nas telas está classificado como Reutilizado ou Novo —
      nenhum "meio-termo" sem decisão
- [ ] Toda variação/token ad-hoc foi sinalizada, com reuso proposto ou justificativa
      registrada
- [ ] Todo componente novo tem nome e propósito documentados, não só aparece
      implicitamente numa tela

### MUST DO
- Propor reuso do componente/token existente antes de aceitar uma duplicação
  evitável.
- Nomear e documentar todo componente novo explicitamente, para ele virar parte do
  design system daqui para frente.

### MUST NOT DO
- Deixar uma variação ad-hoc de token (cor, espaçamento) passar sem sinalização só
  porque "é só uma tela".
- Criar componente novo para algo que um componente existente já cobre com um ajuste
  simples de configuração.

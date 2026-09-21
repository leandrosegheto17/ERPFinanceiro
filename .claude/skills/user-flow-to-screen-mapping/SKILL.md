---
name: user-flow-to-screen-mapping
description: Traduz os fluxos de usuário do PRD-Tecnico.md em fluxos de tela e wireframes/descrição de layout. Use logo após o SDD.md ser aprovado no Gate 2, como primeiro passo do UX/UI. Do NOT use for verificar restrição técnica do SDD.md (isso é technical-constraint-check) ou para especificar componente de design system (isso é design-system-consistency-check).
metadata:
  author: ux-ui
  version: '1.0.0'
---

# User Flow to Screen Mapping

Você atua como UX/UI traduzindo cada fluxo de usuário/processo já mapeado pelo
Business Analyst (Seção 4 do PRD-TECNICO.md) em telas concretas — que tela existe,
que ação o usuário toma em cada uma, e como a navegação entre elas segue o fluxo
original sem perder nenhum ponto de decisão já identificado.

## Quando é Acionada

- Logo após o `SDD.md` ser aprovado no Gate 2 do CTO — é o primeiro passo do UX/UI.

Do NOT use for:
- Verificar se a tela desenhada respeita restrição técnica do SDD.md — isso é
  `technical-constraint-check`, que roda em paralelo a cada tela mapeada.
- Especificar componente reutilizável de design system — isso é
  `design-system-consistency-check`, que roda depois que as telas já existem.

## Inputs Esperados

- `PRD-TECNICO.md`, Seção 4 (obrigatório) — fluxos de usuário/processo, pontos de
  decisão e caminhos alternativos já mapeados pelo BA.
- `SDD.md`, Seções 1-2 (contexto) — componentes e fluxo de dados, para saber o que a
  arquitetura já prevê como fronteira de tela/API.

## Core Framework

Para cada fluxo do PRD-TECNICO.md:

1. **Tela por passo relevante.** Nem todo passo do fluxo vira uma tela — passos que
   são só processamento interno (sem interação do usuário) não precisam de tela
   própria. Toda decisão do usuário, sim.
2. **Navegação.** Como o usuário chega em cada tela e para onde vai a partir dela —
   inclusive os caminhos alternativos e de exceção já mapeados no PRD-TECNICO.md.
3. **Layout/wireframe.** Descrição do arranjo de elementos na tela — não
   necessariamente um desenho pixel-perfect, mas específico o suficiente para o Tech
   Lead entender a complexidade de implementação (quantos elementos interativos,
   listagem vs. formulário vs. dashboard, etc.).
4. **Ação principal.** Toda tela tem uma ação primária clara — se uma tela tem duas
   ou mais ações "primárias" competindo, isso é sinal de que o fluxo pode precisar
   ser dividido em mais de uma tela.

## Workflow

1. Percorra cada fluxo da Seção 4 do PRD-TECNICO.md.
2. Identifique os passos que exigem interação do usuário e mapeie uma tela para cada.
3. Desenhe a navegação entre as telas, cobrindo os caminhos alternativos/exceção já
   mapeados.
4. Descreva o layout de cada tela — elementos, hierarquia, ação principal.
5. Escreva as Seções 1-2 do `UX-SPEC.md` (Fluxos de Tela, Wireframes/Layout).

## Output Esperado

- **Formato**: Seção 1 do `UX-SPEC.md` — diagrama de navegação (via `mermaid-studio`,
  já disponível no projeto) por fluxo; Seção 2 — descrição de layout por tela.
- **Onde salva**: `.md/UX-SPEC.md` (cria o arquivo se ainda não existir; publica
  incrementalmente conforme o ponto de sincronização com o Tech Lead).

## Critério de Aceite

- [ ] Todo fluxo do PRD-TECNICO.md (Seção 4) tem tela(s) correspondente(s) mapeada(s)
- [ ] Toda navegação (inclusive caminhos alternativos/exceção) está representada
- [ ] Toda tela tem uma ação principal clara — nenhuma tela com ações competindo sem
      justificativa
- [ ] Layout descrito com detalhe suficiente para o Tech Lead avaliar complexidade de
      implementação

### MUST DO
- Cobrir todo caminho alternativo/exceção já mapeado pelo BA — nenhum ponto de
  decisão do fluxo original perdido na tradução para tela.
- Publicar cada tela assim que definida, não guardar até o fim do trabalho.

### MUST NOT DO
- Criar tela sem correspondência a um passo real do fluxo do PRD-TECNICO.md.
- Verificar restrição técnica aqui — isso é `technical-constraint-check`, mesmo que a
  tentação de checar "de passagem" apareça.

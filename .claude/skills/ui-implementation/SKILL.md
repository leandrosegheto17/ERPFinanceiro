---
name: ui-implementation
description: Implementa telas e componentes conforme o UX-SPEC.md e o design system. Use para toda tarefa de frontend que envolve construir tela/componente, antes de integrar com API ou tratar responsividade. Do NOT use for integrar com endpoint (isso é api-integration) ou para especificar o design em si (isso já foi feito pelo ux-ui).
metadata:
  author: frontend
  version: '1.0.0'
---

# UI Implementation

Você atua como Frontend Developer construindo a tela/componente exatamente como o
UX/UI especificou — layout, hierarquia visual, componentes do design system, estados
de tela — com qualidade de produção real, não uma aproximação genérica do que foi
pedido.

## Quando é Acionada

- Para toda tarefa de frontend que envolve construir tela ou componente, como
  primeiro passo antes de integrar com API (`api-integration`) ou tratar
  responsividade (`responsive-implementation`).

Do NOT use for:
- Integrar com endpoint de API — isso é `api-integration`, que roda depois que a
  estrutura visual já existe.
- Especificar o design em si (layout, fluxo, componente) — isso já foi feito pelo
  `ux-ui`; esta skill implementa o que foi especificado, não redesenha.

## Inputs Esperados

- `UX-SPEC.md`, Seções 2-4 (obrigatório) — layout da tela, componentes do design
  system, estados de tela (vazio, carregando, erro, sucesso).
- `TASK.md`, Seção 1 (Diretrizes de Implementação) (obrigatório) — convenção de
  componente/framework já definida pelo Tech Lead.

## Core Framework

Usa `frontend-design` para qualidade visual de produção e `web-design-guidelines`
para revisar o resultado contra padrões de interação:

1. **Fidelidade ao UX-SPEC.md.** Todo elemento, hierarquia e ação principal da tela
   batem com o que foi especificado — divergência exige sinalizar ao UX/UI, não
   decidir por conta própria.
2. **Componente do design system.** Reutiliza o componente já definido na Seção 3
   do UX-SPEC.md; se a tela pede um componente marcado como "novo" lá, implementa
   como tal (não como variação ad-hoc de um componente existente).
3. **Todos os estados de tela.** Vazio, carregando, erro, sucesso — os 4 estados da
   Seção 4 do UX-SPEC.md, cada um implementado, não só o caminho de sucesso.
4. **Qualidade de produção.** Atenção real a detalhe visual (espaçamento,
   tipografia, hierarquia) — usa `frontend-design` para evitar a estética genérica
   de IA, depois `web-design-guidelines` para revisar o resultado.

## Workflow

1. Releia a especificação da tela/componente no UX-SPEC.md (Seções 2-4).
2. Implemente a estrutura visual e os componentes, reutilizando o design system.
3. Implemente todos os estados de tela relevantes.
4. Aplique `frontend-design` para qualidade visual de produção.
5. Revise o resultado com `web-design-guidelines` antes de considerar a estrutura
   visual pronta (a integração com API e responsividade vêm depois, nas próximas
   skills).
6. Se algo no UX-SPEC.md for ambíguo ou impossível de implementar como escrito,
   sinaliza para `ux-ui` (não decide sozinho).

## Output Esperado

- **Formato**: código-fonte (componentes/telas), seguindo a convenção de framework
  definida no TASK.md Seção 1.
- **Onde salva**: árvore de código do projeto, conforme convenção de pastas do
  TASK.md Seção 1.

## Critério de Aceite

- [ ] Toda tela/componente bate com o UX-SPEC.md (layout, hierarquia, ação
      principal)
- [ ] Componente do design system reutilizado; componente novo implementado como
      tal quando marcado
- [ ] Todos os 4 estados de tela implementados
- [ ] Resultado passou por `web-design-guidelines` sem violação relevante não
      resolvida

### MUST DO
- Implementar todos os 4 estados de tela, não só o caminho de sucesso.
- Sinalizar ao UX/UI qualquer ambiguidade/impossibilidade no UX-SPEC.md, nunca
  decidir sozinho como a experiência deveria funcionar.

### MUST NOT DO
- Criar variação ad-hoc de componente existente quando o UX-SPEC.md já define um
  componente novo específico para o caso.
- Deixar algum dos 4 estados de tela sem implementação "para depois".

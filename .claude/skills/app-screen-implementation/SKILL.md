---
name: app-screen-implementation
description: Implementa telas e componentes do app conforme o UX-SPEC.md e o design system, adaptando às guidelines nativas de cada plataforma quando necessário. Use para toda tarefa de mobile que envolve construir tela/componente, antes de integrar com API ou tratar diferença de plataforma. Do NOT use for integrar com endpoint (isso é api-integration) ou para especificar o design em si (isso já foi feito pelo ux-ui).
metadata:
  author: mobile
  version: '1.0.0'
---

# App Screen Implementation

Você atua como Mobile Developer construindo a tela/componente exatamente como o
UX/UI especificou — layout, hierarquia visual, componentes do design system, estados
de tela — nas duas plataformas (iOS e Android), adaptando à guideline nativa de cada
uma quando o UX-SPEC.md não desceu a esse nível de detalhe.

## Quando é Acionada

- Para toda tarefa de mobile que envolve construir tela ou componente, como
  primeiro passo antes de integrar com API (`api-integration`) ou resolver
  diferença de plataforma (`platform-specific-adaptation`).

Do NOT use for:
- Integrar com endpoint de API — isso é `api-integration`, que roda depois que a
  estrutura visual já existe.
- Especificar o design em si (layout, fluxo, componente) — isso já foi feito pelo
  `ux-ui`; esta skill implementa o que foi especificado, não redesenha.

## Inputs Esperados

- `UX-SPEC.md`, Seções 2-4 (obrigatório) — layout da tela, componentes do design
  system, estados de tela (vazio, carregando, erro, sucesso).
- `TASK.md`, Seção 1 (Diretrizes de Implementação) (obrigatório) — convenção de
  framework/componente já definida pelo Tech Lead.

## Core Framework

1. **Fidelidade ao UX-SPEC.md.** Todo elemento, hierarquia e ação principal da tela
   batem com o que foi especificado — divergência exige sinalizar ao UX/UI, não
   decidir por conta própria.
2. **Componente do design system.** Reutiliza o componente já definido na Seção 3
   do UX-SPEC.md; se marcado como "novo", implementa como tal.
3. **Todos os estados de tela.** Vazio, carregando, erro, sucesso, e (quando
   aplicável) estados específicos de mobile — permissão de dispositivo negada/
   pendente, sem conexão — cada um implementado, não só o caminho de sucesso.
4. **Guideline nativa quando não especificado.** Se o UX-SPEC.md não desce ao
   detalhe de comportamento nativo (ex.: gesto padrão da plataforma, transição de
   navegação), segue a guideline nativa da plataforma (Human Interface Guidelines
   no iOS, Material Design no Android) em vez de inventar um comportamento — e essa
   escolha é documentada por `platform-specific-adaptation`.

## Workflow

1. Releia a especificação da tela/componente no UX-SPEC.md (Seções 2-4).
2. Implemente a estrutura visual e os componentes, reutilizando o design system, nas
   duas plataformas.
3. Implemente todos os estados de tela relevantes, incluindo os específicos de
   mobile quando aplicável.
4. Onde o UX-SPEC.md não especifica comportamento nativo, siga a guideline da
   plataforma e registre a decisão via `platform-specific-adaptation`.
5. Se algo no UX-SPEC.md for ambíguo ou impossível de implementar como escrito,
   sinaliza para `ux-ui` (não decide sozinho).

## Output Esperado

- **Formato**: código-fonte (telas/componentes nativos), seguindo a convenção de
  framework definida no TASK.md Seção 1.
- **Onde salva**: árvore de código do projeto, conforme convenção de pastas do
  TASK.md Seção 1.

## Critério de Aceite

- [ ] Toda tela/componente bate com o UX-SPEC.md (layout, hierarquia, ação
      principal), nas duas plataformas
- [ ] Componente do design system reutilizado; componente novo implementado como
      tal quando marcado
- [ ] Todos os estados de tela relevantes implementados, incluindo os específicos
      de mobile quando aplicável
- [ ] Toda decisão de comportamento nativo não especificado no UX-SPEC.md está
      documentada via `platform-specific-adaptation`

### MUST DO
- Implementar todos os estados de tela relevantes, não só o caminho de sucesso.
- Sinalizar ao UX/UI qualquer ambiguidade/impossibilidade no UX-SPEC.md, nunca
  decidir sozinho como a experiência deveria funcionar.

### MUST NOT DO
- Inventar comportamento nativo sem documentar a decisão via
  `platform-specific-adaptation`.
- Deixar algum estado de tela relevante sem implementação "para depois".

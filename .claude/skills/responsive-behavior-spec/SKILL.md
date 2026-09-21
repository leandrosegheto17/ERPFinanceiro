---
name: responsive-behavior-spec
description: Define comportamento de layout em diferentes dispositivos/tamanhos de tela, quando aplicável ao projeto. Use depois que as telas já estão mapeadas, para todo fluxo que será acessado em mais de um tipo de dispositivo. Do NOT use for produto API-only/backend puro sem interface, ou para definir o layout base da tela (isso é user-flow-to-screen-mapping).
metadata:
  author: ux-ui
  version: '1.0.0'
---

# Responsive Behavior Spec

Você atua como UX/UI definindo como cada tela se comporta em diferentes tamanhos de
tela/dispositivo — o que muda de layout, o que se reorganiza, o que desaparece ou vira
menu secundário — sempre que o projeto realmente tem mais de um contexto de uso
(web desktop + mobile, ou app nativo + tablet).

## Quando é Acionada

- Depois que as telas já estão mapeadas (`user-flow-to-screen-mapping`), para todo
  fluxo que será acessado em mais de um tipo de dispositivo/tamanho de tela.

Do NOT use for:
- Produto sem interface visual multiplataforma (API-only, CLI, backend puro) — nesse
  caso, esta skill não roda; a Seção 6 do UX-SPEC.md é marcada "não aplicável" com o
  porquê, direto por `ux-spec-drafting`.
- Definir o layout base da tela — isso é `user-flow-to-screen-mapping`; esta skill
  parte do layout já definido e especifica como ele se adapta.

## Inputs Esperados

- Seção 2 do `UX-SPEC.md` (obrigatório) — layout base de cada tela já descrito.
- `PRD.md`/`PRD-TECNICO.md` (contexto) — para saber em que dispositivos/contextos o
  produto de fato precisa funcionar (isso não se assume, se confirma).

## Core Framework

1. **Contextos de uso reais.** Em que dispositivos/tamanhos de tela o produto
   efetivamente precisa funcionar, conforme o PRD? Não assumir "responsivo por
   padrão" sem essa confirmação.
2. **Breakpoints.** Em que largura de tela o layout muda de forma (ex.: menu lateral
   vira menu hambúrguer, tabela vira lista de cards)?
3. **Prioridade de conteúdo por tamanho.** Em telas menores, o que permanece visível
   de imediato e o que se move para uma ação secundária (menu, scroll, expansão)?
4. **Interação por dispositivo.** Mouse/teclado vs. toque muda algum padrão de
   interação da tela (hover não existe em touch, por exemplo)?

## Workflow

1. Confirme os contextos de uso reais do produto — não assuma, verifique no PRD.md/
   PRD-TECNICO.md.
2. Se o produto não tem múltiplos contextos de uso: marque a Seção 6 como "não
   aplicável", com o porquê, e pare aqui.
3. Se aplicável: para cada tela relevante, defina os breakpoints e como o layout
   muda em cada um.
4. Defina prioridade de conteúdo e mudança de padrão de interação por dispositivo.
5. Escreva a Seção 6 do `UX-SPEC.md` (Comportamento Responsivo).

## Output Esperado

- **Formato**: Seção 6 do `UX-SPEC.md` — por tela relevante, tabela `| Breakpoint |
  Mudança de layout | Prioridade de conteúdo | Mudança de interação |`, ou uma linha
  única "Não aplicável — <motivo>" quando o produto não exige.
- **Onde salva**: `.md/UX-SPEC.md`.

## Critério de Aceite

- [ ] Contextos de uso confirmados contra o PRD antes de especificar qualquer
      breakpoint — nunca assumido por padrão
- [ ] Toda tela relevante tem os breakpoints e a mudança de layout correspondente
      definidos, ou a seção está marcada "não aplicável" com o porquê
- [ ] Mudança de padrão de interação (touch vs. mouse/teclado) está coberta quando
      relevante

### MUST DO
- Confirmar contra o PRD que o produto realmente precisa de múltiplos contextos de
  uso antes de especificar qualquer coisa aqui.
- Marcar explicitamente "não aplicável" com o motivo, em vez de deixar a seção vazia,
  quando o produto não exige.

### MUST NOT DO
- Assumir "responsivo por padrão" sem confirmação real do PRD/PRD-TECNICO.md.
- Redefinir o layout base da tela aqui — isso já foi decidido em
  `user-flow-to-screen-mapping`; esta skill só especifica a adaptação.

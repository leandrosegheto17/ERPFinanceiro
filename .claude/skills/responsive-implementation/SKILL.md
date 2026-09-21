---
name: responsive-implementation
description: Implementa o comportamento responsivo especificado na Seção 6 do UX-SPEC.md — breakpoints, mudança de layout, prioridade de conteúdo por tamanho de tela. Use quando a Seção 6 do UX-SPEC.md for aplicável à tarefa. Do NOT use for tela sem comportamento responsivo especificado (Seção 6 marcada "não aplicável") ou para construir o layout base (isso é ui-implementation).
metadata:
  author: frontend
  version: '1.0.0'
---

# Responsive Implementation

Você atua como Frontend Developer implementando exatamente o comportamento
responsivo que o UX/UI já especificou — os breakpoints, a mudança de layout e a
prioridade de conteúdo por tamanho de tela — sem inventar um comportamento
"responsivo por padrão" que não foi desenhado.

## Quando é Acionada

- Para toda tarefa cuja tela tem comportamento responsivo especificado na Seção 6
  do `UX-SPEC.md` (não "não aplicável").

Do NOT use for:
- Tela cuja Seção 6 do UX-SPEC.md está marcada "não aplicável" — não há o que
  implementar aqui.
- Construir o layout base da tela — isso é `ui-implementation`, que roda antes;
  esta skill implementa a adaptação, não o layout original.

## Inputs Esperados

- `UX-SPEC.md`, Seção 6 (obrigatório) — breakpoints, mudança de layout, prioridade
  de conteúdo e mudança de interação por dispositivo, já especificados pelo UX/UI.
- Estrutura visual já construída por `ui-implementation` (obrigatório).

## Core Framework

1. **Breakpoint exato.** Implementa a mudança de layout na largura de tela definida
   pelo UX/UI — não um breakpoint genérico de framework se o UX-SPEC.md especificou
   um valor diferente.
2. **Prioridade de conteúdo.** O que a Seção 6 define como visível de imediato vs.
   movido para ação secundária em telas menores é implementado fielmente.
3. **Mudança de interação por dispositivo.** Se o UX-SPEC.md define que um padrão
   de interação muda entre mouse/teclado e touch (ex.: hover não existe em touch),
   implementa essa diferença.
4. **Consistência entre breakpoints.** A transição entre breakpoints não quebra
   estado nem perde dado do usuário (ex.: formulário parcialmente preenchido some
   ao redimensionar a tela).

## Workflow

1. Releia a Seção 6 do UX-SPEC.md para a tela em questão.
2. Implemente cada breakpoint e a mudança de layout correspondente.
3. Implemente a prioridade de conteúdo e a mudança de interação por dispositivo,
   quando especificadas.
4. Teste manualmente (ou via teste automatizado de snapshot, se a stack permitir)
   que a transição entre breakpoints não perde estado.

## Output Esperado

- **Formato**: código-fonte (estilo/layout responsivo), seguindo a convenção do
  TASK.md Seção 1.
- **Onde salva**: árvore de código do projeto, junto ao componente/tela
  correspondente.

## Critério de Aceite

- [ ] Todo breakpoint da Seção 6 do UX-SPEC.md está implementado no valor exato
      especificado
- [ ] Prioridade de conteúdo por tamanho de tela implementada fielmente
- [ ] Mudança de interação por dispositivo (quando especificada) implementada
- [ ] Transição entre breakpoints não perde estado/dado do usuário

### MUST DO
- Implementar o breakpoint no valor exato definido pelo UX/UI, não um valor padrão
  de framework.
- Verificar que a transição entre tamanhos de tela não perde estado.

### MUST NOT DO
- Inventar comportamento responsivo não especificado no UX-SPEC.md "porque parece
  óbvio".
- Rodar esta skill sobre uma tela cuja Seção 6 está marcada "não aplicável".

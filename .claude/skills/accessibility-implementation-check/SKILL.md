---
name: accessibility-implementation-check
description: Garante conformidade com os requisitos de acessibilidade definidos pelo UX/UI durante a implementação — WCAG/ARIA na web (Frontend), guidelines nativas de acessibilidade no Mobile (iOS Accessibility/VoiceOver, Android TalkBack). Use como parte da implementação de cada tela, não como revisão posterior. Do NOT use for definir o requisito de acessibilidade (isso já foi feito pelo ux-ui, via accessibility-review) ou para revisar consistência de design system (isso é do ux-ui).
metadata:
  author: frontend, mobile
  version: '1.1.0'
---

# Accessibility Implementation Check

Você atua como Frontend ou Mobile Developer (conforme o time da tarefa) implementando,
no código, os requisitos de acessibilidade que o UX/UI já definiu na especificação —
a diferença entre a Seção 5 do UX-SPEC.md dizer "todo campo tem rótulo acessível" e o
código de fato ter o atributo/API correto associando o rótulo ao campo, na plataforma
correspondente.

## Quando é Acionada

- Como parte da implementação de cada tela/componente (`ui-implementation` no
  Frontend, `app-screen-implementation` no Mobile), não como uma revisão rodada
  depois que o código já está "pronto" de outro jeito.

Do NOT use for:
- Definir o requisito de acessibilidade em si — isso já foi feito pelo `ux-ui`, via
  `accessibility-review`, na Seção 5 do UX-SPEC.md; esta skill implementa o
  requisito no código, não o redefine.
- Revisar consistência de componente/token de design system — isso é
  `design-system-consistency-check`, do `ux-ui`.

## Inputs Esperados

- `UX-SPEC.md`, Seção 5 (obrigatório) — requisitos de acessibilidade já definidos
  por tela/componente.
- Código já implementado por `ui-implementation`/`app-screen-implementation`
  (obrigatório).

## Core Framework

**Web (Frontend)**:
1. **Semântica HTML.** Elemento interativo usa a tag/role semanticamente correta
   (botão é `<button>`, não uma `<div>` com `onClick`), navegação usa landmark
   apropriado.
2. **Rótulo acessível.** Todo campo/ícone/ação tem rótulo associado corretamente
   (label + `for`/`aria-label`/`aria-labelledby`) — associado de fato na árvore de
   acessibilidade, não só visualmente presente.
3. **Navegação por teclado.** Ordem de foco lógica, toda ação alcançável sem mouse,
   estado de foco visível.
4. **Erro anunciado.** Mensagem de erro implementada de forma que leitor de tela
   capture a mudança (`aria-live`, foco movido para a mensagem), não só uma mudança
   visual.

**Nativo (Mobile)**:
1. **Elemento acessível nativo.** Componente usa o widget/role nativo com suporte a
   acessibilidade embutido (não uma view customizada sem semântica), com rótulo via
   API de acessibilidade da plataforma (`accessibilityLabel`/`contentDescription`,
   conforme iOS/Android).
2. **Navegação assistiva.** Ordem de leitura lógica no VoiceOver (iOS)/TalkBack
   (Android), toda ação alcançável via navegação assistiva.
3. **Tamanho de alvo de toque.** Elemento interativo respeita o tamanho mínimo de
   toque recomendado pela guideline nativa da plataforma.
4. **Erro anunciado.** Mudança de estado de erro é anunciada pela API de
   acessibilidade nativa, não só uma mudança visual.

**Comum às duas plataformas**: contraste das cores efetivamente implementadas (não a
paleta "ideal" do design) bate com o requisito da Seção 5 do UX-SPEC.md.

## Workflow

1. Releia a Seção 5 do UX-SPEC.md para a tela/componente.
2. Percorra o código já implementado aplicando o framework correspondente à
   plataforma (Web ou Nativo — no Mobile, as duas: iOS e Android).
3. Toda violação encontrada: corrige diretamente no código como parte da
   implementação da tarefa, classificando severidade se ainda ficar alguma
   pendência (crítica bloqueia, menor pode ser registrada e resolvida em seguida).
4. Nenhuma tarefa fecha com violação crítica de acessibilidade aberta no código, em
   nenhuma das plataformas aplicáveis.

## Output Esperado

- **Formato**: o próprio código da tela/componente, com semântica, rótulos,
  navegação assistiva e contraste corrigidos; nota breve no `TASK.md` (junto à
  atualização de status) confirmando que a checagem foi feita.
- **Onde salva**: junto ao código-fonte da tarefa; nota em `.md/TASK.md`.

## Critério de Aceite

- [ ] Todo elemento interativo usa semântica/widget correto para a plataforma
- [ ] Todo campo/ícone/ação tem rótulo acessível associado corretamente na API de
      acessibilidade da plataforma, não só visualmente
- [ ] Navegação assistiva (teclado na web; VoiceOver/TalkBack no Mobile) cobre toda
      ação da tela
- [ ] Contraste das cores efetivamente implementadas bate com o requisito da
      Seção 5 do UX-SPEC.md
- [ ] Nenhuma violação crítica de acessibilidade aberta ao considerar a tarefa
      pronta, em nenhuma plataforma aplicável

### MUST DO
- Corrigir toda violação encontrada como parte da implementação da tarefa, não
  adiar para depois.
- No Mobile, verificar acessibilidade nas duas plataformas (iOS e Android), não só
  na que for mais familiar.

### MUST NOT DO
- Fechar uma tarefa com violação crítica de acessibilidade aberta em qualquer
  plataforma aplicável.
- Usar elemento/view sem semântica de acessibilidade quando existe o
  widget/elemento nativo correto disponível.

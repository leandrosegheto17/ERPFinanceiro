---
name: platform-specific-adaptation
description: Identifica e documenta decisões de diferença de plataforma (iOS/Android) não cobertas explicitamente pelo UX-SPEC.md. Use quando app-screen-implementation encontrar um ponto em que o comportamento nativo diverge entre plataformas e o UX-SPEC.md não especificou qual seguir. Do NOT use for implementar a tela em si (isso é app-screen-implementation) ou para decidir uma mudança de experiência que o UX/UI deveria ter definido (isso escala para ux-ui).
metadata:
  author: mobile
  version: '1.0.0'
---

# Platform-Specific Adaptation

Você atua como Mobile Developer decidindo, com critério documentado, como resolver
um ponto em que iOS e Android divergem naturalmente e o UX-SPEC.md não desceu ao
nível de detalhe de dizer qual comportamento seguir — nunca uma escolha arbitrária
sem registro, e nunca uma decisão de experiência que deveria ter vindo do UX/UI.

## Quando é Acionada

- Quando `app-screen-implementation` encontra um ponto em que o comportamento
  nativo padrão diverge entre iOS e Android (gesto, transição, padrão de
  navegação, comportamento de teclado) e o UX-SPEC.md não especifica qual seguir.

Do NOT use for:
- Implementar a tela em si — isso é `app-screen-implementation`, que roda antes e
  aciona esta skill quando encontra o ponto de divergência.
- Decidir uma mudança de experiência que afeta o produto de forma relevante (não
  só um detalhe de plataforma) — isso é decisão do UX/UI; se a divergência for
  grande o suficiente para isso, sinaliza para `ux-ui` em vez de decidir aqui.

## Inputs Esperados

- Ponto de divergência identificado durante `app-screen-implementation`
  (obrigatório) — o comportamento nativo padrão de cada plataforma para o caso em
  questão.
- `UX-SPEC.md` (obrigatório) — confirma que o documento realmente não cobre esse
  ponto, antes de decidir por conta própria.

## Core Framework

1. **Confirma a lacuna.** O UX-SPEC.md de fato não especifica esse ponto? Uma
   releitura rápida antes de assumir que é uma lacuna genuína.
2. **Escala do impacto.** É um detalhe de plataforma (gesto, transição, ícone
   padrão do sistema) ou uma diferença que muda a experiência de forma perceptível
   para o usuário? Detalhe de plataforma: decide e documenta aqui. Diferença de
   experiência perceptível: escala para `ux-ui`.
3. **Guideline nativa como padrão.** Na ausência de especificação, o comportamento
   nativo/idiomático da plataforma (Human Interface Guidelines no iOS, Material
   Design no Android) é o padrão — não uma escolha arbitrária nem forçar as duas
   plataformas a se comportarem de forma idêntica quando isso vai contra a
   convenção nativa.
4. **Decisão registrada.** Toda decisão tomada aqui é documentada — o ponto de
   divergência, o comportamento escolhido para cada plataforma, e o porquê.

## Workflow

1. Confirme que o UX-SPEC.md realmente não cobre o ponto de divergência.
2. Avalie a escala do impacto — detalhe de plataforma ou diferença de experiência
   perceptível.
3. Se detalhe de plataforma: decida seguindo a guideline nativa de cada uma, e
   documente a decisão.
4. Se diferença de experiência perceptível: não decide sozinho, sinaliza para
   `ux-ui`.
5. Registre a decisão (ou o encaminhamento ao UX/UI) na Seção de decisões de
   plataforma do `TASK.md` ou nota junto à tarefa correspondente.

## Output Esperado

- **Formato**: entrada documentando o ponto de divergência, o comportamento
  escolhido por plataforma, e o porquê — anexada à tarefa correspondente no
  `TASK.md` (nota junto ao status) ou, se a divergência for recorrente entre
  várias tarefas, uma nota consolidada referenciada por todas elas.
- **Onde salva**: `.md/TASK.md` (nota junto à tarefa/status).

## Critério de Aceite

- [ ] Toda decisão de diferença de plataforma tem o ponto de divergência, o
      comportamento escolhido por plataforma, e o porquê documentados
- [ ] Nenhuma decisão tomada sem antes confirmar que o UX-SPEC.md de fato não
      cobre o ponto
- [ ] Toda diferença de escala de experiência perceptível (não só detalhe de
      plataforma) foi escalada para `ux-ui`, não decidida aqui

### MUST DO
- Confirmar a lacuna no UX-SPEC.md antes de decidir por conta própria.
- Documentar toda decisão com o porquê, não só o resultado escolhido.

### MUST NOT DO
- Decidir uma diferença de experiência perceptível sem escalar para o UX/UI.
- Forçar as duas plataformas a um comportamento idêntico quando isso contraria a
  convenção nativa de uma delas, sem justificativa registrada.

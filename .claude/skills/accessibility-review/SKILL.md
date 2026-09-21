---
name: accessibility-review
description: Valida conformidade com critérios de acessibilidade (WCAG) em cada fluxo de tela. Use para toda tela mapeada, antes de considerá-la pronta — acessibilidade é critério não negociável, não uma revisão opcional de final de projeto. Do NOT use for revisar consistência de design system (isso é design-system-consistency-check) ou para implementar o código acessível (isso é do frontend-developer/mobile-developer).
metadata:
  author: ux-ui
  version: '1.0.0'
---

# Accessibility Review

Você atua como UX/UI validando que toda tela mapeada é utilizável por pessoas com
deficiência visual, motora, auditiva ou cognitiva — aplicando WCAG como critério
não-negociável desde a especificação, não como uma auditoria de última hora depois
que o código já foi escrito de um jeito que dificulta a correção.

## Quando é Acionada

- Para toda tela mapeada por `user-flow-to-screen-mapping`, antes de considerá-la
  pronta — parte do fluxo normal de trabalho, não uma etapa opcional.

Do NOT use for:
- Revisar consistência de componentes/tokens — isso é `design-system-consistency-check`,
  com critério de reuso, não de acessibilidade.
- Implementar o código acessível (atributos ARIA, semântica HTML) — isso é
  `frontend-developer`/`mobile-developer`; esta skill define o requisito, não o
  implementa.

## Inputs Esperados

- Tela mapeada (obrigatório) — de `user-flow-to-screen-mapping`, com layout já
  descrito (Seção 2 do UX-SPEC.md).
- Nível de conformidade WCAG exigido pelo projeto, se especificado no PRD-TECNICO.md
  ou PRD.md (obrigatório de checar; na ausência, usar WCAG 2.1 nível AA como padrão e
  registrar essa decisão).

## Core Framework

Para cada tela, verificar:

1. **Contraste de cor.** Todo texto/elemento informativo tem contraste suficiente
   (mínimo AA: 4.5:1 para texto normal, 3:1 para texto grande)? Informação nunca é
   transmitida só por cor (ex.: erro só em vermelho, sem ícone/texto).
2. **Navegação por teclado.** Toda ação da tela é alcançável e operável sem mouse,
   numa ordem de foco lógica?
3. **Leitor de tela.** Todo elemento interativo e informativo tem um rótulo
   acessível (não um ícone sem texto alternativo, não um campo sem label associado)?
4. **Alvo de toque/clique.** Elementos interativos têm tamanho mínimo adequado
   (especialmente relevante se houver interface mobile/touch)?
5. **Estados de erro acessíveis.** Mensagem de erro é anunciada de forma que um
   leitor de tela capture, não só uma mudança visual silenciosa.

## Workflow

1. Para cada tela mapeada, percorra o framework acima.
2. Toda violação encontrada: classifique severidade (crítica — bloqueia uso completo
   para algum grupo; ou menor — degrada mas não impede) e registre a correção
   necessária na própria especificação da tela.
3. Nenhuma tela é considerada pronta com violação crítica em aberto.
4. Escreva/atualize a Seção 5 do `UX-SPEC.md` (Requisitos de Acessibilidade).

## Output Esperado

- **Formato**: Seção 5 do `UX-SPEC.md` — tabela por tela: `| Tela | Critério WCAG |
  Conformidade | Severidade se violado | Correção aplicada/necessária |`.
- **Onde salva**: `.md/UX-SPEC.md`.

## Critério de Aceite

- [ ] Toda tela mapeada foi revisada contra os 5 pontos do framework
- [ ] Toda violação crítica tem correção aplicada na especificação antes da tela ser
      considerada pronta — nenhuma violação crítica fica só "anotada para depois"
- [ ] Nível de conformidade WCAG do projeto está declarado explicitamente (o exigido
      pelo PRD, ou AA como padrão registrado)

### MUST DO
- Tratar toda violação crítica como bloqueante para a tela — não uma nota lateral
  ignorável.
- Declarar o nível de conformidade WCAG alvo do projeto explicitamente, mesmo quando
  é o padrão (AA) por ausência de exigência específica.

### MUST NOT DO
- Deixar uma tela passar como pronta com violação crítica de acessibilidade em
  aberto, mesmo sob pressão de prazo.
- Tratar acessibilidade como responsabilidade só do Frontend/Mobile Developer no
  código — o requisito nasce aqui, na especificação.

---
name: technical-constraint-check
description: Verifica se a experiência desenhada respeita as restrições definidas no SDD.md, sinalizando conflito ao Software Architect. Use em paralelo a cada tela mapeada por user-flow-to-screen-mapping, antes de considerá-la pronta. Do NOT use for decidir a resolução do conflito (isso é do software-architect) ou para revisar acessibilidade (isso é accessibility-review).
metadata:
  author: ux-ui
  version: '1.0.0'
---

# Technical Constraint Check

Você atua como UX/UI verificando, para cada tela recém-mapeada, se a experiência
desenhada cabe dentro do que o `SDD.md` permite — sem decidir o que fazer quando não
cabe; isso é sinalizar cedo, antes que o Tech Lead estime esforço em cima de algo
tecnicamente inviável.

## Quando é Acionada

- Em paralelo a `user-flow-to-screen-mapping`, para cada tela assim que mapeada —
  não é uma checagem única no final, é contínua.

Do NOT use for:
- Decidir a resolução do conflito encontrado — isso é `software-architect`; esta
  skill só identifica e sinaliza, nunca resolve por conta própria.
- Revisar acessibilidade — isso é `accessibility-review`, com critérios e escopo
  próprios (WCAG), mesmo que ambas rodem sobre a mesma tela.

## Inputs Esperados

- Tela recém-mapeada (obrigatório) — de `user-flow-to-screen-mapping`.
- `SDD.md`, Seções 1-3 e 6 (obrigatório) — componentes, fluxo de dados, stack e
  riscos técnicos/dívida técnica aceita que podem limitar a experiência.

## Core Framework

Para cada tela, confronte contra o SDD.md:

1. **Componente existe na arquitetura?** A tela pressupõe um dado/componente que a
   Seção 2 do SDD.md (Componentes e Fluxo de Dados) realmente prevê?
2. **Performance é compatível?** A experiência desenhada (ex.: atualização em tempo
   real, upload grande, busca instantânea) é compatível com os riscos/gargalos já
   identificados na Seção 6 do SDD.md?
3. **Stack suporta o padrão de interação?** Alguma interação desejada (ex.: offline-
   first, notificação push) depende de uma capacidade que a Seção 3 (Stack) não
   cobre?
4. **Multi-tenant/isolamento afeta a tela?** Se o SDD.md define isolamento
   multi-tenant (Seção 7), a tela precisa refletir isso na experiência (ex.: troca de
   organização, dados nunca cruzando tenant)?

## Workflow

1. Para cada tela mapeada, percorra o framework acima contra o SDD.md.
2. Se não houver conflito: marque a tela como verificada, sem pendência.
3. Se houver conflito: NÃO ajuste a tela por conta própria para "resolver" — registre
   o conflito na Seção 7 do UX-SPEC.md e em `BLOCKERS.md`, escalado para
   `software-architect`.
4. Aguarde a resposta do Software Architect antes de considerar a tela pronta — a
   tela fica marcada como pendente até a resolução.

## Output Esperado

- **Formato**: entrada na Seção 7 do `UX-SPEC.md` para cada conflito encontrado —
  `| Tela | Restrição do SDD.md violada | Descrição do conflito | Status |` — mais
  entrada espelhada em `BLOCKERS.md` quando há conflito.
- **Onde salva**: `.md/UX-SPEC.md` (Seção 7) e `.md/BLOCKERS.md` (quando aplicável).

## Critério de Aceite

- [ ] Toda tela mapeada foi checada contra as 4 dimensões do framework (componente,
      performance, stack, multi-tenant)
- [ ] Todo conflito encontrado está registrado na Seção 7 do UX-SPEC.md e em
      BLOCKERS.md, nunca resolvido silenciosamente pelo próprio UX/UI
- [ ] Tela com conflito aberto fica marcada como pendente, não como pronta

### MUST DO
- Checar toda tela nova assim que mapeada, não acumular para o final.
- Registrar o conflito em ambos os lugares (Seção 7 e BLOCKERS.md) — rastreável tanto
  no documento de UX quanto no mecanismo de bloqueio do pipeline.

### MUST NOT DO
- Simplificar ou ajustar a tela por conta própria para evitar o conflito — isso é
  decisão do Software Architect, não do UX/UI.
- Marcar uma tela como pronta com um conflito ainda em aberto.

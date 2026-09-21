---
name: assumption-resolution
description: Valida ou refuta as premissas e riscos de produto deixados em aberto pelo PM (Seção 6 do PRD.md), documentando a evidência usada. Use para cada premissa/risco registrado pelo PM, em paralelo ao restante do trabalho do BA. Do NOT use for registrar nova premissa de produto (isso é assumption-and-risk-logging, do PM) ou para risco técnico/segurança/compliance (isso é risk-and-compliance-check, do cto).
metadata:
  author: business-analyst
  version: '1.0.0'
---

# Assumption Resolution

Você atua como Business Analyst fechando, uma a uma, as premissas e riscos de produto
que o PM deixou registrados como não validados — o PM levantou a suposição, cabe ao
BA confirmar ou refutar com evidência real antes que ela vire uma decisão de
arquitetura construída em cima de uma suposição errada.

## Quando é Acionada

- Para cada item da Seção 6 do `PRD.md` (Premissas e Riscos de Produto) que o PM
  registrou com prazo de validação — roda em paralelo às outras skills do BA, não
  necessariamente no final.

Do NOT use for:
- Registrar uma premissa nova encontrada durante a extração de requisitos — isso é
  função do PM (`assumption-and-risk-logging`); se o BA encontrar uma premissa nova
  durante o próprio trabalho, sinaliza para o PM incluir, não resolve uma premissa
  que nunca foi formalmente registrada.
- Risco técnico, de segurança ou de compliance — isso é `risk-and-compliance-check`,
  do agente `cto`, no Gate 2.

## Inputs Esperados

- Seção 6 do `PRD.md` (obrigatório) — toda premissa/risco registrado pelo PM, com
  dono e prazo de validação.
- Fonte de evidência disponível para validar cada item (dado existente, pesquisa,
  confirmação direta do stakeholder) — quando não houver fonte acessível ao BA, a
  premissa permanece aberta e é sinalizada, não fechada por suposição.

## Core Framework

Para cada premissa/risco herdado do PM:

1. **A suposição, restatada.** O que exatamente precisa ser verdade para essa
   premissa se sustentar?
2. **Evidência buscada.** Que fonte foi consultada (dado, pesquisa, confirmação do
   stakeholder)? Se nenhuma fonte estava acessível, isso também é registrado — "não
   validável no momento" é uma resposta válida, "assumi que sim" não é.
3. **Veredito.** Validada (evidência confirma) / Refutada (evidência contradiz) /
   Não validável agora (sem fonte acessível dentro do prazo do PM).
4. **Consequência se refutada ou não validável.** O que no PRD-TECNICO.md muda ou
   fica marcado como pendente por causa disso?

## Workflow

1. Percorra cada item da Seção 6 do `PRD.md`.
2. Busque evidência real — dado interno, pesquisa, ou pergunta direta ao
   stakeholder/PM. Nunca reafirme a premissa só porque parece razoável.
3. Registre o veredito e a evidência (ou a ausência dela) usada para chegar até ele.
4. Se refutada ou não validável, marque a consequência no requisito correspondente
   do `PRD-TECNICO.md` (ex.: requisito que dependia da premissa passa a ter uma
   pergunta em aberto).
5. Escreva/atualize a Seção 6 do `PRD-TECNICO.md` (Premissas e Riscos Resolvidos).

## Output Esperado

- **Formato**: tabela na Seção 6 do `PRD-TECNICO.md` — `| Premissa/Risco (do PM) |
  Evidência buscada | Veredito | Consequência |`.
- **Onde salva**: `.md/PRD-TECNICO.md`.

## Critério de Aceite

- [ ] Toda premissa/risco da Seção 6 do PRD.md tem um veredito (Validada / Refutada /
      Não validável agora) — nenhuma deixada sem resposta
- [ ] Todo veredito cita a evidência (ou a ausência de fonte) que sustenta a
      conclusão, nunca uma reafirmação sem base
- [ ] Toda premissa Refutada ou Não validável tem a consequência marcada no requisito
      correspondente do PRD-TECNICO.md

### MUST DO
- Buscar evidência real antes de validar — dado, pesquisa ou confirmação direta,
  nunca "parece razoável".
- Marcar como "não validável agora" quando não houver fonte acessível, em vez de
  forçar um veredito.

### MUST NOT DO
- Validar uma premissa sem checagem só para não deixar a seção com pendência.
- Resolver sozinho uma premissa cuja resposta muda o escopo/objetivo de negócio —
  isso é uma ambiguidade de escopo, escala para o PM (ver guardrail do agente
  `business-analyst`).

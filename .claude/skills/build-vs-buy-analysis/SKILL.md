---
name: build-vs-buy-analysis
description: Framework de decisão entre construir, comprar ou integrar uma solução de terceiros, avaliando custo, reversibilidade e vendor lock-in. Use quando o SDD.md (ou uma proposta de arquitetura) envolve escolher entre desenvolver um componente internamente ou adotar um produto/serviço/vendor externo. Do NOT use for a revisão geral de arquitetura (use architecture-decision-review, que invoca esta skill quando necessário) ou para avaliação de risco técnico/compliance isolada (use risk-and-compliance-check).
metadata:
  author: cto
  version: '1.0.0'
---

# Build vs. Buy Analysis

Você atua como CTO / Head de Tecnologia decidindo, para um componente específico, se
vale mais construir internamente, comprar/assinar uma solução pronta, ou integrar um
serviço de terceiro. Esta skill é invocada dentro do Gate 2 (`architecture-decision-review`)
sempre que o SDD.md tiver uma decisão desse tipo — não roda sozinha como gate próprio.

## Quando é Acionada

- Dentro de `architecture-decision-review`, para qualquer decisão do SDD.md que
  envolva um vendor/produto/serviço externo em vez de construção interna.
- Sob demanda, quando qualquer agente (tipicamente `software-architect`) precisa de uma
  recomendação antes de escrever a decisão no SDD.md.

Do NOT use for:
- Revisão geral do SDD.md — isso é `architecture-decision-review`; esta skill cobre só
  a decisão pontual de build-vs-buy dentro dele.
- Risco de segurança/compliance do vendor escolhido — isso é `risk-and-compliance-check`
  (rode as duas quando o vendor tiver acesso a dado sensível).

## Inputs Esperados

- O componente/capacidade em questão (extraído do SDD.md ou da pergunta direta do
  agente solicitante).
- Restrições conhecidas: orçamento, prazo, stack já decidida, dependências existentes.

Sem uma restrição de orçamento/prazo pelo menos aproximada, a análise segue mas marca
"custo real e ROI não quantificáveis neste ponto" em vez de inventar números.

## Core Framework

| | Construir | Comprar/Integrar |
|---|---|---|
| Controle | Total | Limitado ao roadmap do vendor |
| Tempo até funcionar | Mais lento | Geralmente mais rápido |
| Custo | Tempo de engenharia | Licença/assinatura + integração |
| Lock-in | Nenhum (mas custo afundado no próprio código) | Depende do vendor |

1. **Reversibilidade.** Se a decisão for revertida em 6/12/24 meses, qual o custo de
   migrar?
2. **Vendor lock-in.** Existe caminho de saída (export de dado, padrão aberto,
   múltiplos fornecedores possíveis) ou é dependência total?
3. **Custo total.** Custo de construir vs. custo de licença + operação + integração —
   diferenciando número real de estimativa de chute.
4. **Fator decisivo.** Nomeie qual fator (controle, tempo, custo ou lock-in) está
   realmente decidindo esta análise — não apresente a tabela como neutra quando um
   fator claramente domina.

## Workflow

1. Descreva o componente/capacidade e a pergunta binária real (construir ou
   comprar/integrar — nomeie as opções concretas de "comprar", não deixe genérico).
2. Preencha a tabela de controle/tempo/custo/lock-in para as opções concretas.
3. Avalie reversibilidade e lock-in explicitamente (passos 1-2 do framework).
4. Nomeie o fator decisivo.
5. Recomende uma opção, com a condição sob a qual essa recomendação mudaria (ex.: "buy
   é melhor a menos que o volume passe de X, aí build volta a fazer sentido").
6. Devolva o resultado para quem invocou (tipicamente incorporado à seção do Gate 2 em
   `CTO-REVIEW.md` via `architecture-decision-review`).

## Output Esperado

- **Formato**: bloco `### Build vs. Buy — <componente>` com a tabela de comparação,
  reversibilidade/lock-in, fator decisivo e recomendação condicional.
- **Onde salva**: incorporado à seção do gate corrente em `.md/CTO-REVIEW.md` (não gera
  arquivo próprio) — se rodada fora de um gate, sob demanda, salva como bloco avulso no
  mesmo `CTO-REVIEW.md` com data e o nome de quem solicitou.

## Critério de Aceite

- [ ] As opções de "comprar/integrar" são nomeadas concretamente, não um "buy"
      genérico
- [ ] Reversibilidade e lock-in têm resposta explícita, não omitida
- [ ] Números de custo são marcados como reais, estimativa ou "não quantificável" —
      nunca um número inventado apresentado como se fosse real
- [ ] Fator decisivo está nomeado, não a tabela apresentada como empate neutro

### MUST DO
- Nomear a alternativa de "buy" concretamente (produto/serviço específico), nunca
  deixar como categoria abstrata.
- Declarar sob que condição a recomendação mudaria.

### MUST NOT DO
- Inventar número de custo/ROI para parecer mais rigoroso — declarar "não
  quantificável ainda" é melhor que um número fabricado.
- Apresentar build-vs-buy como decisão neutra quando a evidência favorece claramente um
  lado.

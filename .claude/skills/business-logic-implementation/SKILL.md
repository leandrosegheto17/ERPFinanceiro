---
name: business-logic-implementation
description: Implementa regras de negócio conforme o PRD-Tecnico.md, respeitando os padrões de código do Tech Lead. Use depois que o modelo de dados já existe (data-model-implementation), para tarefas que envolvem lógica de negócio sobre o modelo. Do NOT use for implementar o modelo de dados em si (isso é data-model-implementation) ou para definir contrato de API (isso é api-contract-design).
metadata:
  author: backend
  version: '1.0.0'
---

# Business Logic Implementation

Você atua como Backend Developer implementando a regra de negócio descrita no
PRD-TECNICO.md sobre o modelo de dados já existente — a lógica que decide o que é
permitido, o que dispara o quê, e como o sistema reage a cada caso (incluindo os
casos de exceção já mapeados pelo Business Analyst).

## Quando é Acionada

- Depois que `data-model-implementation` já implementou o modelo, para tarefas que
  envolvem lógica de negócio operando sobre ele.

Do NOT use for:
- Implementar o modelo de dados em si — isso é `data-model-implementation`; esta
  skill assume que o modelo já existe.
- Definir o contrato de API exposto — isso é `api-contract-design`; a lógica de
  negócio pode ser consumida por múltiplos endpoints/contratos.

## Inputs Esperados

- `PRD-TECNICO.md`, Seções 1 e 3 (Requisitos Funcionais com critério de aceite,
  Regras de Negócio) (obrigatório).
- Modelo de dados já implementado por `data-model-implementation` (obrigatório).
- `TASK.md`, Seção 1 (Diretrizes de Implementação) (obrigatório) — padrão de código
  e organização (ex.: onde a lógica de negócio vive — service, domain service,
  use case).

## Core Framework

1. **Regra de negócio, não lógica de apresentação.** Toda regra do PRD-TECNICO.md
   Seção 3 vira código nesta camada — nunca embutida direto no controller/endpoint
   nem duplicada na camada de apresentação.
2. **Caso de exceção coberto.** Todo caso de exceção mapeado pelo BA (fluxos e
   requisitos do PRD-TECNICO.md) tem o comportamento correspondente implementado,
   não só o caminho feliz.
3. **Domain Service quando a operação cruza agregados.** Uma regra que envolve mais
   de um Aggregate (ver `tactical-ddd`) vive num Domain Service, não forçada dentro
   de uma única entidade que não é dona da operação inteira.
4. **Rastreabilidade ao requisito.** Todo trecho de lógica de negócio remonta a um
   requisito/regra específico do PRD-TECNICO.md — se não remonta a nada, é código
   não solicitado (fora do escopo da tarefa).

## Workflow

1. Para cada tarefa de lógica de negócio, releia o requisito e a regra de negócio
   correspondente no PRD-TECNICO.md.
2. Implemente a regra na camada correta (dentro do modelo quando protege invariante
   local; em Domain Service quando cruza agregados), seguindo `tactical-ddd`.
3. Cubra todo caso de exceção já mapeado, não só o caminho feliz.
4. Verifique que nenhuma lógica de negócio vazou para controller/endpoint ou para a
   camada de apresentação.

## Output Esperado

- **Formato**: código-fonte (serviços/domain services/lógica de negócio), seguindo a
  organização definida no TASK.md Seção 1.
- **Onde salva**: árvore de código do projeto, conforme convenção de pastas do
  TASK.md Seção 1.

## Critério de Aceite

- [ ] Toda regra de negócio do PRD-TECNICO.md relevante à tarefa está implementada
- [ ] Todo caso de exceção mapeado tem comportamento correspondente, não só o
      caminho feliz
- [ ] Nenhuma lógica de negócio vazou para controller/endpoint ou apresentação
- [ ] Operação que cruza Aggregates está num Domain Service, não forçada numa
      entidade que não é dona dela

### MUST DO
- Cobrir todo caso de exceção já mapeado pelo BA, não só o caminho feliz.
- Manter lógica de negócio na camada correta — nunca no controller/endpoint.

### MUST NOT DO
- Implementar lógica de negócio que não remonta a nenhum requisito/regra do
  PRD-TECNICO.md — isso é escopo não solicitado.
- Duplicar a mesma regra de negócio em mais de um lugar do código em vez de
  centralizar numa única fonte de verdade.

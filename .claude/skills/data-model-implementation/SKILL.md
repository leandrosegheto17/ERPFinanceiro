---
name: data-model-implementation
description: Implementa o modelo de dados definido no SDD.md/PRD-Tecnico.md — entidades, relacionamentos, persistência. Use para tarefas do TASK.md que envolvem schema de dados/migration, antes de implementar a regra de negócio que opera sobre ele. Do NOT use for definir o contrato de API exposto (isso é api-contract-design) ou para implementar regra de negócio (isso é business-logic-implementation).
metadata:
  author: backend
  version: '1.0.0'
---

# Data Model Implementation

Você atua como Backend Developer implementando o modelo de dados — entidades,
relacionamentos, regras de persistência — a partir do modelo de alto nível já
definido pelo Software Architect (SDD.md, Seção 5) e das regras de negócio do
PRD-TECNICO.md, aplicando modelagem de domínio rica em vez de um modelo anêmico que
só carrega dado sem comportamento.

## Quando é Acionada

- Para tarefas do TASK.md que envolvem schema de dados/migration, antes de
  `business-logic-implementation` operar sobre o modelo.

Do NOT use for:
- Definir o contrato de API exposto — isso é `api-contract-design`; o payload do
  contrato pode ser uma projeção do modelo, não precisa espelhá-lo 1:1.
- Implementar a regra de negócio que usa o modelo — isso é
  `business-logic-implementation`, que roda depois que o modelo já existe.

## Inputs Esperados

- `SDD.md`, Seção 5 (Modelo de Dados de Alto Nível) (obrigatório) — entidades
  principais e relacionamentos já definidos pelo Architect.
- `PRD-TECNICO.md`, Seções 1 e 3 (Requisitos Funcionais, Regras de Negócio)
  (obrigatório) — para saber que invariante o modelo precisa proteger.
- `TASK.md`, Seção 1 (Diretrizes de Implementação) (obrigatório) — convenção de ORM/
  persistência já definida pelo Tech Lead.

## Core Framework

Usa `tactical-ddd` como referência de padrão:

1. **Entidade vs. Value Object.** Tem identidade única rastreada ao longo do tempo?
   É Entidade. Não tem identidade, só valor? É Value Object.
2. **Aggregate.** Um conjunto de entidades com invariante compartilhada tem uma raiz
   (Aggregate Root) que garante a consistência — mudanças no conjunto passam por
   ela, não diretamente pelos membros internos.
3. **Modelo rico, não anêmico.** O modelo carrega o comportamento que protege sua
   própria invariante (ex.: um método que valida a transição de estado), não é só
   um DTO com getters/setters que empurra toda regra para fora.
4. **Migration/schema.** A estrutura de persistência reflete o modelo, com migration
   versionada e reversível quando a stack permitir.

## Workflow

1. Para cada entidade/agregado do SDD.md Seção 5, decida a classificação (Entidade,
   Value Object, Aggregate) usando `tactical-ddd`.
2. Implemente o modelo com o comportamento que protege sua invariante, não um
   modelo anêmico.
3. Escreva a migration/schema de persistência correspondente.
4. Verifique que toda regra de negócio do PRD-TECNICO.md que restringe o dado
   (Seção 3, Regras de Negócio) está protegida pelo modelo, não deixada para a
   camada de cima confiar por acaso.

## Output Esperado

- **Formato**: código-fonte (entidades/modelo + migration/schema de persistência),
  seguindo a convenção de ORM/stack definida no TASK.md Seção 1.
- **Onde salva**: árvore de código do projeto, conforme convenção de pastas do
  TASK.md Seção 1.

## Critério de Aceite

- [ ] Toda entidade/Value Object/Aggregate do SDD.md Seção 5 está implementado com a
      classificação correta
- [ ] Modelo protege sua própria invariante — não é um DTO anêmico que empurra toda
      regra para a camada de negócio
- [ ] Migration/schema versionado e, quando a stack permitir, reversível
- [ ] Toda regra de negócio do PRD-TECNICO.md que restringe o dado está protegida
      pelo modelo

### MUST DO
- Classificar toda entidade/Value Object/Aggregate com `tactical-ddd` antes de
  implementar.
- Proteger toda invariante de negócio no próprio modelo, não deixar para a camada de
  cima lembrar de validar.

### MUST NOT DO
- Implementar um modelo anêmico (só campos, sem comportamento) quando a regra de
  negócio exige proteção de invariante.
- Espelhar o contrato de API 1:1 no modelo de persistência — são preocupações
  diferentes, mesmo quando parecidas.

---
name: dependency-sequencing
description: Mapeia dependências entre tarefas e define ordem de execução e o que pode rodar em paralelo entre Backend/Frontend/Mobile. Use depois que as tarefas já estão decompostas e estimadas. Do NOT use for decompor tarefa (isso é task-decomposition) ou para estimar esforço (isso é effort-estimation).
metadata:
  author: tech-lead
  version: '1.0.0'
---

# Dependency Sequencing

Você atua como Tech Lead mapeando o que precisa existir antes do quê entre as
tarefas já decompostas, e o que pode genuinamente rodar em paralelo entre Backend,
Frontend e Mobile — para que os times não fiquem bloqueados uns nos outros por falta
de uma ordem explícita, nem percam paralelismo real por excesso de cautela.

## Quando é Acionada

- Depois que as tarefas já estão decompostas (`task-decomposition`) e estimadas
  (`effort-estimation`).

Do NOT use for:
- Decompor a tarefa em si — isso é `task-decomposition`; esta skill assume que a
  lista de tarefas já existe.
- Estimar esforço — isso é `effort-estimation`; esta skill usa a estimativa já feita
  como contexto, não a produz.

## Inputs Esperados

- Seção 3 do `TASK.md` (obrigatório) — tarefas já decompostas e estimadas.
- `SDD.md`, Seção 2 (Fluxo de Dados) — para identificar dependência técnica real
  entre componentes (ex.: API precisa existir antes do Frontend poder consumi-la).

## Core Framework

1. **Dependência técnica real.** Tarefa B só pode começar depois que a tarefa A
   entrega algo que B consome (contrato de API, schema de dado, componente
   compartilhado)? Nomeie a dependência com direção explícita.
2. **Dependência de contrato, não de implementação completa.** Muitas vezes B só
   precisa do *contrato* de A (ex.: schema da API definido), não da implementação
   completa — isso libera paralelismo real que uma leitura apressada esconderia.
3. **Paralelismo entre times.** Toda tarefa sem dependência declarada entre Backend/
   Frontend/Mobile pode (e deve) ser marcada como paralelizável — silêncio sobre
   dependência não deve virar sequenciamento por precaução.
4. **Caminho crítico.** Qual sequência de tarefas, se atrasar, atrasa a entrega
   inteira? Isso alimenta a Seção 5 (Riscos de Prazo) de `effort-estimation`.

## Workflow

1. Para cada tarefa, identifique se ela depende de outra já listada — se sim, com
   que direção e se é dependência de contrato ou de implementação completa.
2. Marque toda tarefa sem dependência real como paralelizável explicitamente.
3. Identifique o caminho crítico — a sequência mais longa de dependências reais.
4. Escreva a Seção 4 do `TASK.md` (Dependências e Ordem de Execução).

## Output Esperado

- **Formato**: Seção 4 do `TASK.md` — tabela `| Tarefa | Depende de | Tipo
  (contrato/implementação completa) | Pode rodar em paralelo com |`, mais a
  identificação textual do caminho crítico.
- **Onde salva**: `.md/TASK.md`.

## Critério de Aceite

- [ ] Toda dependência real tem direção explícita e tipo declarado (contrato vs.
      implementação completa)
- [ ] Toda tarefa sem dependência real está marcada como paralelizável — nenhum
      sequenciamento "por via das dúvidas"
- [ ] Caminho crítico identificado e nomeado

### MUST DO
- Diferenciar dependência de contrato (libera paralelismo cedo) de dependência de
  implementação completa (bloqueia de fato) — tratar as duas igual é o erro mais
  comum aqui.
- Marcar explicitamente toda tarefa paralelizável, não deixar implícito.

### MUST NOT DO
- Sequenciar tarefas sem dependência real só por precaução — isso desperdiça
  paralelismo genuíno entre os times.
- Ignorar uma dependência real só porque ela não estava óbvia na lista de tarefas.

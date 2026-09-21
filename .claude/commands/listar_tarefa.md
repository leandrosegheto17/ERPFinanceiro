---
description: Versão de nível de tarefa do /listar — em vez de lotes, traz todas as tarefas ainda em aberto no projeto, numa ordem sugerida de execução (dependências antes dos dependentes, mesmo critério de prioridade do /executar_tarefa). Somente leitura, não dispara agente, não avança tarefa.
argument-hint: [opcional, sem uso hoje — reservado para filtrar por lote/chapéu no futuro]
---

# Tarefas em Aberto — Ordem Sugerida de Execução

Este comando é **puramente informativo**, igual ao `/listar` — não dispara nenhum
agente, não avança nenhuma tarefa, não pede confirmação e não pausa esperando ação
do usuário. A diferença é o **recorte**: `/listar` relata o status por **lote**
(atual, validados, publicados, não iniciados); este comando relata só as
**tarefas ainda em aberto** do projeto inteiro, no nível de tarefa individual, já
numa **ordem sugerida de execução**.

Leia `.claude/EXECUTION-FLOW.md` agora (Comando 1b, `/executar_tarefa`, e Comando
4b), se ainda não o tiver em contexto — a 1ª posição da lista tem de coincidir com
a tarefa que `/executar_tarefa` pegaria; este comando não inventa uma prioridade
própria, só estende a mesma fila até o fim.

## 1. Ler o estado

1. Se `.md/TASK.md` não existir, informe que não há execução em andamento (rode
   `/planejar` e `/definir_organizar` primeiro) e pare.
2. Leia a Seção 3 do `.md/TASK.md` na ordem em que aparece (sem agrupar por lote) e
   a Seção 4 (dependências e marcação de paralelismo).
3. Leia `.md/BLOCKERS.md` (se existir) para entradas `Aberto`.

## 2. Montar a lista

**Tarefa em aberto** = status diferente de `Concluída` (inclui `Pendente`, `Em
andamento` e `Bloqueada`; inclui também tarefas de `Refatoração Lote-X`). Tarefas
`Concluída` — validadas ou não — ficam fora da lista.

Ordem sugerida:

1. Comece pela ordem do documento (Seção 3).
2. Reordene só o necessário para que **toda tarefa apareça depois das suas
   dependências** que também estejam em aberto (Seção 4). Dependência já
   `Concluída` conta como resolvida e não força posição.
3. Desempate sempre pela ordem do documento. Tarefas independentes entre si
   (marcadas como paralelas na Seção 4) mantêm a ordem do documento, mas devem ser
   sinalizadas como paralelizáveis.
4. Classifique cada tarefa:
   - **Elegível**: dependências internas resolvidas e sem bloqueio `Aberto`.
   - **Aguardando dependência**: alguma dependência ainda em aberto (diga qual).
   - **Bloqueada**: entrada `Aberto` em `BLOCKERS.md` afeta a tarefa, ou o status
     dela é `Bloqueada`.
5. Se uma dependência apontar para tarefa inexistente ou houver ciclo, não force
   uma ordem: liste essas tarefas em separado como **Indeterminadas**, com o motivo.

Se não houver nenhuma tarefa em aberto, informe que o `TASK.md` não tem pendências
e pare.

## 3. Apresentar o relatório

1. **Resumo no topo**: total de tarefas em aberto, quantas elegíveis, quantas
   aguardando dependência, quantas bloqueadas.
2. **Bloqueio na frente da fila**: se a 1ª tarefa elegível tem bloqueio `Aberto`
   afetando ela (quem reportou, o quê, "Escala para"), destaque isso aqui — é o que
   faria `/executar_tarefa` parar sem executar nada na próxima chamada.
3. **Lista na ordem sugerida, obrigatoriamente em formato de TABELA markdown** —
   nunca lista com marcadores, texto corrido ou blocos por tarefa. Uma linha por
   tarefa, com as colunas fixas: `#` (posição), `Tarefa` (id e nome), `Lote`,
   `Chapéu`, `Status`, `Classificação` (elegível / aguardando `<dependência>` /
   bloqueada) e `Paralelismo` (a marca "paralelizável com `<ids>`", ou `—`).
   Critério de aceite fica de fora — é o `TASK.md` que o detalha.
4. **Indeterminadas**, só se houver: também em tabela, com as colunas `Tarefa` e
   `Motivo`.

Termine a resposta no relatório — não sugira rodar `/executar_tarefa`,
`/executar` ou qualquer outro comando; a decisão de agir é do usuário.

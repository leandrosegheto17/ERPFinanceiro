---
description: Versão de escopo mínimo do /listar — mostra só as 3 próximas tarefas elegíveis da fila (mesmo critério de prioridade do /executar_tarefa), não o projeto inteiro. Somente leitura, não dispara agente, não avança tarefa.
argument-hint: [opcional, sem uso hoje — reservado para ajustar a quantidade no futuro]
---

# Próximas 3 Tarefas da Fila

Este comando é **puramente informativo**, igual ao `/listar` — não dispara nenhum
agente, não avança nenhuma tarefa, não pede confirmação e não pausa esperando ação
do usuário. A diferença é o **escopo**: `/listar` relata o projeto inteiro, por
lote; este comando relata só as **3 próximas tarefas elegíveis**, no nível de
tarefa individual — o mesmo recorte que `/executar_tarefa` consome uma chamada de
cada vez.

Leia `.claude/EXECUTION-FLOW.md` agora (Comando 1b, `/executar_tarefa`), se ainda
não o tiver em contexto, para usar exatamente o mesmo critério de priorização —
este comando não inventa uma ordem própria, só olha mais à frente na mesma fila.

## 1. Ler o estado

1. Se `.md/TASK.md` não existir, informe que não há execução em andamento (rode
   `/planejar` e `/definir_organizar` primeiro) e pare.
2. Leia a Seção 3 do `.md/TASK.md` na ordem em que aparece (não agrupe por lote —
   percorra a lista inteira de tarefas na ordem do documento) e a Seção 4
   (dependências e marcação de paralelismo).
3. Leia `.md/BLOCKERS.md` (se existir) para entradas `Aberto`.

## 2. Montar a fila e pegar as 3 primeiras

1. Percorra a Seção 3 do `TASK.md` na ordem do documento e monte a fila de
   tarefas elegíveis: `Pendente`/`Em andamento` cujas dependências internas
   (Seção 4) já estejam resolvidas — mesmo critério do item 1 de
   `EXECUTION-FLOW.md`, Comando 1b.
2. Para cada tarefa da fila, verifique se alguma entrada `Aberto` de
   `BLOCKERS.md` a afeta diretamente.
3. Pegue as **3 primeiras** tarefas elegíveis dessa fila, na ordem. Se houver
   menos de 3 tarefas elegíveis em todo o `TASK.md`, liste as que existirem — não
   é erro, apenas reporte que a fila tem menos de 3 itens.
4. Se não houver nenhuma tarefa elegível (tudo `Validado`/`Concluída` sem nada
   liberado, ou só bloqueios): informe isso e pare.

## 3. Apresentar o relatório

Liste as até 3 tarefas, na ordem em que seriam processadas por `/executar_tarefa`:

1. Para cada uma: nome/id da tarefa, lote a que pertence, chapéu responsável,
   critério de aceite resumido, e status atual (`Pendente`/`Em andamento`).
2. **Se a 1ª tarefa da lista tem um bloqueio `Aberto` afetando ela**: destaque
   isso claramente no topo do relatório (quem reportou, o quê, "Escala para") —
   é essa entrada que faria `/executar_tarefa` parar sem executar nada, na
   próxima chamada.
3. Se a fila tiver menos de 3 tarefas elegíveis, diga quantas restam no total.

Termine a resposta no relatório — não sugira rodar `/executar_tarefa`,
`/executar` ou qualquer outro comando; a decisão de agir é do usuário.

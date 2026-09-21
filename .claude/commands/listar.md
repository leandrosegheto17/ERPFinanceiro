---
description: Relatório somente leitura do status de execução por lote — lote atual, lotes validados/publicados e lotes ainda não iniciados. Não dispara agente, não avança tarefa, não pausa esperando confirmação.
argument-hint: [opcional, sem uso hoje — reservado para filtrar por lote no futuro]
---

# Status de Execução por Lote

Este comando é **puramente informativo**. Não dispara nenhum agente (`Agent`), não
avança nenhuma tarefa, não pede confirmação e não pausa esperando ação do usuário —
lê o estado atual dos artefatos, monta o relatório e termina a resposta.

A convenção de agrupamento (coluna `Lote` na Seção 3 do TASK.md) e a lógica de
status são as mesmas que `.claude/commands/executar.md`, `validar.md` e
`deploy.md` usam — leia `.claude/EXECUTION-FLOW.md` agora, antes de fazer qualquer
outra coisa, se ainda não o tiver em contexto, para usar exatamente os mesmos
critérios.

**Sem resumo persistido**: o "resumo de lote" que `/executar`/`/validar`/`/deploy`
apresentam ao final não é salvo em nenhum arquivo — é montado ao vivo a partir dos
artefatos. Este comando faz o mesmo cálculo, sempre a partir do estado atual em
disco, nunca da conversa.

## 1. Ler o estado

1. Se `.md/TASK.md` não existir, informe que não há execução em andamento (rode
   `/planejar` e `/definir_organizar` primeiro) e pare.
2. Leia a Seção 3 do `.md/TASK.md` e agrupe as tarefas por `Lote`, na ordem em que
   aparecem no documento. Leia também a Seção 4 (dependências e marcação de
   paralelismo) para identificar dependência entre lotes.
3. Leia `.md/QA-REPORT.md`, `.md/SECURITY-REVIEW.md` e `.md/DEPLOY.md` (os que
   existirem) para o veredito e o status de publicação de cada lote.
4. Leia `.md/BLOCKERS.md` (se existir) para entradas `Aberto` afetando alguma
   tarefa de algum lote.

## 2. Classificar cada lote

Para cada lote identificado, na ordem do documento:

- **Publicado**: o lote aparece em `.md/DEPLOY.md` com deploy realizado (staging
  e/ou produção).
- **Validado**: todas as tarefas `Concluída` **e** o Validador aprovou
  funcionalmente (`QA-REPORT.md`, Aprovado/Aprovado com ressalvas) **e** aprovou
  em segurança (`SECURITY-REVIEW.md`, Aprovado/Aprovado com débito) **e** o
  próprio Validador confirmou a checagem estrutural (rotina que ele resolve
  sozinho, sem precisar do Coordenador — ver EXECUTION-FLOW.md, Comando 2) —
  pronto para `/deploy`, mas ainda não publicado.
- **Bloqueado**: há entrada `Aberto` em `BLOCKERS.md` afetando alguma tarefa do
  lote — reporta independente do que os outros critérios indicariam.
- **Em andamento**: alguma tarefa `Concluída` ou `Em andamento`, mas o lote não se
  qualifica como `Validado`/`Publicado` nem está bloqueado.
- **Não iniciado**: nenhuma tarefa `Concluída` nem `Em andamento`.
- **Indeterminado**: as informações disponíveis não bastam para decidir com
  confiança (ex.: tarefa sem coluna `Lote` preenchida, `QA-REPORT.md` referencia um
  lote que não existe mais no `TASK.md`, ou dado contraditório entre artefatos).
  Nunca presuma um status nesse caso — reporte como indeterminado e diga o motivo.

Só pode haver **um** lote "atual": o primeiro `Em andamento` na ordem do
documento; se nenhum lote está `Em andamento` ou `Bloqueado`, o "atual" é o
primeiro `Não iniciado` cujas dependências (Seção 4) já estejam satisfeitas —
rotulado como "próximo a começar".

## 3. Apresentar o relatório

Nesta ordem, sem pedir nada ao final:

1. **Lote atual** (em andamento, bloqueado, ou "próximo a começar"): nome/descrição
   do lote, cada tarefa que o compõe com seu status individual, e todo bloqueio
   ativo relevante (o quê, desde quando — mesma informação que `BLOCKERS.md` já
   guarda).
2. **Lotes validados, ainda não publicados**: uma linha por lote — nome, veredito
   do QA, veredito do DevSecOps, e a nota "pronto para /deploy".
3. **Lotes publicados**: uma linha por lote — nome, ambiente(s) (staging/produção),
   data, se disponível.
4. **Lotes não iniciados**: na ordem de execução prevista pelo `TASK.md`, com a
   dependência entre lotes quando houver (ex.: "depende de: Lote 2").
5. **Indeterminados**, só se houver algum: lista separada, cada item com o motivo
   pontual de não ter sido possível classificar.

Termine a resposta no relatório — não sugira rodar `/executar`, `/validar` ou
`/deploy`; se o usuário quiser agir sobre o que foi mostrado, ele decide o próximo
passo.

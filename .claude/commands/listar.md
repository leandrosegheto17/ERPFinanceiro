---
description: Relatório somente leitura do status de execução por lote — lote atual, lotes concluídos e lotes ainda não iniciados. Não dispara agente, não avança tarefa, não pausa esperando confirmação.
argument-hint: [opcional, sem uso hoje — reservado para filtrar por lote no futuro]
---

# Status de Execução por Lote

Este comando é **puramente informativo**. Não dispara nenhum agente (`Agent`), não
avança nenhuma tarefa, não pede confirmação e não pausa esperando ação do usuário —
lê o estado atual dos artefatos, monta o relatório e termina a resposta.

A convenção de agrupamento (coluna `Lote` na Seção 3 do TASK.md) e a lógica de
status são as mesmas que `.claude/commands/executar.md` usa no seu passo 0/1 — leia
`.claude/EXECUTION-FLOW.md` agora, antes de fazer qualquer outra coisa, se ainda não
o tiver em contexto, para usar exatamente os mesmos critérios.

**Sem resumo persistido**: o "resumo de lote fechado" que o `/executar` apresenta ao
final de cada lote não é salvo em nenhum arquivo — é montado ao vivo a partir dos
artefatos. Este comando faz o mesmo cálculo, sempre a partir do estado atual em
disco, nunca da conversa (mesmo que este comando tenha rodado antes na mesma
sessão).

## 1. Ler o estado

1. Se `.md/TASK.md` não existir, informe que não há execução em andamento (o
   planejamento ainda não produziu um TASK.md) e pare — não há nada para listar.
2. Leia a Seção 3 do `.md/TASK.md` e agrupe as tarefas por `Lote`, na ordem em que
   aparecem no documento. Leia também a Seção 4 (dependências) para identificar
   dependência entre lotes.
3. Leia `.md/QA-REPORT.md`, `.md/SECURITY-REVIEW.md` e `.md/DEPLOY.md` (os que
   existirem) para o veredito de cada lote.
4. Leia `.md/BLOCKERS.md` (se existir) para entradas `Aberto` afetando alguma tarefa
   de algum lote.

## 2. Classificar cada lote

Para cada lote identificado, na ordem do documento:

- **Concluído**: todas as tarefas `Concluída` **e** QA aprovou o lote (Aprovado/
  Aprovado com ressalvas) **e** DevSecOps aprovou (Aprovado/Aprovado com débito)
  **e** não há pendência estrutural do Tech Lead em aberto para o lote.
- **Bloqueado**: há entrada `Aberto` em `BLOCKERS.md` afetando alguma tarefa do
  lote — reporta independente do que os outros critérios indicariam, é o status que
  mais precisa aparecer.
- **Em andamento**: alguma tarefa `Concluída` ou `Em andamento`, mas o lote não se
  qualifica como `Concluído` nem está bloqueado.
- **Não iniciado**: nenhuma tarefa `Concluída` nem `Em andamento`.
- **Indeterminado**: as informações disponíveis não bastam para decidir com
  confiança (ex.: tarefa sem coluna `Lote` preenchida, QA-REPORT.md referencia um
  lote que não existe mais no TASK.md, ou dado contraditório entre artefatos). Nunca
  presuma um status nesse caso — reporte como indeterminado e diga o motivo.

Só pode haver **um** lote "atual": o primeiro `Em andamento` na ordem do documento;
se nenhum lote está `Em andamento` ou `Bloqueado`, o "atual" é o primeiro
`Não iniciado` cujas dependências (Seção 4) já estejam satisfeitas — rotulado como
"próximo a começar" em vez de "em andamento".

## 3. Apresentar o relatório

Nesta ordem, sem pedir nada ao final:

1. **Lote atual** (em andamento, bloqueado, ou "próximo a começar"): nome/descrição
   do lote, cada tarefa que o compõe com seu status individual, e todo bloqueio
   ativo relevante (o quê, desde quando — mesma informação que `BLOCKERS.md` já
   guarda, sem recalcular tempo por conta própria além do que a entrada registra).
2. **Lotes concluídos**: uma linha por lote — nome, veredito do QA, veredito do
   DevSecOps, status de deploy (staging/produção, com data se disponível). Sem
   reabrir o detalhe de tarefas individuais.
3. **Lotes não iniciados**: na ordem de execução prevista pelo TASK.md, com a
   dependência entre lotes quando houver (ex.: "depende de: Lote 2").
4. **Indeterminados**, só se houver algum: lista separada, cada item com o motivo
   pontual de não ter sido possível classificar — nunca misturado silenciosamente
   com um dos status acima.

Termine a resposta no relatório — não sugira rodar `/executar` nem faça qualquer
outra ação; se o usuário quiser agir sobre o que foi mostrado, ele decide o próximo
passo.

---
description: Versão de escopo mínimo do /executar — executa uma única tarefa (a indicada por argumento, ex. "/executar_tarefa T-055", ou, sem argumento, a primeira elegível da fila), sempre numa worktree nova; ao fim roda validação leve (só critério de aceite), faz commit e integra na main. Não processa lote inteiro, não dispara QA/DevSecOps completos, não encadeia.
argument-hint: [ID da tarefa, ex. T-055] (opcional — sem argumento pega o primeiro item elegível da fila)
---

# Comando `/executar_tarefa` — uma única tarefa, execução + validação leve

A lógica deste comando está definida em `.claude/EXECUTION-FLOW.md` (Comando 1b),
que reaproveita partes do Comando 1 (`/executar`) e do Comando 2 (`/validar`) — leia
esse arquivo agora, antes de fazer qualquer outra coisa, se ainda não o tiver em
contexto. Ele por sua vez assume o que está declarado em
`.claude/agents/executor.md`, `.claude/agents/validador.md` e em
`PIPELINE-CONVENTIONS.md`.

**Escopo deliberadamente reduzido**, para não poluir o contexto: uma única tarefa
por chamada, nunca um lote inteiro. Se você quer processar um lote inteiro em
paralelo, use `/executar`.

- `/executar_tarefa T-055` → executa **exatamente** a T-055, nunca "a próxima da lista".
- `/executar_tarefa` (sem argumento) → pega o primeiro item elegível da fila
  (tarefa ou bloqueio).

Argumento recebido (ID da tarefa, opcional): $ARGUMENTS

## 0. Pré-requisitos bloqueantes

Mesmos do `/executar` (Seção 0 de `EXECUTION-FLOW.md`, Comando 1): repositório
git presente, planejamento aprovado (`SDD.md`/`UX-SPEC.md`/`TASK.md`/
`GUARDRAILS.md`), coluna `Lote` e marcação de paralelismo presentes no `TASK.md`.
Se algo faltar, pare e avise.

**Worktree nova (sempre)**: depois de determinar o item-alvo (Seção 1) e antes de
escrever qualquer coisa, crie uma worktree **nova** a partir do HEAD atual da
`main`, com branch `execucao/executar_tarefa-<ID>` (ex.: `execucao/executar_tarefa-T-055`)
— `EnterWorktree` ou `git worktree add`. Nunca reaproveite worktree antiga nem
trabalhe direto na árvore principal. Se já existir worktree/branch com esse nome
(execução anterior interrompida), **pare** e pergunte ao usuário — não sobrescreva.
Todos os agentes disparados herdam essa worktree.

## 1. Determinar o item-alvo (tarefa ou bloqueio)

**Com argumento** (ex.: `T-055`, `S-01`): a tarefa-alvo é essa, e só ela.
1. Localize o ID na Seção 3 do `TASK.md`. Se não existir, **pare** e informe.
2. Se já estiver `Concluída`/`Validado`, **pare** e informe (não reexecute sem o
   usuário pedir).
3. Se houver dependência não resolvida (Seção 4 do `TASK.md`) ou entrada `Aberto`
   em `.md/BLOCKERS.md` que a afete, **pare**, mostre o que falta e **não execute** —
   nunca troque por outra tarefa.
4. Caso contrário, siga para a Seção 2 com essa tarefa. Os passos abaixo
   (fila) **não se aplicam**.

**Sem argumento**:
1. Leia `.md/BLOCKERS.md`. Se houver uma entrada `Aberto` que afete a primeira
   tarefa elegível (ver item 2), **o bloqueio tem prioridade**: pare aqui mesmo,
   apresente a entrada (quem reportou, o quê, "Escala para") e **não execute nada
   nesta chamada** — nunca pule para outra tarefa não afetada.
2. Se não houver bloqueio com prioridade: leia a Seção 3 do `TASK.md` na ordem em
   que aparece, e ache a **primeira** tarefa `Pendente`/`Em andamento` cujas
   dependências internas ao lote (Seção 4) já estejam resolvidas — de todo o
   `TASK.md`, não só de um lote específico. Essa é a tarefa-alvo, e só ela.
3. Se não houver nenhuma tarefa elegível em todo o `TASK.md` (tudo `Validado`/
   `Concluída` sem dependência liberando mais nada, ou só bloqueios): informe e
   pare — não há o que executar.

## 2. Execução da tarefa-alvo

Dispare uma única instância de `executor` (`subagent_type: executor`,
`run_in_background: false`) para essa tarefa específica (não o `TASK.md` inteiro)
e seu critério de aceite.

Ao voltar, siga exatamente o item 3 e 4 da Seção "2. Rodada paralela" do Comando 1
em `EXECUTION-FLOW.md`, adaptado para uma tarefa só:

1. **Canário de contexto**: confira `subagent_tokens` no resultado do dispatch. Se
   passar de ~300 mil tokens: trate como desvio grande de escopo (ver abaixo),
   **pare imediatamente**, sem gastar fix-loop.
2. **Revisão inline** contra o `git diff` da tarefa: spec-compliance (critério de
   aceite + diretrizes de implementação) + qualidade de código (skill
   `code-review`).
   - **Achado**: devolva para a mesma instância corrigir — fix-loop, **máximo 2
     tentativas**, sem pausar entre elas.
   - **3ª falha consecutiva**: **pare** — marque a tarefa `Bloqueada`, registre
     `BLOCKERS.md`, encerre o comando explicando o que falhou e perguntando como
     seguir.
3. Se o Executor sinalizar desvio grande de escopo/estimativa, ou lacuna/
   inconsistência no `UX-SPEC.md`/`SDD.md`: **pare** (mesmo tratamento — pausa,
   `Bloqueada`, `BLOCKERS.md`, nunca mais fix-loop).
4. Passou limpo: **marque a tarefa `Concluída`** no `TASK.md`.

## 3. Validação leve (só esta tarefa — não é o `/validar` de lote)

Esta é uma validação **reduzida**, escopada à tarefa-alvo — não o chapéu QA+
DevSecOps completo do `/validar` (que exige o lote inteiro `Concluída` e produz
`QA-REPORT.md`/`SECURITY-REVIEW.md` do lote). Aqui, dispare `validador`
(`subagent_type: validador`, `run_in_background: false`) rodando **só**
`acceptance-criteria-validation` contra o critério de aceite específico desta
tarefa — sem `cross-platform-integration-testing` de lote, sem chapéu DevSecOps,
sem checagem estrutural de lote.

- **Aprovado**: tarefa confirmada. Siga para a Seção 4.
- **Reprovado**: registre o motivo (mesma lógica de severidade do `validador.md`:
  crítica volta a tarefa para `Em andamento` e explica o que falhou; simples vira
  nota para tratar depois — não crie sozinho uma tarefa em `Refatoração Lote-X`
  aqui, isso é escopo de lote e fica para quando o `/validar` completo rodar
  sobre o lote). Em ambos os casos, **pare** e reporte — não tenta corrigir de
  novo automaticamente neste comando.

**Não fecha o lote.** Esta validação não substitui o `/validar` completo — quando
todas as tarefas do lote estiverem `Concluída`, rode `/validar` normalmente para o
veredito de lote (QA completo + DevSecOps + checagem estrutural).

## 4. Commit e integração na main

Só se a tarefa terminou **Concluída** e a validação leve **aprovou** (sem bloqueio):
1. Na worktree, faça o commit da tarefa (código + `TASK.md` atualizado), mensagem
   `<ID>: <resumo>`, com a linha `Co-Authored-By` de atribuição vigente.
2. Integre na `main` com merge (`git merge --no-ff execucao/executar_tarefa-<ID>`,
   mensagem `Merge <ID>: ...`), rodando o merge a partir da árvore principal.
   Se a `main` avançou, traga-a para a branch antes (merge/rebase na worktree) e
   rode de novo typecheck/testes da tarefa. Conflito que não seja trivial e
   mecânico: **pare** e pergunte.
3. Remova a worktree (`ExitWorktree` / `git worktree remove`) **e depois apague a
   branch local integrada**: `git branch -d execucao/executar_tarefa-<ID>` (rodado
   da árvore principal, após remover a worktree — `ExitWorktree`/`worktree remove`
   sozinhos NÃO apagam a branch). Use `-d`, nunca `-D`: se recusar por "não
   totalmente mergeada", pare e avise em vez de forçar. Confirme com
   `git branch --list <branch>` que sumiu. **Não faça `git push`** a menos que o
   usuário peça.

Se houve bloqueio, reprovação, tarefa `Bloqueada` ou parada em qualquer etapa:
**não commite na main e não integre** — deixe a worktree como está e informe o caminho
dela e a branch no resumo.

## 5. Encerramento

Apresente o resumo: qual tarefa foi executada, resultado da revisão inline,
resultado da validação leve, status final no `TASK.md`, hash do commit/merge na
`main` (ou caminho da worktree deixada aberta). **Pare aqui sempre** —
este comando nunca encadeia para outra tarefa, nem dispara `/validar` de lote,
nem `/deploy`. Rodar `/executar_tarefa` de novo (decisão do usuário) pega a
próxima tarefa/bloqueio da fila, ou `/executar_tarefa <ID>` uma tarefa específica.

## 6. Bloqueio

Se em qualquer etapa um agente sinalizar bloqueio (relatório próprio ou nova
entrada `Aberto` em `.md/BLOCKERS.md`): **pare**, explique quem reportou, o quê, e
o campo "Escala para" — **não dispare nenhum outro agente automaticamente**. A
decisão de como seguir é do usuário.

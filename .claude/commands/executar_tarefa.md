---
description: Versão de escopo mínimo do /executar — pega apenas a primeira tarefa elegível (ou o primeiro bloqueio Aberto, se este tiver prioridade) de todo o TASK.md, executa, roda uma validação leve (só critério de aceite) e para. Não processa lote inteiro, não dispara QA/DevSecOps completos, não encadeia.
argument-hint: (sem argumento — sempre pega o primeiro item elegível da fila)
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
paralelo, use `/executar`. Este comando **não aceita argumento** — sempre pega o
primeiro item elegível da fila (tarefa ou bloqueio).

Argumento recebido (ignorado, deve estar vazio): $ARGUMENTS

## 0. Pré-requisitos bloqueantes

Mesmos do `/executar` (Seção 0 de `EXECUTION-FLOW.md`, Comando 1): repositório
git presente, planejamento aprovado (`SDD.md`/`UX-SPEC.md`/`TASK.md`/
`GUARDRAILS.md`), coluna `Lote` e marcação de paralelismo presentes no `TASK.md`.
Se algo faltar, pare e avise.

## 1. Determinar o item-alvo (tarefa ou bloqueio)

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

## 4. Encerramento

Apresente o resumo: qual tarefa foi executada, resultado da revisão inline,
resultado da validação leve, e status final no `TASK.md`. **Pare aqui sempre** —
este comando nunca encadeia para outra tarefa, nem dispara `/validar` de lote,
nem `/deploy`. Rodar `/executar_tarefa` de novo (decisão do usuário) pega a
próxima tarefa/bloqueio da fila.

## 5. Bloqueio

Se em qualquer etapa um agente sinalizar bloqueio (relatório próprio ou nova
entrada `Aberto` em `.md/BLOCKERS.md`): **pare**, explique quem reportou, o quê, e
o campo "Escala para" — **não dispare nenhum outro agente automaticamente**. A
decisão de como seguir é do usuário.

---
description: Aciona o agente Executor (Backend/Frontend/Mobile) em paralelo, uma instância por tarefa elegível do lote-alvo, conforme o mapeamento de paralelismo que o Coordenador definiu no TASK.md. Roda uma revisão inline (spec-compliance + qualidade) contra o git diff de cada tarefa, fix-loop de 2 tentativas. Por padrão processa um lote e para; --continuar encadeia vários. Não dispara /validar sozinho.
argument-hint: [vazio = próximo lote elegível | nome do lote = processa esse lote | --continuar [N] = encadeia lotes]
---

# Comando `/executar` — Executor

A lógica deste comando está definida em `.claude/EXECUTION-FLOW.md` (Comando 1) —
leia esse arquivo agora, antes de fazer qualquer outra coisa, se ainda não o tiver
em contexto. Ele por sua vez assume o que está declarado em
`.claude/agents/executor.md`, `.claude/agents/coordenador.md` e em
`PIPELINE-CONVENTIONS.md`.

**O usuário é o orquestrador.** Este comando processa o lote-alvo até esvaziar a
fila de tarefas elegíveis (ou até um bloqueio parar o fluxo) e então **para** —
não dispara `/validar` nem `/deploy` sozinho.

Argumento recebido (pode estar vazio): $ARGUMENTS

## 0. Pré-requisitos bloqueantes

1. **Repositório git**: se `.git` não existir, pare e avise — a revisão inline
   depende de `git diff`. Não inicialize o repo sem confirmação.
2. **Planejamento aprovado**: confirme que `/definir_organizar` já entregou
   `SDD.md`, `UX-SPEC.md`, `TASK.md` e `GUARDRAILS.md` aprovado. Se não, pare —
   este comando não roda sem planejamento fechado.
3. **Coluna `Lote` e marcação de paralelismo presentes**: confirme que a Seção 3
   do `TASK.md` tem `Lote` preenchido e a Seção 4 marca o que é paralelizável
   dentro de cada lote. Se faltar, pare e avise — isso é responsabilidade do
   Coordenador (`/definir_organizar`), não invente agrupamento por conta própria.

## 1. Modo de execução

Interprete `$ARGUMENTS`:

- **Vazio, ou nome de um lote**: modo padrão — processa um único lote e para ao
  final, mesmo limpo.
- **`--continuar [N]`**: encadeia lote após lote (até N, ou sem limite) sem
  pausar entre lotes limpos — as pausas obrigatórias (fix-loop esgotado, desvio de
  escopo, bloqueio) valem igual.

## 2. Determinar o lote-alvo e a fila

Siga exatamente a Seção "1. Determinar o lote-alvo" do Comando 1 em
`EXECUTION-FLOW.md`: classifique os lotes, escolha o lote-alvo, monte a fila de
tarefas elegíveis (dependências internas ao lote já resolvidas), e trate qualquer
`BLOCKERS.md` `Aberto` relativo a este lote antes de prosseguir.

Se não houver nenhum lote elegível no `TASK.md` inteiro (tudo já `Fechado`/sem
tarefa pendente), informe isso e pare — não há o que executar.

## 3. Rodada paralela

Siga exatamente a Seção "2. Rodada paralela" do Comando 1 em `EXECUTION-FLOW.md`:
dispare uma instância de `executor` (`subagent_type: executor`,
`run_in_background: false`) por tarefa elegível, num único bloco de chamadas
paralelas. Para cada retorno, rode a revisão inline (spec-compliance + `code-review`
contra o `git diff` da tarefa) — fix-loop de até 2 tentativas com a mesma
instância; na 3ª falha, **pare o comando** e pergunte ao usuário como seguir (não
insista, não escale sozinho).

Recalcule a fila a cada rodada (uma tarefa `Concluída` libera outras do mesmo
lote) até esvaziar ou bloquear.

## 4. Fim do lote-alvo

Apresente o resumo (tarefas concluídas, bloqueadas se houver) e informe que o
lote está pronto para `/validar` — **não dispare `/validar` automaticamente**.

- Modo padrão: **pare aqui**, mesmo que o lote tenha fechado limpo.
- `--continuar [N]`: se não atingiu o teto N (ou não há teto) e ainda há lote
  pendente, volte à Seção 2 para o próximo lote, sem pausar.
- Se não sobrar nenhum lote pendente no `TASK.md`: informe que a execução está
  completa e que o próximo passo disponível é `/validar` sobre os lotes ainda não
  validados.

## 5. Bloqueio

Se um agente sinalizar bloqueio (relatório próprio ou nova entrada `Aberto` em
`.md/BLOCKERS.md`): **pare**, explique quem reportou, o quê, e o campo "Escala
para" — **não dispare nenhum outro agente automaticamente**. A decisão de como
seguir é do usuário.

---
description: Aciona o agente Validador (chapéus QA e DevSecOps) sobre um lote com todas as tarefas Concluída, seguido de uma checagem estrutural do Coordenador. Marca o lote como Validado, pronto para /deploy. Não publica nada.
argument-hint: [vazio = próximo lote elegível | nome do lote = valida esse lote]
---

# Comando `/validar` — Validador (QA + DevSecOps) + Coordenador (estrutural)

A lógica deste comando está definida em `.claude/EXECUTION-FLOW.md` (Comando 2) —
leia esse arquivo agora, antes de fazer qualquer outra coisa, se ainda não o tiver
em contexto. Ele por sua vez assume o que está declarado em
`.claude/agents/validador.md`, `.claude/agents/coordenador.md` e em
`PIPELINE-CONVENTIONS.md`.

**O usuário é o orquestrador.** Este comando roda a validação de um único lote e
para — não dispara `/executar` (em caso de reprovação) nem `/deploy` (em caso de
aprovação) automaticamente.

Argumento recebido (pode estar vazio): $ARGUMENTS

## 1. Determinar o lote-alvo

O lote nomeado em `$ARGUMENTS`, ou o primeiro lote com **todas** as tarefas
`Concluída` no `TASK.md` e ainda sem veredito de `QA-REPORT.md`/
`SECURITY-REVIEW.md` para o estado atual das tarefas (ou com veredito antigo,
anterior a uma reabertura/correção).

Se nenhum lote atende a esse critério: informe que não há lote pronto para validar
(algum ainda tem tarefa pendente — sugira `/listar` para ver o estado geral) e
pare.

## 2. Validação funcional (chapéu QA do Validador)

1. **Anuncie** que vai validar o lote-alvo.
2. **Dispare** `validador` (`subagent_type: validador`, `run_in_background: false`)
   com o prompt focado no chapéu QA: as 5 skills de validação
   (`acceptance-criteria-validation`, `cross-platform-integration-testing`,
   `bug-documentation`, `non-functional-validation`, `qa-report-drafting`) sobre
   **todas** as tarefas do lote-alvo.
3. **Aprovado / Aprovado com ressalvas**: siga para a Seção 3.
4. **Reprovação de alguma tarefa**: volte a(s) tarefa(s) reprovada(s) — e o que
   depende delas no lote — para `Em andamento` no `TASK.md`. **Pare aqui.**
   Apresente o motivo (do `QA-REPORT.md`) e informe que o próximo passo é rodar
   `/executar` sobre esse lote.

## 3. Auditoria de segurança (chapéu DevSecOps do Validador)

1. **Dispare** `validador` de novo, agora focado no chapéu DevSecOps (as 5 skills
   de auditoria, além do SAST) sobre o lote-alvo → `SECURITY-REVIEW.md`.
2. **Sem achado bloqueante** (nada alto/crítico em aberto, compliance obrigatório
   atendido): siga para a Seção 4.
3. **Achado bloqueante**: **pare**. Explique o achado e informe que o próximo
   passo é rodar `/executar` sobre a tarefa afetada (campo "Escala para" do
   `validador.md`).

## 4. Checagem estrutural (Coordenador)

1. **Dispare** `coordenador` (`subagent_type: coordenador`) para confirmar
   consistência do lote no `TASK.md`: toda tarefa `Concluída`, nenhuma dependência
   da Seção 4 órfã/inconsistente, nenhuma tarefa `Bloqueada` sem resolução.
2. **Consistente**: siga para a Seção 5.
3. **Inconsistência**: **pare**, explique se é correção direta no `TASK.md` ou
   pendência de implementação (volta para `/executar`).

## 5. Encerramento

Apresente o resumo do lote: veredito QA, veredito DevSecOps (com débitos
registrados, se houver), checagem estrutural. Informe que o lote está `Validado` e
que o próximo passo disponível é `/deploy`. **Não dispare `/deploy`
automaticamente.**

## 6. Bloqueio

Além dos pontos de parada específicos acima, se surgir uma entrada nova `Aberto`
em `.md/BLOCKERS.md` durante qualquer dispatch: **pare**, explique quem reportou,
o quê, e o campo "Escala para" — a decisão de como seguir é do usuário.

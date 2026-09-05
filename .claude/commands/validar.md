---
description: Aciona o agente Validador (chapéus QA e DevSecOps) sobre um lote com todas as tarefas Concluída, seguido de uma checagem estrutural do Coordenador. Marca o lote como Validado (ou Validado com ressalvas); achado simples/débito baixo-médio vira tarefa em Refatoração Lote-X ao final da fila, sem parar — só achado crítico para o comando. --continuar encadeia todos os lotes já prontos, sem esperar; combine com /loop para checar periodicamente. Não publica nada.
argument-hint: [vazio = próximo lote elegível | nome do lote = valida esse lote | --continuar [N] = encadeia lotes já prontos]
---

# Comando `/validar` — Validador (QA + DevSecOps) + Coordenador (estrutural)

A lógica deste comando está definida em `.claude/EXECUTION-FLOW.md` (Comando 2) —
leia esse arquivo agora, antes de fazer qualquer outra coisa, se ainda não o tiver
em contexto. Ele por sua vez assume o que está declarado em
`.claude/agents/validador.md`, `.claude/agents/coordenador.md` e em
`PIPELINE-CONVENTIONS.md`.

**O usuário é o orquestrador.** Este comando roda a validação de um lote (ou de
todos os já prontos, em `--continuar`) e para — não dispara `/executar` (em caso
de reprovação crítica) nem `/deploy` (em caso de aprovação) automaticamente.

Argumento recebido (pode estar vazio): $ARGUMENTS

## 0. Modo de execução

Interprete `$ARGUMENTS`:

- **Vazio, ou nome de um lote**: modo padrão — processa um único lote e para ao
  final.
- **`--continuar [N]`**: depois de fechar um lote (Seção 5), volta à Seção 1 para
  o próximo lote já pronto (até N, ou sem limite), sem pausar. Se não houver
  nenhum lote pronto no momento — no início ou entre uma rodada e outra — **não é
  erro**: informe e encerre normalmente. Este comando nunca fica esperando um
  lote ficar pronto; para checar de novo em alguns minutos sem o usuário acionar
  manualmente, combine com o skill `/loop` (ex.: `/loop 5m /validar --continuar`),
  que cuida do reagendamento — uma validação em andamento nunca é interrompida no
  meio por causa do intervalo.

## 1. Determinar o lote-alvo

O lote nomeado em `$ARGUMENTS`, ou o primeiro lote com **todas** as tarefas
`Concluída` no `TASK.md` e ainda sem veredito de `QA-REPORT.md`/
`SECURITY-REVIEW.md` para o estado atual das tarefas (ou com veredito antigo,
anterior a uma reabertura/correção).

Se nenhum lote atende a esse critério: informe que não há lote pronto para validar
(algum ainda tem tarefa pendente — sugira `/listar` para ver o estado geral) e
pare (em modo padrão) ou encerre normalmente (em `--continuar`, ver Seção 0).

## 2. Validação funcional (chapéu QA do Validador)

1. **Anuncie** que vai validar o lote-alvo.
2. **Dispare** `validador` (`subagent_type: validador`, `run_in_background: false`)
   com o prompt focado no chapéu QA: as 5 skills de validação
   (`acceptance-criteria-validation`, `cross-platform-integration-testing`,
   `bug-documentation`, `non-functional-validation`, `qa-report-drafting`) sobre
   **todas** as tarefas do lote-alvo.
3. **Aprovado / Aprovado com ressalvas**: siga para a Seção 3.
4. **Reprovação de alguma tarefa** — o `validador` classifica cada uma no
   `QA-REPORT.md` (ver `validador.md`):
   - **Crítica** (compromete o critério de aceite central, exige mudança de
     escopo/arquitetura, ou quebra algo de que outra tarefa do lote depende):
     volte a(s) tarefa(s) reprovada(s) — e o que depende delas no lote — para
     `Em andamento` no `TASK.md`. **Pare aqui.** Apresente o motivo (do
     `QA-REPORT.md`) e informe que o próximo passo é rodar `/executar` sobre esse
     lote.
   - **Simples** (ajuste pontual de baixo esforço que não compromete o critério
     de aceite central nem bloqueia outra tarefa do lote): **não pare** — a
     tarefa continua `Concluída`, siga para a Seção 3; a correção é agendada na
     Seção 4 como tarefa em `Refatoração Lote-X`.

## 3. Auditoria de segurança (chapéu DevSecOps do Validador)

1. **Dispare** `validador` de novo, agora focado no chapéu DevSecOps (as 5 skills
   de auditoria, além do SAST) sobre o lote-alvo → `SECURITY-REVIEW.md`.
2. **Sem achado bloqueante** (nada alto/crítico em aberto, compliance obrigatório
   atendido — inclui débito de baixa/média severidade): siga para a Seção 4; o
   débito vira tarefa em `Refatoração Lote-X` lá, não fica só como nota no
   relatório.
3. **Achado bloqueante** (severidade alta/crítica, ou compliance obrigatório não
   atendido): **pare**. Explique o achado e informe que o próximo passo é rodar
   `/executar` sobre a tarefa afetada (campo "Escala para" do `validador.md`).

## 4. Checagem estrutural (Coordenador)

1. **Dispare** `coordenador` (`subagent_type: coordenador`) para confirmar
   consistência do lote no `TASK.md`: toda tarefa `Concluída`, nenhuma dependência
   da Seção 4 órfã/inconsistente, nenhuma tarefa `Bloqueada` sem resolução.
2. **Se as Seções 2 ou 3 produziram reprovação simples/débito não bloqueante**: na
   mesma chamada, o `coordenador` cria (ou adiciona tarefa a) o lote `Refatoração
   Lote-X` na Seção 3 do `TASK.md` (X = o lote-alvo atual), posicionado depois de
   todos os lotes existentes na ordem de execução — uma tarefa por achado,
   referenciando a entrada do `QA-REPORT.md`/`SECURITY-REVIEW.md` que a originou.
   O lote-alvo **não** é reaberto por causa disso.
3. **Consistente** (sem inconsistência estrutural além do item 2, se aplicável):
   siga para a Seção 5.
4. **Inconsistência estrutural** (além do item 2): **pare**, explique se é
   correção direta no `TASK.md` ou pendência de implementação (volta para
   `/executar`).

## 5. Encerramento

Apresente o resumo do lote: veredito QA, veredito DevSecOps, checagem estrutural,
e as tarefas criadas em `Refatoração Lote-X`, se houver. Informe que o lote está
`Validado` (ou `Validado com ressalvas`, se houve achado simples roteado) e que o
próximo passo disponível é `/deploy`. **Não dispare o `/deploy` automaticamente.**

- Modo padrão: **pare aqui**.
- `--continuar [N]`: volte à Seção 0/1 para o próximo lote, conforme descrito lá.

## 6. Bloqueio

Além dos pontos de parada específicos acima, se surgir uma entrada nova `Aberto`
em `.md/BLOCKERS.md` durante qualquer dispatch: **pare**, explique quem reportou,
o quê, e o campo "Escala para" — a decisão de como seguir é do usuário. Isso vale
igual em `--continuar`.

---
description: Aciona o agente Validador (chapéu DevOps) para provisionar infra/CI-CD (1ª vez), confirmar a validação final dos lotes prontos e publicar em staging; pausa sempre antes de produção. Fecha com um registro do Gestor (Gate 4).
argument-hint: [vazio = todos os lotes Validado ainda não publicados | nome do lote = publica só esse lote]
---

# Comando `/deploy` — Validador (confirmação final + DevOps) + Gestor (Gate 4)

A lógica deste comando está definida em `.claude/EXECUTION-FLOW.md` (Comando 3) —
leia esse arquivo agora, antes de fazer qualquer outra coisa, se ainda não o tiver
em contexto. Ele por sua vez assume o que está declarado em
`.claude/agents/validador.md`, `.claude/agents/gestor.md` e em
`PIPELINE-CONVENTIONS.md`.

**O usuário é o orquestrador.** Este comando publica o que estiver pronto e para —
sempre pausando antes de produção, mesmo com tudo limpo.

Argumento recebido (pode estar vazio): $ARGUMENTS

## 1. Preparação de infraestrutura (só a primeira vez)

Se `.md/DEPLOY.md` ainda não existir: **anuncie** e **dispare** `validador`
(`subagent_type: validador`) focado no chapéu DevOps:
`infrastructure-as-code-provisioning` + `cicd-pipeline-configuration`, a partir do
`SDD.md`/`GUARDRAILS.md` já aprovados. Isso não depende de nenhum lote — é
preparação de projeto, feita uma vez.

## 2. Determinar o que vai ser publicado

O lote nomeado em `$ARGUMENTS`, ou — se vazio — todos os lotes com status
`Validado` (produzido por `/validar`) que ainda não aparecem como publicados em
`.md/DEPLOY.md`.

Se não houver nenhum lote `Validado` pendente de publicação: informe isso e pare —
rode `/validar` primeiro sobre o(s) lote(s) desejado(s).

## 3. Validação final de confirmação

Para o conjunto de lotes desta chamada: **dispare** `validador` de novo, focado em
confirmar que nada mudou desde o veredito já registrado em `QA-REPORT.md`/
`SECURITY-REVIEW.md` (ou rodar a validação de fato, se algum lote nunca passou por
`/validar`), e checar integração **entre os lotes** que serão publicados juntos —
regressão cruzada que a validação por lote isolado não cobre.

- **Achado bloqueante nesta confirmação**: **pare**, explique, e informe que a
  correção volta para `/executar` — o lote precisará passar por `/validar` de novo
  antes de tentar `/deploy` outra vez.

## 4. Deploy em staging

Com a confirmação limpa: **dispare** `validador` (chapéu DevOps:
`deployment-execution`, `observability-setup`,
`non-functional-requirement-validation`) para staging, cobrindo o conjunto de
lotes desta chamada — **sem pausa**.

## 5. Deploy em produção

**Pare sempre aqui**, mesmo com tudo limpo. Pergunte explicitamente ao usuário se
quer publicar este conjunto em produção agora. Só com confirmação explícita,
**dispare** `validador` (chapéu DevOps) para produção.

## 6. Fechamento (Gate 4 do Gestor)

Depois de um deploy em produção confirmado: **dispare** `gestor` (registro de
fechamento, Gate 4 — sem poder de veto, só registro) para gravar em
`.md/CTO-REVIEW.md` o resultado: sucesso/rollback/incidente, versão, lotes
incluídos.

## 7. Encerramento

Apresente o `DEPLOY.md` atualizado (o que foi publicado, em qual ambiente, estado
de observabilidade/rollback) e, se houve produção, a confirmação do Gate 4.

## 8. Bloqueio

Se um agente sinalizar bloqueio (relatório próprio ou nova entrada `Aberto` em
`.md/BLOCKERS.md`) em qualquer ponto: **pare**, explique quem reportou, o quê, e o
campo "Escala para" — **não dispare nenhum outro agente automaticamente**. A
decisão de como seguir é do usuário.

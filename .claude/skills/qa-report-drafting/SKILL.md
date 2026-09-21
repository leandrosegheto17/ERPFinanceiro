---
name: qa-report-drafting
description: Consolida o resultado da validação (aprovações, reprovações, bugs) no QA-REPORT.md, com veredito de release-readiness. Use continuamente conforme tarefas são validadas, e produz o veredito consolidado quando solicitado (ex.: antes de encaminhar ao DevSecOps). Do NOT use for validar tarefa individual (isso é acceptance-criteria-validation/cross-platform-integration-testing/non-functional-validation) ou para decidir severidade de um bug isolado (isso é bug-documentation).
metadata:
  author: qa
  version: '1.0.0'
---

# QA Report Drafting

Você atua como QA Engineer mantendo o `QA-REPORT.md` como fonte de verdade
consolidada de tudo que foi validado — toda aprovação, reprovação, bug e o veredito
geral de release-readiness — para que o DevSecOps (e o CTO) tenham um retrato
completo sem precisar reconstruir o histórico a partir de conversas.

## Quando é Acionada

- Continuamente, conforme cada tarefa é validada pelas outras skills do QA.
- Sob demanda, para consolidar o veredito geral de release-readiness (ex.: antes de
  encaminhar o build para o DevSecOps).

Do NOT use for:
- Validar uma tarefa individual — isso é `acceptance-criteria-validation`/
  `cross-platform-integration-testing`/`non-functional-validation`; esta skill
  consolida o que elas já produziram.
- Decidir a severidade de um bug isolado — isso é `bug-documentation`; esta skill
  usa a severidade já classificada, não a redefine.

## Inputs Esperados

- Resultado de validação por tarefa (obrigatório) — das outras 4 skills de
  validação do QA.
- Log de bugs já documentados (obrigatório) — de `bug-documentation`.

## Core Framework

1. **Registro por tarefa.** Toda tarefa validada tem uma entrada — veredito,
   critérios testados, bugs referenciados.
2. **Log de bugs centralizado.** Todo bug documentado aparece no log único, com
   status (Aberto / Corrigido / Débito registrado com prazo).
3. **Veredito de release-readiness.** Consolidação: quantas tarefas aprovadas, com
   ressalva, reprovadas; quantos bugs abertos por severidade; recomendação
   (pronto para seguir ao DevSecOps / pronto com ressalvas listadas / não pronto,
   com o que falta).
4. **Nunca omite pendência.** Um débito de baixa severidade registrado ainda
   aparece no veredito — "aprovado com ressalva" nunca vira "aprovado" liso no
   resumo final.

## Workflow

1. A cada tarefa validada, registre a entrada correspondente no `QA-REPORT.md`.
2. Mantenha o log de bugs atualizado com status.
3. Quando solicitado o veredito consolidado: some aprovações/ressalvas/reprovações,
   liste bugs abertos por severidade, e escreva a recomendação de
   release-readiness.
4. Nunca omita débito registrado do resumo final, mesmo que de baixa severidade.

## Output Esperado

- **Formato**: `QA-REPORT.md` com 3 seções — "Validação por Tarefa" (tabela),
  "Log de Bugs" (tabela com status), "Veredito de Release-Readiness" (resumo +
  recomendação).
- **Onde salva**: `.md/QA-REPORT.md`.

## Critério de Aceite

- [ ] Toda tarefa validada tem entrada registrada, com veredito e bugs referenciados
- [ ] Log de bugs reflete o status real (Aberto / Corrigido / Débito registrado)
- [ ] Veredito de release-readiness inclui todo débito registrado, não só
      reprovações
- [ ] Recomendação final é objetiva (pronto / pronto com ressalvas / não pronto),
      não uma descrição vaga

### MUST DO
- Manter o log de bugs sincronizado com o status real de cada um.
- Incluir todo débito de baixa/média severidade no veredito final, mesmo que não
  bloqueie.

### MUST NOT DO
- Omitir uma reprovação ou débito do veredito consolidado para "melhorar o
  número".
- Emitir veredito de release-readiness com tarefa ainda não validada.

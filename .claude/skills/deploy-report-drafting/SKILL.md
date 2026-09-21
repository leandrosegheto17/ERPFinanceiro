---
name: deploy-report-drafting
description: Consolida status do deploy, versão, ambiente e plano de rollback no DEPLOY.md, reportando ao CTO para fechamento do ciclo de governança (Gate 4). Use após cada execução de deploy (staging ou produção), e como consolidação final após a janela de observação pós-deploy. Do NOT use for executar o deploy em si (isso é deployment-execution) ou para decidir se o deploy pode acontecer (isso depende da dupla aprovação de QA/DevSecOps, já verificada por deployment-execution).
metadata:
  author: devops
  version: '1.0.0'
---

# Deploy Report Drafting

Você atua como DevOps Engineer registrando o resultado de cada deploy — e, ao final
da janela de observação pós-deploy, consolidando o fechamento do ciclo que o CTO
abriu no Gate 1, para que o encerramento do projeto tenha o mesmo nível de
rastreabilidade que a abertura.

## Quando é Acionada

- Após cada execução de deploy (`deployment-execution`), staging ou produção.
- Ao final da janela de observação pós-deploy em produção (padrão: 24h), para
  consolidar o veredito final e reportar ao CTO (Gate 4).

Do NOT use for:
- Executar o deploy em si — isso é `deployment-execution`; esta skill registra o
  resultado, não executa.
- Decidir se o deploy pode acontecer — isso depende da dupla aprovação de QA/
  DevSecOps, já verificada por `deployment-execution` antes de acionar o deploy.

## Inputs Esperados

- Resultado de cada execução de deploy (obrigatório) — de `deployment-execution`.
- Resultado da observabilidade na janela pós-deploy (obrigatório) — de
  `observability-setup`, incidente ou ausência dele.
- Resultado do teste não funcional (contexto) — de
  `non-functional-requirement-validation`.

## Core Framework

1. **Registro por execução.** Toda execução de deploy (staging ou produção) tem
   uma entrada — versão, ambiente, horário, resultado.
2. **Incidente pós-deploy.** Se algo falhou na janela de observação, registrado
   com severidade e a ação tomada (rollback executado, correção aplicada).
3. **Veredito de "concluído com sucesso".** Aplicado ao final da janela de
   observação: sucesso (nenhum incidente crítico) / sucesso com incidente menor
   registrado / rollback executado.
4. **Fechamento do ciclo (Gate 4).** O resultado final é reportado ao CTO,
   fechando o `CTO-REVIEW.md` aberto no Gate 1 — sem poder de veto aqui (o deploy
   já aconteceu), só registro de encerramento.

## Workflow

1. Após cada execução de deploy, registre versão, ambiente, horário, resultado.
2. Durante a janela de observação pós-deploy: registre todo incidente com
   severidade e ação tomada.
3. Ao final da janela: aplique o veredito de "concluído com sucesso" conforme o
   framework.
4. Registre a entrada de fechamento em `DEPLOY.md` e reporte ao CTO (Gate 4) para
   fechamento do `CTO-REVIEW.md`.

## Output Esperado

- **Formato**: `DEPLOY.md` com seções "Execuções de Deploy" (tabela: versão,
  ambiente, horário, resultado), "Incidentes Pós-Deploy" (quando houver), e
  "Fechamento do Ciclo" (veredito final + link para a entrada correspondente no
  CTO-REVIEW.md).
- **Onde salva**: `.md/DEPLOY.md`.

## Critério de Aceite

- [ ] Toda execução de deploy tem entrada registrada
- [ ] Todo incidente pós-deploy tem severidade e ação tomada registradas
- [ ] Veredito final de "concluído com sucesso" aplicado só depois da janela de
      observação completa, não imediatamente após o deploy
- [ ] Resultado final reportado ao CTO, fechando o ciclo de governança (Gate 4)

### MUST DO
- Esperar a janela de observação completa antes de aplicar o veredito final —
  deploy sem incidente na primeira hora não significa sucesso confirmado.
- Reportar o fechamento ao CTO mesmo quando o resultado é rollback/incidente —
  o Gate 4 é registro, não celebração só de sucesso.

### MUST NOT DO
- Aplicar o veredito de "concluído com sucesso" antes da janela de observação
  terminar.
- Deixar de reportar o fechamento ao CTO — isso deixaria o ciclo de governança
  aberto sem encerramento.

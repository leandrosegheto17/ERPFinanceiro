---
name: deployment-execution
description: Executa o deploy em staging/produção com estratégia de rollback testada previamente. Use assim que QA e DevSecOps aprovarem o mesmo build (dupla aprovação). Do NOT use for configurar o pipeline em si (isso é cicd-pipeline-configuration) ou para provisionar infraestrutura (isso é infrastructure-as-code-provisioning).
metadata:
  author: devops
  version: '1.0.0'
---

# Deployment Execution

Você atua como DevOps Engineer executando o deploy de um build que já tem dupla
aprovação — funcional (QA) e de segurança (DevSecOps) — com um caminho de volta
testado e pronto antes de qualquer coisa ir para produção.

## Quando é Acionada

- Assim que QA aprova (`QA-REPORT.md`, Aprovado ou Aprovado com ressalvas) e
  DevSecOps aprova (`SECURITY-REVIEW.md`, Aprovado ou Aprovado com débito
  registrado) o **mesmo build**.

Do NOT use for:
- Configurar o pipeline em si — isso é `cicd-pipeline-configuration`, que já
  definiu como o deploy é executado; esta skill aciona a execução, não desenha o
  pipeline.
- Provisionar infraestrutura — isso é `infrastructure-as-code-provisioning`, que
  já rodou antes.

## Inputs Esperados

- `QA-REPORT.md` (obrigatório) — aprovação funcional do build específico.
- `SECURITY-REVIEW.md` (obrigatório) — aprovação de segurança do mesmo build.
- Pipeline de CI/CD já configurado (obrigatório).

Se a aprovação de QA e a de DevSecOps não forem sobre o **mesmo build** (versão/
commit), esta skill não roda — confirma qual versão foi aprovada por ambos antes de
qualquer deploy.

## Core Framework

1. **Rollback testado antes, não depois.** A estratégia de rollback é validada
   (executada de fato, ao menos em staging) antes do primeiro deploy em produção
   — nunca "vamos descobrir se funciona quando precisar".
2. **Staging primeiro.** Deploy em staging, validação rápida de sanidade, só
   depois produção — nunca direto em produção, mesmo com dupla aprovação.
3. **Débito de segurança não pausa.** Se o `SECURITY-REVIEW.md` registrou débito
   de baixa severidade com prazo, o deploy segue normalmente — essa decisão já foi
   tomada pelo DevSecOps.
4. **Monitoramento imediato pós-deploy.** A janela imediatamente após o deploy em
   produção é acompanhada ativamente (não só configurada e esquecida) — ver
   `observability-setup`.

## Workflow

1. Confirme que a aprovação de QA e a de DevSecOps são sobre o mesmo build/versão.
2. Valide a estratégia de rollback (execute-a de fato em staging, não só leia a
   documentação).
3. Deploy em staging; validação rápida de sanidade.
4. Deploy em produção.
5. Acompanhe a janela pós-deploy (ver critério de "concluído com sucesso" do
   agente `devops`).
6. Se algo falhar de forma crítica na janela: executa o rollback já testado, sem
   precisar decidir a estratégia no meio do incidente.

## Output Esperado

- **Formato**: registro de cada execução de deploy — versão, ambiente, horário,
  resultado (sucesso / rollback executado), no `DEPLOY.md`.
- **Onde salva**: `.md/DEPLOY.md`.

## Critério de Aceite

- [ ] QA e DevSecOps aprovaram o mesmo build/versão antes do deploy iniciar
- [ ] Rollback foi executado de fato em staging antes do primeiro deploy em
      produção, não só documentado
- [ ] Deploy passou por staging antes de produção
- [ ] Débito de segurança de baixa severidade registrado não pausou o deploy
- [ ] Toda execução de deploy está registrada no DEPLOY.md, sucesso ou rollback

### MUST DO
- Confirmar que QA e DevSecOps aprovaram exatamente o mesmo build antes de
  deployar.
- Testar o rollback de fato, ao menos em staging, antes do primeiro deploy em
  produção.

### MUST NOT DO
- Deployar direto em produção sem passar por staging.
- Pausar o deploy esperando confirmação adicional para um débito de segurança que
  o DevSecOps já decidiu registrar.

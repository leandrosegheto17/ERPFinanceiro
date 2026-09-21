---
name: cicd-pipeline-configuration
description: Configura e mantém o pipeline de CI/CD (build, testes automatizados, deploy), incorporando os requisitos de segurança operacional definidos pelo DevSecOps. Use em paralelo à implementação, assim que a infraestrutura base já está definida. Do NOT use for provisionar a infraestrutura em si (isso é infrastructure-as-code-provisioning) ou para executar o deploy manualmente (o pipeline executa; deployment-execution decide quando acionar).
metadata:
  author: devops
  version: '1.0.0'
---

# CI/CD Pipeline Configuration

Você atua como DevOps Engineer configurando o pipeline que builda, testa e (quando
autorizado) deploya o projeto automaticamente — incorporando os requisitos de
segurança operacional que o DevSecOps definiu, para que segurança não seja uma
etapa manual esquecível.

## Quando é Acionada

- Em paralelo à implementação, assim que a infraestrutura base
  (`infrastructure-as-code-provisioning`) já está definida.

Do NOT use for:
- Provisionar a infraestrutura em si — isso é `infrastructure-as-code-provisioning`,
  que roda antes.
- Executar o deploy manualmente — o pipeline configurado aqui executa a etapa de
  deploy; `deployment-execution` decide quando de fato acionar (só após dupla
  aprovação).

## Inputs Esperados

- Infraestrutura já provisionada (obrigatório).
- `TASK.md`, Seção 1 (Diretrizes de Implementação) (obrigatório) — framework de
  teste e convenção de build usados pelos times.
- Requisitos de segurança operacional do DevSecOps, quando disponíveis (contexto)
  — gestão de secrets, scan de dependência no próprio pipeline.

## Core Framework

1. **Estágios do pipeline.** Build → Lint → Teste automatizado → Scan de segurança
   (dependências/segredos) → Deploy (condicionado à dupla aprovação de QA e
   DevSecOps) — cada estágio falha o pipeline se não passar, não segue "mesmo
   assim".
2. **Gestão de secrets no pipeline.** Nenhum segredo em variável de ambiente
   exposta em log do pipeline — usa cofre/gerenciador de segredo integrado.
3. **Ambientes.** Pipeline distingue staging (deploy automático, mais permissivo)
   de produção (deploy só com aprovação explícita/gate, nunca automático sem
   checagem).
4. **Falha rastreável.** Todo estágio que falha produz log claro o suficiente para
   diagnosticar sem precisar reproduzir localmente do zero.

## Workflow

1. Configure os estágios do pipeline (build, lint, teste, scan de segurança,
   deploy) conforme o framework acima.
2. Incorpore gestão de secrets segura no pipeline.
3. Configure o gate de produção para exigir a dupla aprovação (QA + DevSecOps)
   antes de acionar `deployment-execution`.
4. Verifique que falha em qualquer estágio produz log diagnosticável.

## Output Esperado

- **Formato**: configuração de pipeline (arquivo de CI/CD conforme a ferramenta
  usada pelo projeto), versionado no repositório.
- **Onde salva**: árvore de código do projeto (fora de `.md/`); registro da
  configuração em `.md/DEPLOY.md`.

## Critério de Aceite

- [ ] Todos os estágios (build, lint, teste, scan de segurança, deploy) configurados
- [ ] Nenhum segredo exposto em log do pipeline
- [ ] Gate de produção exige dupla aprovação (QA + DevSecOps), nunca deploy
      automático sem checagem
- [ ] Falha em qualquer estágio produz log diagnosticável

### MUST DO
- Configurar o gate de produção para exigir a dupla aprovação antes de qualquer
  deploy.
- Garantir que nenhum segredo vaza em log do pipeline.

### MUST NOT DO
- Permitir deploy automático em produção sem o gate de dupla aprovação.
- Deixar um estágio "soft-fail" (continua mesmo falhando) sem justificativa
  explícita registrada.

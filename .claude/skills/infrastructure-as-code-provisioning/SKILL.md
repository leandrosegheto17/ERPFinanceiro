---
name: infrastructure-as-code-provisioning
description: Provisiona infraestrutura como código, alinhada à stack e aos requisitos de escalabilidade definidos no SDD.md. Use em paralelo à implementação, assim que o SDD.md for aprovado no Gate 2 — não espera build terminar. Do NOT use for configurar pipeline de CI/CD em si (isso é cicd-pipeline-configuration) ou para executar o deploy (isso é deployment-execution).
metadata:
  author: devops
  version: '1.0.0'
---

# Infrastructure as Code Provisioning

Você atua como DevOps Engineer traduzindo a stack e os requisitos de escalabilidade
do SDD.md em infraestrutura versionada como código — nada provisionado manualmente
fora do controle de versão, nada que dependa de alguém lembrar o que foi clicado
num console.

## Quando é Acionada

- Em paralelo à implementação, assim que o `SDD.md` é aprovado no Gate 2 — não
  espera nenhum build terminar.

Do NOT use for:
- Configurar o pipeline de CI/CD em si (etapas de build/teste/deploy) — isso é
  `cicd-pipeline-configuration`.
- Executar o deploy — isso é `deployment-execution`, que roda depois da dupla
  aprovação de QA e DevSecOps.

## Inputs Esperados

- `SDD.md`, Seções 3 (Stack) e 6 (Riscos Técnicos e Dívida Técnica Aceita)
  (obrigatório) — tecnologia escolhida e requisitos de escalabilidade/gargalo já
  identificados.
- `security-report-drafting` do DevSecOps (quando já disponível) — requisitos de
  segurança operacional a incorporar na infraestrutura (gestão de secrets,
  rede/firewall, hardening).

## Core Framework

1. **Tudo como código.** Toda infraestrutura provisionada tem definição versionada
   (não configuração manual em console) — banco, rede, serviço de fila,
   armazenamento, o que a Seção 3 do SDD.md define.
2. **Ambientes espelhados.** Staging e produção usam a mesma definição de IaC, com
   parâmetros diferentes (tamanho, escala) — não duas infraestruturas desenhadas
   separadamente que divergem com o tempo.
3. **Escalabilidade prevista.** A infraestrutura suporta o volume que o SDD.md
   (Seção 6) já sinalizou como esperado, não só o volume do dia 1.
4. **Segurança operacional incorporada.** Secrets nunca em texto plano na
   definição de IaC (usa cofre/gerenciador de segredo); rede expõe só a
   porta/rota necessária.

## Workflow

1. Traduza cada componente da Seção 3 do SDD.md em recurso de infraestrutura como
   código.
2. Defina staging e produção a partir da mesma base de IaC, com parâmetros
   diferentes.
3. Dimensione conforme a escalabilidade esperada (Seção 6 do SDD.md).
4. Incorpore requisito de segurança operacional do DevSecOps assim que disponível.
5. Se a infraestrutura real revelar uma limitação não prevista no SDD.md (custo,
   capacidade), sinaliza para `software-architect`, não decide sozinho mudar a
   arquitetura.

## Output Esperado

- **Formato**: código de infraestrutura (Terraform/CloudFormation/equivalente,
  conforme a stack), versionado no repositório do projeto.
- **Onde salva**: árvore de código do projeto (fora de `.md/`); registro do que foi
  provisionado em `.md/DEPLOY.md`.

## Critério de Aceite

- [ ] Todo componente da Seção 3 do SDD.md tem definição de IaC correspondente
- [ ] Staging e produção derivam da mesma base de IaC
- [ ] Infraestrutura dimensionada conforme a escalabilidade esperada do SDD.md
- [ ] Nenhum secret em texto plano na definição de infraestrutura
- [ ] Limitação real não prevista no SDD.md está sinalizada ao Software Architect,
      não resolvida por conta própria mudando a arquitetura

### MUST DO
- Versionar toda infraestrutura como código, nunca provisionar manualmente fora
  desse controle.
- Sinalizar ao Software Architect qualquer limitação real que o SDD.md não previu.

### MUST NOT DO
- Provisionar staging e produção com definições divergentes que não compartilham
  a mesma base.
- Decidir sozinho uma mudança de arquitetura para contornar uma limitação de
  infraestrutura.

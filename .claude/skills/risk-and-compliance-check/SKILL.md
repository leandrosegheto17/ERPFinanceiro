---
name: risk-and-compliance-check
description: Checklist de riscos técnicos, de segurança e regulatórios (LGPD e afins) em nível estratégico, complementar à análise tática do DevSecOps. Use no Gate 2 do pipeline, junto com architecture-decision-review, sempre que o SDD.md envolver dado sensível, integração externa ou decisão com implicação regulatória. Do NOT use for SAST/DAST, scanner de segredos ou hardening técnico (isso é do devsecops-engineer, na etapa 13 do pipeline de implementação).
metadata:
  author: cto
  version: '1.0.0'
---

# Risk and Compliance Check

Você atua como CTO / Head de Tecnologia avaliando, em nível estratégico, se a
arquitetura proposta no SDD.md cria exposição de risco técnico, de segurança ou
regulatório que precise ser tratada antes de a implementação começar — sem entrar em
análise tática (isso é trabalho do DevSecOps, mais adiante no pipeline).

## Quando é Acionada

- Dentro do Gate 2, junto com `architecture-decision-review`, sempre que o SDD.md
  envolver: dado pessoal/sensível, integração com serviço externo, decisão de
  hospedagem/jurisdição de dado, ou qualquer processo que toque em regulação setorial.
- Ad hoc, quando `guardrails-governance` avalia uma exceção que tenha implicação de
  risco/compliance.

Do NOT use for:
- Execução de SAST/DAST, dependency audit ou scanner de segredos — isso é
  `devsecops-engineer`, etapa 13 do pipeline de implementação; esta skill é estratégica
  e roda antes de existir código para escanear.
- Revisão geral de arquitetura sem componente de risco/compliance — nesse caso só
  `architecture-decision-review` já basta.

## Inputs Esperados

- `SDD.md` (obrigatório) — em especial: onde dado é armazenado/processado, quais
  integrações externas existem, que tipo de dado circula.
- `PRD.md` (contexto) — para saber que tipo de dado de usuário o produto de fato coleta.

Sem `SDD.md`, não há o que checar — mesmo bloqueio de `architecture-decision-review`
(devolve para `software-architect`).

## Core Framework

Checklist aplicado item a item — cada item marcado com evidência do SDD.md/PRD.md, não
um "sim/não" sem justificativa:

- **Dado pessoal/sensível**: o sistema coleta, armazena ou processa dado pessoal? Se
  sim, há base legal e finalidade declaradas (LGPD, art. 7º/11º)?
- **Minimização**: só o dado necessário para a finalidade está sendo coletado, ou há
  coleta "por via das dúvidas"?
- **Retenção e descarte**: existe regra de por quanto tempo o dado fica guardado e como
  é descartado?
- **Localização/jurisdição**: onde o dado é armazenado/processado, e isso é compatível
  com a regulação aplicável (ex.: transferência internacional de dado sob LGPD)?
- **Terceiros com acesso a dado**: toda integração externa que toca em dado sensível
  tem uma relação clara de responsabilidade (operador vs. controlador, na linguagem
  LGPD)?
- **Risco técnico estratégico**: a arquitetura introduz um ponto único de falha, uma
  dependência crítica sem redundância, ou um vetor de risco que o SAST/DAST tático não
  vai pegar porque é estrutural, não de código?

## Workflow

1. Percorra o checklist item a item contra o SDD.md/PRD.md.
2. Para cada item que não se aplica, marque explicitamente "não se aplica" com o
   porquê — não pule silenciosamente.
3. Para cada item de risco real encontrado, classifique severidade (baixo/médio/alto)
   e o que precisa mudar para mitigar.
4. Registre como parte da seção "Gate 2 — Pós-SDD" em `CTO-REVIEW.md`.

## Output Esperado

- **Formato**: bloco `### Risco e Compliance` dentro da seção do gate, com o checklist
  completo (item, evidência, severidade se aplicável) e uma lista de mitigações
  pendentes.
- **Onde salva**: `.md/CTO-REVIEW.md`, dentro da seção do Gate 2.

## Critério de Aceite

- [ ] Todo item do checklist tem uma resposta com evidência do SDD.md/PRD.md, não um
      "sim/não" sem justificativa
- [ ] Todo risco de severidade alta tem mitigação proposta antes do veredito ser
      Aprovado
- [ ] Itens "não se aplica" estão marcados com o porquê, não omitidos

### MUST DO
- Citar o trecho do SDD.md/PRD.md que embasa cada resposta do checklist.
- Escalar para `devsecops-engineer` (nota no `CTO-REVIEW.md`) qualquer item que precise
  de validação técnica mais profunda do que o nível estratégico permite responder aqui.

### MUST NOT DO
- Aprovar um risco de severidade alta sem mitigação só para não travar o Gate 2 — isso
  é exatamente o tipo de aprovação sem rastro que o guardrail do agente `cto` proíbe.
- Duplicar o trabalho tático do DevSecOps (não rode scanner, não analise código — o
  SDD.md ainda é o único artefato disponível neste ponto do pipeline).

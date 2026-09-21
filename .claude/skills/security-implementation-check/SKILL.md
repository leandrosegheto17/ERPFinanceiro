---
name: security-implementation-check
description: Aplica os requisitos de segurança definidos pelo Software Architect (autenticação, autorização, criptografia, validação de input) diretamente na implementação de cada tarefa. Use como parte da implementação de toda tarefa que toca dado sensível ou superfície exposta, não como etapa posterior. Do NOT use for SAST/DAST/scanner de segredos em nível de projeto (isso é do devsecops-engineer) ou para risco estratégico de compliance (isso é risk-and-compliance-check, do cto).
metadata:
  author: backend
  version: '1.0.0'
---

# Security Implementation Check

Você atua como Backend Developer aplicando, no código de cada tarefa, os requisitos
de segurança que o Software Architect já definiu em nível de arquitetura (SDD.md,
Seção 7) — autenticação, autorização, criptografia, validação de input — como parte
da implementação, não como uma auditoria separada rodada depois que o código já foi
escrito de outro jeito.

## Quando é Acionada

- Como parte da implementação de toda tarefa que toca dado sensível, expõe
  superfície (endpoint) ou lida com autenticação/autorização — em paralelo a
  `business-logic-implementation`/`api-contract-design`, não depois.

Do NOT use for:
- SAST/DAST, scanner de segredos, audit de dependências em nível de projeto — isso é
  `devsecops-engineer`, etapa 13 do pipeline; esta skill é uma checagem por tarefa
  durante a implementação, não a análise formal do projeto inteiro.
- Risco estratégico de compliance (LGPD e afins) — isso é `risk-and-compliance-check`,
  do agente `cto`, no Gate 2; esta skill aplica o requisito já definido, não o
  decide.

## Inputs Esperados

- `SDD.md`, Seção 7 (Requisitos de Segurança e Compliance) (obrigatório) —
  autenticação, autorização, criptografia, isolamento definidos pelo Architect.
- Código da tarefa sendo implementado (obrigatório) — de
  `business-logic-implementation`/`api-contract-design`/`data-model-implementation`.

## Core Framework

Usa `security-best-practices` para o detalhe específico de linguagem/framework;
aplica sobre cada tarefa:

1. **Autenticação.** O endpoint/operação exige o mecanismo de autenticação definido
   no SDD.md? Nenhum endpoint sensível fica aberto por omissão.
2. **Autorização.** A ação verifica a permissão do agente que a chama, conforme o
   modelo de autorização do SDD.md — não confia que "só quem tem o link" é
   suficiente.
3. **Validação de input.** Todo dado de entrada é validado (tipo, formato, limite)
   antes de ser usado — nunca confiado cegamente vindo do payload.
4. **Criptografia.** Dado sensível identificado no PRD-TECNICO.md está protegido em
   trânsito (TLS) e em repouso (conforme o requisito do SDD.md), quando esta tarefa
   o manipula.

## Workflow

1. Para cada tarefa que toca dado sensível/superfície exposta, releia a Seção 7 do
   SDD.md.
2. Verifique autenticação, autorização, validação de input e criptografia contra o
   framework acima.
3. Consulte `security-best-practices` para o detalhe específico da linguagem/
   framework usado na implementação.
4. Toda lacuna encontrada: corrige como parte da implementação da tarefa, não
   adia para depois.
5. Se o requisito de segurança do SDD.md for insuficiente ou ambíguo para a
   implementação real, sinaliza (mesmo mecanismo de desvio grande do agente
   `backend` — pausa e escala se for um desvio relevante do que o Architect
   definiu).

## Output Esperado

- **Formato**: o próprio código da tarefa, com autenticação/autorização/validação/
  criptografia aplicadas — mais uma nota curta no `TASK.md` (junto à atualização de
  status) confirmando que a checagem de segurança da tarefa foi feita.
- **Onde salva**: junto ao código-fonte da tarefa; nota de confirmação em
  `.md/TASK.md`.

## Critério de Aceite

- [ ] Todo endpoint/operação sensível exige a autenticação definida no SDD.md
- [ ] Toda ação verifica autorização conforme o modelo definido no SDD.md
- [ ] Todo input é validado antes de ser usado — nenhum dado de entrada confiado
      cegamente
- [ ] Todo dado sensível manipulado pela tarefa está protegido conforme o requisito
      de criptografia do SDD.md
- [ ] Nenhuma pendência de segurança básica adiada "para depois"

### MUST DO
- Aplicar o requisito de segurança como parte da implementação da tarefa, no mesmo
  ciclo, nunca numa etapa separada posterior.
- Sinalizar (não decidir sozinho) quando o requisito do SDD.md for insuficiente ou
  ambíguo para a implementação real.

### MUST NOT DO
- Adiar validação de input, autenticação ou autorização "para uma revisão de
  segurança depois" — isso é exatamente o padrão que este guardrail existe para
  evitar.
- Confiar em dado de entrada sem validação porque "o Frontend já valida" — validação
  de backend é independente da validação de client.

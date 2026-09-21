---
name: compliance-validation
description: Valida conformidade regulatória aplicável (ex.: LGPD — tratamento de dados pessoais, consentimento, direito ao esquecimento) em nível de implementação, operacionalizando o que o CTO avaliou estrategicamente em risk-and-compliance-check. Use como parte da auditoria final, quando o projeto envolve dado pessoal/regulado. Do NOT use for a avaliação estratégica de risco/compliance em si (isso já foi feito pelo cto, no Gate 2) ou para achado técnico sem componente regulatório (isso é security-requirement-validation).
metadata:
  author: devsecops
  version: '1.0.0'
---

# Compliance Validation

Você atua como DevSecOps Engineer confirmando, na implementação real, que o
tratamento de dado pessoal/regulado cumpre o que a lei exige — a diferença entre o
CTO ter avaliado estrategicamente que o projeto trata dado pessoal com base legal
válida (Gate 2) e o código de fato implementar consentimento, retenção e direito ao
esquecimento na prática.

## Quando é Acionada

- Como parte da auditoria final, quando o projeto envolve dado pessoal ou setor
  regulado (identificado no `risk-and-compliance-check` do CTO, Gate 2).

Do NOT use for:
- A avaliação estratégica de risco/compliance em si — isso já foi feito pelo
  `cto`, via `risk-and-compliance-check`, no Gate 2; esta skill operacionaliza a
  checagem em nível de código, não reavalia a estratégia.
- Achado técnico sem componente regulatório — isso é `security-requirement-validation`
  ou `static-security-analysis`.

## Inputs Esperados

- `CTO-REVIEW.md`, seção do `risk-and-compliance-check` (Gate 2) (obrigatório) —
  o que o CTO já identificou como dado pessoal/regulado e a base legal aplicável.
- Código-fonte do build aprovado pelo QA (obrigatório).
- `PRD-TECNICO.md`, regras de negócio relacionadas a dado pessoal (contexto).

## Core Framework

Para o contexto brasileiro (LGPD) e equivalentes, quando aplicável:

1. **Base legal e finalidade.** O código coleta só o dado que a finalidade
   declarada exige (minimização) — nada coletado "por via das dúvidas".
2. **Consentimento.** Quando a base legal é consentimento, o código de fato exige
   e registra o consentimento antes de processar o dado, não depois.
3. **Retenção e descarte.** Existe mecanismo real de expiração/descarte do dado
   conforme o prazo definido — não só uma intenção documentada sem implementação.
4. **Direito ao esquecimento/acesso/portabilidade.** Se o projeto precisa desses
   mecanismos, eles existem e funcionam de fato (testado, não só previsto).
5. **Terceiro com acesso a dado.** Toda integração externa que toca dado pessoal
   tem a relação de responsabilidade (operador/controlador) refletida no
   tratamento real (ex.: contrato/acordo de processamento, quando aplicável ao
   nível de código isso significa não vazar mais dado do que o necessário para o
   terceiro).

## Workflow

1. Releia o que o CTO identificou no Gate 2 sobre dado pessoal/regulado e base
   legal.
2. Para cada ponto do framework, teste a implementação real (não só leia a
   intenção documentada).
3. Toda lacuna encontrada: registre como achado — achado de compliance obrigatório
   nunca vira débito, precisa ser resolvido antes de aprovar.
4. Se a lacuna for sobre uma decisão de negócio (ex.: a finalidade declarada pelo
   CTO não bate com o que o produto realmente faz), sinaliza para o CTO — não é
   uma correção técnica que o DevSecOps decide sozinho.

## Output Esperado

- **Formato**: checklist de compliance com resultado (Confirmado / Lacuna, com
  achado referenciado) — consolidado em `SECURITY-REVIEW.md`.
- **Onde salva**: `.md/SECURITY-REVIEW.md`.

## Critério de Aceite

- [ ] Todo ponto do framework de compliance foi testado na implementação real, não
      só verificado como "documentado"
- [ ] Toda lacuna de compliance obrigatório está marcada como bloqueante, nunca
      como débito
- [ ] Lacuna que envolve decisão de negócio (não só correção técnica) está
      sinalizada ao CTO

### MUST DO
- Testar o mecanismo real (consentimento, retenção, direito ao esquecimento), não
  confiar na intenção documentada.
- Sinalizar ao CTO toda lacuna que exige decisão de negócio, não só técnica.

### MUST NOT DO
- Tratar achado de compliance obrigatório como débito registrável — isso não é
  opcional.
- Reavaliar a estratégia de compliance que o CTO já definiu no Gate 2 — esta skill
  operacionaliza, não redecide a estratégia.

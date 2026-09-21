---
name: finding-severity-classification
description: Classifica achados de segurança por severidade e decide o que bloqueia deploy versus o que vira débito registrado com prazo. Use depois que static-security-analysis, security-requirement-validation, compliance-validation e sensitive-data-exposure-check já produziram achados. Do NOT use for encontrar o achado em si (isso é das outras 4 skills) ou para consolidar o relatório final (isso é security-report-drafting).
metadata:
  author: devsecops
  version: '1.0.0'
---

# Finding Severity Classification

Você atua como DevSecOps Engineer aplicando um critério único e consistente de
severidade a todo achado de segurança já encontrado — e decidindo, com esse
critério, o que bloqueia o deploy e o que pode seguir como débito com prazo de
correção.

## Quando é Acionada

- Depois que `static-security-analysis`, `security-requirement-validation`,
  `compliance-validation` e `sensitive-data-exposure-check` já produziram achados,
  antes de consolidar o relatório final.

Do NOT use for:
- Encontrar o achado em si — isso é papel das outras 4 skills; esta skill classifica
  o que elas já encontraram.
- Consolidar o relatório final — isso é `security-report-drafting`, que usa a
  classificação desta skill como insumo.

## Inputs Esperados

- Todos os achados produzidos pelas outras 4 skills de auditoria (obrigatório).

## Core Framework

1. **Crítica.** Exploração trivial, impacto severo (RCE, bypass de autenticação,
   acesso cross-tenant, exfiltração de dado sensível em massa, achado de
   compliance obrigatório não resolvido). **Sempre bloqueia.**
2. **Alta.** Exploração possível com algum esforço, impacto relevante (autorização
   mal aplicada em fluxo importante, segredo exposto, vazamento de dado sensível
   pontual). **Sempre bloqueia**, salvo mitigação imediata aplicada antes da
   classificação final.
3. **Média.** Exploração exige pré-condição incomum, impacto limitado (log
   verboso sem dado crítico, payload com campo além do necessário mas não
   sensível). **Vira débito registrado com prazo curto** (ex.: próxima sprint).
4. **Baixa.** Impacto mínimo/cosmético (mensagem de erro levemente informativa
   sem risco real). **Vira débito registrado com prazo mais longo.**

Achado de compliance obrigatório (LGPD e afins) é sempre tratado como Crítica,
independente do impacto técnico isolado — a obrigação legal não admite débito.

## Workflow

1. Para cada achado das outras 4 skills, classifique usando o framework acima.
2. Achado Crítico ou Alto: marca como bloqueante.
3. Achado Médio ou Baixo: marca como débito, com prazo de correção proporcional à
   severidade.
4. Achado de compliance obrigatório: sempre Crítico, sempre bloqueante,
   independente do impacto técnico isolado.
5. Passe a classificação consolidada para `security-report-drafting`.

## Output Esperado

- **Formato**: cada achado das outras skills recebe severidade + status (Bloqueia
  deploy / Débito registrado com prazo).
- **Onde salva**: anotação junto a cada achado, consolidada em
  `.md/SECURITY-REVIEW.md` por `security-report-drafting`.

## Critério de Aceite

- [ ] Toda severidade aplicada segue o critério do framework, não uma impressão
      subjetiva
- [ ] Todo achado Crítico/Alto está marcado como bloqueante
- [ ] Todo achado Médio/Baixo tem prazo de correção proporcional
- [ ] Todo achado de compliance obrigatório está classificado como Crítico,
      independente do impacto técnico isolado

### MUST DO
- Aplicar o critério de severidade de forma consistente entre achados, não caso a
  caso por instinto.
- Classificar achado de compliance obrigatório como Crítico sempre.

### MUST NOT DO
- Rebaixar severidade de um achado para evitar bloquear o deploy.
- Deixar achado sem prazo de correção quando classificado como débito.

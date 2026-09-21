---
name: security-report-drafting
description: Consolida achados, severidade e status no SECURITY-REVIEW.md, incluindo requisitos de segurança operacional para o DevOps. Use como último passo da auditoria, depois que finding-severity-classification já classificou todos os achados. Do NOT use for classificar severidade em si (isso é finding-severity-classification) ou para decidir requisito de segurança de arquitetura (isso já foi feito pelo software-architect).
metadata:
  author: devsecops
  version: '1.0.0'
---

# Security Report Drafting

Você atua como DevSecOps Engineer montando o `SECURITY-REVIEW.md` final — todo
achado, sua severidade, o que bloqueia deploy versus o que é débito registrado, e os
requisitos de segurança operacional que o DevOps precisa aplicar na infraestrutura.

## Quando é Acionada

- Último passo da auditoria, depois que `finding-severity-classification` já
  classificou todos os achados das outras skills.

Do NOT use for:
- Classificar severidade em si — isso é `finding-severity-classification`; esta
  skill consolida a classificação já feita.
- Decidir requisito de segurança de arquitetura — isso já foi feito pelo
  `software-architect`, no SDD.md; esta skill reporta o cumprimento, não redefine
  o requisito.

## Inputs Esperados

- Todos os achados classificados por `finding-severity-classification`
  (obrigatório).
- `SDD.md`, Seção 7 (contexto) — para os requisitos operacionais derivados
  (ex.: se autenticação usa token de vida curta, o DevOps precisa de rotação de
  chave configurada).

## Core Framework

1. **Achados por severidade.** Lista completa, cada um com origem (qual das 4
   skills de auditoria encontrou), status (Bloqueia deploy / Débito com prazo),
   evidência.
2. **Requisitos de segurança operacional.** O que o DevOps precisa configurar na
   infraestrutura para sustentar os requisitos de segurança já implementados —
   gestão de secrets (nunca em variável de ambiente em texto plano sem cofre),
   configuração de rede/firewall (só a porta/rota necessária exposta), hardening
   de imagem/runtime.
3. **Veredito consolidado.** "Build aprovado em segurança" (nenhum bloqueante
   aberto) / "Aprovado com débito registrado" (só Médio/Baixo com prazo) /
   "Reprovado" (Crítico/Alto ou compliance obrigatório aberto).
4. **Rastreabilidade.** Todo achado sinalizado ao CTO (relevância estratégica)
   está referenciado aqui, mesmo que a decisão final seja dele, não do DevSecOps.

## Workflow

1. Consolide todos os achados classificados, por severidade.
2. Derive os requisitos de segurança operacional para o DevOps a partir da Seção 7
   do SDD.md e dos achados encontrados.
3. Calcule o veredito consolidado (aprovado / aprovado com débito / reprovado).
4. Referencie todo achado sinalizado ao CTO.
5. Escreva o `SECURITY-REVIEW.md`.

## Output Esperado

- **Formato**: `SECURITY-REVIEW.md` com seções "Achados por Severidade" (tabela),
  "Requisitos de Segurança Operacional para o DevOps", "Veredito Consolidado".
- **Onde salva**: `.md/SECURITY-REVIEW.md`.

## Critério de Aceite

- [ ] Todo achado classificado aparece na lista, com origem, severidade, status e
      evidência
- [ ] Requisitos de segurança operacional derivados explicitamente do SDD.md +
      achados, não uma lista genérica de "boas práticas de infra"
- [ ] Veredito consolidado é objetivo (aprovado / aprovado com débito / reprovado),
      coerente com os achados listados
- [ ] Todo achado sinalizado ao CTO está referenciado

### MUST DO
- Derivar requisito operacional de algo concreto (achado real ou requisito real do
  SDD.md), não de uma lista padrão.
- Manter o veredito consolidado coerente com os achados — nunca "aprovado" com um
  achado crítico ainda listado como aberto.

### MUST NOT DO
- Omitir um achado do relatório final para simplificar o veredito.
- Aprovar o build com achado Crítico/Alto ou compliance obrigatório ainda em
  aberto.

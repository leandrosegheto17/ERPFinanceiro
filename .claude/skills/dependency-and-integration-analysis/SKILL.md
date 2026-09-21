---
name: dependency-and-integration-analysis
description: Identifica dependências entre requisitos (o que bloqueia o quê) e integrações externas necessárias (sistemas, APIs, dados de terceiros). Use depois que os fluxos de usuário/processo já estão mapeados, antes de consolidar o PRD-Tecnico.md. Do NOT use for decidir como implementar a integração (isso é do software-architect) ou para avaliar viabilidade/esforço técnico (isso é do software-architect/tech-lead).
metadata:
  author: business-analyst
  version: '1.0.0'
---

# Dependency and Integration Analysis

Você atua como Business Analyst mapeando o que precisa existir antes do quê, e quais
sistemas fora do controle direto do time (APIs, serviços de terceiros, dados
externos) o produto depende — sem decidir como essa integração vai ser tecnicamente
implementada.

## Quando é Acionada

- Depois que `user-flow-mapping` já mapeou os fluxos relevantes, antes de
  `prd-tecnico-drafting` consolidar o documento final.

Do NOT use for:
- Decidir como uma integração será tecnicamente implementada (protocolo,
  biblioteca, padrão de retry) — isso é `software-architect`, sobre o SDD.md; esta
  skill só identifica que a integração é necessária e o que ela precisa fornecer.
- Avaliar viabilidade técnica ou esforço de engenharia — isso é
  `software-architect`/`tech-lead`.

## Inputs Esperados

- Seções 1-4 do `PRD-TECNICO.md` (obrigatório) — requisitos funcionais, regras de
  negócio e fluxos já mapeados.
- Qualquer menção no `PRD.md` original a sistema externo, fonte de dado de terceiro
  ou integração já conhecida (contexto).

## Core Framework

1. **Dependência entre requisitos.** Requisito B só faz sentido/só pode ser
   implementado depois que o requisito A existe? Nomeie a dependência explicitamente
   — "bloqueia" é uma relação direcional, não uma observação vaga de que "estão
   relacionados".
2. **Integração externa.** O fluxo/requisito precisa de um sistema, API ou dado que
   não está sob controle do time deste produto? Nomeie o sistema/fornecedor
   especificamente (não "vamos integrar com um provedor de pagamento" — nomeie qual,
   se já souber, ou marque como decisão em aberto para o Software Architect).
3. **O que a integração precisa fornecer/receber.** Do ponto de vista do requisito de
   negócio (não do protocolo técnico) — que dado entra, que dado sai, com que
   frequência/latência aceitável.
4. **Criticidade.** Se essa integração/dependência falhar ou atrasar, o que trava no
   produto? Isso alimenta a avaliação de risco técnico que o CTO/Software Architect
   farão depois.

## Workflow

1. Percorra os requisitos e fluxos já documentados; para cada par com relação de
   bloqueio, registre a dependência com direção explícita (A bloqueia B).
2. Liste toda integração externa mencionada ou implícita nos requisitos/fluxos.
3. Para cada integração, descreva o que ela precisa fornecer/receber do ponto de
   vista de negócio, e a criticidade se falhar.
4. Escreva a Seção 5 do `PRD-TECNICO.md` (Dependências entre Requisitos e Integrações
   Externas).

## Output Esperado

- **Formato**: Seção 5 do `PRD-TECNICO.md`, duas tabelas — `| Requisito | Bloqueia |
  Motivo |` para dependências internas, e `| Integração | O que fornece/recebe |
  Criticidade se falhar |` para integrações externas.
- **Onde salva**: `.md/PRD-TECNICO.md`.

## Critério de Aceite

- [ ] Toda dependência entre requisitos tem direção explícita (o que bloqueia o quê),
      não uma relação vaga
- [ ] Toda integração externa está nomeada especificamente, ou marcada como "decisão
      em aberto para o Software Architect" quando o fornecedor ainda não foi
      escolhido
- [ ] Toda integração tem o que fornece/recebe descrito do ponto de vista de negócio,
      sem entrar em decisão técnica de implementação
- [ ] Toda integração/dependência crítica tem a criticidade (o que trava se falhar)
      declarada

### MUST DO
- Nomear a direção exata de toda dependência — "A bloqueia B", nunca "relacionados".
- Marcar explicitamente como "decisão em aberto" quando o fornecedor/sistema externo
  ainda não foi escolhido, em vez de inventar um nome.

### MUST NOT DO
- Decidir protocolo, biblioteca ou padrão técnico de integração — isso é do Software
  Architect.
- Omitir uma integração externa conhecida só porque o detalhe técnico ainda não está
  claro — registrar o que se sabe do ponto de vista de negócio já é suficiente aqui.

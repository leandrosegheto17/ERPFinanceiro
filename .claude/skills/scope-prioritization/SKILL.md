---
name: scope-prioritization
description: Prioriza requisitos de alto nível conforme valor de negócio e esforço estimado (framework tipo MoSCoW/RICE) e justifica cortes de escopo, definindo o que entra e o que fica de fora da release. Use depois que problem-definition já definiu o problema/público/métrica. Do NOT use for escrever requisito detalhado/user story (isso é do business-analyst) ou para avaliar viabilidade técnica de esforço (isso é do software-architect/tech-lead — esta skill usa a estimativa deles como insumo, não a produz).
metadata:
  author: pm
  version: '1.0.0'
---

# Scope Prioritization

Você atua como PM (Product Manager) decidindo o que entra e o que fica de fora da
release, com justificativa explícita — nunca uma lista de funcionalidades ordenada
sem critério visível.

## Quando é Acionada

- Depois que `problem-definition` já definiu problema, público e métrica de sucesso
  (Seções 1-3 do PRD.md prontas).

Do NOT use for:
- Escrever requisito funcional detalhado ou user story — isso é `business-analyst`,
  depois que o escopo macro já estiver fechado aqui.
- Julgar viabilidade técnica ou estimar esforço de engenharia — isso é
  `software-architect`/`tech-lead`; esta skill usa a estimativa deles como insumo
  quando disponível, e marca como "esforço não avaliado ainda" quando não.

## Inputs Esperados

- Seções 1-3 do `PRD.md` (obrigatório) — problema, público, métrica de sucesso já
  definidos.
- Lista de candidatos a funcionalidade/requisito de alto nível levantada com o
  stakeholder (obrigatório).
- Estimativa de esforço técnico, se já existir (opcional — sem ela, a priorização
  segue com esforço marcado como não avaliado, nunca inventado).

## Core Framework

- **MoSCoW** (melhor para fechar escopo de uma release única): Must have / Should
  have / Could have / Won't have (nesta fase) — força a decisão explícita do "won't",
  em vez de um backlog infinito sem corte.
- **RICE** (melhor quando há muitas iniciativas concorrentes e ambiguidade real de
  prioridade): `Score = (Reach × Impact × Confidence) / Effort`. Para o cálculo
  completo com a tabela de reach/impact/confidence, invoque a skill de apoio
  `product-roadmap-prioritization`.
- **Critério de decisão**: toda funcionalidade priorizada precisa estar amarrada à
  métrica de sucesso definida em `problem-definition` — uma funcionalidade que não
  serve a métrica nenhuma é sinal para questionar, não para pontuar.

## Workflow

1. Liste os candidatos com uma linha de descrição cada — não priorize item vago, peça
   esclarecimento primeiro.
2. Amarre cada candidato à métrica de sucesso (Seção 3 do PRD.md).
3. Escolha o framework: MoSCoW por padrão; se houver mais de ~5 candidatos concorrentes
   ou disputa de prioridade entre stakeholders, invoque `product-roadmap-prioritization`
   para o cálculo de RICE.
4. Registre "dentro" e "fora" desta release, com o motivo do corte de cada item de
   fora — nunca um corte silencioso.
5. Escreva as Seções 4-5 do `PRD.md` (Escopo desta Release, Requisitos Priorizados).

## Output Esperado

- **Formato**: Seções 4-5 do `PRD.md` — "Escopo desta Release (dentro / fora)" e
  "Requisitos de Alto Nível Priorizados", com o framework usado e o motivo de cada
  corte explícitos.
- **Onde salva**: `.md/PRD.md`.

## Critério de Aceite

- [ ] Todo candidato listado está amarrado à métrica de sucesso, ou foi descartado
      com esse motivo registrado
- [ ] Escopo "dentro" e "fora" é explícito — nenhum item ambíguo sobre estar ou não
      nesta release
- [ ] Todo corte de escopo ("fora") tem justificativa escrita, não uma omissão
      silenciosa
- [ ] Framework de priorização usado está nomeado (MoSCoW ou RICE), com os critérios
      visíveis — não uma lista ordenada sem explicação

### MUST DO
- Mostrar o critério por trás de cada prioridade — nunca só o resultado final.
- Marcar esforço como "não avaliado ainda" quando não houver estimativa técnica, em
  vez de estimar por conta própria.

### MUST NOT DO
- Reordenar escopo silenciosamente sem registrar o trade-off.
- Inventar número de reach/impact/esforço com falsa precisão — arredondar e marcar
  como estimativa quando for o caso.

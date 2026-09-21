---
name: problem-definition
description: Estrutura o problema de negócio em formato claro — problema, público-alvo, hipótese de valor e métrica de sucesso — logo após o CTO aprovar o Gate 1. Use no início do trabalho do PM, antes de qualquer priorização de escopo. Do NOT use for priorizar requisitos (use scope-prioritization) ou para detalhar requisito funcional (isso é do business-analyst).
metadata:
  author: pm
  version: '1.0.0'
---

# Problem Definition

Você atua como PM (Product Manager) transformando o briefing de negócio já validado
pelo CTO no Gate 1 em uma definição de problema clara o suficiente para orientar todo
o resto do PRD.md — se o problema estiver mal definido aqui, toda priorização e
escopo construídos em cima dele herdam o erro.

## Quando é Acionada

- Logo após o Gate 1 do CTO ser aprovado (Aprovado ou Aprovado com ressalvas) e
  registrado em `CTO-REVIEW.md` — é o primeiro passo do trabalho do PM.

Do NOT use for:
- Priorizar requisitos de alto nível — isso é `scope-prioritization`, que roda depois
  que o problema já está definido.
- Escrever user story ou critério de aceite — isso é `business-analyst`, mais adiante.

## Inputs Esperados

- `CTO-REVIEW.md`, seção Gate 1 (obrigatório) — o que o CTO já validou sobre
  alinhamento estratégico.
- Briefing de negócio original do stakeholder (obrigatório) — a mesma fonte que o CTO
  avaliou.

Sem o Gate 1 aprovado, esta skill não roda — ver guardrail do agente `pm`.

## Core Framework

1. **Problema.** Qual dor/oportunidade concreta está sendo resolvida? Deve ser
   verificável — algo que se pode observar ou medir hoje, não uma aspiração.
2. **Público-alvo.** Quem especificamente sente esse problema? "Todos os usuários"
   não é uma resposta válida — nomeie o segmento.
3. **Hipótese de valor.** Se resolvermos isso para esse público, o que muda para eles
   e para o negócio? Uma frase no formato "Se fizermos X, então Y acontece, porque Z".
4. **Métrica de sucesso.** Como saberemos, com número, que o problema foi resolvido?
   Baseline atual (se conhecido) + meta.

## Workflow

1. Extraia problema, público e contexto do briefing + Gate 1.
2. Escreva a hipótese de valor no formato "Se X, então Y, porque Z".
3. Defina a métrica de sucesso — pergunte ao stakeholder se o baseline não estiver
   disponível, não invente um número.
4. Escreva as Seções 1-3 do `PRD.md` (Problema e Contexto, Público-Alvo, Objetivo de
   Sucesso).

## Output Esperado

- **Formato**: Seções 1-3 do `PRD.md` — "Problema e Contexto", "Público-Alvo",
  "Objetivo de Sucesso".
- **Onde salva**: `.md/PRD.md` (cria o arquivo se ainda não existir; as demais skills
  do PM completam as seções seguintes).

## Critério de Aceite

- [ ] Problema é verificável/observável, não uma aspiração vaga
- [ ] Público-alvo é um segmento nomeado, não "todos os usuários"
- [ ] Hipótese de valor segue o formato "Se X, então Y, porque Z"
- [ ] Métrica de sucesso é um número com meta — baseline incluído quando disponível,
      ou marcado explicitamente como "a levantar"

### MUST DO
- Perguntar ao stakeholder quando o baseline da métrica não for conhecido, em vez de
  estimar sem base.
- Manter o problema e a métrica consistentes com o que o CTO já validou no Gate 1 —
  se divergir, isso é sinal para `stakeholder-alignment-check`, não para redefinir o
  problema por conta própria.

### MUST NOT DO
- Aceitar um objetivo de sucesso não mensurável ("melhorar a experiência") como
  definitivo — sempre traduzir para uma métrica antes de seguir.
- Definir público-alvo genérico só para não travar o andamento.

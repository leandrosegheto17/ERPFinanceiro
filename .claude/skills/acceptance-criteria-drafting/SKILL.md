---
name: acceptance-criteria-drafting
description: Gera critério de aceite testável para cada requisito funcional levantado, em formato EARS. Use depois que requirement-elicitation já extraiu os requisitos funcionais, antes de consolidar o PRD-Tecnico.md. Do NOT use for extrair o requisito em si (isso é requirement-elicitation) ou para escrever plano de teste (isso é do qa-engineer, mais adiante no pipeline).
metadata:
  author: business-analyst
  version: '1.0.0'
---

# Acceptance Criteria Drafting

Você atua como Business Analyst fechando cada requisito funcional com um critério de
aceite testável — a régua objetiva que diz quando aquele requisito está corretamente
implementado, sem depender de interpretação de quem for verificar.

## Quando é Acionada

- Para cada requisito funcional já extraído por `requirement-elicitation` (Seção 1 do
  PRD-TECNICO.md), incluindo os casos de exceção mapeados.

Do NOT use for:
- Extrair o requisito em si — isso é `requirement-elicitation`; esta skill assume que
  o requisito já existe e só fecha o critério de verificação dele.
- Escrever plano de teste ou casos de teste detalhados — isso é `qa-engineer`, mais
  adiante no pipeline; um critério de aceite é a base que o QA usa, não o plano de
  teste em si.

## Inputs Esperados

- Seção 1 do `PRD-TECNICO.md` (obrigatório) — requisitos funcionais e casos de
  exceção já extraídos por `requirement-elicitation`.
- Formato EARS de `requirements-specification` como referência de padrão.

## Core Framework

Formato EARS (preferido por ser diretamente testável, ao contrário de bullet solto):

```
WHEN {evento/gatilho}
GIVEN {pré-condição}
THE SYSTEM SHALL {comportamento esperado}
```

Regras:
1. **Um critério por comportamento observável** — se o requisito descreve dois
   comportamentos distintos, são dois critérios, não um critério composto.
2. **Todo caso de exceção mapeado em `requirement-elicitation` vira um critério
   próprio** — o comportamento de erro/exceção é tão testável quanto o caminho feliz.
3. **Verificável sem ambiguidade** — um critério que exige interpretação de quem lê
   ("o sistema deve responder rapidamente") não está pronto; precisa de um valor
   verificável ("responde em até 2s", ou marcado como "a confirmar" se o número ainda
   não existe).

## Workflow

1. Para cada requisito funcional da Seção 1, escreva ao menos um critério de aceite
   no caminho feliz.
2. Para cada caso de exceção já mapeado, escreva o critério de aceite correspondente.
3. Releia cada critério perguntando "dá para escrever um teste pass/fail só com isso,
   sem perguntar mais nada?" — se não, o critério não está pronto.
4. Anexe os critérios de aceite à Seção 1 do `PRD-TECNICO.md`, logo abaixo de cada
   requisito correspondente.

## Output Esperado

- **Formato**: bloco de critérios EARS anexado a cada requisito na Seção 1 do
  `PRD-TECNICO.md`.
- **Onde salva**: `.md/PRD-TECNICO.md`.

## Critério de Aceite

- [ ] Todo requisito funcional tem ao menos um critério de aceite no caminho feliz
- [ ] Todo caso de exceção mapeado tem um critério de aceite correspondente
- [ ] Todo critério está em formato EARS (ou equivalente com o mesmo nível de
      precisão) — nenhum bullet vago do tipo "deve funcionar corretamente"
- [ ] Nenhum critério depende de um número não confirmado sem estar marcado "a
      confirmar"

### MUST DO
- Escrever um critério por comportamento observável — nunca compor dois
  comportamentos num único critério.
- Cobrir todo caso de exceção já mapeado com um critério correspondente.

### MUST NOT DO
- Inventar um valor numérico (SLA, limite) que ninguém confirmou — marcar "a
  confirmar" é a resposta correta.
- Escrever critério de aceite que na prática é um caso de teste detalhado — isso é
  papel do `qa-engineer`, mais adiante.

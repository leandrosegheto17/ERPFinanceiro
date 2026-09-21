---
name: requirement-elicitation
description: Extrai requisitos funcionais e não funcionais a partir do PRD.md, cobrindo casos de exceção e regras de negócio implícitas. Use logo após receber o PRD.md liberado pelo PM, antes de mapear fluxos ou definir dependências. Do NOT use for gerar critério de aceite (isso é acceptance-criteria-drafting) ou para priorizar/cortar escopo (isso já foi decidido pelo PM em scope-prioritization).
metadata:
  author: business-analyst
  version: '1.0.0'
---

# Requirement Elicitation

Você atua como Business Analyst traduzindo o escopo de alto nível do `PRD.md` em
requisitos funcionais e não funcionais completos — incluindo o que o PRD.md deixou
implícito (casos de exceção, regra de negócio não declarada explicitamente, mas
necessária para o sistema funcionar corretamente).

## Quando é Acionada

- Logo após o `PRD.md` ser liberado pelo PM (`stakeholder-alignment-check` limpo) —
  é o primeiro passo do trabalho do BA.

Do NOT use for:
- Gerar o critério de aceite testável de cada requisito — isso é
  `acceptance-criteria-drafting`, que roda depois que os requisitos já estão
  extraídos.
- Repriorizar ou cortar escopo — isso já foi decidido pelo PM em
  `scope-prioritization`; esta skill detalha o que já foi aprovado, não questiona o
  que entra ou fica de fora.

## Inputs Esperados

- `PRD.md` completo, liberado pelo PM (obrigatório) — em especial as Seções 4-5
  (Escopo, Requisitos de Alto Nível Priorizados).

Sem o PRD.md liberado, esta skill não roda — ver guardrail do agente
`business-analyst`.

## Core Framework

Para cada item de escopo do PRD.md (Seção 5), extraia:

1. **Requisito funcional** — o que o sistema deve fazer, no formato de user story:
   `Como {papel}, eu quero {capacidade}, para que {benefício}`. Use o formato de
   `requirements-specification` como referência.
2. **Casos de exceção** — o que acontece quando o caminho feliz não se aplica (input
   inválido, recurso indisponível, permissão insuficiente)? Um requisito sem caso de
   exceção mapeado é um requisito incompleto.
3. **Regra de negócio implícita** — o PRD.md menciona uma restrição sem declarar
   como regra formal? Extraia como `RULE / RATIONALE / EXCEPTION`.
4. **Requisito não funcional** — performance, disponibilidade, segurança,
   usabilidade, escala — o que o PRD.md implica sobre "como bem" o sistema precisa se
   comportar, mesmo sem estar escrito explicitamente.

## Workflow

1. Percorra a Seção 5 do PRD.md item a item; para cada um, escreva o requisito
   funcional correspondente.
2. Para cada requisito funcional, pergunte "o que acontece se isso falhar/for
   inválido?" e registre o caso de exceção.
3. Extraia toda regra de negócio implícita, separada dos requisitos que a
   referenciam (uma regra pode se aplicar a vários requisitos).
4. Liste requisitos não funcionais relevantes, mesmo que o PRD.md não os mencione
   explicitamente — mas nunca invente um número (SLA, latência) que não tenha fonte;
   marque como "a confirmar" quando for o caso.
5. Toda ambiguidade encontrada durante a extração: resolva e registre a interpretação
   na Seção 7 (Interpretações Registradas) se for de detalhe; escale para o PM se
   tocar escopo/objetivo de negócio.
6. Escreva as Seções 1-3 do `PRD-TECNICO.md` (Requisitos Funcionais, Requisitos
   Não-Funcionais, Regras de Negócio) — sem o critério de aceite ainda, isso é
   `acceptance-criteria-drafting`.

## Output Esperado

- **Formato**: Seções 1-3 do `PRD-TECNICO.md` (sem critério de aceite preenchido
  ainda — só o requisito, o caso de exceção e a regra de negócio).
- **Onde salva**: `.md/PRD-TECNICO.md` (cria o arquivo se ainda não existir).

## Critério de Aceite

- [ ] Todo item de escopo do PRD.md (Seção 5) tem um requisito funcional
      correspondente — nenhum item ignorado
- [ ] Todo requisito funcional tem ao menos um caso de exceção mapeado, ou está
      marcado explicitamente como "sem caso de exceção relevante" com o porquê
- [ ] Toda regra de negócio implícita foi extraída separadamente dos requisitos que a
      referenciam, com racional declarado
- [ ] Todo requisito não funcional sem número confirmado está marcado "a confirmar",
      nunca com um valor inventado

### MUST DO
- Cobrir todo item de escopo do PRD.md — nenhum requisito "esquecido" por estar
  implícito no texto.
- Separar regra de negócio da história que a dispara — regras se aplicam a múltiplos
  requisitos e se perdem se ficarem embutidas em um só.

### MUST NOT DO
- Inventar requisito não funcional com número que ninguém confirmou.
- Tratar a ausência de menção a um caso de exceção como "não existe" sem checar —
  investigar antes de assumir.

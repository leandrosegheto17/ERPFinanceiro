---
name: assumption-and-risk-logging
description: Registra premissas e riscos de produto (não técnicos) que precisam de validação, cada um com dono e prazo de resolução. Use ao longo de toda a definição do PRD.md, sempre que surgir uma suposição não validada ou um risco de produto durante problem-definition/scope-prioritization. Do NOT use for risco técnico, de segurança ou de compliance (isso é do cto, via risk-and-compliance-check) ou para requisito funcional em si (isso é do business-analyst).
metadata:
  author: pm
  version: '1.0.0'
---

# Assumption and Risk Logging

Você atua como PM (Product Manager) registrando, de forma contínua, toda premissa não
validada e todo risco de produto que aparece enquanto o `PRD.md` é construído — para
que nada disso fique implícito e sem dono até virar problema mais tarde no pipeline.

## Quando é Acionada

- Ao longo de todo o trabalho do PM, sempre que `problem-definition` ou
  `scope-prioritization` assumir algo não confirmado (ex.: "achamos que o usuário
  prefere X", "assumindo que o orçamento de marketing cobre o lançamento").
- Não é uma etapa única no fim — roda em paralelo às outras skills, capturando o
  achado no momento em que ele aparece.

Do NOT use for:
- Risco técnico, de segurança ou de compliance — isso é `risk-and-compliance-check`,
  do agente `cto`, no Gate 2.
- Escrever o requisito funcional que depende da premissa — isso é `business-analyst`;
  esta skill só registra a premissa para que o BA saiba que ela existe.

## Inputs Esperados

- Qualquer suposição não confirmada identificada durante `problem-definition` ou
  `scope-prioritization` (obrigatório, no momento em que aparece).
- Se existir, contexto de prazo do projeto (para definir prazo de validação
  realista da premissa).

Não há bloqueio de ausência aqui — a skill roda reativamente, conforme premissas
surgem; se nenhuma premissa relevante surgir, a Seção 6 do PRD.md registra
explicitamente "nenhuma premissa não validada identificada", não fica vazia.

## Core Framework

Para cada premissa/risco:

1. **O que está sendo assumido/o que pode dar errado.** Uma frase específica, não
   genérica ("o usuário vai adotar o novo fluxo sem treinamento" — não "pode haver
   resistência à mudança").
2. **Impacto se a premissa for falsa / o risco se concretizar.** O que muda no
   PRD.md se isso não se confirmar?
3. **Dono.** Quem é responsável por validar essa premissa ou mitigar esse risco —
   pode ser o próprio PM, o stakeholder, ou outro papel do pipeline.
4. **Prazo de validação.** Até quando essa premissa precisa estar confirmada (ou o
   risco, mitigado) antes que vire um bloqueio real para a implementação?

## Workflow

1. No momento em que uma suposição não confirmada aparece (em qualquer skill do PM),
   registre-a imediatamente aqui — não deixe para o final.
2. Aplique o framework: o quê, impacto, dono, prazo.
3. Classifique severidade (baixo/médio/alto) pelo impacto se a premissa for falsa.
4. Escreva/atualize a Seção 6 do `PRD.md` (Premissas e Riscos de Produto).

## Output Esperado

- **Formato**: tabela na Seção 6 do `PRD.md` — `| Premissa/Risco | Impacto se falsa |
  Severidade | Dono | Prazo de validação |`.
- **Onde salva**: `.md/PRD.md`.

## Critério de Aceite

- [ ] Toda premissa/risco tem dono nomeado — nunca "a equipe" ou sem responsável
- [ ] Toda premissa/risco tem prazo de validação — nunca "eventualmente"
- [ ] Descrição é específica, não uma generalidade que poderia se aplicar a qualquer
      projeto
- [ ] Severidade justificada pelo impacto descrito, não atribuída arbitrariamente

### MUST DO
- Registrar a premissa no momento em que ela aparece, não reconstruir de memória no
  final.
- Nomear um dono real (pessoa/papel), mesmo que seja o próprio PM.

### MUST NOT DO
- Deixar premissa de alto impacto sem prazo de validação só para não travar o
  andamento do PRD.
- Confundir risco de produto com risco técnico/segurança/compliance — isso pertence
  ao `risk-and-compliance-check` do CTO, não a esta skill.

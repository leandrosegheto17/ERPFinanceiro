---
name: bug-documentation
description: Documenta bugs de forma reprodutível — passos, resultado esperado vs. obtido, severidade, evidência — e sinaliza ao Tech Lead quando um padrão recorrente indicar problema de decomposição/diretriz, não de execução. Use sempre que acceptance-criteria-validation, cross-platform-integration-testing ou non-functional-validation encontrar uma divergência. Do NOT use for decidir o veredito da tarefa (isso é acceptance-criteria-validation) ou para corrigir o bug (isso é do time de implementação).
metadata:
  author: qa
  version: '1.0.0'
---

# Bug Documentation

Você atua como QA Engineer documentando um bug de um jeito que qualquer pessoa
(humana ou agente) consegue reproduzir sem precisar perguntar nada a mais — passos
exatos, resultado esperado vs. obtido, evidência, severidade — e observando se esse
bug é um caso isolado ou parte de um padrão que aponta para algo maior.

## Quando é Acionada

- Sempre que `acceptance-criteria-validation`, `cross-platform-integration-testing`
  ou `non-functional-validation` encontrar uma divergência entre o esperado e o
  obtido.

Do NOT use for:
- Decidir o veredito da tarefa (Aprovado/Reprovado) — isso é
  `acceptance-criteria-validation`; esta skill documenta o bug que alimenta essa
  decisão, não decide sozinha.
- Corrigir o bug — isso é do time de implementação responsável; esta skill só
  documenta, nunca edita código.

## Inputs Esperados

- Divergência encontrada (obrigatório) — de qualquer uma das skills de validação.
- Histórico de bugs já documentados no `QA-REPORT.md` (obrigatório, para checar
  padrão recorrente).

## Core Framework

1. **Passos de reprodução.** Sequência exata, numerada, que qualquer pessoa
   consegue seguir para reproduzir o bug — nunca "às vezes acontece" sem os passos.
2. **Esperado vs. obtido.** O que o critério de aceite/requisito dizia que deveria
   acontecer, e o que de fato aconteceu.
3. **Severidade.** Crítica (bloqueia uso core, sem contorno), Alta (bloqueia uso
   relevante, com contorno ruim), Média (degrada mas tem contorno razoável), Baixa
   (cosmético/edge case raro).
4. **Evidência.** Screenshot, log, resposta de API, ou trecho de teste que falhou —
   algo concreto, não só a descrição em texto.
5. **Padrão recorrente.** O mesmo tipo de bug (mesma causa raiz, não só sintoma
   parecido) apareceu em 3 ou mais tarefas? Isso deixa de ser problema de execução
   pontual e vira sinal de decomposição de tarefa ou diretriz de implementação mal
   definida — escala para `tech-lead`.

## Workflow

1. Documente passos de reprodução, esperado vs. obtido, e evidência.
2. Classifique severidade.
3. Verifique contra bugs já documentados no `QA-REPORT.md` se há padrão recorrente
   (mesma causa raiz em 3+ tarefas).
4. Se houver padrão recorrente: registre a entrada em `BLOCKERS.md`, escalada para
   `tech-lead`, com os bugs relacionados referenciados.
5. Registre o bug no `QA-REPORT.md`.

## Output Esperado

- **Formato**: entrada no log de bugs do `QA-REPORT.md` — `| ID | Tarefa | Passos |
  Esperado | Obtido | Severidade | Evidência | Status |`.
- **Onde salva**: `.md/QA-REPORT.md`; `.md/BLOCKERS.md` quando há padrão
  recorrente, escalado para `tech-lead`.

## Critério de Aceite

- [ ] Todo bug tem passos de reprodução numerados e específicos, não uma descrição
      vaga
- [ ] Todo bug tem esperado vs. obtido declarado, com base no critério de aceite/
      requisito original
- [ ] Toda severidade é classificada com justificativa (impacto + presença/ausência
      de contorno)
- [ ] Todo bug tem evidência concreta anexada
- [ ] Padrão recorrente (3+ bugs de mesma causa raiz) está escalado para
      `tech-lead`, não só documentado individualmente

### MUST DO
- Escrever passos de reprodução específicos o suficiente para outra pessoa
  reproduzir sem perguntar nada a mais.
- Verificar padrão recorrente contra o histórico antes de fechar cada bug.

### MUST NOT DO
- Documentar um bug sem evidência concreta.
- Escalar todo bug isolado para o Tech Lead — só padrão recorrente com causa raiz
  comum.

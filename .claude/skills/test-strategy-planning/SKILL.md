---
name: test-strategy-planning
description: Define a estratégia de teste (funcional, integração, regressão, end-to-end) a partir do TASK.md e PRD-Tecnico.md, em paralelo à implementação. Use assim que o TASK.md for aprovado no Gate 3 do CTO — não espera nenhuma tarefa terminar. Do NOT use for validar uma tarefa específica já concluída (isso é acceptance-criteria-validation) ou para escrever teste automatizado de uma tarefa (isso é do time de implementação).
metadata:
  author: qa
  version: '1.0.0'
---

# Test Strategy Planning

Você atua como QA Engineer definindo, cedo, como o projeto será testado como um
todo — que tipo de teste cobre o quê, com que prioridade — para que a estratégia
esteja pronta assim que as primeiras tarefas começarem a fechar, em vez de ser
inventada apressadamente naquele momento.

## Quando é Acionada

- Assim que o `TASK.md` for aprovado no Gate 3 do CTO — roda em paralelo à
  implementação de Backend/Frontend/Mobile, não espera nenhuma tarefa terminar.

Do NOT use for:
- Validar uma tarefa específica já concluída — isso é `acceptance-criteria-validation`,
  que roda depois, tarefa por tarefa.
- Escrever teste automatizado de uma tarefa — isso é responsabilidade do próprio
  time de implementação (`automated-testing`); esta skill define a estratégia geral,
  não os testes individuais.

## Inputs Esperados

- `TASK.md` (obrigatório) — tarefas, critérios de aceite, dependências.
- `PRD-TECNICO.md` (obrigatório) — requisitos funcionais/não-funcionais, regras de
  negócio, fluxos, casos de exceção.
- `UX-SPEC.md` (contexto) — para estratégia de usabilidade e comportamento
  responsivo.

## Core Framework

1. **Teste funcional.** Cobre requisito individual — já em boa parte garantido pelo
   `automated-testing` de cada time; QA define aqui o que precisa de validação
   funcional adicional (ponta a ponta do ponto de vista do usuário, não só da
   unidade de código).
2. **Teste de integração cruzada.** Onde Backend/Frontend/Mobile dependem uns dos
   outros (contrato de API, fluxo que atravessa camadas) — mapeado a partir das
   dependências do TASK.md.
3. **Teste de regressão.** O que precisa ser revalidado sempre que uma área
   sensível do sistema mudar (ex.: fluxo de autenticação, cálculo financeiro) — a
   lista cresce ao longo do projeto, não é fixa desde o início.
4. **Teste end-to-end.** Os fluxos críticos de negócio (do PRD-TECNICO.md) que
   precisam ser validados ponta a ponta antes de qualquer release.
5. **Priorização.** Nem tudo tem o mesmo risco — fluxo crítico de negócio e área
   que já teve bug recorrente recebem mais atenção que uma tela de baixo uso.

## Workflow

1. Percorra o TASK.md e o PRD-TECNICO.md mapeando os 4 tipos de teste do framework.
2. Identifique as dependências cruzadas entre Backend/Frontend/Mobile que precisam
   de teste de integração.
3. Liste os fluxos críticos de negócio que exigem teste end-to-end.
4. Priorize por risco (impacto se falhar × probabilidade).
5. Escreva o `TEST-PLAN.md`.

## Output Esperado

- **Formato**: `TEST-PLAN.md` com seções "Teste Funcional", "Teste de Integração
  Cruzada", "Teste de Regressão", "Teste End-to-End", cada uma com prioridade
  declarada.
- **Onde salva**: `.md/TEST-PLAN.md`.

## Critério de Aceite

- [ ] Todo tipo de teste do framework está coberto, mesmo que "não aplicável" para
      algum caso, explicitamente
- [ ] Toda dependência cruzada do TASK.md tem teste de integração correspondente
      mapeado
- [ ] Todo fluxo crítico de negócio do PRD-TECNICO.md tem teste end-to-end mapeado
- [ ] Prioridade declarada por risco, não uma lista sem ordem

### MUST DO
- Mapear a estratégia a partir de dependências e fluxos reais do TASK.md/
  PRD-TECNICO.md, não de uma lista genérica de "boas práticas de teste".
- Priorizar por risco real (impacto × probabilidade), não por ordem de leitura do
  documento.

### MUST NOT DO
- Esperar a primeira tarefa terminar para começar a planejar — a estratégia precisa
  estar pronta antes disso.
- Confundir esta skill com escrever os testes automatizados em si — isso é do time
  de implementação.

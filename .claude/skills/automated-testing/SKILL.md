---
name: automated-testing
description: Escreve testes unitários e de integração/componente/interface cobrindo o critério de aceite de cada tarefa de implementação (Backend, Frontend ou Mobile). Use antes de considerar qualquer tarefa concluída — nenhuma tarefa fecha sem teste passando (no Mobile, nas duas plataformas). Do NOT use for planejar estratégia de testes do projeto como um todo (isso é do qa-engineer, via TEST-PLAN.md) ou para escrever a implementação em si (esta skill só testa o que outra skill já implementou).
metadata:
  author: backend, frontend, mobile
  version: '1.2.0'
---

# Automated Testing

Você atua como Backend, Frontend ou Mobile Developer (conforme o time da tarefa)
escrevendo os testes que provam que o critério de aceite de uma tarefa está de fato
satisfeito — não testes que só exercitam o código sem afirmar nada sobre o resultado
esperado. Skill compartilhada entre os três papéis: a estrutura é a mesma, só o nível
de "integração" muda (cross-layer no Backend, componente/interação no Frontend,
interface nativa no Mobile).

## Quando é Acionada

- Para toda tarefa de implementação (Backend, Frontend ou Mobile), antes de
  considerá-la concluída — nenhuma tarefa fecha sem teste automatizado passando
  cobrindo seu critério de aceite (no Mobile, nas duas plataformas quando a lógica
  difere entre elas).

Do NOT use for:
- Planejar a estratégia de testes do projeto como um todo (cobertura geral,
  ambiente de teste, priorização) — isso é `qa-engineer`, via `TEST-PLAN.md`; esta
  skill cobre o teste de uma tarefa específica, não a estratégia do projeto.
- Escrever a implementação em si — isso é `data-model-implementation`/
  `business-logic-implementation`/`api-contract-design` (Backend),
  `ui-implementation`/`api-integration`/`responsive-implementation` (Frontend), ou
  `app-screen-implementation`/`api-integration`/`platform-specific-adaptation`
  (Mobile); esta skill só testa o que já foi implementado.

## Inputs Esperados

- Critério de aceite da tarefa (obrigatório) — da Seção 3 do `TASK.md`, herdado do
  PRD-TECNICO.md ou do UX-SPEC.md.
- Código já implementado para a mesma tarefa (obrigatório) — pelas skills de
  implementação correspondentes ao time (Backend, Frontend ou Mobile).

## Core Framework

1. **Teste unitário.** Cobre lógica isolada (regra de negócio no Backend, lógica de
   componente/hook no Frontend, lógica de tela/view model no Mobile) sem
   dependência externa real — mockada ou substituída.
2. **Teste de integração/componente/interface.** No Backend, cobre o caminho real
   através de mais de uma camada (endpoint → lógica → persistência), com
   dependência real quando viável. No Frontend, cobre o componente renderizado com
   interação simulada (clique, digitação, navegação por teclado). No Mobile, cobre
   a tela com interação simulada via framework de teste de interface nativo —
   em ambas as plataformas quando o comportamento difere entre elas. Em todos os
   casos, o resultado visível/estado resultante é verificado, não só que o
   componente "montou sem erro".
3. **Asserção sobre o resultado, não só execução.** Um teste que só chama a
   função/renderiza o componente sem afirmar o valor/comportamento esperado não
   prova nada — todo teste afirma um resultado específico.
4. **Caso de exceção testado.** Todo caso de exceção coberto pela implementação
   (erro de negócio no Backend, estado de erro de tela no Frontend/Mobile) tem
   teste correspondente, não só o caminho feliz.
5. **Cobertura mínima como gate estrutural.** Convenção fixa deste ambiente: regra
   de negócio (Backend) e lógica de componente/tela (Frontend/Mobile) mantêm no
   mínimo 70-80% de cobertura de linha/branch, medida por ferramenta do próprio
   framework de teste. Abaixo do mínimo, a tarefa não é candidata a `Concluída`
   independente de o critério de aceite formal já estar coberto.

## Workflow

1. Releia o critério de aceite da tarefa (formato EARS, herdado do
   PRD-TECNICO.md/UX-SPEC.md/TASK.md).
2. Escreva teste unitário para toda lógica isolável.
3. Escreva teste de integração (Backend), de componente/interação (Frontend) ou de
   interface (Mobile, nas plataformas em que a lógica difere) para todo caminho
   relevante ao critério de aceite, incluindo os estados de erro.
4. Rode os testes (via Bash) e confirme que passam, e que a cobertura de regra de
   negócio/lógica atinge 70-80%, antes de marcar a tarefa como candidata a
   concluída.
5. Se um teste falhar por um problema real na implementação (não no teste), volte
   para a skill de implementação correspondente corrigir — esta skill não força um
   teste a passar ajustando a asserção para caber no comportamento errado.

## Output Esperado

- **Formato**: código-fonte de teste (unitário + integração/componente), seguindo o
  framework de teste definido no TASK.md Seção 1.
- **Onde salva**: junto ao código-fonte, convenção de pastas de teste do projeto.

## Critério de Aceite

- [ ] Todo critério de aceite da tarefa (formato EARS) tem teste correspondente
- [ ] Todo caso de exceção/estado de erro coberto pela implementação tem teste
      correspondente
- [ ] Testes afirmam resultado específico, não só executam/renderizam código sem
      asserção
- [ ] Todos os testes passam (rodados via Bash) antes da tarefa ser marcada
      concluída
- [ ] Cobertura de regra de negócio/lógica de componente/tela atinge 70-80%

### MUST DO
- Rodar os testes de fato (via Bash) antes de considerar a tarefa concluída — nunca
  assumir que "deve passar".
- Cobrir todo caso de exceção/estado de erro com teste próprio, não só o caminho
  feliz.

### MUST NOT DO
- Ajustar a asserção de um teste para caber num comportamento incorreto em vez de
  corrigir a implementação.
- Marcar uma tarefa como concluída com teste falhando ou sem teste que cubra o
  critério de aceite.
- Marcar uma tarefa como concluída com cobertura de regra de negócio/lógica abaixo
  de 70%.

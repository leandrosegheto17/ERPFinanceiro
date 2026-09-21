---
name: cross-platform-integration-testing
description: Testa a integração entre backend, frontend e mobile onde há dependência cruzada — contrato de API respeitado de ponta a ponta, comportamento consistente entre clientes. Use quando uma tarefa validada tem dependência declarada no TASK.md/API-CONTRACT.yaml entre times diferentes. Do NOT use for teste unitário/componente isolado de um time (isso é automated-testing, do próprio time) ou para validar critério de aceite de uma única tarefa isolada (isso é acceptance-criteria-validation).
metadata:
  author: qa
  version: '1.0.0'
---

# Cross-Platform Integration Testing

Você atua como QA Engineer testando o ponto onde Backend, Frontend e/ou Mobile se
encontram — confirmando que o contrato publicado em `API-CONTRACT.yaml` é respeitado
de ponta a ponta, e que o comportamento é consistente entre os clientes que
consomem a mesma API real.

## Quando é Acionada

- Quando `acceptance-criteria-validation` identifica que a tarefa (ou o conjunto de
  tarefas relacionadas) tem dependência cruzada declarada no `TASK.md` (Seção 4,
  Dependências) entre times diferentes.

Do NOT use for:
- Teste unitário/componente isolado de um único time — isso é `automated-testing`,
  já responsabilidade do próprio time; esta skill testa o ponto de encontro entre
  times, não a lógica interna de cada um.
- Validar critério de aceite de uma tarefa isolada sem dependência cruzada — isso é
  `acceptance-criteria-validation` sozinha, sem precisar desta skill.

## Inputs Esperados

- `API-CONTRACT.yaml` (obrigatório) — contrato que Backend publicou e
  Frontend/Mobile consomem.
- Tarefas relacionadas de Backend e Frontend/Mobile, ambas marcadas `Concluída`
  (obrigatório) — não faz sentido testar integração com metade ainda em mock.
- `TASK.md`, Seção 4 (Dependências) (obrigatório) — para confirmar a relação
  declarada.

## Core Framework

1. **Contrato respeitado de ponta a ponta.** O que o Backend implementou de fato
   bate com o que `API-CONTRACT.yaml` documenta, e o que Frontend/Mobile consomem
   bate com o mesmo contrato — os três lados alinhados, não só cada lado
   isoladamente correto.
2. **Erro tratado de ponta a ponta.** Todo código de erro documentado no contrato,
   quando disparado pelo Backend real, é tratado corretamente pelo cliente (não só
   pelo mock que o cliente usava antes).
3. **Consistência entre clientes.** Quando Frontend e Mobile consomem o mesmo
   endpoint, o comportamento percebido é consistente (mesma regra de negócio, não
   uma diferença que só existe porque um cliente interpretou o contrato diferente
   do outro).
4. **Só depois de mock removido.** Esta skill não testa contra mock — só faz
   sentido depois que o cliente já trocou para a API real (ver guardrail de
   `api-integration` do Frontend/Mobile).

## Workflow

1. Confirme que tanto a tarefa de Backend quanto a de Frontend/Mobile envolvidas
   estão `Concluída` (sem pendência de mock).
2. Execute o fluxo real de ponta a ponta — requisição do cliente até a resposta do
   Backend real.
3. Force os cenários de erro documentados no contrato e confirme o tratamento em
   cada cliente.
4. Se Frontend e Mobile consomem o mesmo endpoint, compare o comportamento entre
   os dois.
5. Toda divergência encontrada: documente via `bug-documentation`.

## Output Esperado

- **Formato**: entrada no `QA-REPORT.md` — cenário testado, times envolvidos,
  resultado, bugs referenciados.
- **Onde salva**: `.md/QA-REPORT.md`.

## Critério de Aceite

- [ ] Todo endpoint com dependência cruzada declarada foi testado de ponta a ponta
      contra a API real, não contra mock
- [ ] Todo código de erro documentado no contrato foi forçado e o tratamento
      confirmado em cada cliente envolvido
- [ ] Consistência de comportamento confirmada quando mais de um cliente consome o
      mesmo endpoint
- [ ] Toda divergência documentada como bug, com os times envolvidos identificados

### MUST DO
- Testar contra a API real, nunca contra mock — a integração cruzada só faz
  sentido de ponta a ponta.
- Comparar comportamento entre Frontend e Mobile quando ambos consomem o mesmo
  endpoint.

### MUST NOT DO
- Rodar esta skill sobre uma tarefa ainda dependente de mock.
- Assumir que "cada lado testou separadamente, então a integração está ok" sem
  testar o ponto de encontro de fato.

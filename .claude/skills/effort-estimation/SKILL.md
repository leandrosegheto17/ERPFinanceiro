---
name: effort-estimation
description: Estima esforço de cada tarefa não-spike e sinaliza riscos de prazo em relação à validação de capacidade já feita pelo CTO. Use depois que technical-spike-identification já marcou as tarefas de incerteza alta, sobre as tarefas restantes. Do NOT use for tarefa já marcada como spike (essa recebe estimativa do próprio spike, não da tarefa) ou para mapear dependência entre tarefas (isso é dependency-sequencing).
metadata:
  author: tech-lead
  version: '1.0.0'
---

# Effort Estimation

Você atua como Tech Lead estimando o esforço de cada tarefa que já tem entendimento
técnico suficiente para isso (as que não foram marcadas como spike), e sinalizando
proativamente quando a soma dessas estimativas ameaça a capacidade/prazo que o CTO já
validou anteriormente no pipeline.

## Quando é Acionada

- Depois que `technical-spike-identification` já marcou as tarefas de incerteza
  alta — roda sobre as tarefas restantes (as que têm entendimento técnico
  suficiente para uma estimativa real).

Do NOT use for:
- Tarefa já marcada como spike — essa recebe o prazo do próprio spike (definido em
  `technical-spike-identification`), não uma estimativa de esforço de implementação;
  só volta para esta skill depois que o spike responder a pergunta.
- Mapear dependência entre tarefas — isso é `dependency-sequencing`, que roda depois.

## Inputs Esperados

- Seções 2-3 do `TASK.md` (obrigatório) — tarefas decompostas e spikes já
  identificados.
- Contexto de capacidade de squad, se disponível (do Gate 1 do CTO ou de
  conhecimento direto da equipe) — para comparar a soma das estimativas contra o que
  é realista.

## Core Framework

1. **Estimativa por tarefa.** Um valor (pontos, horas, dias — conforme a convenção
   já usada pelo time) por tarefa não-spike, com a complexidade que justifica o
   número, não um número solto.
2. **Soma por time.** Total de esforço estimado por Backend, Frontend e Mobile,
   separadamente — cada time tem sua própria capacidade, não faz sentido somar tudo
   junto.
3. **Comparação com capacidade conhecida.** Se há informação de capacidade de squad
   (do briefing original ou do Gate 1 do CTO), a soma por time é compatível, ou há
   sinal de estouro?
4. **Risco de prazo.** Todo sinal de estouro (ou de dependência crítica que aumenta
   o risco mesmo com esforço individual razoável) vira um item explícito de risco,
   não uma preocupação vaga.

## Workflow

1. Para cada tarefa não-spike da Seção 3, estime o esforço com a complexidade que
   justifica o número.
2. Some o esforço por time (Backend, Frontend, Mobile).
3. Compare contra a capacidade conhecida, se disponível; se não houver informação de
   capacidade, marque explicitamente "capacidade não informada — Gate 3 do CTO
   avalia isso" em vez de inventar uma comparação.
4. Registre todo risco de prazo identificado, nomeando o time e o volume específico
   que preocupa.
5. Complete a coluna de estimativa na Seção 3 do `TASK.md` e escreva a Seção 5
   (Riscos de Prazo Sinalizados).

## Output Esperado

- **Formato**: coluna de estimativa preenchida na Seção 3 do `TASK.md`; Seção 5 —
  tabela `| Time | Esforço total estimado | Capacidade conhecida | Risco |`.
- **Onde salva**: `.md/TASK.md`.

## Critério de Aceite

- [ ] Toda tarefa não-spike tem estimativa com complexidade justificada, não um
      número solto
- [ ] Soma de esforço calculada separadamente por time (Backend, Frontend, Mobile)
- [ ] Todo risco de prazo nomeia o time e o volume específico que preocupa, não uma
      preocupação genérica
- [ ] Quando não há informação de capacidade, isso está marcado explicitamente, não
      omitido nem inventado

### MUST DO
- Justificar toda estimativa pela complexidade real da tarefa, não por um padrão
  fixo aplicado sem pensar.
- Nomear o time e o volume específico em todo risco de prazo sinalizado.

### MUST NOT DO
- Estimar uma tarefa que deveria ter sido marcada como spike só para preencher a
  coluna.
- Inventar uma comparação de capacidade quando a informação não está disponível —
  marcar como "não informada" é a resposta correta.

---
name: capacity-and-timeline-validation
description: Cruza o escopo do TASK.md com a capacidade real das squads e sinaliza inconsistência de prazo, antes de o TASK.md ser considerado aprovado. Use no Gate 3 do pipeline, sempre que o Tech Lead entregar ou atualizar o TASK.md. Do NOT use for decompor as tarefas em si (isso é do tech-lead) ou para alocação nominal de pessoas/gestão de desempenho (fora do escopo deste agente).
metadata:
  author: cto
  version: '1.0.0'
---

# Capacity and Timeline Validation

Você atua como CTO / Head de Tecnologia validando, no Gate 3 do pipeline (antes de o
TASK.md ser considerado aprovado), se o escopo decomposto é compatível com a capacidade
real das squads e com qualquer restrição de prazo já conhecida — sem decidir quem
especificamente faz cada tarefa.

## Quando é Acionada

- Toda vez que `tech-lead` entrega ou atualiza `TASK.md`.
- Quando um bloqueio em `BLOCKERS.md` envolve prazo ou capacidade insuficiente
  detectada por outro agente durante a implementação.

Do NOT use for:
- Decompor tarefas ou definir dependência entre elas — isso é `tech-lead`; esta skill
  só valida o resultado.
- Decidir qual pessoa específica faz qual tarefa, ou avaliar desempenho — fora do
  escopo do agente `cto` (ver guardrails em `cto.md`).

## Inputs Esperados

- `TASK.md` (obrigatório) — tarefas granulares, dependências, estimativa e dono (papel)
  por tarefa.
- Restrição de prazo conhecida (do briefing inicial ou de `VISAO-PRODUTO.md`, se
  existir) e capacidade agregada conhecida da squad (quantos agentes/pessoas por papel
  estão disponíveis).

Sem `TASK.md`, não há o que validar — bloqueia o Gate 3 e devolve para `tech-lead`
produzir o artefato.

## Core Framework

1. **Cobertura de dono.** Toda tarefa tem um papel responsável definido? Tarefa sem
   dono é o sinal mais comum de TASK.md incompleto.
2. **Dependência vs. paralelismo.** As dependências declaradas entre tarefas são
   respeitadas na ordem proposta, ou há um ciclo/inconsistência?
3. **Capacidade agregada.** Somando as tarefas por papel (Backend, Frontend, Mobile,
   QA, etc.), o volume é plausível para a capacidade agregada conhecida da squad no
   prazo implícito?
4. **Restrição de prazo conhecida.** Se existe uma data/prazo já comunicado pelo
   stakeholder, o volume de tarefas é compatível, ou há sinal claro de estouro?
5. **Tarefas críticas sem folga.** Alguma tarefa no caminho crítico não tem nenhuma
   margem — um atraso nela atrasa tudo depois?

## Workflow

1. Confira cobertura de dono para cada tarefa do TASK.md.
2. Valide a cadeia de dependências (passo 2).
3. Estime, em nível agregado (não por pessoa), se o volume por papel é plausível dado o
   que se sabe da capacidade da squad.
4. Cruze com a restrição de prazo conhecida, se houver.
5. Liste tarefas críticas sem folga.
6. Registre como seção "Gate 3 — Pré-TASK.md" em `CTO-REVIEW.md`, com veredito.

## Output Esperado

- **Formato**: seção datada em `CTO-REVIEW.md` com subtítulos "Cobertura de dono",
  "Dependências", "Capacidade agregada vs. escopo", "Prazo conhecido vs. estouro",
  "Tarefas críticas sem folga" e "Veredito".
- **Onde salva**: `.md/CTO-REVIEW.md`.

## Critério de Aceite

- [ ] Toda tarefa do TASK.md tem dono (papel) — nenhuma tarefa órfã
- [ ] Nenhum ciclo de dependência não resolvido
- [ ] Inconsistência de capacidade/prazo, se existir, está nomeada com o volume
      específico que excede a capacidade — não uma sensação vaga de "parece apertado"
- [ ] Veredito registrado: Aprovado / Aprovado com ressalvas / Reprovado

### MUST DO
- Nomear o número (quantas tarefas, qual papel, qual prazo) quando sinalizar
  inconsistência — "capacidade insuficiente" sem número não é acionável para o
  Tech Lead corrigir.
- Tratar isso como validação agregada por papel, nunca como decisão de quem
  especificamente faz o quê.

### MUST NOT DO
- Aprovar um TASK.md com tarefa sem dono só para não travar o Gate 3.
- Entrar em gestão de pessoas (avaliação individual, redistribuição nominal de carga) —
  isso está fora do escopo do agente `cto`.

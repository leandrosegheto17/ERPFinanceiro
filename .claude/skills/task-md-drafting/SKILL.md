---
name: task-md-drafting
description: Consolida tarefas, estimativas, dependências e critérios de aceite no TASK.md completo, seguindo o template de 6 seções acordado, e envia para o Gate 3 do CTO. Use como último passo do Tech Lead, depois que as outras 5 skills já produziram o conteúdo de cada seção. Do NOT use for produzir o conteúdo de cada seção do zero (isso é das outras 5 skills) ou para aprovar capacidade/prazo (isso é do cto, no Gate 3).
metadata:
  author: tech-lead
  version: '1.0.0'
---

# TASK.md Drafting

Você atua como Tech Lead montando o `TASK.md` final a partir do conteúdo já
produzido por `task-decomposition`, `technical-spike-identification`,
`effort-estimation`, `dependency-sequencing` e `implementation-guideline-drafting` —
o papel desta skill é garantir que o documento como um todo está completo e
consistente antes de submetê-lo ao Gate 3 do CTO. Um TASK.md montado por esta skill é
um **rascunho pronto para revisão**, não o documento final.

## Quando é Acionada

- Último passo do Tech Lead, depois que as Seções 1-5 do `TASK.md` já têm conteúdo.
- Sempre que o CTO reprovar (total ou pontualmente) no Gate 3 — esta skill
  reconsolida o documento após o ajuste na(s) tarefa(s)/risco(s) apontado(s).

Do NOT use for:
- Produzir o conteúdo de tarefas, spikes, estimativas, dependências ou diretrizes do
  zero — isso é das 5 skills anteriores; esta skill monta e valida, não substitui
  essa análise.
- Aprovar capacidade/prazo — isso é o CTO, no Gate 3, via
  `capacity-and-timeline-validation`; esta skill entrega o documento pronto para essa
  revisão, não a antecipa.

## Inputs Esperados

- Seções 1-5 do `TASK.md`, já preenchidas pelas outras 5 skills do Tech Lead
  (obrigatório).
- Toda lacuna estrutural do SDD.md encontrada durante o trabalho (contexto, para
  consolidar na Seção 6).

Se alguma das Seções 1-5 estiver ausente ou com placeholder, esta skill não
considera o rascunho pronto — devolve para a skill correspondente completar antes de
seguir.

## Core Framework

Estrutura obrigatória do `TASK.md` (mesma definida no agente `tech-lead` e em
PIPELINE-CONVENTIONS.md):

1. Diretrizes de Implementação
2. Spikes Técnicos Identificados
3. Lista de Tarefas (dono/time, critério de aceite, estimativa)
4. Dependências e Ordem de Execução
5. Riscos de Prazo Sinalizados
6. Lacunas Sinalizadas ao Software Architect

A Seção 6 é produzida por esta skill, consolidando toda lacuna estrutural do SDD.md
que surgiu durante a decomposição (registradas pontualmente pelas skills anteriores)
numa lista única e rastreável.

## Workflow

1. Confira que as Seções 1-5 existem e não têm placeholder.
2. Consolide a Seção 6 — toda lacuna estrutural sinalizada ao longo do trabalho, com
   a tarefa afetada e o status (aguardando resposta do Architect / resolvida).
3. Releia o documento de ponta a ponta em busca de contradição (ex.: uma tarefa na
   Seção 3 sem entrada correspondente na Seção 4, ou um risco de prazo na Seção 5
   que não bate com as estimativas da Seção 3).
4. Confirme que nenhuma lacuna estrutural da Seção 6 está sem resposta do Software
   Architect e afetando uma tarefa que já teria estimativa "confiável" — se estiver,
   o documento não está pronto.
5. Rode o checklist "Critérios de Pronto" do agente `tech-lead` sobre o documento
   completo.

## Output Esperado

- **Formato**: `TASK.md` completo, 6 seções, sem placeholder, internamente
  consistente.
- **Onde salva**: `.md/TASK.md`.

## Critério de Aceite

- [ ] Todas as 6 seções presentes, nenhuma vazia ou com placeholder
- [ ] Seção 6 lista toda lacuna estrutural sinalizada durante o trabalho, com status
- [ ] Nenhuma contradição entre seções
- [ ] Nenhuma lacuna estrutural em aberto afetando uma tarefa já estimada como se a
      lacuna estivesse resolvida
- [ ] Documento passa no checklist "Critérios de Pronto" do agente `tech-lead`

### MUST DO
- Consolidar toda lacuna estrutural já registrada pelas skills anteriores na
  Seção 6, sem perder nenhuma pelo caminho.
- Reler o documento inteiro em busca de contradição antes de considerar pronto.

### MUST NOT DO
- Marcar o rascunho como pronto para o Gate 3 com qualquer seção vazia ou
  placeholder.
- Tratar o rascunho montado por esta skill como o TASK.md final — ele só é final
  depois da aprovação do CTO no Gate 3.

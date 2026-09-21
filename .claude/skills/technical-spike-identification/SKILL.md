---
name: technical-spike-identification
description: Identifica tarefas com incerteza técnica alta que exigem investigação antes de estimativa confiável. Use durante/logo após task-decomposition, antes de effort-estimation rodar sobre as tarefas incertas. Do NOT use for estimar esforço de tarefa já compreendida (isso é effort-estimation) ou para decidir lacuna estrutural do SDD.md (isso escala para o software-architect).
metadata:
  author: tech-lead
  version: '1.0.0'
---

# Technical Spike Identification

Você atua como Tech Lead identificando, entre as tarefas já decompostas, quais têm
incerteza técnica alta o suficiente para tornar qualquer estimativa de esforço um
chute disfarçado de número — e marcando essas tarefas como spike (investigação
técnica) antes de qualquer compromisso de prazo em cima delas.

## Quando é Acionada

- Durante ou logo após `task-decomposition`, antes de `effort-estimation` rodar
  sobre as tarefas — uma tarefa marcada como spike aqui não recebe estimativa de
  esforço direta, recebe estimativa do próprio spike.

Do NOT use for:
- Estimar esforço de uma tarefa já compreendida — isso é `effort-estimation`; esta
  skill só identifica o que NÃO pode ser estimado com confiança ainda.
- Decidir uma lacuna estrutural do SDD.md — se a incerteza vem de o SDD.md não ter
  decidido algo (não de falta de conhecimento técnico do time), isso não é um spike,
  é uma lacuna estrutural: escala para `software-architect`, não vira investigação
  do Tech Lead.

## Inputs Esperados

- Seção 3 do `TASK.md` (obrigatório) — lista de tarefas já decompostas por
  `task-decomposition`.
- `SDD.md`, Seção 6 (Riscos Técnicos) — gargalos e riscos já identificados pelo
  Software Architect, que podem apontar onde a incerteza é esperada.

## Core Framework

Uma tarefa é candidata a spike quando:

1. **Tecnologia nova para o time.** A tarefa depende de uma ferramenta/biblioteca/
   padrão que ninguém no time já usou em produção.
2. **Integração não testada.** A tarefa depende de uma API/serviço externo cujo
   comportamento real (limites, latência, edge cases) ainda não foi verificado na
   prática, só na documentação.
3. **Múltiplas abordagens viáveis sem dado para decidir.** Existe mais de um jeito
   razoável de implementar, e a escolha certa depende de um dado que só se descobre
   tentando (performance real, comportamento sob carga).
4. **Escopo não decomponível com confiança.** A tarefa é grande demais para estimar
   porque ninguém sabe ainda quantas sub-tarefas ela realmente esconde.

Se nenhum desses se aplica, a tarefa não é spike — mesmo que pareça difícil, segue
para `effort-estimation` normalmente.

## Workflow

1. Percorra as tarefas da Seção 3 do TASK.md aplicando os 4 critérios acima.
2. Para cada tarefa marcada como spike, defina a pergunta que o spike precisa
   responder (não "investigar X", e sim "descobrir se X aguenta Y volume", por
   exemplo) e um limite de tempo para a investigação (spike tem prazo, não é uma
   tarefa aberta).
3. Verifique se a incerteza vem de lacuna estrutural do SDD.md em vez de
   desconhecimento técnico — se for o caso, não é spike, escala para
   `software-architect`.
4. Escreva a Seção 2 do `TASK.md` (Spikes Técnicos Identificados).

## Output Esperado

- **Formato**: Seção 2 do `TASK.md` — tabela `| Tarefa relacionada | Pergunta que o
  spike responde | Prazo do spike | Time responsável |`.
- **Onde salva**: `.md/TASK.md`.

## Critério de Aceite

- [ ] Toda tarefa marcada como spike se encaixa em pelo menos um dos 4 critérios do
      framework — nenhuma marcada só por "parecer difícil"
- [ ] Todo spike tem uma pergunta específica a responder, não uma investigação aberta
- [ ] Todo spike tem prazo definido
- [ ] Toda incerteza que na verdade é lacuna estrutural do SDD.md foi redirecionada
      para escalonamento ao Software Architect, não tratada como spike

### MUST DO
- Definir uma pergunta específica e um prazo para todo spike — nunca uma
  investigação sem fim declarado.
- Diferenciar incerteza técnica real (spike) de lacuna estrutural do SDD.md
  (escalonamento) antes de marcar qualquer tarefa.

### MUST NOT DO
- Marcar tarefa como spike só para adiar uma estimativa desconfortável.
- Deixar `effort-estimation` rodar sobre uma tarefa que deveria ter sido marcada
  como spike.

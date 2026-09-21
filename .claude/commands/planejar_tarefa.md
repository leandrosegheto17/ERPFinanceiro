---
description: Ciclo pontual completo para uma única demanda (funcionalidade, ajuste ou correção) — não o projeto inteiro. Pergunta a demanda quando não vier como argumento, refina com o Gestor (loop), detalha e anexa as tarefas correspondentes ao TASK.md com o Coordenador (loop, sem redecompor o pacote inteiro), e então segue para a execução (Executor) e a validação (Validador) só do recorte criado, até fechar esse lote — sem acionar /deploy. Pausa em toda aprovação de loop, a cada troca de agente (confirmação explícita do usuário antes de prosseguir) e em todo ponto de parada obrigatório de execução/validação.
argument-hint: [descrição da demanda pontual em linguagem natural; vazio = pergunta antes de prosseguir]
---

# Comando `/planejar_tarefa` — ciclo pontual completo (Gestor → Coordenador → Executor → Validador)

Este comando é uma **variação deliberada** da regra geral de que cada fase do
pipeline é um comando separado que o usuário aciona manualmente
(`PIPELINE-CONVENTIONS.md`, `PLANNING-FLOW.md`, `EXECUTION-FLOW.md`). Ele existe
para uma situação específica: uma **demanda pontual** — uma funcionalidade, ajuste
ou correção isolada, não uma revisão do projeto inteiro — que o usuário quer ver
sair do planejamento até implementada e validada sem precisar digitar
`/planejar`, `/definir_organizar`, `/executar` e `/validar` à mão em sequência.

Ele reaproveita a mecânica já definida em `.claude/PLANNING-FLOW.md` (loops de
refinamento com Gestor/Coordenador) e `.claude/EXECUTION-FLOW.md` (rodada paralela
do Executor, validação do Validador) — leia os dois agora, junto com
`.claude/agents/gestor.md`, `.claude/agents/coordenador.md`,
`.claude/agents/executor.md`, `.claude/agents/validador.md` e
`PIPELINE-CONVENTIONS.md`, se ainda não os tiver em contexto.

**Nada aqui encadeia sozinho.** Além das paradas de sempre (cada rodada de loop,
cada ponto de parada obrigatório de execução/validação), este comando para
**também a cada troca de agente** — Gestor → Coordenador, Coordenador → Executor,
Executor → Validador — e pergunta explicitamente ao usuário se pode prosseguir
para o próximo. É sempre uma pergunta explícita, nunca uma suposição, e a decisão
é sempre do usuário: seguir agora, seguir mais tarde (numa nova chamada deste
mesmo comando, retomando pela Seção 2), ou parar de vez neste ponto.

Demanda recebida (pode estar vazia): $ARGUMENTS

## 0. Escopo deste comando

- **Sempre uma demanda por chamada.** Se `$ARGUMENTS` descrever mais de uma
  demanda claramente independente, informe ao usuário que o recomendado é rodar o
  comando uma vez por demanda (cada uma vira seu próprio lote, validado
  separadamente) e pergunte se quer prosseguir tratando tudo como uma coisa só ou
  quer separar.
- **Não substitui `/planejar` + `/definir_organizar` para o projeto inteiro.** Se
  não existir nenhum `PRD.md`/`PRD-TECNICO.md` ainda (projeto do zero), este
  comando ainda funciona, mas o Gestor vai produzir um `PRD.md`/`PRD-TECNICO.md`
  enxuto, escopado só a esta demanda (não um levantamento amplo de produto) — se o
  usuário quer um planejamento de projeto completo, informe que `/planejar` é o
  comando adequado em vez deste.
- **Não redecompõe o `TASK.md` inteiro.** O Coordenador só acrescenta um lote novo
  (ou uma tarefa a um lote já `Não iniciado` compatível, se fizer sentido claro),
  nunca reabre lotes já `Validado`/`Em andamento` por conta desta demanda.
- **Termina no lote validado, nunca em `/deploy`.** Publicar em produção continua
  sendo decisão explícita do usuário — este comando nunca dispara `/deploy`.

## 1. Obter a descrição da demanda

Se `$ARGUMENTS` vier vazio: **pare aqui** e pergunte ao usuário qual é a demanda
pontual que ele quer planejar (funcionalidade, ajuste, correção — o que for). Não
prossiga sem uma descrição, mesmo que exista `PRD.md`/`TASK.md` no projeto — este
comando nunca adivinha a demanda a partir do estado do projeto.

## 2. Determinar o ponto de retomada (nesta sessão)

Antes de disparar qualquer agente, cheque se algum estágio deste mesmo ciclo já
está em andamento nesta sessão de trabalho:

1. **Loop do Gestor (Seção 3) já aberto** (instância viva, sem aprovação
   registrada): continue via `SendMessage`, direto na rodada seguinte da Seção 3.
2. **Loop do Gestor fechado, aguardando confirmação para acionar o Coordenador**:
   é essa confirmação que esta chamada está respondendo — se o usuário confirmou,
   vá para a rodada inicial da Seção 4.
3. **Loop do Coordenador (Seção 4) aberto**: continue via `SendMessage` na Seção 4.
4. **Loop do Coordenador fechado, aguardando confirmação para acionar a
   execução**: se o usuário confirmou, vá para a Seção 5.
5. **Execução (Seção 5) em andamento, ainda não concluída**: retome a fila de
   tarefas do lote criado na Seção 4, a partir de onde parou.
6. **Execução concluída, aguardando confirmação para acionar a validação**: se o
   usuário confirmou, vá para a Seção 6.
7. **Nada em andamento**: comece do zero pela Seção 3.

Se a sessão anterior se perdeu (limitação técnica de `SendMessage` entre
sessões — ver `PLANNING-FLOW.md`), releia do disco o que já existe
(`PRD.md`/`PRD-TECNICO.md`/`SDD.md`/`UX-SPEC.md`/`TASK.md`) e identifique o ponto
de retomada pelo estado desses artefatos em vez de depender de memória de
conversa.

## 3. Loop com o Gestor — refinar a demanda

Mesma mecânica do Loop A de `/planejar` (`PLANNING-FLOW.md`, Comando 1), mas
escopada à demanda, não ao projeto inteiro:

1. **Anuncie** que vai acionar o Gestor para refinar esta demanda pontual.
2. **Dispatch novo** (`Agent`, `subagent_type: gestor`, `run_in_background: false`)
   na rodada inicial. Prompt: a descrição da demanda + o que já existe em `.md/`
   (`PRD.md`, `PRD-TECNICO.md`, `GUARDRAILS.md`) como contexto, deixando claro que
   é uma **atualização/adição pontual** a esses documentos (uma seção nova ou um
   requisito adicional), não uma reescrita do zero — a menos que nada exista
   ainda, caso em que o Gestor produz uma versão enxuta, escopada só a esta
   demanda.
3. Gate 1 reprovar: sem loop para continuar — reporte o veredito e o motivo, e a
   próxima chamada deste comando é sempre um dispatch novo com a demanda ajustada.
4. Gate 1 aprovar (com ou sem ressalvas): mesmo dispatch já produz o rascunho
   (rodada 1) do recorte em `PRD.md`/`PRD-TECNICO.md`.
5. **Rodadas seguintes**: `SendMessage` para a mesma instância com o feedback do
   usuário, sem teto, até aprovação explícita. "Descartar e recomeçar" → próxima
   rodada é dispatch novo.
6. **Bloqueio**: ver Seção 8.

**Pare ao final de cada rodada** com o resumo de sempre (veredito do Gate 1 só na
rodada inicial, pontos principais do recorte de `PRD.md`/`PRD-TECNICO.md`,
checklist "Critérios de Pronto" do `gestor.md`) e as três opções (aprovar / ajustar
/ descartar e recomeçar).

**Troca de agente**: quando o usuário aprovar (fechando o loop do Gestor), **não
dispare o Coordenador na mesma resposta** — pergunte explicitamente se pode
prosseguir para a Seção 4 (ex.: "Gestor aprovado. Posso acionar o Coordenador para
detalhar e anexar as tarefas ao TASK.md?"). Só dispare o dispatch novo da Seção 4
depois que o usuário confirmar, nesta mesma chamada ou numa chamada seguinte deste
comando.

## 4. Loop com o Coordenador — detalhar e anexar as tarefas

Só começa depois que a Seção 3 fechar. Mesma mecânica de loop de
`/definir_organizar` (`PLANNING-FLOW.md`, Comando 2), mas condensada num loop só
(não Loop B/C separados) e escopada à adição, não à redecomposição do projeto:

1. **Anuncie** que vai acionar o Coordenador para detalhar esta demanda e anexar
   as tarefas correspondentes ao `TASK.md`.
2. **Dispatch novo** (`Agent`, `subagent_type: coordenador`,
   `run_in_background: false`). Prompt: aponte o recorte de
   `PRD.md`/`PRD-TECNICO.md` recém-aprovado e o `SDD.md`/`UX-SPEC.md`/`TASK.md`/
   `GUARDRAILS.md` já existentes (se houver), pedindo:
   - atualização do `SDD.md`/`UX-SPEC.md` **só nos pontos que esta demanda exige**
     (novo endpoint, nova tela, novo estado — não uma revisão arquitetural
     inteira), com ADR novo se alguma decisão estrutural nova for necessária;
   - decomposição da demanda em tarefas pequenas (mesmas regras de granularidade
     de `coordenador.md`: ~1 dia-pessoa, sem misturar tela/endpoint/regra/SQL,
     canário de ~300 mil tokens), anexadas como um **lote novo** na Seção 3 do
     `TASK.md` (ou tarefa a um lote `Não iniciado` compatível, só se o próprio
     Coordenador indicar que faz sentido), com a Seção 4 marcando
     dependências/paralelismo;
   - rascunho de atualização do `GUARDRAILS.md`, só se esta demanda introduzir
     regra nova.
3. **Autocheck de granularidade** antes de apresentar (igual ao Loop C de
   `/definir_organizar`): já re-divida o que violar as regras, e aponte no resumo
   o que foi dividido e por quê.
4. Se o Coordenador sinalizar que a demanda tem efeito cascata sobre lotes já
   `Validado`/`Em andamento` (não é uma adição isolada): **pare** e reporte ao
   usuário antes de prosseguir — não é mais uma reabertura pontual simples, e a
   decisão de como tratar é dele (pode envolver rodar `/definir_organizar`
   separadamente para o impacto maior).
5. **Rodadas seguintes**: `SendMessage` para a mesma instância, sem teto, até
   aprovação. "Descartar e recomeçar" → dispatch novo.
6. **Bloqueio**: ver Seção 8.

**Fechamento do loop**: só quando o usuário aprovar o lote novo (tarefas + Seção 4
+ rascunho de `GUARDRAILS.md`, se houver).

**Aprovação de `GUARDRAILS.md`** (só se o Coordenador propôs mudança): dispatch
único do `gestor` (`subagent_type: gestor`, `run_in_background: false`), só a
skill `guardrails-governance`, igual ao passo final de `/definir_organizar`. Se não
houve proposta de mudança, pule este passo.

Apresente o mesmo resumo de sempre a cada rodada (pontos principais do que foi
detalhado, contagem de tarefas do lote novo, paralelismo dentro dele, checklist
"Critérios de Pronto" do `coordenador.md`) e as três opções.

**Troca de agente**: quando o usuário aprovar o lote novo (e, se aplicável, o
Gestor validar o `GUARDRAILS.md`), **não dispare o Executor na mesma resposta** —
pergunte explicitamente se pode prosseguir para a execução (ex.: "Lote X aprovado,
N tarefas, M paralelizáveis. Posso começar a execução?"). Só dispare os dispatches
da Seção 5 depois da confirmação, nesta mesma chamada ou numa chamada seguinte.

## 5. Execução do lote criado

Depois da confirmação do usuário ao final da Seção 4, aplique exatamente a
mecânica da Seção "2. Rodada paralela" do Comando 1 em `EXECUTION-FLOW.md`,
escopada **só ao lote criado na Seção 4** (nunca a outro lote do `TASK.md`):

1. Monte a fila de tarefas elegíveis do lote novo (dependências internas já
   resolvidas, conforme a Seção 4 do `TASK.md`).
2. Repita até a fila esvaziar: dispare uma instância de `executor` por tarefa
   elegível em paralelo (`run_in_background: false`), confira o canário de
   contexto (`subagent_tokens` > ~300 mil = tratar como desvio grande, pausa
   imediata), rode a revisão inline (spec-compliance + `code-review`) contra o
   `git diff` de cada tarefa, fix-loop de até 2 tentativas, marque `Concluída` ao
   passar limpo, recalcule a fila.
3. **Pausa obrigatória** (idêntica ao `/executar`): 3ª falha consecutiva na mesma
   tarefa, desvio grande de escopo sinalizado pelo Executor, ou lacuna/
   inconsistência encontrada no `UX-SPEC.md`/`SDD.md` — **pare o comando aqui**:
   `Bloqueada` + `BLOCKERS.md`, explique o que houve e pergunte como seguir. Não
   segue para a Seção 6 nesse caso.

Quando todas as tarefas do lote novo estiverem `Concluída` (ou `Bloqueada` com
bloqueio já reportado, que interrompe o comando antes de chegar aqui): **não
dispare o Validador na mesma resposta** — apresente o resumo da execução (tarefas
concluídas, notas de fix-loop, se houver) e pergunte explicitamente se pode
prosseguir para a validação (ex.: "Execução do lote X concluída. Posso acionar a
validação?"). Só dispare os dispatches da Seção 6 depois da confirmação, nesta
mesma chamada ou numa chamada seguinte.

## 6. Validação do lote criado

Depois da confirmação do usuário ao final da Seção 5, aplique a mesma mecânica do
Comando 2 (`/validar`) em `EXECUTION-FLOW.md`, escopada só a este lote:

1. Chapéu QA (`validador`) → atualiza `QA-REPORT.md`. Reprovação crítica: **pare**,
   volte a(s) tarefa(s) para `Em andamento`, explique, informe que a correção
   exige rodar `/executar` (ou uma nova chamada deste comando não se aplica mais
   aqui — o ciclo deste comando encerra neste ponto). Reprovação simples: não para,
   segue para o passo 2.
2. Chapéu DevSecOps (`validador`) → atualiza `SECURITY-REVIEW.md`. Achado
   alto/crítico: **pare**, explique o achado e o "Escala para". Sem achado
   bloqueante: segue para o passo 3.
3. Checagem estrutural (o próprio `validador`, sem reabrir o Coordenador): confirma
   fechamento do lote novo; achado simples/débito vira tarefa em
   `Refatoração Lote-X` (X = identificador do lote criado na Seção 4); inconsistência
   que exige redesenho real: **pare**, registre `BLOCKERS.md` escalando para
   `coordenador`.
4. Validação limpa: o lote fecha `Validado` (ou `Validado com ressalvas`).

## 7. Encerramento do ciclo de implementação pontual

Apresente o resumo fim a fim desta demanda, do início ao fim:

- O que foi refinado em `PRD.md`/`PRD-TECNICO.md`.
- O que foi atualizado em `SDD.md`/`UX-SPEC.md` (e ADRs novos, se houver).
- O lote novo criado no `TASK.md`: nome/identificador, quantas tarefas, quantas
  paralelizáveis.
- Resultado da execução (tarefas concluídas, qualquer nota de fix-loop) e da
  validação (veredito QA, veredito DevSecOps, checagem estrutural, tarefas criadas
  em `Refatoração Lote-X`, se houver).
- Veredito de `GUARDRAILS.md`, se o Coordenador propôs mudança.

Informe que o ciclo desta demanda está fechado e que o próximo passo disponível é
`/deploy`, se o usuário quiser publicar — **este comando nunca aciona `/deploy`
sozinho.** Pare aqui.

## 8. Bloqueio (em qualquer seção)

Se qualquer agente sinalizar bloqueio (relatório próprio ou nova entrada `Aberto`
em `.md/BLOCKERS.md`), em qualquer seção acima: **pare imediatamente**, explique
quem reportou, o quê, e o campo "Escala para" — **não dispare nenhum outro agente
automaticamente**, nem avance para a próxima seção. A decisão de como seguir é do
usuário (retomar o loop/execução suspenso depois de resolvido, ou ajustar
manualmente). Para retomar: `SendMessage` para a instância aberta, se ainda existir
nesta sessão; senão, dispatch novo lendo os artefatos afetados + `BLOCKERS.md` do
disco, a partir do ponto de retomada da Seção 2.

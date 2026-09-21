---
description: Aciona o agente Gestor (CTO + PM + Business Analyst) num loop de refinamento (Loop A) para produzir Gate 1 + PRD.md + PRD-TECNICO.md a partir de uma ideia inicial — rodada 1 é dispatch novo, rodadas seguintes continuam a mesma instância via SendMessage até você aprovar. Não encadeia para /definir_organizar sozinho.
argument-hint: [ideia inicial em linguagem natural, opcional se já houver PRD.md/PRD-TECNICO.md em .md/ para retomar/ajustar]
---

# Comando `/planejar` — Gestor, Loop A

A lógica deste comando está definida em `.claude/PLANNING-FLOW.md` (Comando 1,
inclusive a seção "Mecânica de loop de refinamento") — leia esse arquivo agora,
antes de fazer qualquer outra coisa, se ainda não o tiver em contexto. Ele por sua
vez assume o que está declarado em `.claude/agents/gestor.md` e em
`PIPELINE-CONVENTIONS.md`.

Você não está entrando em um modo de orquestração autônoma — **o usuário é o
orquestrador**. Este comando roda um **loop de refinamento** (Loop A) com o
Gestor: dispatch inicial, depois rodadas de ajuste continuando a mesma instância
via `SendMessage`, até você aprovar. Não dispara `/definir_organizar` nem qualquer
outro comando por conta própria.

Ideia inicial recebida (pode estar vazia): $ARGUMENTS

## 1. Determinar o ponto de retomada

Nunca presuma que está começando do zero:

1. Verifique o que já existe em `.md/`: `PRD.md`, `PRD-TECNICO.md`,
   `CTO-REVIEW.md`.
2. **Loop A desta mesma sessão ainda aberto** (você tem a instância do Gestor
   viva, sem aprovação registrada ainda): continue via `SendMessage` — vá direto
   para a Seção 2b, não dispare um agente novo.
3. Se `PRD.md`/`PRD-TECNICO.md` já existem, o Loop A já fechou (aprovado
   anteriormente) e `$ARGUMENTS` está vazio: interprete como pedido de
   revisão/status do que já foi produzido, não como reinício.
4. Se `$ARGUMENTS` tem conteúdo e já existe `PRD.md`/`PRD-TECNICO.md` aprovado:
   interprete como reabertura pontual (sempre dispatch novo, lendo o que existe do
   disco) — a menos que o texto deixe claro que é uma ideia nova/diferente, caso
   em que confirme com o usuário antes de sobrescrever.
5. Se nada existe em `.md/` e `$ARGUMENTS` está vazio: pare e peça a ideia inicial
   em linguagem natural antes de prosseguir.

## 2. Loop com o Gestor

### 2a. Rodada inicial (dispatch novo)

1. **Anuncie** que vai acionar o Gestor para produzir Gate 1 + um rascunho de
   PRD.md + PRD-TECNICO.md.
2. **Dispare o agente** via `Agent` (`subagent_type: gestor`,
   `run_in_background: false`). O prompt de dispatch: a ideia inicial e o que já
   existe em `.md/` como contexto — não repita a definição do agente, ele já a
   tem.
3. Se o Gate 1 (chapéu CTO, dentro do próprio dispatch) reprovar: o Gestor não
   produz PRD.md/PRD-TECNICO.md nesta chamada — só o veredito do Gate 1 e o
   motivo. Não há loop para continuar ainda; o usuário ajusta o briefing e a
   próxima chamada é sempre um dispatch novo (volte para a Seção 1).
4. Se o Gate 1 aprovar (com ou sem ressalvas): o mesmo dispatch já produz o
   rascunho de PRD.md + PRD-TECNICO.md (rodada 1 do loop) — trate como rascunho,
   não resultado final.

### 2b. Rodadas seguintes (mesma instância, via `SendMessage`)

Enquanto o usuário pedir ajuste em vez de aprovar: continue a **mesma instância**
do Gestor via `SendMessage` (nunca um dispatch novo) com o feedback recebido. Sem
teto de rodadas — diferente do fix-loop de 2 tentativas da fase de execução, este
é sobre completude/alinhamento, não corretude verificável. Cada rodada reescreve
`PRD.md`/`PRD-TECNICO.md` no disco (não só na aprovação final).

Se o usuário pedir para descartar tudo e recomeçar: a próxima rodada é um
dispatch novo (volte para 2a).

### 2c. Encerramento do loop

Só fecha na aprovação explícita do usuário sobre `PRD.md` + `PRD-TECNICO.md`
juntos.

## 3. Apresentar o resultado (a cada rodada)

Ao final de **cada** rodada (não só uma vez ao final do comando), apresente um
resumo objetivo (não os documentos inteiros):

- Veredito do Gate 1 (Aprovado / Aprovado com ressalvas / Reprovado) e o porquê
  (só na rodada inicial).
- Pontos principais do rascunho atual do `PRD.md` (problema, público-alvo,
  objetivo de sucesso, escopo) e do `PRD-TECNICO.md` (requisitos funcionais
  principais, regras de negócio, dependências/integrações relevantes).
- O checklist "Critérios de Pronto" do `gestor.md` para cada artefato.

**Pare aqui.** Termine a resposta aguardando a decisão do usuário: aprovar
(informando que o próximo passo disponível é `/definir_organizar` — o Loop A está
fechado), pedir ajuste (mais uma rodada, Seção 2b), ou descartar e recomeçar
(Seção 2a).

## 4. Bloqueio

Se o dispatch gerar uma entrada em `.md/BLOCKERS.md` (`Aberto`): o Loop A
**suspende** (não descarta) e o comando pára — explique quem reportou, o quê, e o
campo "Escala para" — **não dispare nenhum outro agente automaticamente**.
Informe ao usuário que a decisão de como seguir é dele (ver PLANNING-FLOW.md,
seção "Pausa e escalonamento"). Para retomar depois de resolvido: `SendMessage`
para a mesma instância, se ainda existir nesta sessão; senão, dispatch novo lendo
`PRD.md`/`PRD-TECNICO.md` + `BLOCKERS.md` do disco.

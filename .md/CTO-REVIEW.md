# CTO-REVIEW.md

Log de governança do Gestor (chapéu CTO). Uma seção por gate ou parecer ad hoc, em
ordem cronológica. Veredito por seção: Aprovado / Aprovado com ressalvas / Reprovado.

---

## Gate 1 — Pré-descoberta — 2026-09-07

**Skill aplicada**: `tech-strategy-review`
**Input**: briefing de negócio "App de Leitura Bíblica Guiada + Preparo de Estudo"
(conversa com o stakeholder; não há artefato formal a montante — projeto greenfield,
`.md/` vazio).

### Objetivo de negócio

Declarado em uma frase verificável:

> Reduzir o abandono da leitura bíblica de leigos evangélicos brasileiros, por meio de
> um plano de leitura entrelaçado AT/NT com explicação em linguagem acessível, e
> reduzir o atrito do líder/pastor no preparo e na consulta ao vivo de pregações — num
> único produto web (PWA), com o texto bíblico embutido sob licença aberta.

O briefing atende o requisito mínimo do Gate 1: há problema nomeado, há dois públicos
distintos identificados, há diferencial declarado (intercalação AT/NT) e há restrição
de plataforma e de licenciamento já decidida pelo stakeholder. **Não é um "fazer um
app"** — é uma hipótese de produto com mecanismo causal explícito (o leitor abandona
no bloco de lei ritual do AT; a intercalação e a nota de contexto atacam exatamente
esse ponto).

O que o briefing **não** traz e foi resolvido pelos chapéus PM/BA na mesma chamada
(não é lacuna bloqueante, é matéria de descoberta): métrica de sucesso, corte de
escopo do MVP, formato da camada explicativa e regra de intercalação.

### Alinhamento com roadmap

**Resposta explícita: reforça.** Não existe roadmap corporativo prévio (fundador
solo, projeto novo). O "roadmap" disponível é o próprio critério de seleção declarado
pelo stakeholder entre 17 ideias — monetização recorrente sólida, público fiel e
engajado, diferenciação frente a incumbente (YouVersion). A proposta reforça esse
critério em dois pontos verificáveis:

1. O Módulo 2 (preparo de pregação) é ferramenta de trabalho de uso semanal — é a
   parte da proposta com disposição a pagar plausivelmente mais alta e churn mais
   baixo, e é onde o incumbente é mais fraco.
2. O texto bíblico sob licença aberta remove a dependência de negociação com
   sociedade bíblica — que seria, para um fundador solo, a barreira de entrada mais
   provável de matar o projeto antes do lançamento.

Ponto de atenção estratégico (não bloqueante): o diferencial "plano entrelaçado" é
**copiável** por um incumbente em semanas. A defensabilidade real do produto não está
no plano entrelaçado, está na camada editorial (as notas de contexto em linguagem de
leigo) e no Módulo 2. Isso foi levado ao corte de escopo do `PRD.md`.

### Plausibilidade de orçamento/prazo

Sinalização, não estimativa (estimativa detalhada é `capacity-and-timeline-validation`,
com `TASK.md` em mãos):

- **Restrição real declarada**: uma pessoa, infraestrutura limitada. Isso é a
  restrição dominante do projeto, acima de qualquer preferência técnica.
- **Sinal de incompatibilidade encontrado**: o escopo do briefing lido literalmente
  (dois módulos completos + camada explicativa + áudio narrado + plano de 1 ano +
  site institucional + evolução para nativo) **não é compatível** com um dev solo em
  horizonte de MVP. Não reprova o Gate 1 — o Gate 1 avalia a proposta, não o corte —
  mas vira **ressalva vinculante para o chapéu PM**: o `PRD.md` precisa entregar um
  MVP que uma pessoa consiga lançar, com "fora do escopo" explícito e justificado.
- **Sinal de custo**: as duas escolhas do briefing que mais poderiam gerar custo
  recorrente por usuário são áudio narrado e armazenamento/entrega de áudio.
  Restrição de produto registrada no `PRD.md`: nenhuma dependência paga por usuário
  no MVP.
- **Custo de licenciamento de texto**: zero, confirmado (ver "Risco/compliance"
  abaixo).

### Gap de roster

Gaps nomeados (papel/skill faltante), não genéricos:

| # | Gap | Impacto | Dono proposto |
|---|---|---|---|
| G-01 | **Nenhum agente do pipeline produz conteúdo editorial bíblico.** As "Notas do Dia" (camada explicativa) são texto autoral, não software. O pipeline de 4 agentes cobre arquitetura, código, QA e deploy — não cobre redação e revisão teológica. | O MVP pode ficar pronto tecnicamente e não lançar por falta de conteúdo. | Stakeholder (com revisor externo). Registrado como premissa P-06 no `PRD.md`. |
| G-02 | **Nenhum agente produz ou valida áudio.** Se o áudio deixar de ser gerado no dispositivo e virar ativo produzido, não há dono no pipeline. | Baixo no MVP (áudio restrito a custo marginal zero). | Coordenador, se e quando a restrição mudar. |
| G-03 | **Não há papel jurídico no roster.** A conformidade de licença CC e LGPD é tratada em nível estratégico por este agente e em nível tático pelo Validador (chapéu DevSecOps) — não substitui parecer jurídico. | Médio: o produto trata dado pessoal **sensível** (convicção religiosa, LGPD art. 5º, II). | Stakeholder decide se contrata parecer antes do lançamento público. |

Não há gap de roster para o tipo de projeto em si: PWA + backend leve + offline-first
está integralmente coberto pelos chapéus do Coordenador, Executor e Validador.

### Risco e compliance (nível estratégico)

1. **Licença do texto bíblico — verificada, premissa do briefing confirmada.** A
   "Bíblia Livre" (sigla BLIVRE, `porbr2018`), copyright © 2018 Diego Santos, Mario
   Sérgio e Marco Teles, está sob **Creative Commons Attribution 4.0** (link
   declarado na fonte: `creativecommons.org/licenses/by/4.0/br/`), com 66 livros
   (AT completo + NT completo) verificados no índice da edição. Detalhe operacional:
   a fonte declara o texto como "um trabalho em andamento" e **exige a data da versão
   na atribuição**. Fonte: <https://ebible.org/details.php?id=porbr2018> e
   <https://ebible.org/porbr2018/copyright.htm>. Detalhamento e texto literal de
   atribuição no `PRD-TECNICO.md`, Seção 6.
2. **Premissa do briefing refutada**: a alternativa de reserva "A Bíblia Sagrada,
   Tradução para Tradutores" (`portft`, CC BY-SA 4.0, © 2018 Ellis W. Deibler, Jr.)
   **não é Bíblia completa em português** — o índice da edição lista apenas os 27
   livros do NT. Não serve como fonte auxiliar para o AT, que é exatamente onde está
   a dor do produto. Isso pesou na decisão D1 do `PRD.md` (camada explicativa).
3. **Risco de marca/indução a erro (médio, acionável agora)**: o briefing nomeia a
   tradução como "Almeida Atualizada (Bíblia Livre)". "Almeida Revista e Atualizada"
   (ARA) é obra da Sociedade Bíblica do Brasil, com direitos ativos. Usar o rótulo
   "Almeida Atualizada" na interface ou no marketing induz o usuário a acreditar que
   está lendo a ARA e cria exposição desnecessária. **Ressalva vinculante**: o
   produto deve exibir o nome oficial da obra — "Bíblia Livre (BLIVRE)" — e nunca
   "Almeida Atualizada", "ARA" ou "ARC".
4. **LGPD — dado pessoal sensível (médio-alto)**: os dados do produto (plano de
   leitura, progresso, esboços de pregação) revelam convicção religiosa, que é dado
   pessoal **sensível** pela LGPD (art. 5º, II). Consequência estratégica: base legal
   preferencial é consentimento específico e destacado, e vazamento tem severidade
   maior que a de um app de conteúdo comum. Vira RNF no `PRD-TECNICO.md`; a análise
   tática fica com o Validador (chapéu DevSecOps), não aqui.
5. **Risco de execução (alto, é o risco número um do projeto)**: fundador solo. O
   modo de falha mais provável não é técnico — é o MVP não ser lançado por escopo
   grande demais. Mitigação exigida ao chapéu PM: a Fase 1 do MVP tem de ser
   lançável sozinha.
6. **Vendor lock-in**: nenhum identificado nesta fase. O corpus está sob CC BY 4.0 e
   é portável; o PWA não amarra loja de aplicativos.

### Veredito

**Aprovado com ressalvas** — libera os chapéus PM e BA na mesma chamada.

Ressalvas (vinculantes para os artefatos produzidos a jusante nesta mesma chamada):

- **R-01** O `PRD.md` deve cortar escopo para o que um dev solo lança, com Fase 1
  autonomamente lançável, e justificar cada corte.
- **R-02** O produto não pode nomear a tradução como "Almeida Atualizada"/"ARA"/"ARC".
  Nome oficial: "Bíblia Livre (BLIVRE)", com a atribuição exigida pela CC BY 4.0
  visível no produto.
- **R-03** O gap G-01 (conteúdo editorial sem dono no pipeline) tem de virar premissa
  com dono e prazo na Seção 6 do `PRD.md` — não pode ficar implícito.
- **R-04** O adiamento de free-vs-pago é aceito, mas tem de ter data-limite
  registrada: a decisão precisa existir antes de o produto ter usuário pagante ou
  qualquer promessa de preço em público.
- **R-05** Nenhuma dependência de custo por usuário no MVP (áudio, storage de mídia,
  serviço pago de terceiros) sem voltar a este gate.

Nenhuma ressalva bloqueia o início do levantamento. Bloqueio registrado em
`BLOCKERS.md`: nenhum.

---

## Adendo — Rodada 2 do Loop A — 2026-09-07

**Natureza**: não é um gate novo nem um parecer ad hoc. É o registro de mudança de
status de ressalvas e gaps do Gate 1 em função do pressure-test de concepção de produto
executado na rodada 2 do Loop A (skill `the-fool`: pré-mortem, auditoria de evidência,
red team e verificação de viés). O veredito do Gate 1 permanece **Aprovado com
ressalvas** — nada aqui o altera.

### Mudança de status das ressalvas

| # | Ressalva | Status após a rodada 2 |
|---|---|---|
| R-01 | Escopo tem de caber num dev solo | **Reforçada.** O escopo líquido do MVP diminuiu: saiu o áudio inteiro (1 requisito funcional, seus controles, seus estados e a bateria de testes cross-browser associada) e entraram duas adições pequenas (nota no auto-embed, duplicar esboço) mais melhorias de custo marginal ~zero. Saldo negativo em escopo. |
| R-02 | Nome da tradução | Inalterada — cumprida em CA-01.5 e RN-06. |
| R-03 | Conteúdo editorial com dono e prazo | **Cumprida com natureza melhor.** A nota deixou de ser ativo descartável (ancorada ao dia) e passou a ser ativo reutilizável (ancorada à perícope), com o mesmo volume no MVP. |
| R-04 | Data-limite para free-vs-pago | Inalterada — P-04. |
| R-05 | Nenhum custo por usuário no MVP | **Satisfeita de forma mais forte.** O áudio era a única fonte plausível de custo recorrente por usuário do MVP; com o corte, a ressalva deixa de ter exposição conhecida. |

### Mudança de status dos gaps de roster

| # | Gap | Status após a rodada 2 |
|---|---|---|
| G-01 | Nenhum agente produz conteúdo editorial bíblico | **Ainda aberto, mas com mitigação nova.** Duas mudanças reduzem o risco sem eliminá-lo: (a) o acervo passou a ser reutilizável (nota por perícope), então o esforço editorial deixa de ser perdido se o plano mudar; (b) a exigência de autoria e revisão visíveis na nota cria a contrapartida — crédito público — com que o stakeholder pode recrutar o revisor teológico que hoje não existe. Continua sendo o item que mais provavelmente atrasa o lançamento. |
| G-02 | Nenhum agente produz ou valida áudio | **Fechado.** O requisito que criava o gap saiu do MVP. Reabre automaticamente se o stakeholder vetar o corte (Q-10 do `PRD.md`). |
| G-03 | Não há papel jurídico no roster | Inalterado. Nota adicional: a rodada 2 esclareceu que as Notas de Perícope são obra própria e não obra derivada da tradução licenciada — o que **reduz** a superfície de dúvida sobre a CC BY 4.0, mas não substitui parecer jurídico sobre LGPD. |

### Riscos estratégicos — reavaliação

- O **risco de execução (fundador solo)** continua sendo o risco número um, e a rodada
  2 o reduziu na margem, não estruturalmente.
- Surgiu um risco de produto que o Gate 1 não tinha nomeado: **o abandono precoce
  (entre a sessão 1 e a sessão 3)**. O Gate 1 e a rodada 1 aceitaram o enquadramento do
  briefing — que descreve o abandono da semana 3 a 5 — sem perguntar onde o abandono
  realmente começa. Um mecanismo desenhado para o dia 25 nunca é testado se o usuário
  não voltar no dia 2. A correção está em `PRD.md` §1.1 e nas métricas M-07/M-08, que
  dão sinal em 48 horas em vez de 30 dias. Isso é ancoragem no enquadramento recebido —
  padrão comum quando o briefing já vem com um diagnóstico pronto, e vale registrar
  para as próximas rodadas.

**Veredito do adendo**: registro, sem alteração do veredito do Gate 1. Nenhum bloqueio
aberto em `BLOCKERS.md`.

---

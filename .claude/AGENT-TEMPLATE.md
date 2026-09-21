# AGENT-TEMPLATE.md

Template padrão que **todo** agente do pipeline deve seguir. Este documento define a
estrutura; ele não é, em si, um agente. Ao criar cada um dos 12 agentes (ver ordem em
[PIPELINE-CONVENTIONS.md](PIPELINE-CONVENTIONS.md)), preencha as seções abaixo, nesta
ordem, sem omitir nenhuma.

Todo agente é um único arquivo Markdown (`.claude/agents/<slug>.md`) com frontmatter
YAML seguido de corpo em seções fixas.

---

## 1. Frontmatter (metadados)

```yaml
---
name: <slug-kebab-case>              # identificador único, usado para invocar o agente
role: <Nome de exibição>              # ex.: "Software Architect", "CTO / Head de Tecnologia"
pipeline_position: <1-12>             # posição na cadeia de atuação (ver PIPELINE-CONVENTIONS.md)
description: >
  <o que o agente faz, em 1-3 frases> Use quando <situações/gatilhos que
  disparam o uso deste agente>. Do NOT use for <tarefas que pertencem a outro
  agente> — nesses casos use <agente correto>.
tools: <lista mínima de ferramentas necessárias>   # ex.: Read, Grep, Glob, Edit, Write
upstream: [<slug>, <slug>, ...]       # agentes que entregam artefatos PARA este agente
downstream: [<slug>, <slug>, ...]     # agentes que recebem artefatos DESTE agente
triggers:
  - <evento ou etapa do pipeline que aciona este agente>
  - <pode ser mais de um gate — ex.: "antes do PRD", "após o SDD.md", "ad hoc: escalonamento">
---
```

Regras de preenchimento:
- `description` segue sempre o formato **[o que faz] + [Use quando...] + [Do NOT use
  for...]** — é o que permite a um orquestrador (humano ou agente) escolher o papel
  certo sem abrir o arquivo inteiro.
- `tools` lista só o mínimo necessário. Um agente consultivo/revisor (ex.: CTO, PM)
  normalmente não precisa de `Edit`/`Write`/`Bash`; só ganha essas ferramentas se o seu
  output exigir criar/alterar arquivo diretamente.
- `upstream`/`downstream` devem bater exatamente com as colunas "Dono" e "Consumidores"
  da tabela de artefatos em PIPELINE-CONVENTIONS.md — se um agente lê um artefato de
  outro, esse outro precisa aparecer em `upstream`.

---

## 2. Escopo e Responsabilidades

Lista objetiva do que o agente **faz**. Cada item deve ser uma responsabilidade
verificável, não uma aspiração vaga.

```markdown
## Escopo e Responsabilidades
- <responsabilidade 1>
- <responsabilidade 2>
- ...
```

## Guardrails

O que o agente **nunca** deve fazer, e os limites da sua autoridade. Esta seção é a
mais importante do arquivo — é ela que evita que um agente extrapole papel (ex.: QA
"corrigindo" código em vez de reportar bug; Tech Lead reescrevendo requisito de
negócio em vez de escalar para Business Analyst).

```markdown
## Guardrails
- NUNCA <ação proibida> — em vez disso, <ação correta / para quem delegar>.
- NUNCA <ação proibida> — em vez disso, <ação correta / para quem delegar>.
- Limite de autoridade: <o que o agente pode decidir sozinho> vs. <o que exige
  aprovação de outro agente/gate>.
```

Todo guardrail proibitivo deve vir acompanhado da alternativa correta (para onde
delegar/escalar) — um guardrail sem essa segunda metade vira um beco sem saída para o
agente.

## Inputs Esperados

Tabela com todo artefato que o agente lê antes de produzir seu output.

```markdown
## Inputs Esperados
| Artefato | Origem (agente) | Obrigatório? | Se ausente |
|---|---|---|---|
| <nome-do-arquivo.md> | <slug do agente que produz> | Sim/Não | <bloqueia e escala / segue com suposição documentada> |
```

## Outputs Esperados

Tabela com todo artefato que o agente produz ou atualiza, no formato definido em
PIPELINE-CONVENTIONS.md (nome de arquivo, local, versionamento).

```markdown
## Outputs Esperados
| Artefato | Formato | Onde salva | Consumidores |
|---|---|---|---|
| <nome-do-arquivo.md> | <estrutura esperada, seções obrigatórias> | <caminho, ver convenção de pastas> | <slugs dos agentes downstream> |
```

## Critérios de Pronto (Definition of Done)

Checklist objetivo — não "parece bom", e sim itens binários que qualquer pessoa (ou
agente) consegue verificar sim/não. Se o agente atua como gate de aprovação sobre
artefato de outro (ex.: CTO revisando o SDD.md), o veredito final usa sempre a mesma
escala de três valores, para ficar consistente em todo o pipeline:

- **Aprovado** — critérios todos atendidos, segue para o próximo agente sem ressalva.
- **Aprovado com ressalvas** — segue, mas com pendências registradas que não bloqueiam
  o andamento (viram item de acompanhamento, não re-trabalho imediato).
- **Reprovado** — não segue; motivo e o que precisa mudar ficam registrados no próprio
  artefato revisado ou em BLOCKERS.md (ver PIPELINE-CONVENTIONS.md), e o dono do
  artefato é quem re-trabalha, não o revisor.

```markdown
## Critérios de Pronto
- [ ] <critério objetivo 1>
- [ ] <critério objetivo 2>
- ...

Veredito: Aprovado / Aprovado com ressalvas / Reprovado
```

Para agentes que não são gates de aprovação (ex.: Backend implementando uma tarefa),
"Critérios de Pronto" é simplesmente o checklist que define quando aquele output está
pronto para ser consumido pelo próximo agente — sem a escala de veredito.

## Bloqueios e Escalonamento

Como este agente especificamente sinaliza que não consegue prosseguir, e para quem.
Todo agente segue o mecanismo comum descrito em PIPELINE-CONVENTIONS.md
("Governança — reporte de inconsistência"); aqui só se define o caso específico deste
papel.

```markdown
## Bloqueios e Escalonamento
- Bloqueio típico deste agente: <situação mais comum que trava este papel>.
- Escala para: <slug do agente responsável por resolver> (ou CTO, se for conflito
  entre pares sem dono claro).
- Formato do registro: entrada em BLOCKERS.md, campos conforme
  PIPELINE-CONVENTIONS.md — nunca resolvido silenciosamente por conta própria fora do
  próprio escopo.
- Recebe reabertura de: <todo agente que, em seu próprio "Escala para", nomeia este
  papel> — liste-os aqui espelhando cada lugar em que este agente aparece como
  destino de escalonamento em outro arquivo, para o vínculo não ficar declarado só
  do lado de quem envia.
```

Regra padrão de reciprocidade: todo agente **aceita implicitamente** reabertura via
uma entrada em `BLOCKERS.md` endereçada a ele — isso vale mesmo sem uma linha
`triggers` explícita para cada origem possível. Ainda assim, ao criar ou revisar um
agente, confira se cada "Escala para: X" em outro arquivo tem o espelho correspondente
em "Recebe reabertura de" no arquivo de X — evita que o vínculo exista só do lado de
quem escala.

---

## Checklist de auto-revisão antes de considerar um agente pronto

Antes de dar um arquivo de agente por concluído, confirme:

- [ ] `description` tem "Use quando" **e** "Do NOT use for"
- [ ] `tools` é o mínimo necessário (nenhuma ferramenta "por via das dúvidas")
- [ ] `upstream`/`downstream` batem com a tabela de artefatos do PIPELINE-CONVENTIONS.md
- [ ] Todo guardrail proibitivo tem a alternativa/delegação junto
- [ ] Todo input tem uma coluna "Se ausente" preenchida (o agente sabe o que fazer sem
      o artefato)
- [ ] Todo output aparece na tabela de artefatos do PIPELINE-CONVENTIONS.md com o
      mesmo nome de arquivo
- [ ] Critérios de Pronto são verificáveis objetivamente (sem "boa qualidade",
      "suficientemente claro" e afins sem uma régua concreta por trás)

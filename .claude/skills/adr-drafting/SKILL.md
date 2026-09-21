---
name: adr-drafting
description: Documenta decisões arquiteturais relevantes no formato ADR padrão (MADR), como arquivo imutável e numerado em .md/adr/. Use para toda decisão de architecture-design ou tech-stack-selection marcada como relevante o suficiente para registro formal. Do NOT use for decisão de rotina sem consequência estrutural (nem tudo precisa de ADR) ou para revisar/aprovar a decisão (isso é do cto, no Gate 2).
metadata:
  author: software-architect
  version: '1.0.0'
---

# ADR Drafting

Você atua como Software Architect registrando formalmente uma decisão arquitetural já
tomada em `architecture-design` ou `tech-stack-selection` — o ADR é o registro
histórico de *por que* a decisão foi tomada, para quem ler daqui a um ano não precisar
reconstruir o raciocínio do zero.

## Quando é Acionada

- Para toda decisão de `architecture-design` ou `tech-stack-selection` relevante o
  suficiente para merecer registro formal (padrão arquitetural escolhido, escolha de
  stack com trade-off real, decisão que seria cara de reverter).
- Sempre que o CTO reprovar pontualmente uma decisão no Gate 2 — o ADR antigo é
  marcado `Superseded by`, um novo ADR é criado com a alternativa.

Do NOT use for:
- Decisão de rotina sem consequência estrutural (ex.: nome de uma variável de
  configuração) — nem toda escolha técnica precisa de ADR; reserve para decisões que
  um engenheiro futuro genuinamente precisaria entender o "porquê".
- Revisar ou aprovar a decisão registrada — isso é o CTO, no Gate 2, via
  `architecture-decision-review`; esta skill só documenta, não valida.

## Inputs Esperados

- A decisão já tomada em `architecture-design` ou `tech-stack-selection` (obrigatório)
  — contexto, alternativas consideradas, trade-off.
- Se for uma revisão pós-reprovação: o ADR original que está sendo superseded
  (obrigatório nesse caso).

## Core Framework

Usa o formato **MADR** de `create-adr` como padrão do projeto — título como frase
afirmativa (nunca uma pergunta), contexto explicando as forças em jogo (não só o que
foi decidido), decisão com o porquê, consequências positivas E negativas (nunca só as
positivas), alternativas com prós/contras.

Regra de imutabilidade (adotada de `create-adr`, obrigatória neste projeto): um ADR
aceito **nunca é editado**. Mudar de decisão é sempre um novo ADR, com
`Status: Superseded by ADR-NNN` escrito no antigo, apontando para o novo.

## Workflow

1. Confirme se a decisão é relevante o suficiente para ADR (ver "Do NOT use for").
2. Escreva o ADR no formato MADR (usar `create-adr` para o template e o checklist de
   qualidade).
3. Numere sequencialmente — verifique o maior número já existente em `.md/adr/` antes
   de atribuir o próximo.
4. Se for revisão pós-reprovação do Gate 2: marque o ADR antigo como
   `Status: Superseded by ADR-NNN` (não o edite além dessa linha de status) e crie o
   novo ADR com a alternativa aprovada.
5. Salve o arquivo em `.md/adr/NNN-titulo-kebab-case.md`.
6. Atualize o índice na Seção 4 do `SDD.md`, linkando para o novo arquivo.

## Output Esperado

- **Formato**: um arquivo Markdown por decisão, formato MADR (título, status, data,
  contexto, decisão, consequências positivas/negativas, alternativas consideradas).
- **Onde salva**: `.md/adr/NNN-titulo-kebab-case.md` (arquivo próprio, numerado
  sequencialmente — ver PIPELINE-CONVENTIONS.md, exceção de nomenclatura para ADRs).
  A Seção 4 do `.md/SDD.md` indexa e linka, não duplica o conteúdo.

## Critério de Aceite

- [ ] Título é uma frase afirmativa que registra a decisão, não uma pergunta
- [ ] Contexto explica as forças em jogo, não só repete o que foi decidido
- [ ] Consequências incluem trade-off honesto — nunca só os pontos positivos
- [ ] Numeração sequencial confere com o maior número já existente em `.md/adr/`
- [ ] ADR superseded aponta para o novo e vice-versa — nunca um link quebrado ou
      unidirecional
- [ ] Índice na Seção 4 do SDD.md está atualizado com o novo arquivo

### MUST DO
- Verificar o maior número existente em `.md/adr/` antes de numerar um novo ADR.
- Registrar consequência negativa em todo ADR — um ADR só com pontos positivos perde
  credibilidade com o tempo.

### MUST NOT DO
- Editar o conteúdo de um ADR já aceito além da linha de status ao ser superseded.
- Criar ADR para decisão de rotina sem consequência estrutural real — isso infla o
  índice e dilui o valor dos ADRs que realmente importam.

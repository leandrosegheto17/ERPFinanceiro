# ADR-005: Implementar o reconhecimento de referência bíblica como módulo puro compartilhado, com resolução conservadora

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: dominio, parser, qualidade, fase-1

## Contexto e Problema

O reconhecimento de referência (RN-05) é usado por **dois módulos e três funções**:
auto-embed no editor de esboço (RF-13), resolução das referências do plano e das
perícopes na validação de conteúdo (ADR-004), e resolução da nota associada a um
trecho (CA-13.2, RN-13). É a peça compartilhada mais frágil do produto.

A força que decide o desenho é o custo assimétrico do erro, explicitado em RN-05 e
I-05: **inserir o versículo errado num esboço de pregação só é descoberto no púlpito,
em público**. Um falso positivo é catastrófico e irreversível; um falso negativo é um
aviso na tela que o usuário corrige em dois segundos. A regra de ouro — "em qualquer
dúvida, não insere nada" — não é uma preferência de UX, é a especificação do parser.

Além disso, CA-13.3 exige resolução **exclusivamente local, sem rede**.

## Decision Drivers

- Falso positivo é inaceitável; falso negativo é aceitável e visível.
- Lista de formatos **fechada** (RN-05), com exclusões explícitas: intervalos entre
  capítulos, listas, referências sem livro, nomes fora da tabela.
- Uso por dois módulos em fases diferentes — a Fase 2 não pode obrigar a mexer na
  Fase 1.
- Precisa rodar em build (validador de conteúdo, Node) e em runtime (navegador,
  offline).

## Opções Consideradas

- **A. Módulo TypeScript puro, determinístico, com gramática fechada e resultado
  em soma de tipos `Resolved | Unresolved`** ✅ escolhida
- **B. Biblioteca de terceiro de parsing de referência bíblica**
- **C. Heurística tolerante com correção aproximada de nome de livro (fuzzy match)**

## Decision Outcome

Escolhida a opção **A**. Características fixadas por esta decisão:

- **Puro e sem dependências**: nenhuma chamada de rede, nenhum acesso a
  armazenamento, nenhuma dependência de React/DOM/Node. Recebe `(entrada: string,
  índice: CorpusIndex)` e devolve um valor. Isso é o que o torna executável nos três
  contextos (build, navegador, teste) e testável exaustivamente.
- **Resultado explícito**: `Resolved { book, chapter, verseStart, verseEnd,
  normalizedLabel }` ou `Unresolved { reason }`, com `reason` em enumeração fechada
  (`formato-nao-reconhecido`, `livro-desconhecido`, `capitulo-inexistente`,
  `versiculo-inexistente`, `intervalo-invalido`, `ambiguo`). Nunca lança exceção,
  nunca devolve "melhor palpite". A camada de UI mapeia `reason` para a mensagem de
  CA-13.4.
- **Validação contra o corpus real**: o índice de ADR-003 informa contagem de
  capítulos e versículos por livro. `Romanos 8:99` é `Unresolved`, não um intervalo
  truncado silenciosamente.
- **Tabela de nomes explícita e versionada**: nomes canônicos e abreviações de uso
  corrente no Brasil, numerados em arábico e romano, normalizados por
  minúsculas + remoção de acentos (`NFD` + remoção de diacríticos) + colapso de
  espaços e pontos. Toda entrada da tabela é dado, não código.
- **Ambiguidade = `Unresolved`**: se uma entrada casa com mais de um livro da tabela,
  o resultado é `ambiguo` — nunca o primeiro casamento.
- **Nota associada** (RN-05, último parágrafo): a nota é a da perícope que contém o
  intervalo resolvido; havendo sobreposição com mais de uma perícope com nota, o
  módulo devolve **uma só**, a de maior sobreposição, com desempate determinístico
  pelo id da perícope (nunca por ordem de iteração).

**Suíte de teste obrigatória e simétrica**: além da tabela de casos que **devem**
resolver, uma tabela de casos que **não podem** resolver, derivada literalmente das
exclusões de RN-05 — `Romanos 8:28-9:2`, `Rm 8:28; 12:2`, `v. 28`, `Gálatas 7`,
`Salmo 151`, nomes inexistentes. Um teste que passe a resolver um caso desta segunda
tabela é regressão de severidade máxima, no mesmo nível de M-04.

### Consequências Positivas

- A regra de ouro fica no tipo de retorno, não na disciplina de quem chama: não há
  como "esquecer" de tratar o caso não resolvido.
- Cobertura de teste barata e exaustiva — função pura, entrada/saída tabelada.
- A Fase 2 herda a peça pronta e testada da Fase 1, que é a razão declarada de
  sequenciar os módulos (`PRD.md` §4.1).

### Consequências Negativas

- O usuário digitará referências válidas em português que o MVP recusa —
  `Romanos 8:28-9:2` e listas são casos reais no ofício de quem prega. Aceito por
  RN-05, mas é atrito real e o primeiro candidato de ampliação pós-MVP. A mensagem de
  CA-13.4 precisa dizer **por que** não reconheceu, para não parecer defeito
  (`UX-SPEC.md` §4).
- Manter a tabela de abreviações é trabalho editorial contínuo e invisível.
- Sem fuzzy match, erro de digitação (`Romamos 8:28`) não é corrigido — é sinalizado.
  É a consequência deliberada da assimetria de custo.

## Links

- `PRD-TECNICO.md` RN-05, RN-13, CA-13.1 a CA-13.5, I-05
- Relacionado: [ADR-003](003-importar-o-corpus-blivre-em-build-como-artefato-estatico-verificado.md),
  [ADR-012](012-separar-fase-1-e-fase-2-em-modulos-com-dependencia-unidirecional.md)

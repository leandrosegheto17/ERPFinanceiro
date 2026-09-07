# SPIKE-03 — Resultado

- **Spike**: Gramática de reconhecimento de referência bíblica em pt-BR:
  abreviações ambíguas, intervalos entre capítulos, variações de digitação
  reais.
- **Pergunta a responder**: Fechar a tabela de abreviações e a lista de casos
  "não resolvido" antes de escrever a suíte simétrica.
- **Bloqueava**: TASK-010 (`core/reference`).
- **Origem do risco**: RT-05, ADR-005.
- **Status**: Resolvido — 2026-09-07.
- **Base**: `.md/PRD-TECNICO.md` RN-05, `.md/adr/005-implementar-o-reconhecimento-de-referencia-como-modulo-puro.md`, `src/tools/corpus-import/canon.ts` (66 livros, ids USFM já fechados por TASK-006).

---

## 1. Gramática fechada (formatos reconhecidos)

Confirmando literalmente RN-05, insensível a maiúsculas e a acentos (normalização:
minúsculas + NFD com remoção de diacríticos + colapso de espaços múltiplos +
remoção de pontos finais de abreviação):

| Forma | Exemplo | Resultado |
|---|---|---|
| Livro + capítulo + versículo | `Romanos 8:28`, `Rm 8.28` | `Resolved` (verseStart = verseEnd = 28) |
| Livro + capítulo + intervalo de versículos, mesmo capítulo | `Romanos 8:28-30`, `Rm 8.28–30` | `Resolved` (verseStart 28, verseEnd 30) |
| Livro + capítulo inteiro | `Romanos 8`, `Rm 8` | `Resolved` (verseStart = 1, verseEnd = último versículo do capítulo, conforme índice de ADR-003) |
| Livro numerado, forma arábica | `1 Coríntios 13:4`, `1Co 13:4` | `Resolved` |
| Livro numerado, forma romana | `I Coríntios 13:4` | `Resolved` (equivalente ao arábico — ver Seção 2) |

**Decisão de espaçamento** (fecha a lacuna "variações de digitação reais" do
enunciado do spike): é exigido exatamente **um** espaço entre o token do livro
(nome/abreviação, incluindo o prefixo numérico quando houver) e o número do
capítulo. `Rm8:28` (sem espaço) é `Unresolved { reason: "formato-nao-reconhecido" }`
— não é tolerância adicional além do que RN-05 já exemplifica, e evita abrir
ambiguidade entre "onde termina o prefixo numérico do livro e onde começa o
capítulo" em casos como `1Co13:4` vs. `1 Co13:4` vs. `1Co 13:4`.

**Separador capítulo:versículo**: `:` ou `.` (ambos aceitos, conforme os dois
exemplos de RN-05 — `Romanos 8:28` e `Rm 8.28`).

**Separador de intervalo**: hífen `-` ou travessão `–` (ambos aceitos, conforme
`Romanos 8:28-30` e `Rm 8.28–30`).

## 2. Tabela de abreviações fechada (66 livros)

Uma abreviação-base por livro (a normalização de caixa/acento/pontuação cobre as
variações triviais — não é necessário listar `Êx` e `Ex` como entradas
separadas, por exemplo). Livros numerados aceitam o prefixo arábico (`1`, `2`,
`3`) **e** o romano (`I`, `II`, `III`) como formas literalmente distintas (não
equivalentes por normalização — dígito e letra são caracteres diferentes),
conforme o terceiro exemplo de RN-05 (`I Coríntios 13:4`).

### Antigo Testamento (39)

| id (USFM) | Nome canônico | Abreviação | Numerado |
|---|---|---|---|
| GEN | Gênesis | Gn | — |
| EXO | Êxodo | Ex | — |
| LEV | Levítico | Lv | — |
| NUM | Números | Nm | — |
| DEU | Deuteronômio | Dt | — |
| JOS | Josué | Js | — |
| JDG | Juízes | Jz | — |
| RUT | Rute | Rt | — |
| 1SA | 1 Samuel | Sm | 1/I |
| 2SA | 2 Samuel | Sm | 2/II |
| 1KI | 1 Reis | Rs | 1/I |
| 2KI | 2 Reis | Rs | 2/II |
| 1CH | 1 Crônicas | Cr | 1/I |
| 2CH | 2 Crônicas | Cr | 2/II |
| EZR | Esdras | Ed | — |
| NEH | Neemias | Ne | — |
| EST | Ester | Et | — |
| JOB | Jó | *(nenhuma — ver Seção 3, ambiguidade com João)* | — |
| PSA | Salmos | Sl; também aceita a forma singular `Salmo` como alias do nome do livro (uso comum em citação de um só capítulo, ex. "Salmo 23") | — |
| PRO | Provérbios | Pv | — |
| ECC | Eclesiastes | Ec | — |
| SNG | Cânticos | Ct | — |
| ISA | Isaías | Is | — |
| JER | Jeremias | Jr | — |
| LAM | Lamentações | Lm | — |
| EZK | Ezequiel | Ez | — |
| DAN | Daniel | Dn | — |
| HOS | Oséias | Os | — |
| JOL | Joel | Jl | — |
| AMO | Amós | Am | — |
| OBA | Obadias | Ob | — |
| JON | Jonas | Jn | — |
| MIC | Miquéias | Mq | — |
| NAM | Naum | Na | — |
| HAB | Habacuque | Hc | — |
| ZEP | Sofonias | Sf | — |
| HAG | Ageu | Ag | — |
| ZEC | Zacarias | Zc | — |
| MAL | Malaquias | Ml | — |

### Novo Testamento (27)

| id (USFM) | Nome canônico | Abreviação | Numerado |
|---|---|---|---|
| MAT | Mateus | Mt | — |
| MRK | Marcos | Mc | — |
| LUK | Lucas | Lc | — |
| JHN | João | *(nenhuma — só o nome canônico completo; ver Seção 3)* | — |
| ACT | Atos | At | — |
| ROM | Romanos | Rm | — |
| 1CO | 1 Coríntios | Co | 1/I |
| 2CO | 2 Coríntios | Co | 2/II |
| GAL | Gálatas | Gl | — |
| EPH | Efésios | Ef | — |
| PHP | Filipenses | Fp | — |
| COL | Colossenses | Cl | — |
| 1TH | 1 Tessalonicenses | Ts | 1/I |
| 2TH | 2 Tessalonicenses | Ts | 2/II |
| 1TI | 1 Timóteo | Tm | 1/I |
| 2TI | 2 Timóteo | Tm | 2/II |
| TIT | Tito | Tt | — |
| PHM | Filemom | Fm | — |
| HEB | Hebreus | Hb | — |
| JAS | Tiago | Tg | — |
| 1PE | 1 Pedro | Pe | 1/I |
| 2PE | 2 Pedro | Pe | 2/II |
| 1JN | 1 João | Jo | 1/I |
| 2JN | 2 João | Jo | 2/II |
| 3JN | 3 João | Jo | 3/III |
| JUD | Judas | Jd | — |
| REV | Apocalipse | Ap | — |

Todo livro também aceita seu **nome canônico completo** como entrada válida
(ex. "Gênesis 1:1"), além da abreviação da tabela — a abreviação nunca
substitui o nome completo, é um alias adicional.

## 3. Ambiguidade real encontrada: "Jó" × "João" — decisão

A normalização exigida por RN-05 (insensível a acento) reduz `"Jó"` e `"Jo"` ao
mesmo token normalizado (`jo`). Sem tratamento, isso criaria uma colisão real
entre **Jó** (livro do AT) e **João** (evangelho do NT) — exatamente o cenário
que ADR-005 resolve com `Unresolved { reason: "ambiguo" }`, mas aqui a colisão
é **evitável na própria tabela**, não uma ambiguidade genuína de conteúdo (são
nomes de livros completamente distintos que só colidem por causa da
normalização de acento).

**Decisão**: não registrar `"Jo"` como abreviação de **João** (o evangelho).
`"Jó"` continua sendo o único token curto aceito, exclusivo do livro de Jó —
que já é seu nome canônico completo, sem abreviação adicional necessária.
**João** (o evangelho) só é reconhecido pelo nome canônico completo
(`"João"`), nunca pela forma curta de 2 letras. As epístolas joaninas
numeradas (`1 João`/`1Jo`, `2 João`/`2Jo`, `3 João`/`3Jo`) **não** colidem —
o prefixo numérico as distingue de Jó, que nunca é numerado.

Verificação de exaustão: nenhuma outra colisão foi encontrada ao normalizar
(minúsculas + sem acento) todas as abreviações e nomes canônicos das Seções
1–2 — nenhum outro par de livros reduz ao mesmo token. **Consequência para a
suíte de teste de TASK-010**: com a tabela fechada acima, não existe nenhum
caso real de ambiguidade entre dois livros do cânon — o `reason: "ambiguo"` do
tipo `Unresolved` (exigido por ADR-005) precisa ser testado via uma tabela de
abreviações injetada artificialmente na função de resolução (não via um caso
biblicamente real), já que a tabela de produção foi desenhada precisamente
para não ter nenhum.

## 4. Lista de casos "não resolvido" (suíte negativa obrigatória, ADR-005)

Casos que a suíte simétrica de TASK-010 **precisa** confirmar que **não**
resolvem — uma regressão em qualquer um destes é severidade máxima (mesmo
nível de M-04, conforme ADR-005):

| Entrada | `reason` | Motivo |
|---|---|---|
| `Romanos 8:28-9:2` | `formato-nao-reconhecido` | Intervalo entre capítulos — excluído explicitamente por RN-05 |
| `Rm 8:28; 12:2` | `formato-nao-reconhecido` | Lista de referências — excluído explicitamente por RN-05 |
| `v. 28` | `formato-nao-reconhecido` | Referência sem livro — excluído explicitamente por RN-05 |
| `Rm8:28` (sem espaço entre livro e capítulo) | `formato-nao-reconhecido` | Decisão de espaçamento da Seção 1 |
| `Romamos 8:28` (erro de digitação no nome do livro) | `livro-desconhecido` | Sem fuzzy match — consequência deliberada de ADR-005, erro é sinalizado, nunca corrigido |
| `Gálatas 7` | `capitulo-inexistente` | Gálatas tem 6 capítulos (índice de ADR-003) |
| `Romanos 8:99` | `versiculo-inexistente` | Romanos 8 não tem 99 versículos (índice de ADR-003) |
| `Salmo 151` | `capitulo-inexistente` | "Salmo" (singular) é alias válido de Salmos (Seção 2) — o livro existe, mas o cânon protestante só tem 150 salmos; não é `livro-desconhecido` |
| `Jo 3:16` (abreviação de 2 letras para João) | `livro-desconhecido` | Decisão da Seção 3 — `"Jo"` não é uma entrada registrada para nenhum livro sem prefixo numérico |
| `Livro Fantasma 1:1` | `livro-desconhecido` | Nome fora da tabela |

## 5. Consumidores deste resultado

- **TASK-010** (`core/reference`): implementa a gramática e a tabela desta
  Seção 1–3 como dado versionado (não código solto, conforme ADR-005 — "toda
  entrada da tabela é dado, não código"), e a suíte simétrica cobrindo
  integralmente a Seção 4 mais os casos positivos da Seção 1–2.
- **TASK-076** (Lote 14, `ReferenciaEmbutida`) e demais consumidores futuros de
  `core/reference` herdam a mesma gramática sem retrabalho.

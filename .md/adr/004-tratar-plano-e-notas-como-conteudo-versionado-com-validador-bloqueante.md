# ADR-004: Tratar plano, perícopes, notas e ganchos como conteúdo versionado no repositório, com validador executável bloqueando a publicação

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: conteudo, validacao, ci, custo

## Contexto e Problema

A CA-02.5 e a CA-06.6 dizem a mesma coisa por dois caminhos: **se o plano violar
RN-01 (C1–C11) ou a regra de cobertura de RN-13, o plano não publica**. RN-01 traz 11
restrições que são explicitamente descritas como "verificáveis mecanicamente". Isso
não é uma tabela de dados: é um **programa** que precisa existir, rodar e ter poder de
veto.

Duas perguntas decorrem: (a) onde esse conteúdo mora — CMS, banco ou repositório? e
(b) quando o validador roda — em build, no carregamento em runtime, ou só como teste?

O contexto que decide: o acervo editorial é o risco número um de lançamento (G-01 /
P-06), o autor é o próprio stakeholder, e o plano de contingência declarado é
**reduzir de 90 para 60 dias**. Ou seja, o conteúdo vai mudar sob pressão, perto do
lançamento, feito por uma pessoa que não é engenheira. É exatamente o cenário em que
uma violação silenciosa de C2 ("no máximo 1 dia consecutivo de material árido")
passaria despercebida e destruiria o mecanismo causal do produto.

## Decision Drivers

- CA-02.5 exige poder de veto real sobre a publicação.
- RNF-06: nenhum CMS pago, nenhum serviço com custo por uso.
- R-01: o stakeholder-autor precisa de retorno imediato sobre o próprio texto, sem
  esperar um deploy para descobrir que quebrou uma restrição.
- RN-13: a nota é ancorada à perícope e reutilizada em três contextos — o modelo de
  conteúdo precisa refletir isso, não o dia do plano.

## Opções Consideradas

- **A. Conteúdo como código no repositório + validador executável como gate de CI**
  ✅ escolhida
- **B. Conteúdo em tabelas do Postgres + validação no servidor ao publicar**
- **C. CMS headless de terceiro (Sanity, Contentful, Strapi gerenciado)**

## Decision Outcome

Escolhida a opção **A**.

**Modelo de conteúdo** (três arquivos-fonte, um por conceito, refletindo RN-13):

- `content/pericopes.json` — id, referência inicial e final, título curto.
- `content/notes/{id}.md` — front-matter (`pericopeId`, `author`, `reviewer`,
  `reviewedAt`) + corpo de 120–200 palavras.
- `content/plan-entrelacado-90.json` — dias 1..N, cada um com porção AT
  (referência + categoria de RN-01), porção NT, `noteIds[]` e `hook` (≤ 140
  caracteres).

**Validador** (`npm run content:validate`, TypeScript puro, sem I/O de rede) verifica,
com o `corpus/index.json` de ADR-003 em mãos: C1, C2, C3, C4 (contagem real de
palavras extraída do corpus, não declarada pelo autor), C5, C6, C7, C8, C9, C11, a
regra de cobertura de RN-13, o tamanho de cada nota (CA-06.3), a presença de autor
(CA-06.4) e a resolubilidade de **toda** referência do plano contra o corpus. C10 é
uma restrição de interface, não de dados: é verificada por teste de UI sobre a
nomenclatura da tela, não aqui.

**Onde roda — nos três pontos, com o mesmo código**:

1. **Localmente**, como CLI, para o autor rodar a cada nota escrita (retorno em
   segundos, é o que torna a restrição utilizável por quem não é engenheiro);
2. **Em CI**, como gate obrigatório — falha = build falha = nada publica. É aqui que
   mora o poder de veto de CA-02.5;
3. **Em teste automatizado**, com o mesmo relatório anexado ao artefato de conteúdo.

O bundle de conteúdo publicado carrega o `validationReport` (versão do validador,
data, restrições verificadas). O cliente **não** revalida em runtime: revalidar 11
restrições no dispositivo a cada carregamento gastaria bateria e tempo de tela para
reconfirmar algo que já é imutável no artefato. O cliente apenas recusa um bundle sem
relatório válido.

O número de dias do plano é **parâmetro** do validador, não constante — o plano B de
P-06 (reduzir para 60 dias, nunca abaixo de 30) não exige mudança de código.

### Consequências Positivas

- Poder de veto real e barato; violação de C2 é impossível de publicar.
- Custo zero: nenhum serviço, nenhum banco, nenhuma licença de CMS.
- Nota ancorada à perícope de verdade — reutilizada nos três contextos (RF-02,
  CA-01.6, CA-13.2) a partir de um único registro.
- Revisão teológica ganha um mecanismo natural: cada nota é um arquivo, revisada por
  Pull Request, com autoria e revisor rastreáveis — o que reforça a moeda de
  recrutamento de revisor descrita em RN-04.

### Consequências Negativas

- **O autor precisa de git.** É a maior fricção desta decisão, e ela recai justo sobre
  o caminho crítico de lançamento (P-06). Mitigações no MVP: um script único
  (`content:new-note`) que cria o arquivo com o front-matter pronto, e o CLI de
  validação com mensagens em português apontando a linha. Um editor git-based
  (Decap/TinaCMS, ambos gratuitos e estáticos) fica como evolução, fora do MVP.
- Corrigir um erro de digitação numa nota exige deploy. Aceitável: o deploy é estático
  e leva minutos (ADR-008).
- O conteúdo entra no bundle do app; crescer o acervo cresce o download. Mitigado por
  bundle de conteúdo separado do bundle de código e versionado por hash.

## Links

- `PRD-TECNICO.md` RN-01, RN-04, RN-13, CA-02.5, CA-06.6, E-08; `PRD.md` §4.2, P-06
- Relacionado: [ADR-003](003-importar-o-corpus-blivre-em-build-como-artefato-estatico-verificado.md)

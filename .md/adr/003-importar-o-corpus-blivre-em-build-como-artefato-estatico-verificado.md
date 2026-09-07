# ADR-003: Importar o corpus BLIVRE em build como artefato estático versionado e verificado por hash

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: dados, licenca, integridade, custo

## Contexto e Problema

O corpus é o ativo central do produto (RA-01) e vem com três obrigações que, juntas,
determinam a arquitetura de dados:

1. **RNF-08** — a leitura do corpus não pode depender de terceiro em runtime.
2. **RNF-10** — o texto exibido deve ser **byte-idêntico** ao da fonte importada.
3. **RN-06** — a atribuição exigida pela CC BY 4.0 inclui a **data da versão
   importada**, porque a fonte declara o texto como trabalho em andamento. Isso não é
   uma string de rodapé: é um dado de proveniência que precisa refletir o snapshot
   efetivamente servido.

Some-se a isso RNF-06 (sem custo por usuário) e RNF-02 (texto legível em ≤ 2,5 s em
4 Mbps): servir ~31 mil versículos por uma API própria é custo de banco e de
computação por requisição, para um dado que **nunca muda entre versões**.

## Decision Drivers

- Integridade verificável, não confiada: "byte-idêntico" precisa de um teste, não de
  uma promessa.
- Proveniência rastreável: a data da versão precisa vir do snapshot, não de uma
  constante que alguém esqueceu de atualizar.
- Custo marginal ≈ zero e primeira leitura rápida.
- Simplicidade operacional: nenhum serviço a manter no caminho crítico da leitura.

## Opções Consideradas

- **A. Pipeline de importação em build gerando artefatos estáticos imutáveis,
  verificados por hash** ✅ escolhida
- **B. Corpus em tabelas do Postgres, servido pela API**
- **C. Consumo direto da API do eBible.org em runtime**

## Decision Outcome

Escolhida a opção **A**. Um pipeline determinístico (`npm run corpus:import`,
TypeScript, executado em CI e localmente) faz:

1. **Aquisição** — baixa o snapshot `porbr2018` do eBible.org e grava o arquivo bruto
   original em `content/source/` junto do seu SHA-256 e da data declarada da versão.
2. **Parse e normalização** — converte para um modelo canônico
   `livro → capítulo → versículo`, sem alterar o texto: nenhuma normalização de
   acentuação, aspas, hífens ou espaços. Qualquer transformação de apresentação é do
   render, não do dado.
3. **Verificação** — reconstitui o texto plano a partir do modelo canônico e compara
   **byte a byte** com a extração do arquivo bruto. Divergência **falha o build**.
   Adicionalmente confere o inventário: 66 livros, contagem de capítulos e de
   versículos por livro.
4. **Projeção** — gera três artefatos a partir da mesma fonte canônica:
   - `corpus/index.json` — livros, nomes, abreviações aceitas, contagem de capítulos e
     de versículos (insumo obrigatório do parser, ADR-005);
   - `corpus/{livro}/{capitulo}.json` — unidade de leitura (RNF-02);
   - `corpus/bundle.br` — corpus integral comprimido, unidade de provisionamento do
     Módulo 2 (ADR-006).
5. **Manifesto** — `corpus/manifest.json` com `sourceId`, `sourceVersionDate`,
   `importedAt`, licença, URL canônica da licença, titulares e SHA-256 de **cada**
   arquivo gerado.

A string de atribuição de RN-06 é **renderizada a partir do manifesto**, nunca
digitada em código. O produto não tem como exibir uma data de versão diferente da que
está servindo — e um teste automatizado falha se a tela de atribuição divergir do
manifesto. O manifesto também registra explicitamente `modifications: none`, que é a
outra exigência da CC BY 4.0.

Os artefatos são publicados como arquivos estáticos com nome versionado por hash e
`Cache-Control: public, max-age=31536000, immutable`.

### Consequências Positivas

- RNF-08 satisfeito por construção: em runtime não existe terceiro no caminho — só
  arquivo estático no CDN e cópia local.
- RNF-10 vira um teste de build que falha, não uma afirmação em documento.
- A obrigação de licença fica presa ao dado: trocar o snapshot troca a atribuição.
- Custo marginal ≈ zero: bytes estáticos em CDN de banda ilimitada (ADR-008), e o
  cliente baixa cada capítulo uma única vez na vida.

### Consequências Negativas

- Atualizar o corpus exige **deploy**, não uma escrita em banco. Aceitável: a fonte é
  atualizada raramente e a atualização precisa de reverificação de integridade de
  qualquer forma.
- Duplicação deliberada de bytes no CDN (capítulos + bundle integral, ~10 MB no
  total, a confirmar na primeira execução do pipeline). Irrelevante em custo, mas é
  duplicação e precisa de uma fonte canônica única — que é o modelo intermediário do
  pipeline, não um dos dois artefatos.
- Se a fonte sair do ar, não há reimportação possível sem o arquivo bruto — por isso
  o snapshot bruto é **versionado no repositório**, não só baixado.

## Links

- `PRD-TECNICO.md` RNF-08, RNF-10, RN-06, E-01, P-01; `CTO-REVIEW.md` Gate 1, item 1
- Relacionado: [ADR-005](005-implementar-o-reconhecimento-de-referencia-como-modulo-puro.md),
  [ADR-006](006-garantir-o-offline-duro-por-esboco-autocontido.md)

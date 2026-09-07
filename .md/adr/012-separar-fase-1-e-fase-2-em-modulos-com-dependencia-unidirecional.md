# ADR-012: Separar Fase 1 e Fase 2 em módulos de funcionalidade com dependência unidirecional, num monorepo de pacotes

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: modularidade, fases, risco-de-lancamento

## Contexto e Problema

"A Fase 1 é lançável sozinha" (`PRD.md` §4.1) é a mitigação declarada do **risco
número um do projeto** (Gate 1, risco 5: o MVP não ser lançado). Se essa separação
existir só como intenção no papel, ela evapora no primeiro atalho de implementação —
um import do Módulo 2 dentro de uma tela do Módulo 1, uma tabela compartilhada, um
`if (moduloDoisHabilitado)` espalhado — e o produto volta a ser tudo-ou-nada.

Ao mesmo tempo, a razão declarada para os dois módulos viverem juntos é o **ativo
compartilhado**: corpus, parser de referência e acervo de notas (`PRD.md` §1.3). A
separação não pode virar duplicação desses ativos, ou perde-se justamente a sinergia
que justifica o produto.

Ou seja: é preciso uma fronteira que **separe as funcionalidades sem separar o núcleo**.

## Decision Drivers

- Fase 1 tem de ir a produção com a Fase 2 inexistente, sem código morto e sem risco.
- O núcleo (corpus, referência, conteúdo) tem de ser um só, usado pelos dois.
- Um dev solo não sustenta ferramenta de monorepo complexa.
- Domínio precisa continuar agnóstico de framework (ADR-001, ADR-002).

## Opções Consideradas

- **A. Monorepo simples com pacotes de núcleo + módulos de funcionalidade com
  dependência unidirecional e carregamento por rota** ✅ escolhida
- **B. Aplicação única com pastas por funcionalidade e uma flag de recurso**
- **C. Dois repositórios/aplicações separadas compartilhando um pacote publicado**

## Decision Outcome

Escolhida a opção **A**. Três camadas, com a regra de dependência apontando sempre
para baixo:

```
núcleo (Fase 1)        core/corpus · core/reference · core/content · core/storage · core/sync · core/design
funcionalidades F1     features/reading · features/plan · features/identity · features/engagement · features/telemetry
funcionalidades F2     features/outline · features/presentation
```

Regras que tornam a separação real, e não intencional:

1. **Dependência unidirecional**: `features/*` importa de `core/*`; `core/*` nunca
   importa de `features/*`; `features/outline` e `features/presentation` **podem**
   importar de `core/*` e **nunca** de outra `features/*` da Fase 1. Violação disso é
   erro de lint (`eslint-plugin-boundaries` ou equivalente), não convenção verbal.
2. **Carregamento por rota**: o Módulo 2 é um chunk carregado sob demanda pela rota
   `/preparo`. Na Fase 1 a rota simplesmente não é registrada — não há flag, não há
   código morto no bundle, não há tela escondida atrás de condicional. "Lançar sem a
   Fase 2" é não registrar uma rota.
3. **Schema do banco por fase**: as tabelas de esboço (`outline`, `outline_version`)
   chegam numa migration própria da Fase 2. A Fase 1 vai a produção com um schema que
   não as contém, e nenhuma tabela da Fase 1 tem coluna que exista apenas por causa da
   Fase 2.
4. **`core/*` é agnóstico de framework**: nenhum import de React, DOM ou API de
   navegador dentro do núcleo de domínio (`corpus`, `reference`, `content` e as regras
   de conciliação). Acesso a armazenamento e rede entra por interface injetada. É o
   que mantém ADR-001 viável e o que permite o validador de conteúdo (ADR-004) rodar
   o **mesmo** código em Node.
5. **Monorepo pobre de propósito**: workspaces do gerenciador de pacotes e caminhos do
   TypeScript. Nenhum orquestrador de build adicional — R-01 não paga por isso.

**Consequência direta na decomposição** (insumo do Loop C): os lotes da Fase 1 podem
ser fechados e lançados sem nenhuma tarefa da Fase 2, e as tarefas de
`features/outline`/`features/presentation` dependem apenas de `core/*` já pronto —
o que dá paralelismo real entre tarefas de módulos distintos dentro da Fase 2.

### Consequências Positivas

- "Fase 1 lançável sozinha" vira propriedade verificável do build, não promessa.
- O ativo compartilhado permanece único: uma implementação de corpus, uma de parser,
  um acervo de notas servindo os três contextos de RN-13.
- Bundle da Fase 1 não carrega um byte do Módulo 2.
- Fronteiras claras dão ao Executor tarefas com pouca sobreposição de arquivo — menos
  conflito entre instâncias paralelas.

### Consequências Negativas

- Um pouco de cerimônia de estrutura (pacotes, aliases, regra de lint) num projeto de
  uma pessoa, que "funcionaria" com uma pasta só.
- A regra de "nenhuma `features/*` importa outra" às vezes obriga a promover código
  para `core/*` cedo demais, ou a duplicar um utilitário pequeno. É o custo conhecido
  de manter o grafo acíclico.
- Refatorar a fronteira depois (por exemplo, se a Fase 3 pedir que o plano conheça
  esboços) exige um ADR novo, não um import direto.

## Links

- `PRD.md` §4.1, §1.3; `CTO-REVIEW.md` Gate 1, risco 5; `PRD-TECNICO.md` §5.1
- Relacionado: [ADR-002](002-concentrar-a-logica-de-negocio-no-cliente-com-servidor-de-sincronizacao.md),
  [ADR-005](005-implementar-o-reconhecimento-de-referencia-como-modulo-puro.md),
  [ADR-011](011-adotar-react-typescript-e-vite-com-tokens-em-css-custom-properties.md)

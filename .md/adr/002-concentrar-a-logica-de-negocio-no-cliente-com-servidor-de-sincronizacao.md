# ADR-002: Concentrar a lógica de negócio no cliente e manter o servidor como substrato de sincronização e identidade

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: arquitetura, offline, simplicidade-operacional

## Contexto e Problema

Um produto que precisa funcionar sem rede (RNF-01) e resolver referências bíblicas
sem terceiro em runtime (RNF-08, CA-13.3) já é obrigado a ter o corpus, o parser e as
regras de exibição **no dispositivo**. A pergunta arquitetural real não é "onde fica a
lógica offline", e sim: **essa mesma lógica também existe no servidor?**

A resposta convencional (backend com regras de negócio + cliente com uma cópia
simplificada para offline) produz duas implementações da mesma regra em dois runtimes.
Para um time, isso é dívida gerenciável. Para **um desenvolvedor solo** (R-01), é a
forma mais barata de criar divergência silenciosa: o cálculo de constância no servidor
começa a discordar do cálculo no cliente e ninguém percebe até um usuário reclamar.

Contrapeso honesto: concentrar a lógica no cliente significa **confiar no cliente**.
Isso é inaceitável em domínios com valor econômico ou dado compartilhado entre
usuários — e é aceitável aqui? Essa é a força a pesar.

## Decision Drivers

- Uma pessoa mantém tudo (R-01): uma implementação por regra, não duas.
- RNF-01/CA-13.3 já forçam a regra a existir no cliente — a escolha é sobre duplicar
  ou não.
- RNF-09: todo dado é privado por titular; **não há dado compartilhado entre
  usuários**, não há moeda, não há placar público, não há nada que um usuário ganhe
  falsificando o próprio progresso.
- RNF-06: menos computação no servidor = menos superfície de custo variável.

## Opções Consideradas

- **A. Cliente autoritativo; servidor como substrato de sincronização e identidade**
  ✅ escolhida
- **B. Servidor autoritativo com API de domínio; cliente com cache offline**
- **C. Lógica duplicada deliberadamente nos dois lados, com testes de paridade**

## Decision Outcome

Escolhida a opção **A**. O servidor guarda, autentica e concilia; ele **não** decide
regra de negócio. Concretamente:

- O servidor **não** calcula "dia corrente" (RN-09), constância (RN-02), cobertura de
  nota (RN-13), validade do plano (RN-01) nem resolução de referência (RN-05). Todos
  são derivados no cliente a partir de dados que o servidor apenas armazena.
- O servidor **é** autoritativo em exatamente quatro coisas, todas estruturais e não
  de negócio: (i) identidade e sessão; (ii) propriedade da linha — nenhum usuário lê
  ou escreve dado de outro (RLS, RNF-09); (iii) a regra de conciliação de escrita
  concorrente (ADR-007); (iv) o relógio de servidor usado para carimbar recebimento.
- Progresso é gravado como **fato append-only** (`(user, plano, dia) → primeira
  conclusão`), não como estado calculado — o que torna a união de RN-08 uma
  consequência do schema, não de código de aplicação.

O critério que torna isso aceitável está explícito e é o limite da decisão: **este
padrão vale enquanto o dado for privado por titular e sem valor econômico**. Se o
produto ganhar cobrança, ranking público, conteúdo compartilhado ou plano de igreja
(F-10), esta decisão precisa ser reaberta com um novo ADR — não estendida por inércia.

### Consequências Positivas

- Uma implementação por regra. Não existe "versão do servidor" para divergir.
- Toda funcionalidade nasce funcionando offline por construção, em vez de ganhar
  offline como adaptação posterior.
- O backend fica pequeno o bastante para caber num serviço gerenciado com plano
  gratuito (ADR-008), o que sustenta RNF-06.
- Depuração de um bug de regra acontece num runtime só, com o dado do próprio
  dispositivo à mão.

### Consequências Negativas

- **Um usuário pode falsificar o próprio progresso** com o devtools aberto. Isso
  polui as métricas do `PRD.md` §3 (D30, M-02, M-07). Aceito conscientemente: sem
  incentivo (não há prêmio, medalha ou pagamento — F-17 proíbe gamificação), o
  volume esperado é irrelevante frente à amostra de 200 usuários.
- Mudança de regra de negócio exige que o usuário atualize o app; usuários com
  service worker antigo rodam a regra antiga por até um ciclo de atualização.
  Mitigação: versionar o bundle de conteúdo e o schema local, e forçar atualização do
  SW no próximo carregamento com rede.
- Nenhuma validação de invariante de negócio no servidor significa que um bug do
  cliente grava dado inconsistente. Mitigação: as invariantes que importam viram
  **restrição de banco** (unicidade, `NOT NULL`, `CHECK`), não código.

## Links

- `PRD-TECNICO.md` RNF-01, RNF-08, RNF-09, RN-02, RN-08, RN-09; `CTO-REVIEW.md` R-01
- Relacionado: [ADR-007](007-sincronizar-por-outbox-local-com-regra-por-entidade.md),
  [ADR-008](008-usar-supabase-como-backend-gerenciado-e-cloudflare-pages-como-entrega.md)

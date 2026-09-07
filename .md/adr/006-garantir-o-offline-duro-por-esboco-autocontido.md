# ADR-006: Garantir o offline duro por esboço autocontido no dispositivo e provisionamento integral do corpus no Módulo 2

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: offline, armazenamento, risco-critico, fase-2

## Contexto e Problema

RNF-01 é o requisito mais rígido do produto e o único cuja falha acontece **em
público, no púlpito** (M-04, meta = 0, severidade máxima). Ele exige: sem qualquer
conectividade, abrir a lista de esboços materializados, entrar no modo apresentação e
navegar por todos os cartões com texto integral dos versículos e notas, com o primeiro
cartão em ≤ 1 s. E declara reprovação se **qualquer** conteúdo do cartão depender de
requisição de rede.

O desenho ingênuo — guardar o esboço com referências e resolver o texto do versículo
no cache do corpus na hora de exibir — reprova em dois cenários reais: (a) o capítulo
referenciado nunca foi lido, então não está em cache; (b) o navegador expurgou parte
do armazenamento entre o preparo e a pregação. Ambos produzem exatamente a falha que
o requisito existe para impedir.

Há ainda uma ameaça de plataforma que o `PRD-TECNICO.md` não podia antecipar e que é
determinante aqui: **o Safari/iOS apaga armazenamento gravável por script após 7 dias
de inatividade do site** — a menos que o PWA esteja **instalado** na tela de início.
O cenário do produto é literalmente "preparei no domingo passado, prego no próximo
domingo".

## Decision Drivers

- Nenhuma resolução de conteúdo em tempo de apresentação — nem local, nem em cache.
- O usuário precisa **verificar antes de subir ao púlpito** que está pronto
  (RN-10, CA-15.3, CA-16.3).
- CA-13.3: resolução de referência no editor também é 100% local, mesmo com rede.
- RNF-03: ≤ 50 MB por usuário, com liberação por LRU e aviso — nunca descarte
  silencioso de esboço materializado.

## Opções Consideradas

- **A. Esboço materializado como documento autocontido + corpus integral provisionado
  ao entrar no Módulo 2** ✅ escolhida
- **B. Esboço com referências + resolução no cache local do corpus na apresentação**
- **C. Precarga do corpus integral para todos os usuários, inclusive Módulo 1**

## Decision Outcome

Escolhida a opção **A**, com três mecanismos:

**1. O esboço materializado é autocontido.** A materialização (RN-10) grava, num
registro único no IndexedDB, um *snapshot* imutável de tudo que a apresentação
precisa: texto de cada bloco, texto **integral** dos versículos de cada referência
resolvida, texto da nota de perícope embutida com autoria, ordem dos blocos, e a
versão do corpus e do bundle de conteúdo usados. Em tempo de apresentação, o
renderizador lê **um** registro e não consulta nenhuma outra store, nenhum cache HTTP
e nenhuma rede. Isso é o que torna o ≤ 1 s alcançável e o que torna o requisito
testável de forma direta: o teste e2e do modo apresentação roda com a rede desligada
**e** com o cache de corpus limpo.

**2. O Módulo 2 provisiona o corpus integral, uma vez.** Ao entrar no Módulo 2 pela
primeira vez, o cliente baixa `corpus/bundle.br` (ADR-003) e o grava localmente.
Motivo: CA-13.3 exige que o auto-embed resolva referência **sem rede**, e um usuário
pode referenciar qualquer um dos 66 livros. Provisionar tudo de uma vez é mais simples
e mais previsível que negociar chunk a chunk, e o custo é uma transferência única de
poucos megabytes (dimensão exata a confirmar na primeira execução do pipeline). O
Módulo 1 continua com cache oportunista por capítulo — F-14 mantém a leitura livre
fora do offline obrigatório.

**3. O selo de materialização é verificado, não lembrado.** O selo exibido na lista
(CA-16.3) não é um booleano gravado no momento do salvamento: é o resultado de uma
verificação real do snapshot no dispositivo — existe, está íntegro, e a versão do
corpus confere. Isso é o que faz o produto sobreviver a um expurgo do navegador: se o
snapshot sumiu, o selo cai para "pendente" e o aviso de CA-15.3 aparece **antes** da
apresentação, não durante.

**Contenção da ameaça de expurgo (iOS 7 dias)**, em três camadas:
- solicitar `navigator.storage.persist()` no primeiro salvamento de esboço;
- convidar à instalação na tela de início antes da primeira apresentação, com o motivo
  dito em português claro — PWA instalado é isento do cap de 7 dias e habilita push
  (ADR-010). O convite é o item de UX de maior consequência do Módulo 2
  (`UX-SPEC.md` §1.7);
- rematerializar automaticamente quando o app abre com rede e detecta snapshot
  ausente ou desatualizado.

**Orçamento e expurgo local (RNF-03)**: prioridade de descarte é (1) capítulos do
corpus em cache oportunista, (2) o bundle integral do corpus se o Módulo 2 não for
usado há mais de 60 dias, (3) versões perdedoras de esboço já expiradas. Snapshot de
esboço materializado **nunca** é descartado silenciosamente; ao chegar no teto, o
usuário é avisado e escolhe (RNF-03).

### Consequências Positivas

- RNF-01 deixa de depender do estado de caches e passa a depender de um registro
  único — o que é verificável, testável e reparável.
- CA-13.3 satisfeita literalmente: o editor resolve referência offline para qualquer
  livro.
- O selo de materialização passa a significar o que o usuário acha que significa.

### Consequências Negativas

- **Duplicação de texto**: o mesmo versículo existe no corpus local e dentro de cada
  snapshot que o referencia. É desperdício deliberado de espaço em troca de garantia —
  e é pequeno frente ao teto de 50 MB.
- **Snapshot pode envelhecer** em relação ao corpus/notas. Mitigado por versão gravada
  no snapshot e rematerialização quando há rede; mas um esboço não aberto há meses
  pode apresentar a versão antiga da nota. Aceito: nunca apresentar conteúdo
  desatualizado seria pior do que nunca apresentar nada.
- **Provisionamento inicial do Módulo 2 é um estado de espera** que o usuário não pediu
  — precisa ser não bloqueante e honesto na UI, e é a única fricção na entrada do
  módulo.
- Não elimina o risco de expurgo em iOS **não instalado**: reduz e torna visível, não
  anula. Continua como risco RT-01 do `SDD.md` §6.

## Links

- `PRD-TECNICO.md` RNF-01, RNF-03, RN-10, RN-14, CA-13.3, CA-15.1 a CA-15.4, CA-16.3
- Relacionado: [ADR-001](001-adotar-pwa-instalavel-como-unica-plataforma-cliente-do-mvp.md),
  [ADR-003](003-importar-o-corpus-blivre-em-build-como-artefato-estatico-verificado.md),
  [ADR-010](010-entregar-o-lembrete-por-web-push-com-chaves-proprias.md)

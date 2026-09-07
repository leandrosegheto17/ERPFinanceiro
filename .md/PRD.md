# PRD.md — App de Leitura Bíblica Guiada + Preparo de Estudo

- **Versão**: rascunho rodada 2 (Loop A do `/planejar`)
- **Data**: 2026-09-07
- **Autor**: gestor (chapéu PM)
- **Habilitado por**: `CTO-REVIEW.md` → Gate 1 (2026-09-07), Aprovado com ressalvas
  R-01 a R-05; adendo de rodada 2
- **O que mudou na rodada 2**: pressure-test de concepção dos dois módulos
  (`the-fool`). Resumo das mudanças no final do documento ("Registro de rodadas").

---

## 1. Problema e Contexto

### 1.1 Dor A — o leitor leigo abandona a leitura bíblica

Planos de leitura tradicionais são sequenciais, livro a livro, a partir de Gênesis. O
leitor sem formação teológica atravessa Gênesis e Êxodo com relativo engajamento
(material narrativo) e **encontra, por volta da terceira a quinta semana, um bloco
contínuo de material legal e ritual (Levítico, Números) e genealógico**, sem
narrativa, sem contexto e sem explicação de por que aquilo existe. O abandono
acontece nesse ponto, e o efeito é permanente para aquela tentativa.

Mas esse **não é o único ponto de abandono, e provavelmente não é o primeiro**. Há
dois abandonos distintos, com causas diferentes, e o produto precisa atacar os dois:

| | **Abandono precoce** (entre a sessão 1 e a sessão 3) | **Abandono do muro** (semanas 3–5) |
|---|---|---|
| Causa | Nada acontece entre uma sessão e a outra. Não há motivo para voltar além do dever. A sessão anterior fechou sem deixar nada em aberto, e o lembrete diz apenas "hora de ler". | O plano entrega um bloco contínuo de material árido, sem contexto, e o leitor conclui que "não é para ele". |
| Alavanca | **Continuidade**: cada sessão precisa deixar uma pergunta aberta e o lembrete precisa carregar essa pergunta, não uma frase genérica. | **Ordem + explicação**: intercalar AT/NT com restrições que impedem blocos áridos contínuos, e explicar por que aquele texto está ali. |
| Onde vira requisito | RN-04 (gancho de continuidade) + RF-05 (lembrete carrega o gancho) | RN-01 (intercalação) + RF-06 (Nota da Perícope) |

O produto trata os dois como hipóteses a medir, não como fatos: não existe baseline
público confiável de retenção de plano de leitura bíblica por dia (Seção 3).

> **Nota de honestidade intelectual (rodada 2)**: a rodada 1 deste documento tratava
> só o abandono do muro, porque era o abandono que o briefing descrevia. Um mecanismo
> perfeito para o dia 25 nunca chega a ser testado se o usuário não voltar no dia 2.
> A tabela acima é a correção desse foco.

### 1.2 Dor B — o líder/pastor prepara e consulta pregação em ferramenta genérica

O líder de célula ou pastor prepara a fala em casa, com calma, organizando tópicos,
versículos de apoio e ilustrações; depois, ao vivo, usa o celular no púlpito como
apoio de consulta. Hoje isso é feito em app de notas genérico (Notas, Google Keep,
Word) ou papel. Quatro atritos concretos:

1. **Inserir versículo dá trabalho** — sair do app de notas, procurar o texto, copiar,
   colar, formatar. Repetido a cada referência.
2. **A tela de edição é a tela de apresentação** — texto corrido, fonte pequena, tema
   claro, notificações aparecendo por cima. Não é feito para ser lido de relance a um
   metro de distância, em pé, com iluminação ruim.
3. **Falha de conexão ao vivo é catastrófica** — se o app depende de rede para exibir
   a nota durante a pregação, o pior cenário do usuário acontece em público.
4. **Cada preparo começa do zero** — o ofício real é recorrente e repetitivo: a mesma
   série de quatro semanas, o mesmo estudo de célula repetido em outro grupo, o
   esboço do ano passado. Uma ferramenta que trata cada esboço como documento órfão
   obriga a redigitar o que já existe.

### 1.3 Por que os dois no mesmo produto

Não é um pacote arbitrário, e a razão vai além do compartilhamento de infraestrutura:

- **Ativo compartilhado**: o corpus bíblico embutido e a capacidade de reconhecer e
  renderizar uma referência ("Romanos 8:28"). O Módulo 2 é barato de construir
  **depois** que o Módulo 1 existe, e caro de construir sozinho.
- **Sinergia de produto** (identificada na rodada 2): o acervo de Notas de Perícope
  criado para o Módulo 1 é exibido também no Módulo 2, quando o líder insere uma
  referência. O esboço deixa de ser um bloco de notas com texto bíblico colado e passa
  a ser um bloco de notas que **sabe alguma coisa sobre o texto**. Nenhum app de
  leitura bíblica faz isso (não têm editor de esboço) e nenhum app de notas faz isso
  (não têm corpus nem acervo de contexto). É o ativo mais defensável do produto, e ele
  só existe porque os dois módulos vivem juntos.
- **Sobreposição de público**: o líder também é leitor.

### 1.4 Contexto de restrição

- Time: **uma pessoa**, infraestrutura limitada (declarado pelo stakeholder).
- Plataforma fixada pelo stakeholder: **web responsivo / PWA primeiro**; nativo depois.
- Texto bíblico fixado pelo stakeholder: tradução sob licença aberta, sem negociação
  com sociedade bíblica.
- Concorrente de referência: YouVersion/Bible App — incumbente com escala, catálogo de
  traduções e distribuição. O produto **não** compete em catálogo nem em features
  genéricas.

---

## 2. Público-Alvo

### 2.1 P1 — "O leitor que já tentou e parou" (Módulo 1)

- Adulto evangélico brasileiro, 25–55 anos, celular como dispositivo primário de
  leitura.
- **Já tentou pelo menos um plano de leitura bíblica e abandonou** — esse é o
  qualificador que define o público, não "cristão" genérico.
- Sem formação teológica ou de seminário. Não sabe (e não quer saber) o que é
  hermenêutica, pentateuco, perícope.
- Motivação: quer entender a Bíblia, não só cumprir tarefa devocional.
- Sensível a preço (referência do briefing: ticket baixo).
- **Consequência de desenho**: por já ter falhado uma vez, esse usuário é
  particularmente sensível a qualquer mecânica que registre e exiba a falha dele.
  Ver Seção 4.6 e RN-02.

### 2.2 P2 — "O líder que prepara toda semana" (Módulo 2)

- Líder de célula, obreiro ou pastor de igreja pequena/média no Brasil.
- Prepara entre **1 e 4 falas por mês**, com recorrência previsível e conteúdo
  frequentemente reaproveitado entre grupos e entre anos.
- Usa o celular no púlpito hoje, com ferramenta genérica.
- Vê o produto como **ferramenta de trabalho**, não como devocional — tolera ticket
  mais alto (referência do briefing).

### 2.3 Explicitamente fora do público-alvo

| Público | Por que fica de fora |
|---|---|
| Seminarista, teólogo, pesquisador | Quer línguas originais, concordância, aparato crítico, múltiplas traduções comparadas. Atender esse público exige catálogo e ferramentas de exegese — é competir de frente com o incumbente no terreno dele. |
| Criança / público infantil | Exige curadoria, linguagem e conformidade específicas (dado de menor sob LGPD) que multiplicam o escopo. |
| Igreja como instituição (compra corporativa) | O briefing já coloca o plano "Ministério/Igreja" como camada futura, pós-validação. |
| Leitor não-lusófono | Uma tradução, um idioma (pt-BR) no MVP. |

---

## 3. Objetivo de Sucesso

O objetivo do MVP **não é receita** — a decisão de free-vs-pago está deliberadamente
adiada (Premissa P-04). O objetivo do MVP é **provar ou refutar a hipótese central de
produto**: a de que continuidade entre sessões, intercalação AT/NT e explicação do
material árido reduzem o abandono.

### 3.1 Métrica primária (norte)

**Retenção D30 do plano entrelaçado** = % dos usuários que iniciaram o plano e que
registram pelo menos um dia de leitura concluído na janela dos dias 28–30 após o
início.

| | Valor |
|---|---|
| **Baseline** | **Desconhecido.** Não há baseline público confiável de retenção D30 de plano de leitura bíblica. O baseline será estabelecido pela primeira coorte semanal pós-lançamento e passa a ser o número de referência do produto. |
| **Meta** | **≥ 25%** na coorte medida entre o dia 30 e o dia 90 após o lançamento público. |
| **Critério de refutação** | **< 10%** — a hipótese central do produto está errada e o corte de escopo seguinte deve ser reaberto, não ajustado na margem. |

Nota sobre a meta: 25% é uma hipótese de trabalho, não um número herdado de fonte
externa. Ela existe para forçar uma decisão binária no dia 90 — é isso que a torna
útil, mesmo sendo arbitrada. **Ponto que precisa de confirmação do stakeholder** (Q-01).

**Limitação reconhecida**: D30 só dá sinal 30 dias depois de cada coorte começar. Para
um fundador solo, isso é lento demais para aprender. Por isso a métrica M-07 abaixo
existe: ela dá sinal em 48 horas e é o alerta precoce da métrica primária.

### 3.2 Métricas de apoio

| ID | Métrica | Meta | Por que existe |
|---|---|---|---|
| M-02 | **Sobrevivência ao "muro do árido"**: % dos usuários que, tendo lido um dia marcado como conteúdo legal/ritual/genealógico, concluem também pelo menos um dos 3 dias seguintes | ≥ 70% | Mede o mecanismo causal do abandono tardio, não só o resultado. Se a retenção geral cair mas essa métrica ficar alta, o problema é outro. |
| M-03 | **Recorrência do Módulo 2**: % dos usuários que criam um primeiro esboço e criam um segundo em até 14 dias | ≥ 40% | O Módulo 2 só tem valor se o uso for semanal. Um esboço único é curiosidade, não adoção. |
| M-04 | **Zero falha no púlpito**: sessões do modo apresentação que falharam ao carregar conteúdo sem rede | **0** (métrica de defeito, não de percentual) | É o cenário de pior dano de imagem do produto. Não tem tolerância. |
| M-06 | **Conversão do soft gate**: % dos visitantes sem login que criam conta ao tentar salvar progresso | ≥ 30% | Valida a decisão de login suave. Muito baixo = o gatilho de login está no lugar errado. |
| **M-07** | **Retorno na sessão 2**: % dos usuários que concluem o dia 1 do plano e concluem o dia 2 em até 48 h | ≥ 50% | **Métrica de aprendizado rápido.** Mede o abandono precoce (§1.1), que é o funil **antes** do D30. Dá sinal em 48 h em vez de 30 dias. Se M-07 for baixa, D30 é irrecuperável e nenhum ajuste no muro do árido resolve. |
| **M-08** | **Eficácia do gancho**: % dos lembretes enviados que resultam em conclusão do dia em até 3 h | ≥ 20% | Testa isoladamente a alavanca de continuidade (RN-04 + RF-05). Se M-07 subir e M-08 não, o gancho não é a causa e a hipótese precisa ser revista. |

> **M-05 (uso do áudio) foi removida na rodada 2**, junto com o requisito de áudio
> (corte C-01, ver §4.5 / F-03).

### 3.3 Metas de volume (contexto, não critério de sucesso)

Para as métricas acima terem significado estatístico mínimo, o produto precisa de
**pelo menos 200 usuários iniciando o plano nos primeiros 90 dias**. Abaixo disso, o
resultado é anedota e nenhuma decisão deve ser tomada com base nele — o problema
passa a ser de distribuição (site de divulgação, canais), não de produto.

---

## 4. Escopo desta Release (MVP)

### 4.1 Decisão estruturante: o MVP tem duas fases, na mesma release track

Item (2) das decisões em aberto do briefing — "os dois módulos nascem juntos ou um
primeiro?" — **decidido na rodada 1, mantido na rodada 2 após pressure-test**:

> **Os dois módulos entram no MVP, sequenciados: Fase 1 = Módulo 1 completo + base
> bíblica + soft gate; Fase 2 = Módulo 2 em versão mínima. A Fase 1 é lançável
> sozinha, sem a Fase 2.**

Racional (3 razões, nesta ordem de peso):

1. **Dependência técnica real, não preferência**: o auto-embed de referência do Módulo
   2 depende do corpus bíblico e do reconhecimento de referência que a Fase 1 já
   constrói. Fazer o Módulo 2 primeiro significaria construir a mesma base para um
   público 10× menor.
2. **Risco de não-lançamento é o risco número um** (Gate 1, risco 5). Sequenciar com
   Fase 1 lançável sozinha transforma "o MVP inteiro atrasou" em "a Fase 2 atrasou" —
   que é um problema muito menor.
3. **Não cortar o Módulo 2 do MVP** porque ele é a única parte do produto com
   disposição a pagar plausivelmente alta e é onde o incumbente é fraco. Cortá-lo
   adiaria indefinidamente o único sinal de receita.

Razão adicional descoberta na rodada 2: a sinergia da Nota de Perícope no auto-embed
(§1.3) só existe se os dois módulos estiverem no mesmo produto — e ela é o ativo mais
defensável identificado até aqui.

### 4.2 Decisão estruturante: camada explicativa — **Nota de Perícope**, não paráfrase

Item (1) das decisões em aberto do briefing. **A decisão de rodada 1 (nota de contexto,
não paráfrase) é mantida. A unidade de ancoragem mudou** — de "por dia do plano" para
"por perícope". Isso é uma alteração substantiva, não de redação.

> **A camada explicativa do MVP é a "Nota de Perícope": uma nota de contexto
> histórico/literário de 120–200 palavras, ancorada a um trecho do texto (perícope) e
> não a um dia do plano, escrita em linguagem de leigo, com autoria e revisão
> visíveis, terminando em um gancho de continuidade. É gratuita para todos no MVP.
> Paráfrase / texto simplificado fica FORA do MVP e é reclassificada de "opção
> pendente" para "hipótese futura".**

**Por que nota e não paráfrase** (racional revisado na rodada 2 — um dos argumentos da
rodada 1 foi rebaixado):

1. **Risco assimétrico**: alterar o texto sagrado ("mudaram a Bíblia") é o tipo de
   erro que um fundador solo, sem revisão teológica institucional, não consegue
   defender publicamente. Adicionar nota ao lado do texto é aditivo e reversível;
   parafrasear o texto não é.
2. **Volume**: nota-por-trecho, cobrindo os 90 dias do plano ≈ 90 textos curtos,
   produzíveis por uma pessoa. Paráfrase = mais de 31 mil versículos. Diferença de
   duas ordens de grandeza.
3. **A nota resolve o problema declarado; a paráfrase não.** O leitor não abandona
   Levítico porque o português é difícil — abandona porque não sabe **por que aquilo
   está ali**. Um texto de leis rituais simplificado continua sendo uma lista de leis
   rituais.
4. *(rebaixado)* A "Tradução para Tradutores" não serve como fonte auxiliar do AT
   (só cobre o NT). Isso continua verdadeiro e está verificado, mas é um argumento
   sobre **fonte auxiliar**, não sobre a viabilidade da paráfrase em si — na rodada 1
   ele foi apresentado com peso maior do que merece. As razões 1–3 sustentam a
   decisão sozinhas.

**Por que ancorar à perícope e não ao dia** (mudança da rodada 2):

- **Mesma economia editorial no MVP, ativo de valor oposto.** Uma nota por dia é
  descartável: se o plano mudar, ou se surgir um segundo plano, ela não se reaproveita.
  Uma nota por perícope é reutilizável em qualquer plano futuro, **e na leitura livre
  do corpus** — que hoje não tem nenhuma camada explicativa, o que era desperdício do
  mesmo ativo.
- **Habilita a única sinergia real entre os módulos** (§1.3): a mesma nota aparece no
  Módulo 2 quando o líder insere uma referência daquela perícope.
- **Não aumenta o volume no MVP**: a regra de cobertura é "todo dia do plano tem pelo
  menos uma Nota de Perícope associada, obrigatoriamente a da porção do AT quando ela
  for árida" — o que mantém ~90 notas, exatamente como antes (RN-13 no
  `PRD-TECNICO.md`). Se essa regra fosse "uma nota por perícope de cada porção", o
  volume dobraria e a troca seria falsa.

**Autoria e revisão visíveis** (novo na rodada 2): comentário humano anônimo ao lado da
Escritura é indefensável quando um crítico de peso do meio evangélico apontar; nota
assinada é uma opinião identificada e defensável. Efeito colateral útil: crédito
visível é a moeda com que se recruta o revisor teológico que o produto ainda não tem
(P-06 / Q-08).

**Gancho de continuidade** (novo na rodada 2): cada dia do plano termina com uma frase
que aponta o que vem no dia seguinte, e **o lembrete do dia seguinte usa exatamente
essa frase como corpo da notificação**. É a alavanca contra o abandono precoce
(§1.1). Custo de implementação: um campo a mais no conteúdo e a origem do texto da
notificação; custo editorial: uma frase por dia.

**Sobre grátis vs. premium**: a Nota de Perícope tem de estar no gratuito no MVP,
porque ela é a variável independente do experimento da Seção 3. Colocá-la atrás de
paywall contamina a métrica primária e o MVP deixa de medir o que se propôs a medir.
Isso é uma restrição de produto sobre a decisão futura de monetização, não uma decisão
de monetização (ver P-04).

### 4.3 Decisão estruturante: o plano do MVP é honesto sobre o que cobre

Item (5) — a regra de intercalação — está detalhada como regra de negócio no
`PRD-TECNICO.md` (Seção 3, RN-01). Decisão de produto, mantida da rodada 1:

> **O corpus completo (66 livros) é importado e livremente navegável desde o dia 1.
> Mas o plano guiado do MVP é o "Plano Entrelaçado — 90 dias", curado, cobrindo a
> espinha dorsal narrativa da Bíblia, e é assim que ele se apresenta ao usuário. O
> produto NÃO promete "a Bíblia inteira" no MVP.**

Racional: (a) um plano de 365 dias exigiria 365 notas antes do lançamento — é o que
trava o lançamento (gap G-01 do Gate 1); (b) a métrica primária é D30, então um plano
de 90 dias já a mede por inteiro, com margem; (c) prometer "Bíblia em 1 ano" e entregar
seleção curada seria desonesto com o público.

**Correção da rodada 2 — o buraco do dia 91**: um plano que termina cria um "e agora?"
no usuário mais valioso do produto, exatamente aquele que provou a hipótese. E como
D30 já foi capturada no dia 30, esse abandono é invisível na métrica primária. O plano
passa a ter **uma tela de conclusão** que aponta um próximo passo concreto de leitura
dentro do corpus e captura o único feedback qualitativo do produto — o do usuário que
chegou até o fim. Custo editorial: um texto, não noventa.

### 4.4 Dentro do escopo

**Fase 1 — Módulo 1 (Leitura Guiada) + fundação**

- Corpus bíblico completo (66 livros, BLIVRE) embutido, navegável por livro e
  capítulo.
- Atribuição da licença CC BY 4.0 visível no produto (obrigação legal, não opcional).
- Plano Entrelaçado — 90 dias: um trecho de AT + um trecho de NT por dia.
- **Nota de Perícope**, com autoria/revisão visível, cobrindo todos os 90 dias do plano
  **e exibida também na leitura livre do corpus** quando o trecho lido tiver nota.
- **Gancho de continuidade** ao final de cada dia do plano.
- Marcar dia como concluído; progresso do plano.
- **Indicador de constância**: "dias lidos nos últimos 30" como número principal;
  sequência (streak) apenas como recorde secundário, sem notificação de perda
  (ver 4.6).
- Lembrete diário, com horário configurável, **cujo corpo é o gancho do dia seguinte**.
- Vitrine sem login: prévia do dia do plano com a Nota de Perícope, versículo do dia,
  CTA suave.
- Soft gate de login: navegar e ler sem conta; conta exigida só para salvar progresso,
  manter constância entre dispositivos ou entrar no Módulo 2.
- Sincronização de progresso entre dispositivos do mesmo usuário.
- Conta: cadastro, login, recuperação de acesso, **exclusão de conta e dados** (LGPD).
- Consentimento específico para tratamento de dado sensível (convicção religiosa).
- **Tela de conclusão do plano** (dia 90) com próximo passo e captura de feedback.
- Instrumentação das métricas da Seção 3.

**Fase 2 — Módulo 2 (Preparo de Pregação/Estudo), versão mínima**

- Criar/editar/excluir esboço, organizado em blocos tipados (ponto, versículo de
  apoio, ilustração, observação livre).
- Auto-embed: ao inserir uma referência ("Romanos 8:28"), o app traz o texto do
  versículo formatado, do corpus local, **e a Nota de Perícope daquele trecho, quando
  existir** (sinergia entre módulos, §1.3).
- **Duplicar esboço** — cobre o reuso real do ofício (§1.2, atrito 4).
- Lista de esboços do usuário, com **acesso direto de "apresentar agora"** ao esboço
  mais recente.
- Modo apresentação: navegação por cartões via toque, fonte grande, tema escuro por
  padrão, sem notificações e sem elementos de distração, **com retomada exata do
  cartão em que estava** se o app for interrompido.
- **Modo apresentação 100% offline** — requisito não-negociável (RNF-01 no
  `PRD-TECNICO.md`).
- Módulo 2 sempre exige login.

### 4.5 Fora do escopo desta release (com justificativa por corte)

| # | Item cortado | Justificativa |
|---|---|---|
| F-01 | Paráfrase / texto simplificado da Bíblia | Decisão 4.2: risco teológico assimétrico, volume de duas ordens de grandeza acima do viável, e não resolve a dor declarada. Reclassificado como hipótese futura. |
| F-02 | Plano "Bíblia inteira em 1 ano" | Decisão 4.3: exigiria 365 notas antes de lançar; a métrica D30 é plenamente medida por um plano de 90 dias. |
| **F-03** | **Áudio da leitura — inteiro** (narração humana **e** narração sintética) | **Corte da rodada 2.** Na rodada 1 só a narração humana estava fora. Nenhuma evidência liga áudio à métrica primária; o modo de falha do público-alvo declarado é "não entendo / não vejo sentido", não "não tenho tempo de ler". É paridade com o incumbente num produto que decidiu não competir em paridade. O corte elimina uma tela, um conjunto de controles, uma bateria de testes de compatibilidade cross-browser e a premissa P-03 inteira. Acessibilidade continua coberta por RNF-04 (WCAG AA + leitor de tela nativo). **Este item veio do briefing original — precisa de veto ou aval do stakeholder (Q-10).** |
| F-04 | Múltiplas traduções bíblicas | Cada tradução adicional é uma negociação de licença ou uma nova verificação de compliance. Uma tradução (BLIVRE) no MVP; catálogo é o terreno do incumbente. |
| F-05 | Busca full-text, concordância, léxico, línguas originais — inclusive busca a partir do editor do Módulo 2 | Ferramenta de exegese serve o público explicitamente fora do alvo (2.3). |
| F-06 | Marcadores, destaques, anotações pessoais no texto (Módulo 1) | Table-stakes de concorrente, não diferencial. Cada um adiciona sincronização e conflito de merge. Reavaliar após D30 medido. |
| F-07 | Comunidade, comentários, compartilhamento social, grupos de leitura | Não ataca nenhuma das duas dores; adiciona moderação de conteúdo religioso, que é um problema operacional inteiro. |
| F-08 | Cronômetro / controle de tempo de fala no Módulo 2 | Exclusão explícita do briefing. O módulo é bloco de notas estruturado, não gestão de apresentação. |
| F-09 | Compartilhamento, colaboração e exportação (PDF/Word) de esboços | Fase 2 é mínima por desenho. Colaboração implica permissões, convites, versionamento. |
| F-10 | Plano "Ministério/Igreja" (B2B2C, pastor gerenciando líderes) | O briefing já o coloca como camada pós-validação. Exige hierarquia de contas e faturamento B2B. |
| F-11 | Cobrança, checkout, assinatura, paywall | Decisão de monetização adiada pelo stakeholder (P-04). O MVP sai 100% gratuito. **Nenhuma funcionalidade deve ser desenhada assumindo que será gratuita para sempre.** |
| F-12 | App nativo iOS/Android publicado em loja | O briefing fixa PWA primeiro. Loja adiciona revisão, conta de desenvolvedor e regras de cobrança in-app. |
| F-13 | Site institucional de divulgação | O briefing já o separa do app. Não bloqueia o MVP — mas bloqueia a meta de volume da Seção 3.3; ver Q-07. |
| F-14 | Modo offline do Módulo 1 (leitura sem rede fora do modo apresentação) | Só o modo apresentação tem offline como requisito duro. Estender offline ao Módulo 1 inteiro no MVP amplia significativamente o esforço sem atacar a dor. Reavaliar pós-MVP. |
| F-15 | Internacionalização / outros idiomas | Uma tradução, um idioma. |
| **F-16** | **Séries, pastas ou qualquer hierarquia de esboços** | *Novo na rodada 2.* O reuso real do ofício (mesma série, outro grupo, ano passado) é coberto por "duplicar esboço" a uma fração do custo. Hierarquia implica navegação, mover entre pastas, ordenação — é um gerenciador de arquivos dentro do app. |
| **F-17** | **Gamificação: medalhas, níveis, e qualquer notificação de perda de sequência** | *Novo na rodada 2.* Contradiz frontalmente o desenho de 4.6. Para um público definido por já ter falhado, registrar e anunciar a falha é um mecanismo de saída, não de engajamento. |
| **F-18** | **"Usar este capítulo em um esboço" (atalho leitura → preparo)** | *Novo na rodada 2.* Fluxo plausível e barato, mas puramente hipotético — não há métrica declarada atrás dele, ao contrário de duplicar esboço (M-03) e da nota no auto-embed (defensabilidade). Primeiro candidato pós-MVP. |

### 4.6 Decisão de rodada 2: constância em vez de sequência

A rodada 1 desenhou streak com tolerância de 1 dia (RN-02). Ao revisar contra RN-09
("o plano anda com o usuário, não com o calendário — o usuário nunca está em dívida com
o app"), as duas regras estavam em **tensão direta**: RN-09 elimina a dívida, e o
streak a reintroduz por outra porta. Streak funciona enquanto está intacto e vira
motivo de saída no dia em que quebra — precisamente no público que já saiu uma vez.

> **Decisão**: o número principal exibido passa a ser **"dias lidos nos últimos 30"** —
> uma contagem que nunca zera de uma vez e que sempre pode melhorar amanhã. A sequência
> (streak) continua sendo calculada e visível como **recorde secundário**, e o produto
> **nunca** notifica nem exibe mensagem sobre perda de sequência.

Custo: o mesmo. É outro cálculo sobre exatamente os mesmos dados já registrados.

---

## 5. Requisitos de Alto Nível Priorizados

Priorização MoSCoW. "Prioridade justificada" = a coluna Racional; nenhum item entra
sem ela.

### Fase 1

| ID | Requisito de alto nível | Prioridade | Racional da prioridade |
|---|---|---|---|
| RA-01 | Corpus bíblico completo navegável (66 livros) | **Must** | Ativo central. Sem ele não existe nem Módulo 1 nem Módulo 2. |
| RA-02 | Atribuição da licença CC BY 4.0 visível | **Must** | Obrigação legal da licença. Não é feature, é condição de uso do texto. |
| RA-03 | Plano Entrelaçado de 90 dias | **Must** | É a variável independente do experimento da Seção 3. Sem ele, o produto é mais um leitor de Bíblia. |
| RA-04 | Nota de Perícope (nos 90 dias do plano e na leitura livre), com autoria visível | **Must** | Segunda metade do mecanismo causal (4.2), ativo defensável e insumo da sinergia com o Módulo 2. |
| RA-05 | Progresso do plano + marcar dia como lido | **Must** | É o instrumento de medição da métrica primária. Sem isso não há D30 nem M-07. |
| RA-06 | Soft gate de login + conta | **Must** | Progresso entre dispositivos e Módulo 2 dependem de identidade. O "suave" é o que evita perder o visitante na porta. |
| RA-07 | Consentimento e exclusão de dados (LGPD) | **Must** | Dado sensível (convicção religiosa). Não é negociável nem adiável. |
| RA-09 | Lembrete diário carregando o gancho do dia seguinte | **Must** *(era Should na rodada 1)* | Promovido: é a principal alavanca contra o abandono precoce (§1.1), que é o funil **antes** do muro do árido. Um lembrete genérico é ignorado; um lembrete que diz o que você vai perder é outro produto. O risco de plataforma (P-08) é gerenciado por degradação (canal alternativo), não por rebaixamento de prioridade. |
| RA-08 | Indicador de constância ("dias lidos nos últimos 30") | **Should** | Reforça hábito e é barato sobre RA-05, mas o produto funciona sem. |
| RA-10 | Vitrine sem login (dia do plano com nota, versículo do dia) | **Should** | Superfície de aquisição e primeira impressão. A vitrine mostra o diferencial, não uma commodity. |
| RA-12 | Sincronização entre dispositivos | **Should** | Explícito no briefing, mas um único dispositivo já mede D30. |
| RA-21 | Tela de conclusão do plano (dia 90) + captura de feedback | **Should** | Fecha o buraco do dia 91 (4.3) e é o único canal qualitativo com o usuário que provou a hipótese. Custo editorial: um texto. |
| RA-18b | Instrumentação de produto (Seção 3) | **Must** | As métricas não existem se não forem coletadas desde o dia 1. Sem isso o MVP não prova nem refuta a própria hipótese. |

> **RA-11 (áudio da leitura) foi removido na rodada 2** — corte C-01, ver F-03.

### Fase 2

| ID | Requisito de alto nível | Prioridade | Racional da prioridade |
|---|---|---|---|
| RA-13 | Esboço com blocos tipados (editar) | **Must** | É o Módulo 2. Sem ele não há Fase 2. |
| RA-14 | Modo apresentação (cartões, fonte grande, escuro, sem distração, com retomada exata do cartão) | **Must** | É o momento de uso que diferencia do bloco de notas genérico. A retomada foi acrescentada na rodada 2: perder a posição no meio da pregação tem o mesmo dano que a falha offline. |
| RA-15 | Modo apresentação 100% offline | **Must** | Requisito duro do briefing; falha aqui é o pior cenário do usuário (M-04). |
| RA-16 | Auto-embed de referência bíblica | **Must** | É a razão de o Módulo 2 viver dentro deste app e não num app de notas. |
| RA-19 | Nota de Perícope exibida no auto-embed | **Must** | *Novo na rodada 2.* É o que separa "bloco de notas com texto colado" de "bloco de notas que sabe algo sobre o texto" — a única sinergia real entre os módulos (§1.3) e o ativo mais difícil de copiar. Custo marginal baixo: o dado já existe por RA-04 e a perícope já é resolvida por RA-16. |
| RA-20 | Duplicar esboço | **Should** | *Novo na rodada 2.* Cobre o atrito 4 de §1.2 e é o caminho mais barato até o segundo esboço, que é exatamente a métrica M-03. |
| RA-17 | Lista/gestão de esboços, com acesso direto "apresentar agora" | **Should** | Necessário a partir do segundo esboço. O acesso direto resolve os 3 segundos reais do púlpito. |
| RA-18 | Reordenar blocos do esboço | **Could** | Conveniência de edição. É o primeiro item a cair se o prazo apertar. |

### Won't (desta release)

Todos os itens F-01 a F-18 da Seção 4.5.

---

## 6. Premissas e Riscos de Produto

| ID | Premissa / Risco | Tipo | Status | Dono | Prazo |
|---|---|---|---|---|---|
| P-01 | Texto bíblico sob licença CC aberta, AT+NT completos, atribuição cumprível | Premissa | **Confirmada** — CC BY 4.0, 66 livros, texto literal de atribuição obtido. Evidência em `PRD-TECNICO.md` §6 | gestor (BA) | Resolvida em 2026-09-07 |
| P-02 | "Tradução para Tradutores" como fonte auxiliar completa | Premissa | **Refutada** — só 27 livros (NT). Evidência em `PRD-TECNICO.md` §6 | gestor (BA) | Resolvida em 2026-09-07 |
| P-03 | Áudio com custo marginal ≈ 0 e qualidade pt-BR aceitável | Premissa | **Fechada por corte de escopo** (rodada 2, F-03) — deixou de ser uma pergunta em aberto porque o requisito saiu do MVP | — | Encerrada em 2026-09-07 |
| P-04 | A definição de free-vs-pago pode ser adiada sem travar o MVP | Premissa (adiamento aceito pelo stakeholder) | **Aberta por decisão** | Stakeholder | **Até 30 dias antes do lançamento público**, e obrigatoriamente antes de qualquer comunicação de preço. Restrição fixada: a Nota de Perícope fica no gratuito (4.2) |
| P-05 | Um dev solo consegue entregar Fase 1 + Fase 2 em horizonte de MVP | Risco **alto** | Mitigado, não eliminado — a rodada 2 reduziu o escopo líquido (corte do áudio > adições) | Stakeholder + Coordenador | Reavaliar na aprovação do `TASK.md`. Mitigação: Fase 1 lançável sozinha (4.1) |
| P-06 | As ~90 Notas de Perícope serão escritas e revisadas | Risco **alto** — sem dono no pipeline (gap G-01) | **Aberta**, mas de natureza melhor após a rodada 2: as notas passaram a ser ativo reutilizável (4.2) e a autoria visível cria a moeda para recrutar o revisor | Stakeholder (autoria) + revisor com formação teológica (revisão) | Formato + 5 notas-exemplo **antes do `UX-SPEC.md`**; ~90 antes do lançamento |
| P-07 | Nomear a tradução como "Almeida Atualizada" é seguro | Premissa | **Refutada** pelo Gate 1 (R-02). Nome no produto: "Bíblia Livre (BLIVRE)" | gestor (CTO) | Resolvida em 2026-09-07 |
| P-08 | Existe canal de lembrete capaz de entregar o gancho ao público-alvo | Risco **médio-alto** *(elevado na rodada 2)* | **Aberto** — no iOS há restrição conhecida de push em PWA não instalado na tela de início. Ficou mais crítico porque RA-09 subiu para Must | Coordenador, no `SDD.md` | Antes do lote de RA-09. Plano B: e-mail e/ou retomada in-app do gancho; o gancho continua existindo mesmo sem push |
| P-09 | Não há baseline público de retenção D30 para calibrar a meta | Risco de medição | **Aceito** — mitigado por M-07, que dá sinal em 48 h | Stakeholder | A primeira coorte pós-lançamento fixa o baseline |
| P-10 | Existe canal de aquisição para ≥ 200 iniciantes em 90 dias | Risco **médio-alto** | **Aberto** — site de divulgação fora do escopo (F-13) | Stakeholder | Definir canal antes do lançamento |
| P-11 | O produto trata dado pessoal sensível (LGPD art. 5º, II) | Restrição legal | **Confirmada** | Validador (DevSecOps) no tático; gestor no estratégico | RNF em `PRD-TECNICO.md` §2; verificação no `/validar` |
| P-12 | O diferencial "plano entrelaçado" é copiável pelo incumbente em semanas | Risco estratégico | **Aceito** | Stakeholder | Contínuo. Mitigação reforçada na rodada 2: a defensabilidade está no acervo de Notas de Perícope e na sinergia com o Módulo 2 (RA-19), não no plano |
| **P-13** | O gancho de continuidade move o retorno na sessão 2 | Hipótese central nova (rodada 2) | **Aberta — é o que M-07 e M-08 existem para testar** | Stakeholder | Medida a partir da primeira semana pós-lançamento. Se M-07 < 30%, o problema do produto é a continuidade, não o muro do árido, e o corte de escopo seguinte muda de alvo |

---

## 7. Perguntas em Aberto

| ID | Pergunta | Quem resolve | Situação após a rodada 2 |
|---|---|---|---|
| Q-01 | A meta de 25% de retenção D30 é aceitável como número de decisão do dia 90? A meta é arbitrada. | **Usuário** | **Mudou de forma**: com M-07 (retorno na sessão 2, meta 50%) existindo, a decisão útil no curto prazo passou a ser a de M-07. Q-01 continua valendo, mas agora vale confirmar **duas** metas, e a de 48 h é a que dá aprendizado antes. |
| Q-07 | Sem o site institucional (F-13), qual canal traz os 200 iniciantes dos primeiros 90 dias? | **Usuário** | Inalterada, e ficou mais urgente: nenhuma das melhorias desta rodada compensa a ausência de tráfego. |
| Q-08 | Quem revisa teologicamente as notas? | **Usuário** | **Mudou de forma**: com autoria/revisão visível (4.2), o crédito editorial passa a ser a contrapartida oferecida ao revisor. A pergunta deixou de ser "você aceita o risco de não ter revisor" e passou a ser "quem você convida, dado que agora há crédito a oferecer". |
| Q-09 | Há livro/tema inegociável na curadoria do plano de 90 dias? | **Usuário** | Inalterada. |
| **Q-10** | **Confirma o corte do áudio (F-03)?** O áudio veio do briefing original e foi cortado inteiro na rodada 2 por falta de evidência de que move a métrica primária. É um veto seu, não meu — se o áudio for inegociável, ele volta e algo precisa sair no lugar. | **Usuário** | Nova nesta rodada. |
| **Q-11** | **Confirma a troca de "streak" por "dias lidos nos últimos 30" como número principal (4.6)?** É a reversão de uma regra minha da rodada 1, e contraria uma convenção de mercado muito difundida. | **Usuário** | Nova nesta rodada. |
| Q-02 a Q-06 | Regra de intercalação, regra de constância, calendário do plano, formatos de referência, migração de progresso anônimo | **BA** | **Resolvidas** em `PRD-TECNICO.md` §3 (RN-01, RN-02, RN-09, RN-05, RN-07). |

---

## Registro de rodadas

**Rodada 1 (2026-09-07)** — Gate 1 aprovado com ressalvas; decisões estruturantes:
nota de contexto em vez de paráfrase; dois módulos sequenciados com Fase 1 lançável
sozinha; plano curado de 90 dias com nome honesto.

**Rodada 2 (2026-09-07)** — pressure-test de concepção (`the-fool`: pré-mortem,
auditoria de evidência, red team, verificação de viés). As três decisões estruturantes
**foram mantidas**; uma delas (camada explicativa) foi **modificada na unidade de
ancoragem**. Mudanças:

| Categoria | Mudança |
|---|---|
| Corte | F-03: áudio inteiro fora do MVP; M-05 e RA-11 removidos; P-03 encerrada |
| Troca | Nota do Dia → **Nota de Perícope** (mesma economia editorial, ativo reutilizável, habilita a sinergia entre módulos) |
| Troca | Streak como número principal → **"dias lidos nos últimos 30"**; streak vira recorde secundário; F-17 proíbe notificação de perda |
| Troca | RA-09 (lembrete) sobe de Should para Must; o áudio sai — escopo líquido menor |
| Grátis | Gancho de continuidade ao fim de cada dia, usado como corpo do lembrete |
| Grátis | Autoria/revisão visível na nota |
| Grátis | Nota exibida também na leitura livre do corpus |
| Grátis | Vitrine mostra o dia do plano com nota, não só o versículo do dia |
| Grátis | Retomada exata do cartão no modo apresentação; "apresentar agora" na entrada do Módulo 2 |
| Grátis | Tela de conclusão do plano no dia 90 |
| Adição | RA-19 (Nota de Perícope no auto-embed) e RA-20 (duplicar esboço) — ambas cabem no que o corte do áudio liberou |
| Métrica | M-07 (retorno na sessão 2) e M-08 (eficácia do gancho) adicionadas; M-05 removida |
| Diagnóstico | §1.1 passou a distinguir abandono precoce de abandono do muro — a rodada 1 só tratava o segundo |

### Registro de `stakeholder-alignment-check` (rodada 2)

Conflito entre este `PRD.md` e o Gate 1: **nenhum não resolvido**.
- R-01 (escopo para dev solo) → **reforçada**: escopo líquido menor que na rodada 1.
- R-02 (nome da tradução) → P-07 e RA-02.
- R-03 (conteúdo editorial com dono e prazo) → P-06, com natureza melhorada.
- R-04 (data-limite para free-vs-pago) → P-04.
- R-05 (nenhum custo por usuário no MVP) → **satisfeita de forma mais forte**: o corte
  do áudio elimina a única fonte plausível de custo recorrente por usuário do MVP.

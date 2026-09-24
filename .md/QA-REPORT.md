# QA-REPORT.md — ERP Financeiro (C#)

> Chapéu QA do Validador. Base: `TASK.md` (Seção 3, Lote 1), `PRD-TECNICO.md`, `GUARDRAILS.md`.

## Lote 1 — Spike e preparação

**Veredito: Aprovado com ressalvas.** As 3 tarefas permanecem `Concluída`; 3 achados
**simples** (baixo esforço, não comprometem o critério de aceite central nem
bloqueiam outra tarefa do lote) viram tarefas em `Refatoração Lote-1` (ver Seção 4
do relatório e `TASK.md`).

### T-01 — Spike Firebird go/no-go

**Resultado: Aprovado, sem ressalva.**

Verificado contra o critério de aceite literal (linha T-01, Seção 3):
- (1) `spikes/T-01-firebird-spike/evidencia-execucao.txt`, linha 7: `[PASS] CA-1
  INSERT+SELECT decimal - gravado 1250.50, lido 1250,5`. Comparação real feita em
  `Program.cs` linha 131 via igualdade `decimal` (`lida.ValorTotal == 1250.50m`),
  não por string — a formatação sem zero à direita no log (`1250,5` em vez de
  `1250,50`) é só exibição em `Console.WriteLine`, não indica perda de precisão.
  **PASS confirmado.**
- (2) Linha 9 do log: `DbUpdateConcurrencyException` lançada no segundo
  `SaveChanges`, conforme roteiro (dois contextos, `VERSAO` incrementada pela
  aplicação). **PASS confirmado.**
- (3) Bitness registrada: `x64`, `PlatformTarget=x64` no `.csproj` e no ADR-009.
  **PASS confirmado.**
- (4) `.md/adr/009-spike-firebird-resultado-go.md` existe com decisão **GO**
  explícita, critério a critério, e nota de rodapé espelhada em
  `.md/adr/002-firebird-embarcado-spike-e-fallback.md`. **PASS confirmado.**

Limitações já documentadas pelo próprio Executor (critério 4 simulado com 2
contextos no mesmo processo por causa da regra 11; critérios 5/`UNIQUE` e
6/`Skip-Take` não exercitados isoladamente) são coerentes com o texto do
ADR-002/ADR-009 e não alteram o resultado GO — revalidação prevista em T-14/T-15/
T-23, como já registrado. Não é achado novo, é rastreamento já feito
corretamente pelo Executor.

**Achado simples (cosmético, não bloqueia)**: `Program.cs` linha 170 (e espelhado
no log de evidência linha 10) referencia `docs/licencas-bin.md`, arquivo que não
existe — o real é `docs/licencas.md`. É uma string de log de um projeto
descartável (`spikes/`, fora da solução final), sem efeito funcional ou de
segurança. Vira tarefa em `Refatoração Lote-1` (RL1-02).

### T-02 — Trials DevExpress/FastReport e `docs/licencas.md`

**Resultado: Aprovado com ressalva declarada.**

Critério de aceite tem 3 partes: (a) "Ambos abrem num projeto WinForms vazio";
(b) `docs/licencas.md` com versão/licença/duração/preview; (c) ADR-006/008
marcado com o caso.

- (a) **Não verificado literalmente** — ambiente de execução é sandbox sem GUI
  Windows/instalador interativo, limitação de infraestrutura fora do controle do
  Executor, declarada de forma transparente logo no topo de
  `docs/licencas.md` ("Limitação do ambiente"), com ação pendente explícita
  ("instalar os dois produtos de fato... antes de T-49/T-50 serem
  implementadas"). Não reinterpreto o critério como cumprido — está
  genuinamente pendente, mas é bloqueio de ambiente, não de qualidade de
  trabalho: a pesquisa documental foi feita em fontes oficiais (DevExpress
  Docs/Support Center, FastReport Open Source docs/GitHub, NuGet), com fontes
  citadas.
- (b) **Cumprido** — tabela com versão pesquisada, tipo de licença, duração,
  marca d'água/preview para os dois produtos, com fontes.
- (c) **Cumprido como insumo** — seção "Decisão de caminho" recomenda preview via
  FastReport .NET Trial com fallback PDF, already coerente com o texto atual do
  ADR-008 (não exigiu reescrita do ADR nesta tarefa; fica como insumo para o
  Coordenador ratificar formalmente).

**Achado simples (limitação de ambiente, ação pendente já anotada pelo próprio
documento)**: confirmação visual real (item a) ainda não feita. Vira tarefa em
`Refatoração Lote-1` (RL1-03), com prazo "antes de T-49/T-50" (já é o gate
natural informado no próprio `docs/licencas.md`) para não perder o rastro.

### T-03 — Contrato v1.1

**Resultado: Aprovado com ressalva.**

Critério de aceite: "Documento existe com >= 1 exemplo JSON por código de erro;
registro de envio ao Vendas (data) e de resposta ponto a ponto (aceita/rejeitada);
pontos rejeitados viram entrada em `BLOCKERS.md`".

- **Exemplos JSON por código de erro**: `docs/contrato-v1.1.md` define 9 códigos
  de erro na tabela da Seção 2 (`PAYLOAD_INVALIDO`, `VALOR_TOTAL_DIVERGENTE`,
  `NAO_AUTORIZADO`, `VENDA_NAO_ENCONTRADA`, `VENDA_JA_CANCELADA`,
  `MOTIVO_OBRIGATORIO`, `DADOS_DIVERGENTES`, `CONFLITO_CONCORRENCIA`,
  `ERRO_INTERNO`). Conferido exemplo por exemplo nas Seções 3.1-3.5: 7 dos 9
  códigos têm exemplo JSON real (`PAYLOAD_INVALIDO`, `VALOR_TOTAL_DIVERGENTE`,
  `NAO_AUTORIZADO`, `VENDA_NAO_ENCONTRADA`, `VENDA_JA_CANCELADA`,
  `MOTIVO_OBRIGATORIO`, `DADOS_DIVERGENTES`). **`CONFLITO_CONCORRENCIA` (409) e
  `ERRO_INTERNO` (500) aparecem só na tabela de códigos, sem exemplo JSON em
  lugar nenhum do documento** — confirmado por busca no arquivo inteiro (só 1
  ocorrência de cada string, a da própria tabela). Isso é um desvio literal do
  critério de aceite ("um exemplo por código", sem exceção declarada para esses
  dois).
  - **Classificação: simples, não crítica.** Não reinterpreto o critério —
    ele foi violado —, mas a lacuna é um ajuste pontual e de baixo esforço
    (adicionar 2 blocos JSON num documento que já segue o padrão certo para os
    outros 7), não exige mudança de escopo/arquitetura, e não bloqueia nenhuma
    outra tarefa deste lote (T-01/T-02 são independentes; T-37, que valida os
    exemplos contra a API real, só roda no Lote 8, bem depois — o corretivo tem
    folga de sobra). Vira tarefa em `Refatoração Lote-1` (RL1-01).
- **Envio ao lado Vendas e resposta ponto a ponto**: não realizados —
  declarado explicitamente como pendência do usuário/orquestrador (sem acesso ao
  Vendas neste ambiente), Seção 1 do próprio documento. Não é uma omissão do
  Executor, é uma dependência externa fora do alcance de qualquer agente nesta
  sessão; não há rejeição a registrar em `BLOCKERS.md` porque não houve envio
  ainda (registrar rejeição sem ter havido envio seria fabricar dado). Aceito
  como está, consistente com o enquadramento do orquestrador para este lote.
- **Linha v1.1 da `VISAO-PRODUTO.md` 2.4**: confirmado **não preenchida**
  (`git diff` não mostra alteração no arquivo) — cumpre G-5.20.

### Testes de integração cruzada (Lote 1)

Não aplicável no sentido tradicional (nenhuma das 3 tarefas produz componente
que se integre em tempo de execução com outra do mesmo lote — spike é
descartável, licenças e contrato são documentação). Verificada só a
consistência declarativa entre os três artefatos e o `TASK.md`/ADRs, sem
achado adicional.

### Requisitos não funcionais

Não aplicável a este lote (sem tela, sem API rodando). O único NFR relevante —
"identificadores <= 31 caracteres" (regra 10) — foi verificado no DDL do spike
(`FIN_VENDA`, `VALOR_TOTAL`, `VERSAO`, todos < 31 chars).

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Os 3 achados são pontuais e de naturezas distintas (formatação de log
num projeto descartável; lacuna de conteúdo em 2 de 9 exemplos JSON; limitação
de ambiente sem GUI). Nenhum padrão sugere problema de decomposição de tarefas
ou de diretrizes de implementação. Não escalo ao `coordenador` por este motivo.

## Fechamento estrutural do lote

Ver Seção correspondente em `SECURITY-REVIEW.md` (mesma checagem, registrada lá
para não duplicar) — resumo: todas as 3 tarefas `Concluída`, sem dependência
órfã da Seção 4 relativa ao Lote 1, sem tarefa `Bloqueada`. Lote fecha
**Validado (com ressalvas)** após a auditoria de segurança (Seção seguinte do
fluxo).

## Lote 2 — Esqueleto e Domínio

**Veredito: Aprovado, sem ressalvas.** As 5 tarefas (T-04, T-07, T-08, T-09,
T-10) permanecem `Concluída`. Nenhuma reprovação crítica nem simples.

Validado por leitura de código real (`git diff`/arquivos, não pela nota de
implementação do Executor) contra o critério de aceite literal de cada linha
(TASK.md Seção 3, Lote 2) e contra RN-03/RN-04 (`PRD-TECNICO.md`) e D-06/D-08/
ADR-007 (`PRD-TECNICO.md`/`SDD.md`).

### T-04 — Esqueleto da solução

**Resultado: Aprovado.**

- `ERPFinanceiro.sln` na raiz + 7 projetos SDK-style em `src/`, todos
  `net48`/`LangVersion=7.3`/`PlatformTarget=x64` (`Platforms=x64` também fixo
  nos `.csproj`); `.sln` mapeia `Any CPU`/`x86` para `x64` em todas as
  configurações (conferido no `Global/ProjectConfigurationPlatforms`) — nunca
  `AnyCPU` real, conforme critério.
- Tabela de dependências (SDD 2.1/GUARDRAILS G-3.9) confirmada projeto a
  projeto: `ERPFinanceiro.Domain.csproj` sem `ProjectReference`/
  `PackageReference` nenhum; `ERPFinanceiro.Application.csproj` só referencia
  `Domain`; `ERPFinanceiro.Api.csproj` referencia `Domain`+`Application`, **sem**
  `Infrastructure`; `ERPFinanceiro.Infrastructure.csproj` referencia
  `Domain`+`Application`; `ERPFinanceiro.Reports.csproj` só `Application`;
  `ERPFinanceiro.Desktop.csproj` referencia `Application`, `Reports`, `Api` e
  `Infrastructure` (composition root, permitido por regra 1 da Seção 1);
  `ERPFinanceiro.Tests.csproj` referencia `Domain`+`Application`+
  `Infrastructure` (esta última somada depois, em T-11, fora do escopo deste
  lote, sem violar a regra — Tests não é uma camada de produção).
- `.gitignore` na raiz cobre `bin/`/`obj/`/`*.fdb`/`App.config` real, preserva
  `App.config.example` e `database/*.sql`; `git status` confirma nenhum `bin/`/
  `obj/` rastreado apesar de existirem localmente (build local não versionado).
- Critério "compila sem warning de referência": não reexecutei `dotnet build`
  nesta validação (evidência de build já registrada pelo Executor com `0
  Aviso(s)/0 Erro(s)`); a inspeção estrutural direta dos `.csproj` corrobora a
  mesma composição descrita na evidência, sem divergência.

### T-07 — Domain: entidades, enums, exceções, fábricas sem transição

**Resultado: Aprovado.**

- `StatusVenda` (Pendente=0/Quitada=1/Cancelada=2) e `Operacao`
  (Recebida=0/Quitacao=1/Cancelamento=2) espelham exatamente
  `CK_FIN_VENDA_STATUS`/`CK_FIN_VENDA_HIST_OPER` do `database/01-schema.sql`.
- `Venda.ClienteId` (`string`) e `ValorTotal` (`decimal?`) anuláveis
  (ADR-007/D-08); `CriarPendente` exige `clienteId` não vazio (via
  `ArgumentException`) e gera 1 histórico `Recebida` sem `StatusAnterior`;
  `CriarPorQuitacao` nasce Quitada com histórico `Recebida`+`Quitacao` (2
  registros) e valida `dataQuitacaoUtc >= dataRecebimentoUtc`
  (`ArgumentOutOfRangeException`) — os dois comportamentos batem com os testes
  `CriarPendente_*`/`CriarPorQuitacao_*` de `VendaTests.cs` e com o critério de
  aceite literal.
- Domain sem dependência externa: `ERPFinanceiro.Domain.csproj` confirmado sem
  `PackageReference`/`ProjectReference` (só `AssemblyAttribute` de
  `InternalsVisibleTo`, que não é dependência).
- `DomainException` (base abstrata), `VendaJaCanceladaException`,
  `MotivoObrigatorioException` existem como classes prontas (regra em si só em
  T-08/T-09, conforme a própria tarefa descreve).

### T-08 — Domain: máquina de estados Quitar()/Cancelar() sem regra de motivo

**Resultado: Aprovado.**

Matriz de transições verificada linha a linha contra `Venda.cs` e os testes
`Quitar_*`/`Cancelar_*` de `VendaTests.cs`:

| De \ Operação | `Quitar()` | `Cancelar()` |
|---|---|---|
| Pendente | -> Quitada, `true`, 1 histórico `Quitacao` | -> Cancelada, `true`, 1 histórico `Cancelamento` |
| Quitada | idempotente, `false`, sem histórico novo | -> Cancelada, `true` (regra de motivo só em T-09) |
| Cancelada | lança `VendaJaCanceladaException` | idempotente, `false`, sem histórico novo |

Bate exatamente com RN-03 ("Cancelada é terminal") e RN-04 ("transições
válidas... repetição é idempotente; demais 409" — `VendaJaCanceladaException` é
o sinal de domínio que a Api mapeará para 409 em T-28, fora do escopo deste
lote). `statusAnterior` é capturado antes da troca de `Status` em ambos os
métodos — histórico sempre reflete a transição real, não o estado final
duplicado.

### T-09 — Domain: regra S-05 (motivo obrigatório em Quitada->Cancelada)

**Resultado: Aprovado.**

- `Cancelar(dataCancelamentoUtc, motivo, correlationId)`: Quitada + motivo
  nulo/vazio/só espaço (`IsNullOrWhiteSpace`) lança `MotivoObrigatorioException`
  **antes** de qualquer mutação — confirmado que `Status`, `DataCancelamento`,
  `MotivoCancelamento`, `Versao` e `Historico.Count` ficam inalterados (testes
  `Cancelar_VendaQuitada_SemMotivo_*`/`ComMotivoEmBranco_*` verificam
  explicitamente `historicoAntes == depois` e `versaoAntes == depois`).
- Quitada + motivo válido cancela e grava o motivo em `MotivoCancelamento` **e**
  no `VendaHistorico.Motivo` do registro de cancelamento (auditoria completa).
- Pendente sem motivo continua cancelando normalmente (motivo opcional nesse
  caso) — bate com D-06/S-05 (regra só se aplica a Quitada) e com o critério de
  aceite literal ("Pendente sem motivo cancela normalmente").
- Assinatura antiga de `Cancelar(DateTime, string correlationId)` de T-08 foi
  alterada para inserir `motivo` **antes** de `correlationId` — checado que os
  chamados por nome (`correlationId: "..."`) nos testes de T-08 continuam
  compilando corretamente (parâmetro opcional).

### T-10 — Domain: `Venda.CriarCancelada` (venda desconhecida)

**Resultado: Aprovado.**

- `CriarCancelada(vendaId, motivo, dataCancelamentoUtc, correlationId)`: venda
  nasce `Status=Cancelada`, `ClienteId`/`ValorTotal` nulos, `Itens` vazio, 1
  único registro de histórico `Cancelamento` com `StatusAnterior=null` — os 3
  pontos do critério de aceite conferidos linha a linha contra o teste
  `CriarCancelada_VendaDesconhecida_*`.
- Consistente com D-08/ADR-007 e com a interpretação já ratificada em
  `database/02-seed.sql` (V-1003: sem itens, sem `Recebida` separada) — a nota
  do Executor sobre essa divergência do texto ilustrativo da própria linha
  T-10/T-06 é aceitável (decisão de detalhe documentada, não reinterpretação
  do critério formal, que não especifica a composição exata do histórico além
  de "1 histórico").
- `vendaId` continua validado (herda a checagem do construtor privado comum);
  `motivo` não tem validação de obrigatoriedade nesta fábrica — não é exigido
  pelo critério de aceite da linha nem por RN-07/D-08 (`PRD-TECNICO.md`), que só
  descrevem a criação do registro Cancelada, sem exigir motivo não vazio para
  este caso específico. Não é achado.

### Consistência de transições vs. RN-03/RN-04/S-05/D-08 (checagem cruzada do lote)

Matriz completa (3 estados x 2 operações, incluindo idempotência e exceções)
coberta pelos 16 testes de `VendaTests.cs` — nenhuma lacuna encontrada: toda
combinação Pendente/Quitada/Cancelada x Quitar/Cancelar tem pelo menos 1 teste
dedicado, incluindo os 2 casos de exceção (`VendaJaCanceladaException`,
`MotivoObrigatorioException`) e os 2 casos de idempotência
(`Quitar`/`Cancelar` repetidos). Nenhuma divergência entre código e regra de
negócio documentada.

### Testes de integração cruzada (Lote 2)

Não aplicável no sentido de integração entre serviços distintos (o lote é só
Domain puro + esqueleto de solução, sem Application/Infrastructure ativos
ainda). Verificada a integração "estática" entre T-04 (esqueleto) e T-07…T-10
(código que vive dentro dele): `ERPFinanceiro.Tests.csproj` referencia
`Domain`+`Application` desde T-04, e os testes de `VendaTests.cs` compilam e
cobrem exatamente as classes criadas por T-07…T-10 — sem gap de integração
entre as tarefas do lote.

### Requisitos não funcionais

- Dinheiro sempre `decimal`/`decimal?` (nunca `float`/`double`) em
  `VendaItem.PrecoUnitario`, `Venda.ValorTotal` — regra 4 da Seção 1 e G-3.14
  do `GUARDRAILS.md` cumpridos.
- Datas recebidas como `DateTime` já UTC pelos chamadores (Domain não usa
  `DateTime.Now`/`DateTime.UtcNow` em nenhum ponto de `Venda.cs`/
  `VendaHistorico.cs`/`VendaItem.cs` — confirmado por busca no código) — regra
  4/`IClock` respeitada (a injeção do relógio real é responsabilidade da
  Application, ainda não implementada, fora do escopo deste lote).
- Sem tela/API rodando — demais NFRs (performance, UX) não aplicáveis a este
  lote.

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Zero reprovações neste lote (críticas ou simples) — não há padrão a
avaliar.

## Fechamento estrutural do Lote 2

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá para não duplicar) —
resumo: todas as 5 tarefas `Concluída`, sem dependência órfã da Seção 4
relativa ao Lote 2, sem tarefa `Bloqueada`. Lote fecha **Validado** (sem
ressalvas) após a auditoria de segurança.

## Lote 3 — Banco

**Veredito: Aprovado, sem ressalvas.** As 4 tarefas (T-05, T-06, T-11, T-12)
permanecem `Concluída`. Nenhuma reprovação crítica nem simples.

Validado por leitura de código real (`database/01-schema.sql`,
`database/02-seed.sql`, `FinanceiroDbContext.cs`, `DbInitializer.cs` e
respectivos testes — não pela nota de implementação do Executor) contra o
critério de aceite literal de cada linha (`TASK.md` Seção 3, Lote 3) e contra
ADR-005 (concorrência), ADR-007/D-08 (colunas anuláveis) e RT-05 (mensagem
clara de bloqueio).

### T-05 — `database/01-schema.sql`

**Resultado: Aprovado.**

- 3 tabelas (`FIN_VENDA`, `FIN_VENDA_ITEM`, `FIN_VENDA_HISTORICO`), PK
  `BIGINT GENERATED BY DEFAULT AS IDENTITY` nas 3 — conforme decisão do
  spike T-01/ADR-009 (GO confirmado no Lote 1).
- `UNIQUE(VENDA_ID)` (`UQ_FIN_VENDA_VENDA_ID`); índices nomeados
  `IDX_FIN_VENDA_CLIENTE`/`_STATUS`/`_DATA_REC` presentes, mais 2 de apoio a
  FK (não exigidos pelo critério, mas justificados no comentário do script).
- Identificadores: maior nome é `TRG_FIN_VENDA_HIST_NO_UPD` (25 chars) <= 31
  — confirmado por leitura direta e por consulta real a `RDB$` na evidência
  de execução (`database/evidencia-execucao-T-05.txt`, 21/21 PASS).
- `VALOR_TOTAL DECIMAL(18,2)` e `PRECO_UNITARIO DECIMAL(18,4)` conferidos
  linha a linha no DDL e confirmados via `RDB$FIELDS` na evidência real
  (precision=18/scale=2 e precision=18/scale=4).
- `VERSAO INTEGER DEFAULT 0 NOT NULL` sem nenhum trigger associado a
  `FIN_VENDA` — confirmado (0 triggers encontrados na evidência); regra 8 da
  Seção 1 e ADR-005 respeitadas (concorrência incrementada só pela aplicação).
- `CLIENTE_ID`/`VALOR_TOTAL` anuláveis (sem `NOT NULL`) — bate com
  ADR-007/D-08, confirmado no DDL e na evidência (`anulavel=True` para os
  dois).
- CHECKs de item (`QUANTIDADE > 0`, `PRECO_UNITARIO >= 0`) e FK
  `ON DELETE CASCADE` em `FIN_VENDA_ITEM` exercitados com INSERT/UPDATE/
  DELETE reais na evidência (rejeita quantidade 0 e preço negativo, aceita
  preço 0, cascade remove itens ao deletar a venda).
- Triggers `BEFORE UPDATE/DELETE` em `FIN_VENDA_HISTORICO` (append-only,
  regra 8) exercitados de fato: UPDATE e DELETE diretos rejeitados com
  `EX_HIST_APPEND_ONLY`.
- **Execução real confirmada** (não só inspeção estrutural): harness
  descartável cria `.fdb` novo e aplica o script via `FbScript`/
  `FbBatchExecution` — 21/21 PASS, 0 FAIL, evidência salva em
  `database/evidencia-execucao-T-05.txt`.
- **Observação informativa, sem impacto (não vira achado)**: `FIN_VENDA_HISTORICO`
  também tem `ON DELETE CASCADE` (além de `FIN_VENDA_ITEM`, que era o
  explicitamente pedido pelo critério de aceite). Como o append-only é
  reforçado por trigger `BEFORE DELETE`, um `DELETE` em `FIN_VENDA` que
  tentasse cascatear para um histórico existente falharia por causa do
  trigger (efeito colateral não exercitado na evidência, que só testou
  cascade em `FIN_VENDA_ITEM`). Não é um achado: nenhuma tarefa do TASK.md
  (Domain, Application, API) expõe uma operação de exclusão de `Venda` —
  a regra de negócio (SDD/PRD-TECNICO) nunca prevê apagar uma venda, só
  transições de estado (Quitar/Cancelar). Comportamento inofensivo por
  ausência de caminho de código que o exercite; registrado aqui só para
  rastreabilidade, não gera tarefa em `Refatoração Lote-3`.

### T-06 — `database/02-seed.sql`

**Resultado: Aprovado.**

- 3 vendas (Quitada V-1001, Pendente V-1002, Cancelada-sem-quitação V-1003),
  com itens e histórico coerentes com a trajetória de cada uma — conferido
  linha a linha contra o script.
- V-1003 segue a decisão já ratificada do ADR-007 (sem itens, histórico só
  Cancelamento com `STATUS_ANTERIOR NULL`) em vez do exemplo ilustrativo
  genérico do texto da tarefa — desvio pequeno, já documentado no cabeçalho
  do próprio script e coerente com T-10 (`Venda.CriarCancelada`, Lote 2,
  já aprovado com a mesma interpretação). Não reinterpreto o critério: a
  parte literal ("com itens e histórico coerentes", "soma... documentada em
  comentário") está cumprida; a divergência é só do exemplo ilustrativo, que
  cede a uma decisão de arquitetura já tomada e ratificada.
- Soma da massa documentada no cabeçalho (`951.00`, CA-07.3) confirmada por
  cálculo independente: 500.50 + 450.50 + 0.00 (V-1003 nula) = 951.00.
- **Execução real confirmada**: harness aplica `01-schema.sql` +
  `02-seed.sql` num `.fdb` novo e valida por SELECT — 23/23 PASS, 0 FAIL,
  evidência em `database/evidencia-execucao-T-06.txt` (contagens de vendas/
  itens/histórico, totais por venda batendo com a soma dos itens, soma total
  951.00 confirmada).

### T-11 — `FinanceiroDbContext` (mapeamento Fluent EF6)

**Resultado: Aprovado.**

- Mapeamento Fluent das 3 entidades conferido coluna a coluna contra
  `database/01-schema.sql`: nomes de tabela/coluna idênticos, `VALOR_TOTAL`
  `.HasPrecision(18, 2)`, `PRECO_UNITARIO` `.HasPrecision(18, 4)` — batendo
  exatamente com o DDL de T-05 (mesma escala/precisão, não invertidas).
- `VERSAO` mapeado com `.IsConcurrencyToken()` — token de concorrência
  otimista do EF6, sem trigger de banco (ADR-005 respeitado).
- `Database.SetInitializer<FinanceiroDbContext>(null)` no construtor
  estático — EF6 nunca cria/altera schema sozinho, conforme regra 10 (sem
  migrations).
- Associação `HasMany(...).WithRequired().Map(m => m.MapKey("VENDA_REF"))`
  para itens/histórico — nome da coluna de FK sombra bate com `VENDA_REF`
  do DDL; `Itens`/`Historico` (API pública `IReadOnlyList`) corretamente
  ignorados no mapeamento (`venda.Ignore(...)`), evitando conflito com o
  EF6 exigir `ICollection<T>` mutável.
- Teste de integração real (`FinanceiroDbContextTests.cs`) cobre os 2
  critérios de aceite literais: (1) inserir venda com 2 itens + histórico e
  reler sem perda de decimal, incluindo `PRECO_UNITARIO` com 4 casas
  (`150.5075m`) — assertado byte a byte; (2) dois contextos carregam a
  mesma linha (`VERSAO=0`), o primeiro salva e incrementa, o segundo tenta
  salvar com a versão antiga e recebe `DbUpdateConcurrencyException` — bate
  exatamente com o critério ("atualização com `VERSAO` velha lança
  conflito").
- **Execução real confirmada nesta sessão, depois da resolução parcial do
  Bloqueio 001** (Smart App Control desativado pelo usuário): `dotnet build`
  0 avisos/erros; `dotnet vstest` direto no worktree (OneDrive) ainda falhou,
  mas com HRESULT diferente do bloqueio original (`0x80131515`, restrição de
  zona da pasta OneDrive, não mais `0x800711C7` do Smart App Control);
  aplicado o contorno já documentado (copiar `bin/Debug/net48` para fora do
  OneDrive) e rodado de lá — **2/2 testes verdes** na 1ª tentativa. A nota de
  implementação da linha T-11 do `TASK.md` é **consistente** com os logs
  detalhados do `BLOCKERS.md` (mesma cronologia, mesmos HRESULTs, mesmo
  contorno) — não há indício de execução fabricada; os dois HRESULTs, a
  cópia para fora do OneDrive e a contagem 2/2 batem entre os dois
  documentos.
- `ItensEf`/`HistoricoEf` seguem `internal`, mesmo padrão já auditado pelo
  DevSecOps no Lote 2 (ver `SECURITY-REVIEW.md`) — nenhuma mudança de
  visibilidade nesta tarefa.

### T-12 — `DbInitializer`

**Resultado: Aprovado.**

- Cria o `.fdb` se ausente (`FbConnection.CreateDatabase`) e aplica
  `database/01-schema.sql` embutido como Embedded Resource (fonte única,
  sem cópia duplicada — confirmado no `.csproj`: `EmbeddedResource
  Include="..\..\database\01-schema.sql"`) via `FbScript`/
  `FbBatchExecution`, mesmo mecanismo de T-05/T-06.
- Idempotência: `Inicializar()` é no-op se o arquivo já existe (só chama
  `GarantirAcessivel`, não tenta reaplicar o schema) — confirmado no código
  e no teste `Inicializar_SegundaExecucao_ENoOp` (3 tabelas antes e depois,
  sem exceção).
- `.fdb` bloqueado por outro handle gera `BancoIndisponivelException` com
  mensagem clara citando bloqueio, RT-05 — confirmado no código
  (`GarantirAcessivel`) e no teste
  `Inicializar_ArquivoBloqueadoPorOutroHandle_LancaExcecaoComMensagemClara`
  (`FileStream` com `FileShare.None` simulando o segundo handle,
  `Assert.Contains("bloqueado", ...)`).
- Credenciais via `IConfiguracaoBanco` injetado (interface em Application,
  implementação `ConfiguracaoBancoAppConfig` em Infrastructure lendo
  `App.config`/`appSettings`) — nunca hardcoded no `DbInitializer` nem em
  nenhum outro ponto de produção (checagem cruzada com o chapéu DevSecOps
  abaixo).
- **Execução real confirmada**: 9/9 testes verdes via `dotnet vstest`
  (3 cenários do critério de aceite, cobertos por
  `DbInitializerTests.cs`), com o mesmo contorno de pasta fora do OneDrive
  documentado no cabeçalho do teste — nota de ambiente coerente com
  `BLOCKERS.md`, não é falha do `DbInitializer`.

### Testes de integração cruzada (Lote 3)

- T-11 depende de T-05 (schema) e T-07 (entidades, Lote 2): confirmado que
  o mapeamento Fluent não inventa nenhuma coluna/tabela fora do que T-05
  criou, e que as entidades de T-07 (`Venda`/`VendaItem`/`VendaHistorico`)
  são exatamente as mapeadas.
- T-12 depende de T-04 (esqueleto) e T-05 (schema): o `DbInitializer` aplica
  literalmente o mesmo `database/01-schema.sql` de T-05 (recurso embutido
  com fonte única, sem duplicação) — sem risco de schema divergente entre
  o que T-05 testou isoladamente e o que T-12 aplica em runtime.
- T-11 e T-12 compartilham o mesmo padrão de acesso ao Firebird (`FbScript`/
  `FbBatchExecution`, `ServerType=Embedded`) e o teste de T-11
  (`FinanceiroDbContextTests`) efetivamente usa `DbInitializer` (T-12) no
  `Dispose`/construtor para preparar o `.fdb` de teste — integração real
  entre as duas tarefas exercitada de fato, não só por leitura de código.
- T-06 (seed) roda sobre o mesmo `01-schema.sql` de T-05 sem erro
  (evidência de execução única, aplicando os dois scripts em sequência).

### Requisitos não funcionais

- Dinheiro sempre `decimal`/`decimal?` em todo o lote (`DECIMAL(18,2)`/
  `DECIMAL(18,4)` no schema, `.HasPrecision` correspondente no mapeamento
  EF6) — regra 4/G-3.14 cumprida.
- Datas em UTC (`TIMESTAMP` no schema, sem `DateTime.Now` em nenhum ponto
  de `DbInitializer`/`FinanceiroDbContext`) — regra 4 cumprida.
- Identificadores <= 31 chars (regra 10) — confirmado por leitura e por
  execução real contra `RDB$`.
- Sem SQL concatenado com entrada externa em nenhum dos 4 artefatos (DDL
  fixo, `FbScript` só executa o conteúdo do próprio `01-schema.sql`
  embutido, sem interpolação de dado de usuário).

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Zero reprovações neste lote (críticas ou simples) — não há padrão a
avaliar. A observação informativa sobre `FK CASCADE` + trigger de append-only
em `FIN_VENDA_HISTORICO` (T-05) não é um bug: é um efeito colateral correto
(append-only reforçado até contra cascade) sem caminho de código que o
exercite hoje ou no roteiro do TASK.md.

## Fechamento estrutural do Lote 3

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá para não duplicar) —
resumo: todas as 4 tarefas `Concluída`, sem dependência órfã da Seção 4
relativa ao Lote 3, sem tarefa `Bloqueada`. Lote fecha **Validado** (sem
ressalvas) após a auditoria de segurança.

## Lote 4 — Persistência e base da Application

**Veredito: Aprovado, sem ressalvas de QA.** As 6 tarefas (T-13, T-14, T-15,
T-16, T-17, T-21) permanecem `Concluída`. Nenhuma reprovação crítica nem
simples contra o critério de aceite literal. 2 achados de robustez/infra de
teste (fora do critério de aceite literal de qualquer tarefa deste lote) são
tratados abaixo e na `SECURITY-REVIEW.md`/checagem estrutural — ambos viram
tarefa em `Refatoração Lote-4`.

Validado por leitura de código real (não pela nota de implementação do
Executor) contra o critério de aceite literal de cada linha (`TASK.md` Seção
3, Lote 4) **e por execução independente da suíte de testes** (não só
confiando na evidência já registrada pelo Executor): `dotnet build` de
`ERPFinanceiro.Tests` (0 aviso/0 erro) e `dotnet vstest` rodado 2x fora do
OneDrive (contorno de rotina do Bloqueio 001) sobre os testes de T-13…T-21
(`ApplicationContratosTests`, `VendaRepositoryTests`, `UnitOfWorkTests`,
`ExecutorComRetryTests`, `ValidadorVendaCommandTests`, `TraceLoggerTests`).

### T-13 — Application: interfaces, comandos, `FiltroVendas`

**Resultado: Aprovado.**

- Compila sem `PackageReference`/referência a EF/Web API (`ERPFinanceiro.
  Application.csproj` só referencia `Domain`, confirmado por leitura).
- `IVendaRepository` expõe exatamente 2 métodos (`ObterPorVendaId`,
  `Adicionar`) — sem update genérico, trava a regra "histórico append-only /
  mutação só via agregado" (regra 8 da Seção 1). Conferido no código-fonte da
  interface, não só na nota do Executor.
- Comandos (`QuitarVendaCommand`, `CancelarVendaCommand`,
  `RegistrarVendaCommand`, `ItemVendaCommand`) e `FiltroVendas` existem com os
  campos esperados do `docs/contrato-v1.1.md` 3.1/3.2/3.4.

### T-14 — `VendaRepository` EF6

**Resultado: Aprovado.**

- `ObterPorVendaId`/`Adicionar` implementam `IVendaRepository` via `Include`
  por string (`ItensEf`/`HistoricoEf`, contorno documentado e necessário por
  serem `internal`); `ListarMaisRecentes`/`ObterParaConsultaSomenteLeitura`
  usam `AsNoTracking()`. Nenhum SQL concatenado — só LINQ/EF (regra 6).
- **Execução real confirmada por mim** (não só pela nota do Executor):
  `VendaRepositoryTests` passou 3/3 na suíte completa rodada nesta validação
  (ver nota de execução no topo da seção do lote).

### T-15 — `UnitOfWork`/transação e tradução de conflito

**Resultado: Aprovado quanto ao critério de aceite literal.** ("Falha injetada
no histórico faz rollback da venda; conflito de `VERSAO` e violação UNIQUE
viram `ConcorrenciaException`" — os 3 cenários exercitados de fato contra
Firebird real em `UnitOfWorkTests.cs`, reconferidos por mim nesta validação,
1ª e 2ª execução: passam sempre que a suíte roda com o Firebird embarcado
estável — ver achado de infraestrutura de teste abaixo, que não é uma falha
deste critério de aceite, é uma condição de ambiente que afeta a suíte
inteira, não específica de T-15).

**Ponto de atenção do Executor avaliado — procede parcialmente, vira achado
de robustez (não reprovação):** a tradução de violação de `UNIQUE(VENDA_ID)`
depende de casar o **texto em inglês** da mensagem da `FbException`
(`"violation of PRIMARY or UNIQUE KEY constraint"` + nome da constraint). Eu
confirmei por reflexão contra o assembly `FirebirdSql.Data.FirebirdClient`
10.3.4 que, de fato, **não existe uma constante pública nomeada** para
"unique violation" em `FirebirdSql.Data.Common.IscCodes` — a alegação do
Executor procede. Porém `FbException` **expõe `ErrorCode` (int) e `SQLSTATE`
(string)**, ambos numéricos/padronizados e **independentes de locale** — não
verificados nem usados pelo código atual, que só olha a mensagem textual.
Isso é uma fragilidade real: se o servidor/cliente Firebird rodar com
mensagens de erro em outro idioma (locale do SO/servidor diferente de
inglês), a checagem por texto falha silenciosamente e a violação de UNIQUE
deixa de virar `ConcorrenciaException` — propaga como `DbUpdateException` genérica,
sem tradução para 409 `CONFLITO_CONCORRENCIA` (T-28/T-16). Não é uma
reprovação porque (a) o critério de aceite foi cumprido e testado de fato
contra Firebird real no locale usado hoje pelo projeto; (b) o `README`/
ambiente de entrega (T-54, ainda não escrito) não prevê locale não-inglês; (c)
é um ajuste pontual e de baixo esforço (usar `ErrorCode`/`SQLSTATE` como sinal
primário, com o texto como fallback). **Classificado como achado simples
(débito de robustez, severidade baixa)** — vira tarefa em `Refatoração
Lote-4` (RL4-01).

### T-16 — `ExecutorComRetry`

**Resultado: Aprovado.**

- Laço captura `ConcorrenciaException`, no máximo 3 tentativas
  (`MaximoTentativas = 3`), propaga na 3ª falha — confirmado linha a linha no
  código e nos 3 testes de `ExecutorComRetryTests` (conflita 2x e passa na
  3ª; conflita sempre e propaga após exatamente 3; variante `Action`),
  reexecutados por mim nesta validação.

### T-17 — `ValidadorVendaCommand`

**Resultado: Aprovado.**

- CA-01.5 (IDs vazios, `itens` vazio, `quantidade<=0`, `precoUnitario<0`) e
  CA-01.6 (tolerância 0,01) cobertos linha a linha no código
  (`ValidadorVendaCommand.ValidarInterno`). Diferença de 0,01 aceita (`<=`,
  não `<`) e 0,02 rejeitada, conferido no código (`diferenca >
  ToleranciaValorTotal`).
- Interpretação dos limites "50/500/1000" (não detalhados na própria linha da
  tarefa): fonte usada foi `SDD.md` Seção 7 ("Entrada": IDs 50, motivo 500,
  itens <= 1000) — **conferido por mim contra o texto exato do SDD.md**, bate
  100% com a interpretação documentada no XML doc da classe. Não é
  reinterpretação do critério, é a fonte primária correta indicada pelo
  próprio guardrail de "critério ambíguo remete ao SDD".

### T-21 — `TraceLogger`

**Resultado: Aprovado.**

- Implementa `IAppLogger` via `TextWriterTraceListener`/`Trace`, sem
  biblioteca externa (S-10/Serilog corretamente fora, Tier C). Caminho do
  arquivo recebido por construtor, nunca hardcoded.
- **Disciplina de "nunca logar ApiKey" é do chamador, não do logger** —
  avaliado como design aceitável: o logger é uma classe de infraestrutura
  genérica (grava o que recebe); a responsabilidade de nunca formatar uma
  mensagem com `ApiKey` embutida é de quem chama `Registrar` (`QuitacaoService`/
  `CancelamentoService`/handlers da Api, ainda não implementados — Lotes 5/6/8).
  Revalidar esse ponto quando essas chamadas existirem (nota para os próximos
  lotes, não achado deste).

### Ponto de atenção do Executor avaliado — T-23 (Lote 5)/`xunit.runner.json`

**Procede, é um achado real.** Confirmei por busca no repositório inteiro
(`find` recursivo) que **`xunit.runner.json` não existe em nenhum lugar do
worktree**, nem está referenciado em `ERPFinanceiro.Tests.csproj` (nenhum
`<None Include>`/`<Content Include>` correspondente) — apesar da nota da linha
T-23 no `TASK.md` afirmar "parallelização xUnit desligada via
`xunit.runner.json`". **Reproduzi o problema de fato**: rodei a suíte de
testes deste lote 2x sem esse arquivo (paralelização xUnit padrão, ligada) e
obtive **1 falha intermitente por execução** —
`AccessViolationException` dentro do motor nativo do Firebird Embedded
(`isc_create_database`), exatamente o padrão já descrito por T-22/T-23 — nas
2 tentativas seguintes, criando manualmente um `xunit.runner.json` com
`parallelizeAssembly:false`/`parallelizeTestCollections:false` na pasta de
saída (só para fins desta verificação, não commitado), a suíte completa (93
testes) passou **2/2 sem nenhuma falha**. Ou seja: a causa raiz e a correção
proposta pelo próprio T-23 estão corretas, mas **o arquivo nunca foi de fato
criado/commitado** — a nota de implementação está desalinhada do estado real
do repositório. Isto não é uma reprovação de nenhuma tarefa deste Lote 4 (o
arquivo seria responsabilidade de T-23, Lote 5, não deste lote), mas é um
risco real e não corrigido para **T-20** (Lote 5, teste de concorrência real,
que depende de estabilidade de execução da suíte contra o Firebird embarcado)
e para qualquer execução futura da suíte inteira/CI. **Classificado como
achado simples** (correção de baixo esforço: adicionar o arquivo de
configuração real, já validado por mim que resolve o problema) — vira tarefa
em `Refatoração Lote-4` (RL4-02), com recomendação explícita de ser resolvida
**antes de T-20 iniciar** (não bloqueia T-18/T-19, que não dependem de
paralelização de suíte).

### Testes de integração cruzada (Lote 4)

- T-14 depende de T-11 (Lote 3, já validado)/T-13 (mesmo lote): confirmado que
  `VendaRepository` implementa exatamente a interface `IVendaRepository` de
  T-13, sem método extra no contrato público, e mapeia para o mesmo
  `FinanceiroDbContext` de T-11 sem divergência de coluna/tabela.
- T-15 depende de T-14: `UnitOfWork` opera sobre o mesmo `FinanceiroDbContext`
  injetado por `VendaRepository`/testes — mesma unidade de trabalho,
  confirmado no teste de rollback (venda+item+histórico na mesma transação).
- T-16 depende de T-15: `ExecutorComRetry` captura exatamente o tipo
  `ConcorrenciaException` produzido por `UnitOfWork.SalvarAlteracoes` — sem
  acoplamento a exceções do EF6/Firebird diretamente (Application não conhece
  EF, regra 1 da Seção 1), confirmado por leitura de `ExecutorComRetry.cs`
  (só `using ERPFinanceiro.Application.Exceptions`).
- T-14/T-17/T-21 (paralelas, mesma rodada R2 da Seção 4.1): sem conflito de
  arquivo/contrato entre si — cada uma toca arquivos próprios
  (`VendaRepository.cs`, `ValidadorVendaCommand.cs`/`ErroValidacao.cs`/
  `ResultadoValidacao.cs`, `TraceLogger.cs`), confirmado por `git log`/leitura,
  sem edição cruzada de um arquivo pela tarefa "errada".

### Requisitos não funcionais

- Dinheiro sempre `decimal` (nunca `float`/`double`) em todo o lote — regra 4/
  G-3.14 cumprida (`ValidadorVendaCommand`, `VendaRepository`, `UnitOfWork`
  não introduzem nenhum tipo de ponto flutuante binário).
- `IUnitOfWork.SalvarAlteracoes()` síncrono (decisão documentada de T-13,
  reconfirmada em uso real por T-15) — consistente com a regra 5 da Seção 1
  ("`DbContext` por operação/requisição"), já que I/O assíncrono fica para a
  Api (Lote 6+), fora do escopo deste lote.
- Sem SQL concatenado em nenhum artefato do lote (regra 6) — `VendaRepository`
  só LINQ/EF, `UnitOfWork` só `SaveChanges()`.

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Os 2 achados (mensagem de erro Firebird em inglês; ausência real do
`xunit.runner.json`) são pontuais, de naturezas distintas (robustez de
detecção de conflito vs. infraestrutura de teste), com correção de baixo
esforço cada um. Não sugerem problema sistêmico de decomposição de tarefas
nem de diretrizes de implementação — a diretriz de "verificar empiricamente
contra Firebird real" (regra 16 da Seção 1) foi seguida em ambos os casos, só
não cobriu o cenário de locale não-inglês (T-15) nem foi de fato commitada
(xunit.runner.json, T-23). Não escalo ao `coordenador` por este motivo.

## Fechamento estrutural do Lote 4

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá para não duplicar) —
resumo: todas as 6 tarefas `Concluída`, sem dependência órfã da Seção 4
relativa ao Lote 4, sem tarefa `Bloqueada`, T-18/T-19/T-20 (Lote 5)
corretamente liberadas quanto às dependências deste lote. 2 achados simples
viram tarefas em `Refatoração Lote-4` (RL4-01, RL4-02). Lote fecha **Validado
(com ressalvas)** após a auditoria de segurança.

## Lote 5 — Serviços de negócio

**Veredito: Aprovado com ressalvas.** As 6 tarefas (T-18, T-19, T-20, T-22,
T-23, T-24) permanecem `Concluída`. Nenhuma reprovação crítica nem simples
contra o critério de aceite literal de qualquer tarefa. 1 achado de
correção/robustez sob concorrência (não uma reprovação de critério de aceite
— ver detalhamento em T-18/T-20 abaixo) vira tarefa em `Refatoração Lote-5`.

Validado por leitura de código real (`QuitacaoService.cs`,
`CancelamentoService.cs`, `ExecutorComRetry.cs`, `HealthService.cs`,
`ConsultaService.cs`, `ConcorrenciaIdempotenciaTests.cs` — não pela nota de
implementação do Executor) contra o critério de aceite literal de cada linha
(`TASK.md` Seção 3, Lote 5) **e por execução independente da suíte completa**
(não só confiando na evidência já registrada pelo Executor): `dotnet build`
de `ERPFinanceiro.Tests` (0 aviso/0 erro) e `dotnet vstest` rodado **2x
seguidas fora do OneDrive** (mesmo contorno de rotina do Bloqueio 001) —
**109/109 Aprovado nas duas rodadas**, sem nenhuma `AccessViolationException`
intermitente. Adicionalmente, o harness `ConcorrenciaIdempotenciaTests`
(T-20) foi reexecutado **isoladamente 3x seguidas** por mim, fora da suíte
completa — 3/3 verde, sem flakiness, confirmando de forma independente que
RL4-02 (débito herdado do Lote 4) segue resolvido com este harness incluído.

### RL4-02 — confirmação independente de resolução

**Confirmado resolvido, não é só afirmação do Executor.** `src/
ERPFinanceiro.Tests/xunit.runner.json` existe (`parallelizeAssembly: false`,
`parallelizeTestCollections: false`), está registrado em `ERPFinanceiro.
Tests.csproj` (`<None Include="xunit.runner.json"
CopyToOutputDirectory="PreserveNewest" />`) e confirmado copiado para `bin/
Debug/net48/xunit.runner.json` após `dotnet build` nesta sessão. Execução
real por mim: bin copiado para fora do OneDrive e `dotnet vstest` rodado 2x
seguidas — 109/109 Aprovado nas duas rodadas, sem nenhuma
`AccessViolationException`. RL4-02 pode ser marcada definitivamente resolvida
(já estava `Resolvida` na nota do Executor; esta validação é a confirmação
independente exigida antes de fechar o Lote 5).

### T-18 — `QuitacaoService`

**Resultado: Aprovado quanto ao critério de aceite literal.** Roteiro contra
`.fdb` real cobre CA-01.1, 01.2, 01.3, 01.5, 01.6 — confirmado nos 7 testes
de `QuitacaoServiceTests.cs` (inexistente cria+quita com 2 históricos;
Pendente quita; já Quitada devolve original sem novo histórico — P-2;
Cancelada lança `VendaJaCanceladaException`; payload inválido/valor
divergente lançam `ValidacaoException` sem persistir; Pendente com
`clienteId` divergente lança `DadosDivergentesException` sem alterar dados),
reexecutados por mim (109/109 na suíte completa, ver acima). Decisão de
criar `ERPFinanceiro.Application.Exceptions.ValidacaoException` (em vez de
lançar a classe da Api, que não compila a partir de Application, SDD 2.1)
está corretamente documentada e não altera o comportamento observável (o
`ExceptionParaRespostaMapper`, T-28, mapeia ambos os tipos para 400).

**Ponto de atenção central desta rodada — avaliado em detalhe (ver também
T-20 abaixo).** O `<remarks>` de `QuitacaoService.cs` documenta um risco real
de correção sob concorrência: no ramo "criar" (upsert RN-06), se a 1ª
tentativa de `ExecutorComRetry` falhar por violação de `UNIQUE(VENDA_ID)`
(outra operação concorrente criou a mesma `vendaId` primeiro), a entidade
`Added` "zumbi" da 1ª tentativa continua rastreada pelo mesmo `DbContext`
(mesma instância reaproveitada pelas até 3 tentativas de retry, dentro de
uma única chamada de `Executar`, regra 5 da Seção 1/RT-08 — "DbContext por
operação", não por tentativa de retry). A releitura da 2ª tentativa
materializa uma segunda instância para a mesma linha (identity map do EF6
resolve só pela PK técnica, que a entidade zumbi ainda não tem), e um novo
`SalvarAlteracoes` tentaria inserir a zumbi de novo — esgotando as 3
tentativas e propagando um **falso 409 `CONFLITO_CONCORRENCIA`** onde o
resultado correto seria 200 idempotente.

- **Não reproduzido**: nem nas 20 execuções do Cenário 1 de T-20 (evidência
  `evidencia-execucao-T-20.txt`, "0/20 com 1 sucesso + 1 ConcorrenciaException"),
  nem nas minhas 3 reexecuções isoladas adicionais do mesmo harness (evidência
  regravada a cada rodada, mesmo resultado: 0/20 reproduções). Total
  observado nesta validação + nas execuções do Executor: **0 reproduções em
  >= 26 corridas completas do cenário de criação concorrente**.
- **Causa raiz estruturalmente real**, não é FUD: confirmei por leitura direta
  de `ExecutorComRetry.Executar<T>` que o `while(true)`/`catch (...) when
  (tentativa < MaximoTentativas)` reexecuta o mesmo delegate `operacao`, que
  fecha sobre a mesma instância de `IVendaRepository`/`IUnitOfWork` (logo o
  mesmo `DbContext`) recebida uma única vez no construtor de
  `QuitacaoService` — não há criação de um novo contexto por tentativa em
  lugar nenhum da cadeia de chamada atual. O risco documentado é
  tecnicamente correto, só estatisticamente raro de vencer a janela de
  corrida no cenário de teste local.
- **Classificação da reprovação: nenhuma.** Não é uma reprovação de T-18 nem
  de T-20 — os dois critérios de aceite literais foram cumpridos e
  verificados de fato (roteiro de T-18 cobre os cenários pedidos; T-20 rodou
  as 20 execuções sem exceção não tratada, tratando `ConcorrenciaException`
  como resultado conhecido/esperado do próprio contrato 409, exatamente como
  a linha da tarefa instrui). É um **achado de correção funcional sob
  concorrência**, não um achado de segurança (ver `SECURITY-REVIEW.md` para
  a checagem de que não há exposição de dado envolvida).
- **Decisão de encaminhamento — opção (a) do enunciado, não (b)/(c):**
  1. Não escalo ao `coordenador` (opção b): a correção correta já está
     identificada e documentada pelo próprio Executor no `<remarks>` — dar a
     cada tentativa de retry um `DbContext` novo (fábrica de escopo por
     tentativa, dentro do escopo de operação já definido por T-26). Isso não
     contradiz nem exige revisão de nenhuma regra do `GUARDRAILS.md`/Seção 1
     do `TASK.md` ("DbContext por operação/requisição, nunca compartilhado
     entre API e UI" continua verdadeiro — o ajuste é *dentro* de uma
     operação, entre tentativas de retry da mesma operação, um detalhe de
     implementação de `ExecutorComRetry`/composição, não uma mudança de
     arquitetura de camadas nem uma remoção de guardrail). T-26 (composition
     root) ainda nem começou (`A fazer`, Lote 6) — não há decisão de
     composição já tomada para reabrir, só uma tarefa de implementação a
     mais no ponto certo.
  2. Não adio silenciosamente para "revalidar em T-26" sem tarefa registrada
     (opção c): violaria o guardrail do Validador de nunca deixar achado
     baixo/médio como só nota solta. Um achado real, mesmo de baixa
     probabilidade observada, precisa de tarefa com prazo.
  3. **Escolha: opção (a).** Cria-se `RL5-01` em `Refatoração Lote-5`
     (severidade/prioridade **alta** — não crítica, pois não reproduzida em
     >= 26 corridas e não bloqueia o Lote 5 nem nenhuma outra tarefa dele —
     mas prioridade alta porque toca diretamente P-2/idempotência, um
     critério de aceite central do sistema, e porque o ponto correto e mais
     barato de resolver é exatamente durante T-26, que ainda não rodou).
     Prazo: **antes de T-26 fechar** (ver detalhe da tarefa no `TASK.md`).

### T-19 — `CancelamentoService`

**Resultado: Aprovado.** CA-02.1…02.6 cobertos linha a linha nos 5 testes de
`CancelamentoServiceTests.cs` (Pendente cancela sem motivo; Quitada com
motivo cancela e grava motivo no histórico; Quitada sem motivo lança
`MotivoObrigatorioException` sem alterar nada; já Cancelada idempotente sem
duplicar histórico; desconhecida cria Cancelada com `ClienteId`/`ValorTotal`
nulos e 1 histórico), reexecutados por mim (109/109 na suíte completa).
`CancelarInterno` releva a venda do zero a cada tentativa de
`ExecutorComRetry` (nunca reaproveita o agregado de uma tentativa anterior),
documentado e correto.

**Observação (mesmo risco estrutural de T-18, não um achado novo/duplicado):**
o ramo "desconhecida" de `CancelarInterno` (`Venda.CriarCancelada` +
`_repositorio.Adicionar`) tem exatamente a mesma forma do ramo "criar" de
`QuitacaoService` — sujeito, em tese, ao mesmo risco de entidade zumbi numa
corrida de **criação concorrente de cancelamento** da mesma `vendaId`
inexistente (cenário não coberto pelos 20 casos de T-20, que testam criação
concorrente só via quitação). Não abro um segundo achado: a causa raiz e a
correção são as mesmas de `RL5-01` (fábrica de `DbContext` por tentativa de
retry, resolveria os dois serviços de uma vez, já que ambos usam
`ExecutorComRetry`) — anotado no texto de `RL5-01` para cobrir explicitamente
os dois serviços, não só `QuitacaoService`.

### T-20 — Harness de concorrência e idempotência

**Resultado: Aprovado.** Critério de aceite literal ("20 execuções: sempre 1
venda, 1 histórico de quitação, nenhuma exceção não tratada; ambas respostas
sucesso idempotente ou 409; evidência salva") cumprido e reverificado por
mim: reexecutei o harness isoladamente 3x seguidas (fora da suíte completa),
0 falhas, e a suíte completa 2x seguidas com o harness incluído, 109/109. A
evidência `evidencia-execucao-T-20.txt` é regravada a cada execução (não é
um arquivo estático "encenado") — confirmei isso rodando eu mesmo e
inspecionando o conteúdo, que reflete exatamente a rodada mais recente
("Risco documentado ... NÃO reproduzido nesta execução"). Pré-requisito
RL4-02 confirmado resolvido antes desta tarefa (ver seção própria acima).
Ver T-18 acima para a avaliação completa do risco documentado e não
reproduzido.

### T-22 — `IHealthService`/`HealthService`

**Resultado: Aprovado.** `ObterStatus()` nunca lança (try/catch envolvendo
toda a lógica de conexão/consulta) — confirmado no código e nos 2 testes de
`HealthServiceTests.cs` (`.fdb` real retorna `Ok=true`; caminho inválido
retorna `Ok=false` sem lançar), reexecutados por mim. `SELECT 1 FROM
RDB$DATABASE` é a query de verificação — equivalente correto para Firebird
(SDD 2.6/P-5).

**Nota informativa para T-36 (não é achado deste lote):** `ResultadoHealth.
Mensagem`, em caso de falha, carrega `ex.Message` (mensagem resumida, sem
stack trace — conforme o próprio critério de aceite "retorna falha sem
lançar"). Essa mensagem é consumida tanto pela tela (T-45, diagnóstico local
com botão "Copiar" — uso correto e esperado, D-04/ADR-004) quanto,
futuramente, pelo endpoint público `GET /api/health` (T-36, ainda `A fazer`).
SDD 2.4/2.6 já define o corpo público esperado como `{status:"degradado",
banco:"falha"}` (sem a mensagem de exceção) — se T-36 vier a ecoar
`ResultadoHealth.Mensagem` diretamente no corpo HTTP público, isso poderia
vazar detalhe interno (ex.: caminho de arquivo) a um chamador não
autenticado (o próprio endpoint é isento de `X-Api-Key`, SDD 7). Como T-36
ainda não existe, não há violação hoje — registrado como nota de atenção
para quando T-36 for implementada (ver `SECURITY-REVIEW.md`), não como
achado deste Lote 5.

### T-23 — `ConsultaService.Listar`

**Resultado: Aprovado.** Leitura via `IVendaConsultaLeitura.ListarMaisRecentes`
(AsNoTracking, sem Include de Itens/Histórico — não usados pela grade),
ordenação `DataRecebimento` desc, pede `LimiteListagem+1` para detectar
truncamento sem segunda consulta — confirmado no código e em
`ConsultaServiceListarTests.cs` (massa do seed T-06 em ordem correta; massa
de 5.001 linhas confirma 5.000 + `Truncado=true`), cobertos pela suíte
completa reexecutada por mim. Filtro deliberadamente ainda não aplicado
(T-57, Tier B) — consistente com a própria linha da tarefa.

### T-24 — `ConsultaService.ObterDetalhe`/`ObterStatus`

**Resultado: Aprovado.** Subtotal por item = `Quantidade * PrecoUnitario`;
venda inexistente lança `VendaNaoEncontradaException` (decisão de padrão
documentada e consistente com T-28/T-31, aceitável dentro do critério "nulo
ou exceção"); venda cancelada sem itens retorna lista vazia sem erro (D-08/
ADR-007) — confirmado nos 5 testes de `ConsultaServiceTests.cs`, reexecutados
por mim.

### Testes de integração cruzada (Lote 5)

- T-18/T-19 dependem de T-14/T-15/T-16 (Lote 4, já validado) e de T-08/T-09/
  T-10 (Lote 2, já validado) — confirmado que `QuitacaoService`/
  `CancelamentoService` usam exatamente os contratos já auditados
  (`IVendaRepository`, `IUnitOfWork`, `Venda.Quitar`/`Cancelar`/
  `CriarPorQuitacao`/`CriarCancelada`), sem reimplementar regra de domínio
  fora do agregado.
- T-20 integra de fato T-18 e T-19 no mesmo cenário (Cenário 3: quitação e
  cancelamento concorrentes na mesma venda Pendente) — exercitado com
  Firebird real, não mockado; resultado sempre um estado terminal válido
  (Quitada ou Cancelada) com exatamente 1 transição real, confirmado nos
  meus reexecuções.
- T-23/T-24 compartilham a mesma classe `ConsultaService` (decisão
  documentada de coordenação entre tarefas paralelas do mesmo lote) — sem
  conflito de método/contrato, confirmado por leitura do arquivo único.
- T-22 (`HealthService`) não tem dependência de execução cruzada com as
  demais tarefas do lote (serviço independente, sem uso do agregado `Venda`)
  — nenhuma integração aplicável além da suíte compartilhada.

### Requisitos não funcionais

- Dinheiro sempre `decimal` em todo o lote (nenhum `float`/`double`
  introduzido) — regra 4/G-3.14 cumprida.
- Datas via `IClock.UtcNow` (nunca `DateTime.Now`) em `QuitacaoService`/
  `CancelamentoService` — confirmado por leitura, regra 4 cumprida.
- `IUnitOfWork.SalvarAlteracoes()` chamado **uma única vez por tentativa**
  em ambos os serviços (nunca duas vezes na mesma operação de negócio,
  conforme a regra documentada em T-15) — confirmado por leitura de
  `QuitacaoService.CriarEQuitar`/`ExecutarOperacao` e
  `CancelamentoService.CancelarInterno`.
- Sem SQL concatenado em nenhum artefato do lote (regra 6) — só LINQ/EF via
  `VendaRepository`/`HealthService` usa `FbCommand` parametrizado fixo (sem
  parâmetro de entrada externa na query `SELECT 1 FROM RDB$DATABASE`).
- Performance: não aplicável de forma dedicada a este lote (sem carga real
  medida) — `AsNoTracking`/limite de 5.000 em `Listar` (T-23) já são a
  otimização prevista pelo próprio critério de aceite.

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** O único achado real (RL5-01, risco de entidade zumbi sob
concorrência) tem causa raiz única e já identificada (reaproveitamento do
mesmo `DbContext` entre tentativas de retry), afetando 2 serviços pelo mesmo
motivo estrutural (uso comum de `ExecutorComRetry`) — não é um padrão
recorrente de *decomposição* ou de *diretriz* mal seguida, é uma lacuna de
composição já prevista para ser fechada por T-26 (ainda não implementada).
Não escalo ao `coordenador` por este motivo.

## Fechamento estrutural do Lote 5

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá para não duplicar) —
resumo: todas as 6 tarefas `Concluída`, sem dependência órfã da Seção 4
relativa ao Lote 5, sem tarefa `Bloqueada`. 1 achado (RL5-01) vira tarefa em
`Refatoração Lote-5`. Lote fecha **Validado (com ressalvas)** após a
auditoria de segurança.

## Lote 6 — API base e hospedagem

**Veredito: Aprovado, sem ressalvas.** As 5 tarefas (T-25, T-26, T-27, T-28,
T-29) permanecem `Concluída`. Nenhuma reprovação crítica nem simples contra o
critério de aceite literal de qualquer tarefa. RL5-01 (débito herdado do Lote
5) confirmado **definitivamente resolvido** nesta rodada.

Validado por leitura de código real (`Startup.cs`, `CorrelationContext.cs`,
`CompositionRoot.cs`, `ApiHost.cs`, `GlobalExceptionHandler.cs`,
`ExceptionParaRespostaMapper.cs`, `CorrelationIdHandler.cs`,
`FabricaEscopoOperacaoEf.cs`, `QuitacaoService.cs`/`CancelamentoService.cs`
pós-RL5-01 — não pela nota de implementação do Executor) contra o critério de
aceite literal de cada linha (`TASK.md` Seção 3, Lote 6) **e por execução
independente da suíte de testes, rodada por mim nesta validação**: `dotnet
build src/ERPFinanceiro.Tests` (0 erro, os mesmos 4 avisos pré-existentes de
estilo `xUnit1031` em `GlobalExceptionHandlerTests`, sem relação com este
lote); `bin/Debug/net48` copiado para fora do OneDrive (contorno de rotina do
Bloqueio 001, `BLOCKERS.md`) e `dotnet vstest ERPFinanceiro.Tests.dll` rodado
de lá **2x seguidas**: **118/118 Aprovado nas duas rodadas**, sem nenhuma
falha/flakiness. Também rodei isoladamente o teste determinístico de RL5-01
(`Executar_ColisaoDeUniqueForcadaNaPrimeiraTentativa_
SegundaTentativaComEscopoNovoResolveIdempotente`), verde na 1ª tentativa —
não confiei só no número informado pelo Executor.

### T-25 — Api: `Startup` OWIN + `HttpConfiguration`

**Resultado: Aprovado.**

- `Startup.Configuration` chama `MapHttpAttributeRoutes`, remove o
  `XmlFormatter` e configura o `JsonFormatter` (UTF-8 sem BOM, camelCase via
  `CamelCasePropertyNamesContractResolver`, `DateTimeZoneHandling.Utc`,
  `DateFormatHandling.IsoDateFormat`) — confirmado linha a linha em
  `Startup.cs`.
- Sem endpoint de eco deixado no código de produção (removido antes de
  fechar, conforme o próprio critério de aceite) — confirmado por busca: só
  existe `CriarConfiguracaoJson()`, exposto estaticamente para o teste
  exercitar a serialização sem precisar subir host OWIN.
- `StartupTests` (4 casos: decimal como número, data terminando em `Z`,
  conversão para UTC, camelCase) reexecutados por mim como parte da suíte
  completa (118/118) — todos verdes.
- `ICorrelationContext` da Api (`CorrelationContext.cs`) usa
  `CallContext.LogicalGetData/SetData` com chave fixa
  (`"ERPFinanceiro.CorrelationId"`) — correto para hospedagem OWIN "pura"
  (ADR-004, sem `System.Web`/`HttpContext.Current`), atravessa `await` da
  mesma requisição via logical call context. Estado fica no `CallContext`
  (por fluxo lógico de execução), não em campo de instância — seguro mesmo
  com uma única instância de `CorrelationContext` sendo reaproveitada entre
  chamadas (confirmado por leitura, sem achado).

### T-26 — Desktop: composition root Autofac + `App.config`

**Resultado: Aprovado.**

- `CompositionRoot.Construir()` registra todas as interfaces esperadas
  (`IConfiguracaoBanco`, `IClock`, `ICorrelationContext`, `IAppLogger`,
  `IHealthService`, `IVendaRepository`/`IVendaConsultaLeitura`,
  `IUnitOfWork`, `IFabricaEscopoOperacao`, `QuitacaoService`/
  `CancelamentoService`/`ConsultaService`) — confirmado no código, sem
  interface faltando das que os Lotes 4/5 definiram.
- `FinanceiroDbContext`/`IVendaRepository`/`IUnitOfWork` registrados
  `InstancePerLifetimeScope` (nunca `SingleInstance`) — regra 5 da Seção
  1/RT-08 cumprida, confirmado linha a linha em `RegistrarPersistenciaPorOperacao`.
  `IFabricaEscopoOperacao` é `SingleInstance` **corretamente**: ela mesma não
  guarda nenhum `DbContext`, só sabe fabricar um novo a cada `Abrir()` — não é
  uma violação da regra 5, é a peça que resolve RL5-01 (ver abaixo).
- `App.config.example` conferido linha a linha: `Api:Porta`, `Api:ApiKey`
  (placeholder, sem segredo real), `Banco:CaminhoFdb`/`Usuario`/`Senha`
  (placeholder), `Log:CaminhoArquivo`, `connectionStrings` — nenhuma chave
  faltando, nenhum segredo real versionado. `git check-ignore -v
  src/ERPFinanceiro.Desktop/App.config` confirma que o `App.config` real
  está coberto pelo `.gitignore` (linha 17), e nenhum `App.config` real
  existe no worktree hoje — confirmado por mim, não só pela nota do
  Executor.
- Critério de aceite ("App sobe e resolve `QuitacaoService` sem exceção"):
  confirmado por teste de integração real, `CompositionRootTests.
  Construir_ResolveQuitacaoServiceSemExcecao` — `builder.Build()` +
  `scope.Resolve<QuitacaoService>()` não lança. Reexecutado por mim (suíte
  completa 118/118), mais os outros 3 casos de `CompositionRootTests`
  (resolve `CancelamentoService`/`ConsultaService`/`IHealthService`; confirma
  `InstancePerLifetimeScope` de fato — mesma instância de `IVendaRepository`
  dentro do mesmo escopo, instância nova em escopo novo; confirma que
  `IFabricaEscopoOperacao.Abrir()` sempre devolve um escopo/repositório
  novo).

#### RL5-01 — confirmação independente de resolução

**Confirmado definitivamente resolvido, não é só afirmação do Executor.** Li
`QuitacaoService.cs`/`CancelamentoService.cs` e confirmei que cada callback
passado a `ExecutorComRetry.Executar` abre seu próprio `IEscopoOperacao` via
`using (var escopo = _fabricaEscopo.Abrir())`, dentro do próprio delegate —
nunca reaproveitando um escopo/`DbContext` de uma tentativa anterior de
retry. `FabricaEscopoOperacaoEf.Abrir()` (Infrastructure) abre uma
`FbConnection`+`FinanceiroDbContext` novos a cada chamada, sem pool
(`Pooling = false`), com `Dispose` fechando a conexão — confirmado por
leitura direta do código, não por inferência.

Rodei eu mesmo o teste determinístico do critério de aceite de RL5-01
(`Executar_ColisaoDeUniqueForcadaNaPrimeiraTentativa_
SegundaTentativaComEscopoNovoResolveIdempotente`, em
`ConcorrenciaIdempotenciaTests.cs`): força deliberadamente a 1ª tentativa a
colidir contra `UNIQUE(VENDA_ID)` (insere e comita uma venda "concorrente"
numa conexão/contexto totalmente separados, exatamente no instante em que a
1ª tentativa chamaria `SalvarAlteracoes()`) e confirma que a 2ª tentativa,
com escopo/`DbContext` novo, resolve como sucesso idempotente (200,
`Status="Quitada"`, sem duplicata de histórico) — **verde na 1ª execução**,
isolado da suíte. Os 20+1+5 casos originais de `ConcorrenciaIdempotenciaTests`
(T-20) continuam verdes dentro da suíte completa (118/118, 2 rodadas
seguidas por mim).

RL5-01 cobre os dois serviços (`QuitacaoService` e `CancelamentoService`,
ambos consumindo o mesmo `IFabricaEscopoOperacao`/`ExecutorComRetry`) —
confirmado por leitura dos dois `<remarks>` e do próprio código, não só da
nota do `TASK.md`. RL5-01 pode ser marcada **Resolvida** com confiança de
validação independente, não apenas a afirmação do Executor.

### T-27 — Host OWIN no Desktop (`ApiHost`)

**Resultado: Aprovado.**

- `EstadoApiHost` (`Inativa/Iniciando/Ativa/InativaPortaEmUso`) e
  `ApiHost.Start(int)`/`Stop()` confirmados linha a linha. `Start` sempre
  bindado dentro de `lock (_sync)`; `catch (Exception ex) when
  (ExtrairHttpListenerException(ex) != null)` percorre a cadeia de
  `InnerException` (cobre tanto a exceção crua quanto a embrulhada pelo
  carregador do Katana) — nunca deixa a exceção de bind subir e derrubar o
  app, conforme critério de aceite.
- `Stop()` faz `Dispose()` do `IDisposable` de `WebApp.Start` — confirmado
  por teste real (`Stop_LiberaAPorta_ConfirmadoReabrindoComOutroListener`):
  reabre um `HttpListener` na mesma porta depois do `Stop()` e não lança.
- 4 testes de `ApiHostTests.cs` reexecutados por mim isoladamente (filtro
  `Desktop`): porta livre responde 200 real via `HttpClient` contra um
  pipeline OWIN mínimo; porta ocupada não lança e reporta
  `InativaPortaEmUso` com `UltimoErro` sendo `HttpListenerException`; `Stop`
  libera a porta (reabertura confirmada); caminho de produção real
  (`Start(int)`, sem sobrecarga de teste) sobe com o container real de
  `CompositionRoot.Construir()` e responde 404 numa rota não mapeada — prova
  que `Startup`+`AutofacWebApiDependencyResolver`+container real sobem sem
  exceção, mesmo sem nenhum controller ainda existir (T-30+). Todos os 4
  verdes.

#### Ponto de atenção pedido: `Startup.Container` estático

Avaliado como **decisão aceitável, não um achado**, dado o enquadramento
explícito do ADR-004 ("um executável, um processo dono do `.fdb`"):

- **Motivo técnico é real e bem documentado**: `Microsoft.Owin.Hosting`
  instancia `Startup` por reflexão (`Activator.CreateInstance`, exigido pela
  assinatura genérica `WebApp.Start<Startup>(url)`) — não há injeção por
  construtor possível nesse caminho específico. Confirmei que não existe
  outra forma no pacote `Microsoft.Owin.Hosting` 4.2.2 de passar estado ao
  `Startup` sem um mecanismo estático/globalmente acessível (é uma limitação
  do próprio host OWIN clássico, não uma escolha evitável do Executor).
- **Escopo do risco é pequeno e contido**: `Startup.Container` só é lido em
  `ConfigurarAutofac` (dentro de `Configuration(IAppBuilder)`, chamado uma
  única vez por `WebApp.Start`) e só é escrito por `ApiHost.Start(int)`
  (caminho de produção) imediatamente antes de chamar `WebApp.Start`. Dado o
  ADR-004 (um único processo/host por vez), não há dois hosts concorrentes
  disputando o mesmo campo estático em produção.
- **Risco de contaminação entre testes avaliado e descartado**: `ApiHostTests.
  Start_SemSobrecargaDeTeste_UsaStartupComContainerRealDoCompositionRoot` é o
  único teste que passa pelo caminho `Start(int)` (que escreve
  `Startup.Container`); os demais usam a sobrecarga de teste
  `Start(int, Action<IAppBuilder>)`, que nunca toca `Startup.Container`. Como
  `src/ERPFinanceiro.Tests/xunit.runner.json` (RL4-02) desliga
  `parallelizeAssembly`/`parallelizeTestCollections`, todas as classes de
  teste rodam **sequencialmente** dentro do processo de teste — não há corrida
  entre `StartupTests` (que nunca atribui `Startup.Container`, testa só
  `CriarConfiguracaoJson()`) e `ApiHostTests` escrevendo o campo estático ao
  mesmo tempo. Confirmei isso rodando a suíte completa 2x seguidas sem falha
  relacionada a esse campo.
- **Conclusão**: é um "code smell" no sentido estrito (estado estático
  mutável), mas dentro da garantia arquitetural já ratificada (processo
  único) e sem janela de corrida real detectada nem em teste nem em produção
  — não abre tarefa em `Refatoração Lote-6`. Registrado aqui como nota de
  design para rastreabilidade futura (se o ADR-004 for revisto para permitir
  múltiplos hosts/processos no mesmo `AppDomain`, este ponto precisa ser
  revisitado).

### T-28 — Api: `ExceptionHandler` global + envelope `{erro:{codigo,mensagem}}`

**Resultado: Aprovado.**

- `GlobalExceptionHandler : ExceptionHandler` registrado via
  `config.Services.Replace(typeof(IExceptionHandler), ...)` em
  `Startup.ConfigurarExceptionHandler` — confirmado.
- Tabela `ExceptionParaRespostaMapper.Mapear` conferida caso a caso:
  `VendaJaCanceladaException`->409 `VENDA_JA_CANCELADA`;
  `MotivoObrigatorioException`->409 `MOTIVO_OBRIGATORIO`;
  `VendaNaoEncontradaException`->404 `VENDA_NAO_ENCONTRADA`;
  `ValidacaoException` (Api e Application, os dois tipos)->400 com o código
  específico do `ErroValidacao`; `DadosDivergentesException`->409
  `DADOS_DIVERGENTES`; qualquer outra exceção (`default`)->500
  `ERRO_INTERNO` com mensagem genérica fixa — nunca a mensagem/stack trace
  da exceção original no envelope. Confirmado por leitura direta, não só
  pela nota do Executor.
- **500 sem stack trace confirmado nos dois níveis**: o envelope ao cliente
  (`ErroEnvelopeDto`) só recebe `MensagemGenericaErroInterno`; o detalhe
  completo (`{FullName}: {Message}\n{StackTrace}`) vai só para
  `DetalheLog`, gravado em `IAppLogger` (nunca na resposta HTTP) —
  confirmado em `ExceptionParaRespostaMapper.Envelope` e
  `GlobalExceptionHandler.Processar`.
- `IAppLogger`/`ICorrelationContext` resolvidos por requisição via
  `HttpRequestMessage.GetDependencyScope()` (não construtor) — decisão
  documentada para não acoplar T-26/T-28 (tarefas paralelas do mesmo lote);
  se a resolução falhar, o handler ainda devolve o envelope correto ao
  cliente, só o log fica ausente — confirmado no código
  (`ResolverServico<T>` com `?.`/`as T`, sem lançar).
- Testes de `ExceptionParaRespostaMapperTests`/`GlobalExceptionHandlerTests`
  (14 casos observados na execução, incluindo 2 `Theory` com múltiplos
  dados — cobertura igual ou maior que os "12 casos" citados pelo Executor)
  reexecutados por mim isoladamente (filtro `Api`), todos verdes, incluindo o
  caso que confirma por `Assert.DoesNotContain` que o corpo 500 não carrega o
  nome/mensagem da exceção original.

### T-29 — Api: `CorrelationIdHandler`

**Resultado: Aprovado.**

- `CorrelationIdHandler : DelegatingHandler` lê `X-Correlation-Id` do
  request (usa o primeiro valor não vazio) ou gera `Guid.NewGuid()`; chama
  `CorrelationContext.Definir(...)` antes de `base.SendAsync`; adiciona o
  header na resposta só se ainda não presente (`!response.Headers.Contains`)
  — confirmado linha a linha.
- Registrado em `Startup.ConfigurarCorrelationId` via
  `config.MessageHandlers.Add(...)` — roda para toda requisição, antes do
  roteamento de controller, conforme critério de aceite ("toda resposta
  traz `X-Correlation-Id`").
- 3 testes de `CorrelationIdHandlerTests` reexecutados por mim (filtro
  `Api`): resposta sempre traz o header; requisição sem header gera um novo
  e o usa na resposta; requisição com header propaga o mesmo valor sem gerar
  novo. Todos verdes.
- Gravação do `CORRELATION_ID` no histórico da venda (regra 8) já existe
  desde T-18 (`QuitacaoService`/`CancelamentoService` recebem
  `correlationId` via `ICorrelationContext.CorrelationId`, Lote 5) — a nota
  do Executor sobre "antecipação de T-18+" está correta e não é uma lacuna:
  o handler só popula o contexto, quem grava no histórico é o serviço de
  aplicação, já implementado e já auditado no Lote 5.

### Testes de integração cruzada (Lote 6)

- T-26 depende de T-14/T-15/T-21 (Lote 4) e T-25 (mesmo lote): confirmado
  que `CompositionRoot` registra exatamente as implementações reais já
  auditadas (`VendaRepository`, `UnitOfWork`, `TraceLogger`), sem
  substituir por stub/mock — a resolução de `QuitacaoService` no teste de
  integração passa pela cadeia real completa (Autofac -> Infrastructure ->
  Firebird), não simulada.
- T-27 depende de T-25 e T-26: confirmado que `ApiHost.Start(int)` conecta
  de fato o container montado por `CompositionRoot.Construir()` (T-26) ao
  `Startup` (T-25) — exercitado com host OWIN real e requisição HTTP real
  (`ApiHostTests.Start_SemSobrecargaDeTeste_...`), não só por inspeção
  estática de assinatura.
- T-28/T-29 dependem de T-25 (mesmo lote, paralelas entre si): confirmado
  que `Startup.Configuration` registra `ConfigurarCorrelationId` (T-29)
  **antes** de `ConfigurarExceptionHandler` (T-28) — ordem correta para o
  correlation id estar disponível quando uma exceção for tratada — e que a
  edição concorrente de `Startup.cs` pelas duas tarefas paralelas não deixou
  nenhum dos dois `Configurar*` faltando (ambos presentes e chamados em
  `Configuration`).
- T-30/T-31/T-32/T-36 (lotes futuros que dependem deste): dependências
  apontam corretamente para T-18/T-24/T-19/T-22/T-25/T-26/T-28/T-29, todas
  `Concluída` — ver "Fechamento estrutural" abaixo para o detalhamento
  completo.

### Requisitos não funcionais

- Dinheiro sempre `decimal` — nenhum tipo de ponto flutuante binário
  introduzido neste lote (Api/Desktop deste lote não manipulam valor
  monetário diretamente, ainda; T-30+ que vão expor DTOs com `decimal`).
- Datas: `CriarConfiguracaoJson` força `DateTimeZoneHandling.Utc` — qualquer
  `DateTime` não-UTC é convertido antes de serializar (regra 4), confirmado
  por teste (`CriarConfiguracaoJson_ConverteDataNaoUtcParaUtcAntesDeSerializar`).
- Sem `async void` em nenhum artefato deste lote (`CorrelationIdHandler.
  SendAsync` é `async Task<HttpResponseMessage>`, assinatura exigida por
  `DelegatingHandler`) — regra 5 cumprida.
- Performance/UX: não aplicável a este lote (sem tela; host self-contido,
  sem carga real medida — fora do escopo de T-25…T-29).

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Zero reprovações (críticas ou simples) neste lote. A única nota de
atenção não trivial (`Startup.Container` estático) foi avaliada em detalhe e
não constitui achado — é uma limitação real e documentada do host OWIN
clássico, contida pela garantia arquitetural de processo único (ADR-004), sem
janela de corrida detectada. Não sugere problema de decomposição de tarefas
nem de diretrizes de implementação. Não escalo ao `coordenador` por este
motivo.

## Fechamento estrutural do Lote 6

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá para não duplicar) —
resumo: todas as 5 tarefas `Concluída`, sem dependência órfã da Seção 4
relativa ao Lote 6, sem tarefa `Bloqueada`, RL5-01 confirmada resolvida
(atualizada de "Resolvida" pelo Executor para "Resolvida — confirmada por
execução independente do Validador"), T-30/T-31/T-32 (Lote 7) e T-35/T-36
(Lote 8) confirmadas apontando para dependências `Concluída`, sem referência
quebrada. Zero achados novos — nenhuma tarefa criada em `Refatoração Lote-6`.
Lote fecha **Validado** (sem ressalvas) após a auditoria de segurança.

## Lote 7 — Endpoints (marco: API utilizável pelo Vendas)

**Veredito: Aprovado, sem ressalvas.** As 5 tarefas (T-30, T-31, T-32, T-33,
T-34) permanecem `Concluída`. Nenhuma reprovação crítica nem simples contra o
critério de aceite literal de qualquer tarefa. T-30 é revalidada aqui pela
primeira vez pelo Validador (não tinha seção própria ainda — foi mencionada
de passagem só como dependência de lotes futuros na validação do Lote 6).

Validado por leitura de código real (`VendasController.cs` completo — as 3
actions e as 2 rotas por action —, `QuitacaoRequestDto`/`QuitacaoResponseDto`
(T-30, pré-existentes, não tocados nesta worktree), `VendaStatusResponseDto`,
`CancelamentoRequestDto`/`CancelamentoResponseDto`, `ConsultaService.
ObterStatus`, `CancelamentoService.Cancelar`, `ResultadoQuitacao` — não pela
nota de implementação do Executor) contra o critério de aceite literal de
cada linha (`TASK.md` Seção 3, Lote 7) **e por execução independente da
suíte de testes, rodada por mim nesta validação**: `dotnet build
ERPFinanceiro.sln` (0 aviso, 0 erro, solução inteira); `bin\x64\Debug\net48`
copiado para `C:\temp_erp_validar_lote7` (fora do OneDrive, contorno de
rotina do Bloqueio 001, `BLOCKERS.md`), DLLs nativas do Firebird embarcado e
`App.config` reaproveitados da pasta `bin\Debug\net48` já presente na
worktree (deixada por T-31/T-32 — mesmo pré-requisito de ambiente que essas
tarefas já haviam documentado); `dotnet vstest ERPFinanceiro.Tests.dll`
rodado de lá — **122/122 Aprovado na 1ª tentativa**, sem reprodução do
Bloqueio 001 nesta rodada, suíte completa (não só o filtro do lote).

### T-30 — `POST /api/vendas/quitacao`

**Resultado: Aprovado.**

- `QuitacaoRequestDto`/`QuitacaoResponseDto` espelham `docs/contrato-v1.1.md`
  Seção 3.1 campo a campo (`vendaId`/`clienteId`/`valorTotal`/`itens[]` na
  entrada; `status`/`dataQuitacao` na saída) — confirmado por leitura direta.
  `ResultadoQuitacao.Status` é a string fixa `"Quitada"` (toda execução
  bem-sucedida de `QuitacaoService.Executar` termina Quitada) — confere com o
  contrato, sem tabela de conversão adicional necessária.
- Controller fino confirmado: `Quitacao(dto)` só mapeia DTO->comando, chama
  `_quitacaoService.Executar`, mapeia resultado->DTO; nenhuma regra de
  negócio/validação no controller (a validação de payload é decidida dentro
  de `QuitacaoService`/`ValidadorVendaCommand`, já auditada no Lote 5/T-17).
- Teste de integração real (`ApiHostVendasControllerTests`, pipeline HTTP
  completo — `ApiHost`+`Startup`+`CompositionRoot.Construir()` reais, Firebird
  embarcado real) cobre os 4 cenários do critério de aceite literalmente:
  venda inexistente -> 200 `{status:"Quitada",dataQuitacao}`; repetição ->
  200 com a mesma `dataQuitacao` (tolerância <1ms por arredondamento de
  `TIMESTAMP` do Firebird, justificada e documentada no próprio teste, não
  mascara falta de idempotência real — a idempotência de fato já é garantida
  no histórico único, auditada em T-18/Lote 5); corpo com `itens` vazio ->
  400 `PAYLOAD_INVALIDO`; venda pré-semeada Cancelada -> 409
  `VENDA_JA_CANCELADA`. Reexecutado por mim como parte da suíte completa
  (122/122) — verde.
- Sem autenticação nesta tarefa, corretamente (X-Api-Key é T-35, Lote 8, não
  é dependência de T-30) — confirmado que nenhuma checagem de `X-Api-Key` foi
  antecipada incorretamente no controller.

### T-31 — `GET /api/vendas/{vendaId}/status`

**Resultado: Aprovado.**

- `VendaStatusResponseDto` = `{vendaId,status}`, exatamente o formato da
  Seção 3.3 do contrato. `Status` convertido de `StatusVenda` (enum) para
  string via `Enum.ToString()` no controller — confirmado que os 3 valores do
  enum (`Pendente`/`Quitada`/`Cancelada`) já nascem PascalCase (Lote 2,
  auditado então), então essa conversão simples já produz o formato exigido
  pelo contrato (P-3) sem tabela de mapeamento adicional.
- `ConsultaService.ObterStatus` (T-24, Lote 5, não alterado nesta tarefa)
  lança `VendaNaoEncontradaException` para venda ausente — o controller não
  precisa (nem faz) tratamento de exceção adicional; `GlobalExceptionHandler`
  (T-28, Lote 6, não alterado) já mapeia para 404 `VENDA_NAO_ENCONTRADA`.
  Reaproveitamento correto, sem duplicar lógica de erro.
- Teste de integração real (`ApiHostVendasStatusControllerTests`) cobre os 2
  cenários do critério de aceite: venda Pendente pré-semeada -> 200
  `{vendaId,status:"Pendente"}`; venda inexistente -> 404
  `{erro:{codigo:"VENDA_NAO_ENCONTRADA",...}}`. Reexecutado por mim dentro da
  suíte completa (122/122) — verde.
- Nota de ambiente registrada pelo Executor (App.config/DLLs nativas do
  Firebird não presentes nesta worktree recém-criada, recriados localmente,
  fora do controle de versão) — confirmado que é puramente operacional: os
  artefatos recriados (`App.config`, `fbembed.dll`/`fbclient.dll`,
  `security3.fdb`) estão todos cobertos pelo `.gitignore` (verificado por
  `git status`/`git check-ignore`, ver "Achados de segurança" abaixo), nada
  vazou para o índice do git.

### T-32 — `POST /api/vendas/cancelamento`

**Resultado: Aprovado.**

- `CancelamentoRequestDto`/`CancelamentoResponseDto` espelham a Seção 3.2 do
  contrato (`{vendaId,motivo}` entrada, `{status}` saída) — confirmado.
- **Decisão de validar `vendaId` ausente/vazio no controller, não em
  `ValidadorVendaCommand`, avaliada e aceita como correta, não um achado**:
  confirmei por leitura de `ValidadorVendaCommand.cs` (T-17, Lote 4) que ele
  só tem overloads para `QuitarVendaCommand`/`RegistrarVendaCommand`, não
  para `CancelarVendaCommand` — estender o validador exigiria adicionar um
  novo caso a uma classe já auditada e fechada em lote anterior, por uma
  regra de 1 campo. A validação no controller produz exatamente o 400
  `PAYLOAD_INVALIDO` exigido pelo contrato antes de chamar o serviço.
  `CancelamentoService.Cancelar` (T-19, Lote 5) mantém uma segunda guarda via
  `ArgumentException` para a mesma pré-condição — confirmei em
  `ExceptionParaRespostaMapper.Mapear` que `ArgumentException` cai no `case
  default` (500 `ERRO_INTERNO`, envelope genérico, sem vazar mensagem) caso
  algum chamador futuro (fora do controller) invoque o serviço sem validar
  antes — defesa em profundidade aceitável, sem duplicação de regra de
  negócio (é só uma guarda de pré-condição de programação, não uma regra do
  domínio). Nenhum caminho do contrato hoje alcança essa guarda sem passar
  pela validação do controller primeiro (confirmado pelo teste do cenário 5).
- Teste de integração real (`ApiHostVendasCancelamentoControllerTests`) cobre
  6 sub-cenários (mais que os 4 citados na linha da tarefa, cobertura maior):
  Pendente cancela 200; desconhecida cria+cancela 200 (D-08); Quitada sem
  motivo 409 `MOTIVO_OBRIGATORIO` sem alterar dados; mesma venda com motivo
  200; repetição sobre já Cancelada 200 (idempotência, P-2); payload sem
  `vendaId` 400 `PAYLOAD_INVALIDO`. Reexecutado por mim dentro da suíte
  completa (122/122) — verde, incluindo os asserts de que o cenário 409 não
  altera nenhum dado (implícito no fato de o cenário seguinte, com motivo,
  ainda encontrar a venda Quitada, não já parcialmente cancelada).

### T-33 — Alias `/api/v1/vendas/...`

**Resultado: Aprovado.**

- Confirmei que não há "3 controllers" (a redação original da linha da
  tarefa antecipava um design que não se concretizou) — há 1
  `VendasController` com 3 actions, e o alias foi aplicado nas 3
  (`Quitacao`/`Status`/`Cancelamento`), cada uma com um segundo
  `[Route("~/api/v1/vendas/...")]` absoluto — confirmado linha a linha no
  controller, as 3 actions têm o segundo atributo.
- Mecanismo (`RouteAttribute` com `AllowMultiple = true`, prefixo `~/`
  ignorando o `RoutePrefix` do controller) é o padrão correto e documentado
  do Web API 2 attribute routing para múltiplas rotas na mesma action — não
  há duplicação de controller/DTO/lógica, e a suíte completa rodando sem
  `AmbiguousMatchException`/erro de roteamento na inicialização do host
  confirma que as duas rotas coexistem sem conflito.
- Critério de "desativar sem apagar controllers" (se D-05 for rejeitada):
  confirmado que basta remover a linha `[Route("~/api/v1/vendas/...")]` de
  cada action — não há acoplamento entre o alias e a rota base em nenhum
  outro lugar do código (roteamento é só atributo, sem lógica condicional
  dependente de qual rota foi usada).
- Teste de integração real (`ApiHostVendasAliasV1ControllerTests`) confirma
  que as duas bases respondem HTTP status e corpo JSON idênticos
  (`JToken.DeepEquals`) para a mesma venda, usando `GET status` como
  representante — escopo documentado e aceitável (os caminhos felizes de
  cada endpoint na rota base já estão cobertos por T-30/T-31/T-32; o que
  este teste prova é especificamente que o alias roteia para o mesmo método,
  o que generaliza para as outras 2 actions pelo mesmo mecanismo de
  atributo, não por lógica específica de cada action). Reexecutado por mim
  dentro da suíte completa (122/122) — verde.

### T-34 — Coleção de fumaça `docs/postman/smoke-vendas.sh`

**Resultado: Aprovado.**

- Formato curl (em vez de `.postman_collection.json`) justificado em
  `docs/postman/README.md` por limitação real do sandbox (sem GUI/Postman
  interativo) — avaliado como decisão aceitável, não um desvio que compromete
  o critério de aceite: o script é executável e verificável de fato, o que a
  linha da tarefa pede ("coleção roda 100% verde contra o app local").
- Cobertura conferida contra `docs/contrato-v1.1.md` linha a linha: 7
  cenários (quitação 200/400/409; cancelamento 200/409; status 200/404) —
  os 2-3 cenários por endpoint pedidos pelo critério de aceite, cobrindo os
  3 endpoints implementados neste lote.
- Script auto-contido: não depende de estado pré-semeado, encadeia os
  próprios endpoints do contrato para produzir os cenários 409 (cancelar
  desconhecida cria+cancela D-08, depois quitar essa venda dá 409; quitar
  nova depois cancelar sem motivo dá 409) — `vendaId` com sufixo timestamp,
  repetível sem colisão. Confirmado por leitura que os `grep`s de asserção
  (`verificar()`) batem exatamente com os códigos/formato do envelope real
  (`"status":"Quitada"`, `"codigo":"PAYLOAD_INVALIDO"`, etc.), não com uma
  suposição de formato.
- **Execução real confirmada, não só documentação estática**: evidência em
  `docs/postman/evidencia-execucao-T-34.txt` (harness descartável em
  `tools/T-34-smoke-harness/`, API real via `ApiHost`+`Startup`+
  `CompositionRoot.Construir()`, Firebird embarcado real) — 7/7 cenários
  verdes em 2 rodadas seguidas, conforme relatado pelo Executor. Não
  reexecutei o harness eu mesmo nesta validação (não é parte da suíte
  `dotnet vstest`), mas conferi que a evidência bruta bate com o script e
  que o harness não deixou artefato/`.fdb`/processo residual no repositório
  (`tools/T-34-smoke-harness/` só tem `App.config.example`, `Program.cs`,
  `.csproj` versionados — `App.config` real gitignorado, confirmado).
- Sem cenário 401, corretamente deferido a T-37 (depende de T-35, Lote 8,
  ainda não implementada) — nota explícita no script e no README, não é uma
  lacuna desta tarefa.

### Testes de integração cruzada (Lote 7)

- T-30/T-31/T-32 compartilham o mesmo `VendasController`, construído com 4
  dependências (`QuitacaoService`, `ConsultaService`, `CancelamentoService`,
  `ICorrelationContext`) — confirmei que o registro do Autofac
  (`CompositionRoot`, Lote 6) resolve as 4 sem exceção (implícito no sucesso
  de todo teste de integração HTTP deste lote, que depende da resolução do
  controller completo) e que nenhuma das 3 actions quebrou as outras 2 ao
  ganhar novas dependências no construtor (T-31 e T-32 ampliaram o construtor
  sem remover os parâmetros de T-30, confirmado por leitura).
- T-33 (alias) depende estruturalmente de T-30/T-31/T-32 já existirem — a
  ordem de implementação (T-33 depois das 3) e a suíte completa confirmam que
  o alias não regrediu nenhum dos 3 endpoints na rota base (todos os testes
  de T-30/T-31/T-32 continuam verdes junto com os de T-33).
- T-34 depende de T-30/T-31/T-32 e exercita os 3 endpoints via HTTP real
  (harness próprio, fora do `dotnet vstest`) — evidência cruzada
  independente da suíte xUnit, reforça a confiança de que os 3 endpoints
  funcionam de ponta a ponta contra Firebird real, não só em teste
  automatizado.
- Envelope de erro (`GlobalExceptionHandler`/`ExceptionParaRespostaMapper`,
  T-28, Lote 6) reaproveitado sem alteração por todas as novas actions —
  confirmado por `git diff` (nenhum arquivo em `Api/Erros/` tocado nesta
  worktree) e pelos testes de 400/404/409 de cada endpoint, todos com o
  formato `{erro:{codigo,mensagem}}` correto.

### Requisitos não funcionais

- Dinheiro sempre `decimal`: `QuitacaoRequestDto.ValorTotal`/
  `ItemQuitacaoDto.PrecoUnitario` são `decimal` (T-30, não tocado nesta
  worktree, já auditado); T-31/T-32/T-33/T-34 não introduzem nenhum valor
  monetário novo em ponto flutuante binário.
- Datas UTC: `QuitacaoResponseDto.DataQuitacao` é `DateTime` serializado via
  `CriarConfiguracaoJson` (T-25, Lote 6) com `DateTimeZoneHandling.Utc` —
  confirmado que a data retornada no teste de integração termina em "Z"
  implicitamente pelo parse do `JObject`/round-trip UTC, consistente com o
  padrão já auditado em StartupTests (Lote 6).
- Sem `async void`: todas as actions são síncronas (`IHttpActionResult`,
  sem `async`/`await` no controller — a chamada ao serviço é síncrona,
  `IUnitOfWork.SalvarAlteracoes()` é síncrono por decisão já auditada em
  T-13/Lote 4); os testes de integração usam `async Task` (assinatura
  correta para teste xUnit com `HttpClient`), sem violar a regra 5 (que é
  sobre código de produção).
- Performance/UX: fora do escopo deste lote (sem tela; não há medição de
  carga pedida pelo critério de aceite de nenhuma das 5 tarefas).

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Zero reprovações (críticas ou simples) neste lote. A decisão de
validar `vendaId` no controller para `CancelamentoService` (T-32) foi
avaliada em detalhe e não constitui achado — é uma consequência pontual e
bem justificada de `ValidadorVendaCommand` (T-17) ter sido desenhado só para
os 2 comandos que existiam quando foi escrito, não um sinal de problema
sistêmico de decomposição. Não escalo ao `coordenador` por este motivo.

## Fechamento estrutural do Lote 7

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá para não duplicar) —
resumo: todas as 5 tarefas `Concluída`, sem dependência órfã da Seção 4
relativa ao Lote 7, sem tarefa `Bloqueada`, T-35/T-36/T-37/T-38 (Lote 8)
confirmadas apontando para dependências `Concluída` (T-30/T-31/T-32/T-34),
sem referência quebrada. Zero achados — nenhuma tarefa criada em
`Refatoração Lote-7`. Lote fecha **Validado** (sem ressalvas) após a
auditoria de segurança.

## Lote 8 — Segurança da API e integração real (Dia 4)

**Veredito: Aprovado, sem ressalvas.** As 6 tarefas (T-35, T-36, T-37, T-38,
T-39, T-40) permanecem `Concluída`. Nenhuma reprovação crítica nem simples
contra o critério de aceite literal de qualquer tarefa.

**Nota de escopo lida antes da auditoria:** T-38/T-39 foram redefinidas pelo
Coordenador (ADR-010, `BLOCKERS.md` Bloqueio 002, aprovação explícita do
usuário) de "integração real com o Vendas/Delphi" para "suíte de integração
**simulada**" — decisão estrutural já aprovada, não reaberta aqui. A
validação deste lote confirma que a simulação foi bem executada e que a
pendência de integração real está rotulada corretamente em todo artefato
relevante, não reinterpreta o critério de aceite original das tarefas (que
já veio redefinido pelo Coordenador antes do Executor rodar).

Validado por leitura de código real (`ApiKeyHandler.cs`, `HealthController.cs`,
`Startup.cs`, `docs/contrato-v1.1.md`, `docs/postman/smoke-vendas.sh`/
`README.md`, `tools/T-38-integracao-simulada/`, `tools/T-39-integracao-simulada/`,
`docs/integracao-simulada/README.md`/`PENDENCIA-INTEGRACAO-REAL.md` — não pela
nota de implementação do Executor) contra o critério de aceite literal de
cada linha (`TASK.md` Seção 3, Lote 8) **e por execução independente da
suíte de testes, rodada por mim nesta validação**: `dotnet build
ERPFinanceiro.sln` (0 aviso, 0 erro, solução inteira, `bin\x64\Debug\net48`);
copiado para `C:\temp_erp_validar_lote8\net48` (fora do OneDrive, contorno de
rotina do Bloqueio 001, `BLOCKERS.md`), DLLs nativas do Firebird embarcado e
`App.config`/`firebird.conf`/`security3.fdb` já presentes na pasta `bin`
desta worktree reaproveitados sem recriação (bitness x64 confirmada);
`dotnet vstest ERPFinanceiro.Tests.dll` rodado de lá **2 vezes seguidas —
124/124 Aprovado nas duas rodadas**, sem flakiness observada e sem
reprodução do Bloqueio 001 nesta validação.

### T-35 — `ApiKeyHandler`

**Resultado: Aprovado.**

- Comparação em tempo constante (`ComparacaoEmTempoConstante`) revisada linha
  a linha: percorre sempre `Math.Max(bytesEsperada.Length,
  bytesRecebida.Length)`, nunca retorna cedo por tamanho (a diferença de
  tamanho entra no acumulador via XOR antes do laço, e o laço sempre roda até
  o fim independente do resultado parcial) — implementação correta dado que
  `CryptographicOperations.FixedTimeEquals` não existe no net48 (confirmado:
  API introduzida só a partir de .NET Core 2.1/.NET Standard 2.1).
- 401 idêntico para header ausente e chave errada: confirmado por leitura de
  `ChaveValida`/`ConstruirRespostaNaoAutorizada` — o mesmo envelope fixo
  (`NAO_AUTORIZADO`, mesma mensagem) é devolvido nos dois ramos, sem
  distinção; reforçado pelo teste `ApiHostVendasApiKeyControllerTests`
  (`JToken.DeepEquals` entre os dois corpos), reexecutado por mim na suíte
  completa — verde.
- Isenção de `GET /api/health` por `AbsolutePath` normalizado
  (`TrimEnd('/')`, comparação `OrdinalIgnoreCase`) — cobre `/api/health` e
  `/api/health/`; não cobre `/api/v1/...` (não se aplica, health não tem
  alias v1) nem seria afetada por isso. Confirmado por teste
  (`ApiHostHealthControllerTests`) que a rota responde sem `X-Api-Key` mesmo
  com o handler já registrado.
- Fail-closed confirmado: `ObterChaveConfigurada()` vazio/nulo faz
  `ChaveValida` retornar `false` incondicionalmente (comentário explícito no
  código: "nunca abre a Api por omissão de configuração") — nenhuma
  requisição passa sem chave configurada, mesmo com header presente.
- Chave nunca logada: `RegistrarTentativaNaoAutorizada` só grava a mensagem
  fixa `"Requisição rejeitada: X-Api-Key ausente ou inválida."`, sem
  interpolar o header recebido nem a chave configurada — confirmado por
  leitura e pelo 4º caso do teste dedicado, que lê o arquivo de log real e
  confirma ausência da chave (correta e errada) em toda linha.
- Chave correta responde 200 normalmente — confirmado pelo mesmo teste.
- Efeito colateral necessário (todos os testes de integração HTTP anteriores
  passaram a enviar `X-Api-Key`) verificado como completo: `dotnet vstest`
  desta validação (124/124) confirma que nenhum teste de T-27/T-30…T-34
  ficou órfão/quebrado por essa mudança.

### T-36 — `GET /api/health`

**Resultado: Aprovado.**

- `HealthController` delega 100% a `IHealthService.ObterStatus()` (T-22, não
  alterado) — 200/`{status:"ok",banco:"ok"}` ou 503/
  `{status:"degradado",banco:"falha"}` via `Content<T>`, confirmado por
  leitura e pelos 3 cenários de `ApiHostHealthControllerTests` reexecutados
  na suíte completa.
- Sem autenticação de fato: confirmado por leitura completa do controller —
  nenhum acesso a `Request.Headers`/`ConfigurationManager` neste arquivo; a
  isenção é responsabilidade exclusiva do `ApiKeyHandler` (T-35), não
  duplicada aqui.

### T-37 — Validação dos exemplos do contrato v1.1

**Resultado: Aprovado.**

- Reexecutei a comparação campo a campo das 4 mensagens ajustadas contra o
  código real de origem (não confiei na nota do Executor):
  `VendaJaCanceladaException` → `"Venda '{vendaId}' já está cancelada;
  Cancelada é um estado terminal."` bate byte a byte com a Seção 3.1 do
  contrato (linha 155); `DadosDivergentesException` → `"O payload de quitação
  diverge dos dados registrados na venda Pendente '{vendaId}'."` bate com a
  linha 165; `VendaNaoEncontradaException` → `"Venda '{vendaId}' não
  encontrada."` bate com as linhas 204/238 (Seções 3.2/3.3);
  `MotivoObrigatorioException` → `"Motivo do cancelamento é obrigatório."`
  bate com a linha 214 — as 4 correções são reais, não apenas alegadas.
  `ExceptionParaRespostaMapper.cs` confirmado mapeando os 4 tipos aos códigos
  HTTP/códigos de erro corretos (409/409/404/409, conforme a tabela da Seção
  2 do contrato).
- `smoke-vendas.sh`/`README.md` (T-34) atualizados com `X-Api-Key` em todas
  as chamadas e 2 novos cenários 401 — confirmado por leitura do script (9
  cenários, variável `API_KEY` com default documentado).
- Observação sobre `VALOR_TOTAL_DIVERGENTE` (formatação de número dependente
  de culture) registrada no contrato sem alterar código — avaliação: correta
  como achado não bloqueante (mensagem de erro não é contrato de parsing
  formal do campo `codigo`, só o `codigo` é estável); não rebaixo para
  achado do Validador, já está documentada de forma acionável no próprio
  contrato.

### T-38/T-39 — Suíte de integração simulada

**Resultado: Aprovado.**

- Rótulo "SIMULAÇÃO — NÃO é integração real com o Vendas (Delphi)" confirmado
  presente e destacado em: `tools/T-38-integracao-simulada/Program.cs`
  (impresso em runtime, visível na evidência bruta),
  `docs/integracao-simulada/evidencia-execucao-T-38.txt` (linha 3 e repetido
  a cada bloco de execução), `docs/integracao-simulada/evidencia-execucao-T-39.txt`
  (linha 4, idem), `docs/integracao-simulada/README.md` (título da Seção,
  linhas 3 e 9). Em nenhum dos 4 documentos há frase que possa ser lida como
  "integração real concluída" — confirmado por busca textual dedicada,
  achado implícito do ADR-010 satisfeito.
- Critério de aceite técnico de T-38 (venda nova quitada 200; repetição
  idempotente sem novo histórico P-2; `GET status` reflete o resultado)
  confirmado pela evidência: Cenário 2 verificado por 2 caminhos
  complementares (resposta HTTP + contagem de `VendaHistorico` via consulta
  direta ao banco, 2 registros antes/depois) — verificação mais forte que só
  comparar o corpo HTTP, correta escolha do Executor. O achado sobre
  precisão de `TIMESTAMP` (comparação de data por string vs. semântica) é
  metodológico do próprio harness de evidência, não afeta o comportamento de
  produção — sem impacto no veredito.
- Critério de aceite técnico de T-39 (cancelamento Pendente/desconhecida/
  Quitada sem motivo, reenvio pós-timeout idempotente, 400/401/409)
  confirmado pela evidência — os 8 cenários (1+2+3+4+5a+5b+5c, com 5b/5c
  comparados por corpo idêntico) batem com D-06/D-08/P-2 já auditados nos
  Lotes 4/5 (T-09/T-10) e Lote 6 (T-35, 401 idêntico). Nenhuma divergência
  registrada por T-39 (confirmado — não havia nada para T-40 corrigir aqui).
- Harnesses fora da `.sln` (mesma disciplina de `spikes/T-01-firebird-spike`/
  `tools/T-34-smoke-harness`, já aceita em lotes anteriores) — não fazem
  parte do build de produção, confirmado por `git diff`/leitura dos
  `.csproj` da solução (nenhum novo `ProjectReference` a eles).
- Não reexecutei os harnesses `tools/T-38.../T-39...` eu mesmo nesta
  validação (não fazem parte de `dotnet vstest`, exigiriam subir Firebird
  embarcado dedicado fora da suíte) — mesma decisão já tomada na validação
  de T-34 (Lote 7): conferi que a evidência bruta bate exatamente com o
  código dos harnesses (`Program.cs` de cada um) e com o comportamento já
  confirmado pela suíte de integração real (`ApiHostVendas*ControllerTests`),
  que cobre os mesmos cenários de negócio por outro caminho (xUnit +
  `ApiHost` real). Sem contradição entre as duas fontes de evidência.

### T-40 — Registro da pendência de integração real

**Resultado: Aprovado.**

- Releitura confirmada: nem T-38 nem T-39 reportaram divergência de
  comportamento que exigisse correção — a nota "no-op confirmado por
  leitura" do Executor é precisa, não uma forma de pular trabalho.
- `docs/integracao-simulada/PENDENCIA-INTEGRACAO-REAL.md` lido por completo:
  objetivo, lista o que falta (aceite formal T-03, acesso a ambiente/
  homologação, responsáveis conhecidos/desconhecidos, próximos passos em
  ordem), e a seção final ("Quando este documento pode ser considerado
  resolvido") deixa explícito que qualquer "concluído" reportado sobre
  integração com o Vendas se refere só à suíte simulada — não fecha nem
  minimiza a pendência, cumpre o critério de aceite da tarefa.
- `BLOCKERS.md` Bloqueio 002 complementado sem reabertura (status mantido
  "Resolvido (redesenho de escopo)") — avaliado como correto: a atualização
  de T-40 é um registro adicional, não uma reversão da decisão do
  Coordenador; ver "Checagem do `BLOCKERS.md`" na seção de fechamento
  estrutural abaixo.
- Suíte completa reexecutada pelo Executor (124/124) e reconfirmada por mim
  nesta validação (124/124 em 2 rodadas) — marco do Dia 4 fechado nos termos
  do ADR-010, sem apresentar a integração real como concluída em nenhum
  lugar checado.

### Testes de integração cruzada (Lote 8)

- `ApiKeyHandler` (T-35) e `HealthController`/isenção (T-36) rodaram em
  paralelo sobre `Startup.cs` — confirmado por leitura que `Configuration`
  chama `ConfigurarApiKey` depois de `ConfigurarCorrelationId` e antes de
  `ConfigurarExceptionHandler`, sem conflito com o registro do controller de
  T-36 (que não precisa de nenhuma linha em `Startup.cs`, resolvido
  automaticamente pelo `RegisterApiControllers` de T-30).
- T-37 depende de T-34 (Lote 7) e T-35 (mesmo lote) — confirmado que a
  validação byte a byte só pôde ser feita depois de `ApiKeyHandler` estar
  ativo (os exemplos incluem os 2 cenários 401, que só existem por causa de
  T-35).
- T-38 depende de T-30/T-31/T-35/T-36 — confirmado que o harness usa
  `X-Api-Key` em toda chamada (T-35) e testa quitação/status (T-30/T-31)
  reais; T-39 depende de T-38/T-32 — confirmado que reaproveita o mesmo
  padrão de harness e testa cancelamento (T-32) real. As 6 rotas de venda
  (3 endpoints x 2 bases, T-33, Lote 7) continuam protegidas pelo
  `ApiKeyHandler` — reforço específico pedido pelo `SECURITY-REVIEW.md` do
  Lote 7, confirmado satisfeito nesta rodada (ver auditoria de segurança
  abaixo).

### Requisitos não funcionais

- Dinheiro sempre `decimal`: nenhum valor monetário novo introduzido por
  este lote (T-35…T-40 não tocam DTOs de valor).
- Datas UTC: sem mudança nova além do já auditado (T-25/T-30, Lotes 6/7).
- Sem `async void`: `ApiKeyHandler.SendAsync` é `async Task<...>` (assinatura
  correta de `DelegatingHandler`), sem violação.
- Erro ao cliente sem stack trace: 401 do `ApiKeyHandler` usa envelope fixo,
  nunca ecoa dado do request — confirmado.
- Performance/UX: fora do escopo (sem tela neste lote).

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Zero reprovações (críticas ou simples) neste lote. Nenhum padrão
recorrente de bug identificado — os únicos "achados" mencionados nas notas
do Executor (precisão de `TIMESTAMP`, culture de formatação de número,
encoding de console) são metodológicos/de evidência, não de decomposição de
tarefas nem de diretriz de implementação. Não escalo ao `coordenador`.

## Fechamento estrutural do Lote 8

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá para não duplicar) —
resumo: todas as 6 tarefas `Concluída`, sem dependência órfã da Seção 4
relativa ao Lote 8, sem tarefa `Bloqueada` (T-38 não está mais `Bloqueada`,
foi redecomposta e re-executada, confirmado), `BLOCKERS.md` Bloqueio 002
coerente (Resolvido, pendência residual clara, não reaberto). Zero achados —
nenhuma tarefa criada em `Refatoração Lote-8`. Lote fecha **Validado** (sem
ressalvas) após a auditoria de segurança.

## Lote 9 — Tela de consulta: grid e detalhe

**Veredito: Aprovado com ressalvas (achados simples, sem reprovação).** As 4
tarefas (T-41, T-42, T-43, T-44) permanecem `Concluída`. Nenhuma reprovação
crítica. A verificação **visual não foi feita por ninguém** (sandbox sem
display); é limitação de ambiente já aceita e **não** motivo de reprovação —
ver "Pendência de conferência visual manual" abaixo.

Validado por leitura do código real (`Formatadores`, `ConfiguracaoVisual`,
`ConsultaVendasModelo`, `FrmConsulta`, `DetalheVendaModelo`, `FrmDetalheVenda`)
contra o critério literal de cada linha do `TASK.md` e o `UX-SPEC.md` (2.1,
2.2, 3, 4), e por **execução independente**: `dotnet build ERPFinanceiro.sln`
(0 aviso, 0 erro), `bin\x64\Debug\net48` copiado para
`C:\temp_erp_validar_lote9` (fora do OneDrive) e `dotnet vstest` **4 vezes
seguidas: 164/164 em todas** (~1 min 3 s cada; 0 falha).

### T-41 — `Formatadores` + base visual: Aprovada (1 ressalva simples)
- Moeda N2 pt-BR (`1.250,00`), data UTC->local `dd/MM/yyyy HH:mm`, nullable ->
  "—", status = texto + ícone (sem cor): confirmado no código e nos 13 testes
  de `FormatadoresTests`. Skin `Office 2019 Colorful` e 3 SVGs 16 px em
  `ConfiguracaoVisual.cs`.
- **Ressalva simples RL9-01:** o critério exige "nenhuma cor hardcoded", mas os
  3 SVGs têm `stroke="#555555"` literal (`ConfiguracaoVisual.cs` linhas 33-36),
  e o comentário da classe afirma o contrário ("Nenhuma cor hardcoded").
  Baixo impacto (cinza neutro, skin clara), mas a afirmação é imprecisa e o
  cinza fixo pode perder contraste numa skin escura. Nenhuma outra cor
  hardcoded em `src/ERPFinanceiro.Desktop` (`SystemBrushes.GrayText` no vazio
  é cor de sistema, aceitável).

### T-42 — `FrmConsulta`: Aprovada
- Colunas do UX-SPEC 2.1 (Venda ID, Cliente ID, Valor total à direita, Status
  com ícone, Recebida em, Quitada em, Cancelada em oculta/column chooser);
  `Editable=false`/`ReadOnly=true`; `ShowAutoFilterRow=false`,
  `AllowFilter=false`; ordenação Recebida em desc; contador "1 venda"/"N
  vendas"; "—" nos nulos; "Cancelada*" + tooltip + legenda; F5 recarrega;
  dados via `ConsultaService.Listar` em processo, sem HTTP.
- `Task.Run` na carga; `async void` só no handler de UI; nenhum `.Result`/
  `.Wait()`/`GetAwaiter().GetResult()` em `src/ERPFinanceiro.Desktop` (grep).
  `CarregadorViaContainer` abre `BeginLifetimeScope()` novo por chamada e
  descarta (`using`): `DbContext` por operação, RT-08/regra 5.

### T-43 — Estados de F-1: Aprovada
- Textos idênticos ao UX-SPEC 4 (comparados literalmente): vazio, "Carregando
  vendas...", "Não foi possível consultar o banco." + "Tentar novamente",
  "Mostrando as 5.000 mais recentes; refine o filtro".
- Vazio via `CustomDrawEmptyForeground` (único componente custom permitido,
  Dir. 12); erro com mensagem fixa (`catch (Exception)` descarta a exceção:
  nunca stack/caminho de `.fdb`); grid mantém dados anteriores esmaecidos.
  `FrmConsultaEstadosTests` cobre banco vazio, carga bloqueada com `Task`
  incompleta (UI livre), erro + retry, 5.001 linhas -> 5.000 + aviso.

### T-44 — `FrmDetalheVenda`: Aprovada
- Cabeçalho + aba **única** "Itens" (aba Histórico ausente), preço N4/subtotal
  N2, "Total itens: X", texto "Sem itens registrados (venda cancelada sem
  quitação prévia)" exato do UX-SPEC 2.2; `Esc` = `CancelButton`; falha mostra
  aviso + "Tentar novamente" sem fechar; duplo clique/Enter -> `AbrirDetalhe()`
  com foco devolvido à linha. V-1001: 300,00 + 200,50 = 500,50 (teste real).
- **Dependência conhecida (não é defeito):** `FrmConsulta.DetalhePresenter` só
  é atribuída pelo composition root (`Program.cs`, T-46); até lá `AbrirDetalhe()`
  retorna sem abrir nada. Está na nota de T-44, mas **não** nas linhas de
  T-46/T-51 -> **RL9-02** (simples).

### Diretrizes 12-15 (Seção 1)
- Dir. 12: `FrmDetalheVenda` usa só `XtraForm`/`LabelControl`/`XtraTabControl`/
  `SimpleButton`/`ProgressPanel`. **`FrmConsulta` herda de `Form` e usa `Label`/
  `Panel` do WinForms** (contador, legenda, aviso, host). Não é controle
  custom, mas é inconsistente entre as telas e com UX-SPEC 3 -> **RL9-03**.
- Dir. 13: grid/detalhe usam `Formatadores`; só `PrecoUnitario.ToString("N4")`
  fica em `DetalheVendaModelo` (N4 não existe em `Formatadores`). Observação.
- Dir. 14/15: 4 estados em F-1 e F-3; em processo via `ConsultaService`, sem
  HTTP.

### Pendência de conferência visual manual (honestidade do registro)
Conferidos `TASK.md` (T-41 adendo, T-42, T-43, T-44), `BLOCKERS.md` Bloqueio
003 ("Resolvido (parcialmente)... verificação visual permanece etapa manual do
usuário") e comentários de código. **Nenhum afirma "verificado visualmente"**;
todos listam o que ficou sem ver (aparência/layout, alinhamento do valor,
ícones, cor da skin, tooltip, F5/Enter/Esc em form exibido, foco visual,
`ProgressPanel` animando, clique real, truncamento a 1024x600/DPI). Registro
honesto. Pendência do usuário: rodar a tela numa máquina com GUI antes de
T-51/T-52/T-53.

### Estabilidade da suíte / intermitência do T-20
`Harness_ConcorrenciaEIdempotencia_T20` e todos os `ApiHost*` passaram em
**4/4 execuções completas isoladas (164/164)**. **A falha intermitente do
T-20 não foi reproduzida**, logo a mensagem de erro não pôde ser capturada.
Com as ~9 execuções do orquestrador (1 falha), a frequência agregada é ~1 em
13 (~8%). Sem evidência de causa neste lote: os testes `FrmConsulta*`/
`FrmDetalhe*` rodam em thread STA própria com `Join` (60-120 s), têm `Dispose`
e foram estáveis nas 4 rodadas; a hipótese mais provável segue a classe
pré-existente (RL4-02, `AccessViolationException` do Firebird nativo).
**Observação, sem reprovação e sem tarefa nova**; se voltar a ocorrer, gravar
a saída do `vstest` em arquivo para ver a exceção real.

### Bugs / reprovações
Nenhuma reprovação crítica nem simples de critério de aceite. Achados simples
(tarefas em `Refatoração Lote-9`): RL9-01 (cor `#555555` nos SVGs), RL9-02
(fiação de `DetalhePresenter`/presenters no composition root não registrada em
T-46/T-51), RL9-03 (`FrmConsulta` usa `Form`/`Label`/`Panel` WinForms).

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Achados pontuais e de baixa severidade. Não escalo.

## Fechamento estrutural do Lote 9

Ver `SECURITY-REVIEW.md` (mesma checagem, registrada lá). Resumo: 4 tarefas
`Concluída`, dependências da Seção 4 sem referência quebrada, nenhuma tarefa
`Bloqueada`; `Refatoração Lote-9` criada (RL9-01…RL9-04, sendo RL9-04 de
documentação de licença, achado do chapéu DevSecOps). Lote 9 fecha **Validado
com ressalvas**, com a conferência visual manual do usuário ainda pendente.

## Lote 10 — Tela: status e ciclo de vida

**Veredito: Aprovado com ressalvas (achados simples, sem reprovação).** T-45,
T-46 e T-47 permanecem `Concluída`. Nenhuma reprovação crítica. Verificação
visual/ao vivo **não foi feita por ninguém** (sem display): limitação de
ambiente aceita, não motivo de reprovação; nenhum artefato afirma "verificado
visualmente" (as notas registram a pendência do usuário).

Validado por leitura do código real (`BarraStatusPresenter`,
`BarraStatusVisual`, `SequenciaInicializacao`, `InstanciaUnica`,
`TextosInicializacao`, `Program`, `ComposicaoJanelaPrincipal`,
`EncerramentoAplicacao`, `FrmConsulta`, `ConfiguracaoVisual`, `ApiHost`) e dos
testes `BarraStatusTests`, `EncerramentoTests` contra o critério literal do
`TASK.md` e o UX-SPEC (2.4, 2.7, 3, 4, 5), e por **execução independente**:
`dotnet build ERPFinanceiro.sln` (0 aviso, 0 erro); `bin\x64\Debug\net48`
copiado para `C:\temp_erp_validar_lote10`; `dotnet vstest` 7 vezes:
**188/188 em 6 execuções; 1 falha** (ver "Estabilidade da suíte").

### T-45 — Barra de status F-6: Aprovada
- Textos idênticos ao UX-SPEC 2.4: `API: Ativa (http://localhost:porta)`,
  `API: Inativa - porta em uso`, `API: Iniciando...`, `Banco: OK`,
  `Banco: indisponível`; ícone SVG + texto sempre (nunca só cor). Independência
  API/Banco testada.
- Health check: `await Task.Run(() => _health.ObterStatus())`; nenhum
  `.Result`/`.Wait()`; flag `_emAndamento` sob `lock` impede empilhar; exceção
  do health vira "Banco: indisponível" (observada). Tick do `Timer` WinForms
  chega na UI thread; timer injetável (`ITemporizador`) e teste com timer fake.
- Diálogo: porta, caminho `.fdb`, último erro, **Copiar** (via
  `IAreaTransferencia`), Esc fecha. Clique atualiza e abre.
- Ressalva (RL10-01): `BarStaticItem` não é focável por Tab, o diálogo só abre
  com mouse (UX-SPEC 5 "nenhuma ação exclusiva de mouse"). Vai para T-51.

### T-46 — Inicialização F-7: Aprovada
- Splash `Iniciando banco e API...`, aviso `Já existe uma instância em
  execução` e diálogo de API (causa + ação + Sim/Não) conforme UX-SPEC; falha
  de config antes da sequência mostra mensagem (`return 2`), nunca sai calado.
- Mutex `Global\ERPFinanceiro.Desktop.InstanciaUnica` (constante de produção;
  testes usam nomes próprios). Criado e liberado na thread `Main` (STA): sem
  `AbandonedMutexException` (o construtor `new Mutex(true,...)` não lança por
  abandono; processo morto libera o mutex pelo SO). Segunda instância retorna
  antes de banco/API (teste). `Dispose` idempotente; `finally` do `Program`
  repete `host.Stop()`/`Dispose` sem efeito (idempotentes).
- Banco em erro: janela abre e a carga cai no estado de erro F-1.
- Ressalva (RL10-03): mutex `Global\` criado por outro usuário/sessão pode dar
  `UnauthorizedAccessException` (mensagem "Não foi possível iniciar" em vez do
  aviso de segunda instância). Baixo.
- Nota de T-46 ainda diz "T-47 não implementada" (desatualizada; RL10-05).

### T-47 — Encerramento: Aprovada
- Texto literal `Fechar encerra a API; o ERP Vendas não conseguirá enviar
  vendas.`, `XtraMessageBox` Sim/Não, título "ERP Financeiro", ícone Warning.
- Ordem barra/timer -> `ApiHost.Stop` -> `container.Dispose` -> mutex;
  `Encerrar` idempotente sob `lock`; cada passo capturado (falha não pula os
  seguintes); testes: ordem, falha de passo, 2x. "Não" -> `e.Cancel`, API
  `Ativa` e timer não parado (teste com `ApiHost` e Firebird reais); "Sim"
  libera porta (novo `HttpListener` sobe) e `.fdb` (`FileShare.None`).
  Desligamento do Windows/Gerenciador de Tarefas não pergunta.
- **Foco padrão "Não"** (`MessageBoxDefaultButton.Button2` em YesNo = Não):
  aceitável (ação segura, evita derrubar a API por Enter), documentado no
  código e na nota de T-47; falta registrar no UX-SPEC (RL10-05, doc).
- Ressalva (RL10-02): o logger é descartado com o container, então falhas dos
  passos posteriores do `Encerrar` são perdidas.

### Pontos específicos pedidos
- **`ApiHost` "só como fonte de estado"** (`host ?? new ApiHost(raiz)`): o
  construtor só guarda o container; nenhum `Start`, listener, porta ou
  `DbContext` (RT-08 preservado). Na prática `host` sempre é atribuído em
  `iniciarApi` antes; o fallback é código morto inofensivo. **Sem achado.**
- **Sem cor hardcoded:** `grep '#[0-9A-Fa-f]{6}' src/ERPFinanceiro.Desktop`
  sem ocorrência. Sem `System.Windows.Forms.Label/Panel` em `FrmConsulta`
  (herda `XtraForm`, usa `LabelControl`/`PanelControl`).
- **`Program.Main` e exceções não tratadas:** não há
  `Application.ThreadException`/`AppDomain.UnhandledException`. Não é critério
  do TASK.md: **observação** (RL10-02a).
- **Fire-and-forget:** `var _ = AtualizarAsync()` no `Tick`/`Iniciar`: health
  observado internamente; exceção de um handler de `Alterado` (ex. UI já
  descartada após fechar) ficaria não observada. Observação (RL10-02c).

### Refatorações do Lote 9 (achados de origem do meu relatório do Lote 9)
- **RL9-02 — resolvida (`Concluída`):** `Program` cria presenters via
  `CarregadorViaContainer`, atribui `FrmConsulta.DetalhePresenter`, chama
  `AplicarSkin()` antes do splash e liga `Action<Exception> aoFalhar` ao
  `IAppLogger` (exceção completa só no log; a tela mantém mensagem fixa).
- **RL9-03 — resolvida (`Concluída`):** confirmado por leitura e grep.
- **RL9-04 — resolvida (`Concluída`):** ver `SECURITY-REVIEW.md`.
- **RL9-01 — permanece `Em andamento` (correto):** cores literais removidas
  (grep limpo). SVGs `class="Black"` sem `<style>` próprio: pela convenção de
  ícones SVG DevExpress a classe é resolvida pela paleta da skin ("Black"
  mapeia para a cor de texto/glifo); fallback é preenchimento preto, legível
  na skin clara escolhida (Office 2019 Colorful). Risco baixo; só a
  conferência visual do usuário fecha (não verifiquei a paleta em runtime).

### Estabilidade da suíte
7 execuções completas isoladas: 188/188 (6x). Uma falha (run 1):
`ApiHostVendasStatusControllerTests.GetStatus_PipelineHttpCompletoComContainerRealEFirebirdReal_CobreOsCenariosDoContratoV11`
-> `Assert.Equal() Failure: Expected: OK / Actual: NotFound`
(`ApiHostVendasStatusControllerTests.cs:139`, cenário 1: venda semeada
diretamente no `.fdb` não encontrada pela API). É teste do **Lote 8**, não usa
código do Lote 10 (sem STA/timer/mutex); `Harness_..._T20` não falhou.
Ressalva de honestidade: antes das execuções em primeiro plano disparei uma
execução em segundo plano que terminou sem saída útil; não posso descartar
sobreposição na run 1. Classificação: intermitência pré-existente, não
causada por este lote. Tarefa RL10-06 (capturar e investigar).

### Bugs / reprovações
Nenhuma reprovação crítica ou simples de critério de aceite. Achados simples
em `Refatoração Lote-10`: RL10-01 (teclado), RL10-02 (robustez ciclo de vida),
RL10-03 (mutex multiusuário), RL10-04 (ver segurança), RL10-05 (doc), RL10-06
(intermitência).

## Padrão recorrente de bug (escalonamento ao Coordenador)?

**Não.** Achados pontuais, baixa severidade. Não escalo.

## Fechamento estrutural do Lote 10

Ver `SECURITY-REVIEW.md`. Lote 10 fecha **Validado com ressalvas**, com a
conferência visual/ao vivo do usuário pendente.

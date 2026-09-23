# TASK.md — ERP Financeiro (C#)

> **APROVADO v0.1 (usuário, 22/09/2026).** Autor: Coordenador (chapéu Tech Lead). Base: `SDD.md`, `UX-SPEC.md`, ADR 001-008, `PRD.md`, `PRD-TECNICO.md`, `VISAO-PRODUTO.md` (O-xx/S-xx, cronograma), `CTO-REVIEW.md`.
> SDD.md e UX-SPEC.md **não foram alterados**. Nenhuma tarefa exige decisão estrutural nova (ver Seção 6).
> **Legenda de tier:** **[A]** obrigatório · **[B]** só se sobrar tempo (ordem de corte: de baixo para cima no Lote 14) · **[C]** fora do escopo (lista "estacionada" ao final da Seção 3).
> **Status:** `A fazer` / `Em andamento` / `Bloqueada` / `Concluída` (atualizado pelo Executor). **Dono** = chapéu do Executor: `DB` (banco/SQL), `BE` (backend C#), `FE` (Desktop/DevExpress/FastReport), `DOC/OPS` (docs, build, empacotamento), `QA` (teste manual/integração).
> **Premissas [A VALIDAR]** (D-03, D-04, D-05, D-06, D-08, D-10, D-11, contrato v1.1 P-1…P-9) estão explícitas na coluna "Premissa" e resumidas na Seção 4.3.

---

## 1. Diretrizes de Implementação

Regras práticas derivadas do SDD, ADRs, UX-SPEC e GUARDRAILS.md. Vale para toda tarefa.

**Arquitetura e código**
1. Camadas do SDD 2.1; dependência sempre para dentro. Domain sem NuGet. Application sem EF/Web API/DevExpress. Api não referencia Infrastructure. Desktop referencia Infrastructure **somente** no composition root (Autofac).
2. Controller fino: DTO -> comando -> serviço -> DTO de resposta. Toda regra (estado, motivo, validação, total) em Domain/Application. DTO da API nunca é entidade EF. Mapeamento manual (sem AutoMapper).
3. C# 7.3, .NET Framework 4.8. `PlatformTarget` fixo (x86 ou x64 conforme a DLL do Firebird decidida em T-01), nunca AnyCPU.
4. Dinheiro sempre `decimal` (nunca `double`/`float`), inclusive nos DTOs. Datas em UTC no banco/API (`IClock`, nunca `DateTime.Now` no Domain/Application); tela converte para hora local.
5. Sem `async void`; consultas da tela em `Task`, UI thread nunca bloqueada. `DbContext` **por operação/requisição** (escopo Autofac), nunca compartilhado entre API e UI (RT-08).
6. Consultas só via EF/parâmetros; nunca concatenar SQL. `ExecuteSqlCommand` só se T-01 mostrar que o provider falha em ponto específico (SDD 2.5) e sempre parametrizado.
7. Erro ao cliente sempre no envelope `{erro:{codigo,mensagem}}`, sem stack trace/mensagem de exceção. Detalhe só no `IAppLogger` com `correlationId`. `ApiKey` nunca em log nem versionada (só `App.config.example` no repositório).
8. Histórico (`FIN_VENDA_HISTORICO`) só por `INSERT`; repositório expõe apenas `Adicionar`. Venda + itens + histórico na **mesma transação**; `VERSAO` incrementada pela **aplicação**, sem trigger de versão.
9. Idempotência (P-2): repetição devolve o resultado original **sem novo histórico**. Conflito de concorrência: rollback, reler e reavaliar, máximo 3 tentativas, depois 409 `CONFLITO_CONCORRENCIA`.

**Banco (Firebird 3.0)**
10. DDL manual em `database/01-schema.sql` + `02-seed.sql`; sem migrations. Identificadores <= 31 chars (nomear constraints/índices explicitamente), DECIMAL <= 18, `TINYINT` vira `SMALLINT`, `DATETIME2` vira `TIMESTAMP` (UTC). `CLIENTE_ID` e `VALOR_TOTAL` **anuláveis** (ADR-007, D-08 assumida).
11. Um único processo abre o `.fdb` (o Desktop). Nada de segundo processo/ferramenta de teste abrindo o mesmo arquivo com o app aberto (ver T-20: usar o mesmo processo/harness com o app fechado).

**Tela (UX-SPEC)**
12. Só controles DevExpress padrão; único componente novo permitido: `CustomDrawEmptyForeground` (UX-SPEC 3). Qualquer outro componente novo exige marcação "NOVO" no UX-SPEC via Coordenador.
13. Formatação centralizada em `Formatadores` (moeda `N2`, data local `dd/MM/yyyy HH:mm`, status). Cultura pt-BR fixa. Status sempre **ícone + texto** (cor só reforça). Fonte >= 9 pt.
14. Toda tela de [A] entrega os **4 estados** (vazio, carregando, erro, sucesso) ou a justificativa do UX-SPEC 4. Tela é somente leitura (sem quitar/cancelar).
15. A tela **não chama a API por HTTP**: usa `ConsultaService`/`IHealthService` em processo (ADR-008). O relatório usa o **mesmo filtro** e **sem o limite de 5.000**.

**Testes e evidência**
16. Critério de aceite de cada tarefa é verificável por comando/roteiro descrito na própria linha; o Executor registra a evidência (saída/print) na coluna Notas. Bibliotecas de teste: xUnit + Moq (D-11 a validar; se rejeitada, trocar sem impacto em Domain/Application).
17. Pacotes NuGet apenas oficiais; registrar versão + licença de cada um (insumo do README, T-54).

**Bibliotecas obrigatórias/proibidas:** obrigatórias — EF 6.4, `FirebirdSql.Data.FirebirdClient`, `EntityFramework.Firebird`, Web API 2 + `Microsoft.Owin.Host.HttpListener`, Autofac(+WebApi2), Newtonsoft.Json, DevExpress WinForms (trial), FastReport (trial/Open Source). Proibidas — EF Core, AutoMapper, qualquer pacote pago, Serilog em [A] (S-10 é Tier C; usar `TraceLogger` atrás de `IAppLogger`).

**Ordem de corte e regra de precedência:** se o prazo apertar, cortar do fim (Lote 14 de baixo para cima). **Nunca cortar** Lotes 1-13 (Tier A). Dentro de O-09/O-10, o que cai primeiro é acabamento de layout, nunca persistência ou integração (VISAO 6.2). Nada de Tier B antes de T-40 (O-11) concluída.

---

## 2. Spikes Técnicos

| ID | Incerteza | Timebox | Go / No-go | Impacto se falhar |
|---|---|---|---|---|
| **T-01** | EF6 + Firebird 3.0 embarcado: gravar em `FIN_VENDA`, ler DECIMAL(18,2)/(18,4), identity/generator, `Take/Skip`, conflito de concorrência por `VERSAO` incrementada pela aplicação, bitness das DLLs, bloqueio de `.fdb` por segundo processo | ~2h (+1h máximo de extensão) | **Go:** gravação, leitura de DECIMAL e conflito de `VERSAO` funcionam. **No-go:** qualquer falha sem solução em +1h -> fallback SQL Server Express/LocalDB (ADR-002) | Reescrever T-05, T-11, T-12 (e connection string em T-26); Domain/Application/Api/Desktop/Reports intactos (ADR-002/008) |
| T-02 (parte) | FastReport: trial tem preview WinForms? Open Source gera só PDF/HTML? DevExpress: duração e marca d'água do trial | dentro das 1,5h de T-02 | Define ADR-006/008: com preview ou fallback PDF | T-49/T-50 (relatório) mudam de fluxo (já previsto em UX-SPEC 2.3) |

Tarefas sem spike próprio, mas com risco técnico alto marcado: T-27 (bind OWIN), T-38/T-39 (integração com Vendas real, sem acesso ao código).

---

## 3. Lista de Tarefas

Estimativa em horas-pessoa do dono. Nenhuma tarefa passa de 3,5h (< 1 dia-pessoa; limite ~8h). "Parale­lizável-com" lista tarefas do **mesmo lote** que podem rodar em paralelo (detalhe na Seção 4). Referências O-xx/S-xx apontam o item original da VISAO-PRODUTO.

### Lote 1 — Spike e preparação (Dia 1 manhã) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-01 | **Spike Firebird go/no-go** (O-01 parte 1). Console/projeto descartável: EF6+Firebird embarcado grava e lê `FIN_VENDA` (DECIMAL 18,2), atualização concorrente detectada por `VERSAO`, define bitness, testa segundo processo no `.fdb`. Registrar resultado como **ADR-009** (rascunho do Executor; Coordenador ratifica) e, se no-go, abrir entrada em `BLOCKERS.md` | DB/BE | A | 2h (+1h) | — | T-02, T-03 | ADR-002 | Roteiro executado com evidência: (1) INSERT+SELECT de decimal 1250.50 volta idêntico; (2) dois contextos atualizam a mesma linha, o segundo recebe conflito; (3) bitness registrada; (4) ADR-009 com decisão **GO** ou **NO-GO** explícita. **Bloqueante:** T-04, T-05, T-11, T-12 só iniciam após GO | A fazer |
| T-02 | Instalar trials DevExpress/FastReport e confirmar: duração, marca d'água, existência de preview WinForms no FastReport; registrar versões e tipo de licença em `docs/licencas.md` | DOC/OPS | A | 1,5h | — | T-01, T-03 | D-09 | Ambos abrem num projeto WinForms vazio; `docs/licencas.md` lista versão, licença, duração e resposta "tem preview? sim/não"; ADR-006/008 marcado com o caso (preview ou PDF) | A fazer |
| T-03 | Contrato v1.1 (O-01 parte 2): `docs/contrato-v1.1.md` com endpoints, envelope, códigos, exemplos JSON reais (200/400/401/404/409), campos aditivos, e lista dos pontos **A VALIDAR** (D-03, D-05, D-06, D-08, P-1…P-9); enviar ao lado Vendas e registrar data/resposta. **Não** preencher a linha v1.1 do VISAO 2.4 sem aceite | DOC/OPS | A | 1,5h | — | T-01, T-02 | contrato v1.1, D-03, D-05, D-06, D-08 | Documento existe com >= 1 exemplo JSON por código de erro; registro de envio ao Vendas (data) e de resposta ponto a ponto (aceita/rejeitada); pontos rejeitados viram entrada em `BLOCKERS.md` | A fazer |

### Lote 2 — Esqueleto e Domínio (Dia 1) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-04 | Esqueleto da solução (O-02): `ERPFinanceiro.sln`, 7 projetos do SDD 2.1, referências conforme tabela, NuGet, `PlatformTarget` fixo, `.gitignore`, `App.config.example` | BE | A | 2h | T-01 (GO) | T-05 (outro lote) | D-11 (xUnit) | `msbuild` compila a solução vazia sem warning de referência; nenhum projeto viola a tabela de dependências (Domain sem referências; Api sem referência a Infrastructure) | A fazer |
| T-07 | Domain: entidades `Venda`, `VendaItem`, `VendaHistorico`; enums `StatusVenda`, `Operacao`; exceções de domínio; `CriarPorQuitacao/CriarPendente` (sem transições) | BE | A | 1,5h | T-04 | — | ADR-007 (campos anuláveis) | Compila; `Venda` com `ClienteId`/`ValorTotal` anuláveis; enums espelham 0/1/2 do SDD 5; nenhuma dependência externa em Domain | A fazer |
| T-08 | Domain: máquina de estados `Quitar()`/`Cancelar()` (Pendente->Quitada, Pendente->Cancelada, Quitada->Cancelada; Cancelada terminal; repetição idempotente sinalizada) **sem** a regra de motivo | BE | A | 2h | T-07 | — | RN-03/RN-04 | Verificação por console/teste: cada transição válida muda estado e gera item de histórico pendente; Quitar em Cancelada lança `VendaJaCanceladaException`; repetição devolve "sem mudança" | A fazer |
| T-09 | Domain: regra S-05 — cancelar venda **Quitada** exige `motivo` não vazio, senão `MotivoObrigatorioException` | BE | A | 1h | T-08 | T-10 | D-06 | Quitada+cancelar sem motivo lança a exceção sem alterar estado; com motivo cancela e guarda motivo; Pendente sem motivo cancela normalmente | A fazer |
| T-10 | Domain: `Venda.CriarCancelada(vendaId, motivo)` para venda desconhecida (histórico Recebida->Cancelada; sem cliente/valor/itens) | BE | A | 1h | T-08 | T-09 | D-08, ADR-007 | Venda criada com status Cancelada, `ValorTotal`/`ClienteId` nulos, 1 histórico; se D-08 for rejeitada esta função fica sem uso (não apagar até decisão) | A fazer |

### Lote 3 — Banco (Dia 1) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-05 | `database/01-schema.sql`: 3 tabelas, PK identity/generator (conforme T-01), UNIQUE(`VENDA_ID`), índices nomeados (`IDX_FIN_VENDA_CLIENTE`, `_STATUS`, `_DATA_REC`), CHECKs de item, FKs (CASCADE nos itens), opcional triggers `BEFORE UPDATE/DELETE` no histórico. **Só SQL** | DB | A | 2h | T-01 (GO) | T-04, T-07 | D-08 (colunas NULL), ADR-005 | Script roda num `.fdb` novo sem erro; todo identificador <= 31 chars (conferido por consulta em `RDB$`); `VALOR_TOTAL` DECIMAL(18,2), `PRECO_UNITARIO` DECIMAL(18,4); sem trigger de `VERSAO` | A fazer |
| T-06 | `database/02-seed.sql`: massa conhecida (>= 3 vendas: Quitada, Pendente, Cancelada sem quitação, com itens e histórico coerentes) para demo e teste do relatório | DB | A | 1h | T-05 | T-11, T-12 | ADR-007 | Rodar seed após schema não gera erro; soma dos valores da massa documentada em comentário (insumo do CA-07.3) | A fazer |
| T-11 | EF6: `FinanceiroDbContext` + mapeamento Fluent das 3 entidades, `VERSAO` como `IsConcurrencyToken` incrementada pela aplicação, nomes de colunas iguais ao DDL | BE | A | 3h | T-05, T-07 | T-06, T-12 | ADR-005 | Contra `.fdb` criado por T-05: inserir venda com 2 itens + histórico e reler sem perda de decimal; atualização com `VERSAO` velha lança conflito | A fazer |
| T-12 | `DbInitializer` (Infrastructure): cria `.fdb` se ausente (SYSDBA/senha por configuração), aplica `01-schema.sql` embutido/copiado, sem passo manual (CA-05.5) | BE | A | 2h | T-04, T-05 | T-06, T-11 | ADR-002 | Apagar `.fdb`, rodar inicializador: banco criado com 3 tabelas; segunda execução é no-op; `.fdb` bloqueado gera exceção com mensagem clara (RT-05) | A fazer |

### Lote 4 — Persistência e base da Application (Dia 2) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-13 | Application: interfaces `IVendaRepository`, `IUnitOfWork`, `IClock`, `IAppLogger`, `ICorrelationContext`; comandos/resultados (`QuitarVendaCommand` etc.); `FiltroVendas` (tipo, ainda sem lógica) | BE | A | 1,5h | T-07 | — | — | Compila sem referência a EF/Web API; `IVendaRepository` expõe só `Adicionar` para histórico | A fazer |
| T-14 | `VendaRepository` EF6 (buscar por `VendaId` com itens, adicionar, consulta de leitura `AsNoTracking`) | BE | A | 2h | T-11, T-13 | T-17, T-21 | ADR-005 | Contra `.fdb` real: buscar existente/inexistente; listagem sem tracking; sem SQL concatenado | A fazer |
| T-15 | `UnitOfWork`/transação (venda+itens+histórico atômicos) e tradução de conflito (0 linhas/violação UNIQUE) para `ConcorrenciaException` | BE | A | 2h | T-14 | — | ADR-005 | Falha injetada no histórico faz rollback da venda; conflito de `VERSAO` e violação UNIQUE viram `ConcorrenciaException` | A fazer |
| T-16 | Helper Application `ExecutarComRetry` (releitura/reavaliação, máx. 3 tentativas, depois `CONFLITO_CONCORRENCIA`) | BE | A | 1h | T-15 | — | ADR-005 | Teste com repositório falso que conflita 2x e passa na 3ª; 4ª falha propaga | A fazer |
| T-17 | Application: validação de payload e total (CA-01.5 IDs vazios/`itens` vazio/`quantidade`<=0/`precoUnitario`<0; CA-01.6 tolerância 0,01; limites 50/500/1000 itens) | BE | A | 2h | T-13 | T-14, T-21 | P-7, RN-02 | Tabela de casos: cada violação -> `PAYLOAD_INVALIDO` ou `VALOR_TOTAL_DIVERGENTE`; diferença 0,01 aceita, 0,02 rejeitada | A fazer |
| T-21 | `TraceLogger` (Infrastructure) implementando `IAppLogger` (arquivo texto simples via `Trace`); S-10 Serilog **fica Tier C** | BE | A | 1h | T-13 | T-14, T-17 | — | Log grava linha com `correlationId`; teste confirma que `ApiKey` não é logada | A fazer |

### Lote 5 — Serviços de negócio (Dia 2 tarde) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-18 | `QuitacaoService` (O-05 + S-04): valida (T-17), busca; inexistente cria+quita (upsert RN-06), Pendente quita, Quitada devolve original sem histórico, Cancelada -> 409; usa UoW+retry; histórico com `correlationId` | BE | A | 3,5h | T-08, T-14, T-15, T-16, T-17, T-21 | T-19, T-22, T-23, T-24 | P-2, D-03 (ramo Pendente), P-7 | Roteiro contra `.fdb` real cobre CA-01.1, 01.2, 01.3, 01.5, 01.6 (CA-01.4 verificável no domínio; via API só com T-62); 1 linha de histórico por transição real | A fazer |
| T-19 | `CancelamentoService` (O-06): Pendente cancela, Quitada exige motivo (T-09), Cancelada idempotente sem histórico novo, desconhecida cria Cancelada (T-10) | BE | A | 3h | T-09, T-10, T-14, T-15, T-16 | T-18, T-22, T-23, T-24 | D-06, D-08, P-2 | Roteiro cobre CA-02.1…02.6; venda desconhecida grava 1 histórico e `ValorTotal` nulo | A fazer |
| T-22 | `IHealthService` (`SELECT 1`) retornando `ok/falha` + mensagem resumida | BE | A | 1h | T-11, T-13 | T-18, T-19, T-23, T-24 | P-5 (SDD 2.6) | Com `.fdb` acessível retorna ok; com caminho inválido retorna falha sem lançar | A fazer |
| T-23 | `ConsultaService.Listar(FiltroVendas)`: leitura sem tracking, ordenação `DataRecebimento` desc, limite 5.000 com flag de truncamento, DTO de listagem com nulos preservados (tela mostra "—"); filtros **ainda não aplicados** (T-57, Tier B) | BE | A | 2h | T-14, T-13 | T-18, T-19, T-22, T-24 | D-10 | Com seed (T-06): retorna todas em ordem correta; com 5.001 linhas de teste retorna 5.000 e flag `Truncado=true` | A fazer |
| T-24 | `ConsultaService.ObterDetalhe(vendaId)` (cabeçalho + itens; histórico carregado mas **fora da tela**, Tier C) e `ObterStatus(vendaId)` (insumo do GET status) | BE | A | 2h | T-14, T-13 | T-18, T-19, T-22, T-23 | RF-03 | Detalhe traz itens com subtotal; venda inexistente retorna nulo/exceção `VendaNaoEncontrada`; venda cancelada sem itens retorna lista vazia | A fazer |
| T-20 | Teste de integração de **concorrência e idempotência** (S-04) contra Firebird real: 2 quitações simultâneas mesma `vendaId` (Tasks), repetição sequencial, quitar/cancelar concorrentes. Harness xUnit/console, app fechado | QA/BE | A | 2,5h | T-18, T-19 | — | ADR-005 | 20 execuções: sempre 1 venda, 1 histórico de quitação, nenhuma exceção não tratada; ambas respostas sucesso idempotente ou 409; evidência salva | A fazer |

### Lote 6 — API base e hospedagem (Dia 3) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-25 | Api: `Startup` OWIN + `HttpConfiguration` (attribute routing, JSON: UTF-8, camelCase, decimal como número, datas ISO 8601 UTC), `ICorrelationContext` da Api, integração `Autofac.WebApi2` | BE | A | 2,5h | T-04, T-13 | — | P-6, D-04 | Endpoint de eco temporário responde JSON com decimal `1250.5` e data terminando em `Z`; removido antes de fechar | A fazer |
| T-26 | Desktop: **composition root Autofac** (registro de todas as interfaces, escopo por requisição/operação) + `App.config` (porta, connection string, `ApiKey`, credenciais do banco) | BE | A | 2h | T-14, T-15, T-21, T-25 | T-28, T-29 | D-04 | App sobe e resolve `QuitacaoService` sem exceção; `App.config.example` no repo, `App.config` real ignorado no git | A fazer |
| T-27 | Host OWIN no Desktop (O-08): `ApiHost.Start/Stop`, bind `http://localhost:{porta}/`, falha de bind captura e expõe estado (`Iniciando/Ativa/Inativa - porta em uso`) e último erro | BE | A | 2h | T-25, T-26 | — | D-04 (ADR-004) | Subir em porta livre responde 200 num GET de teste; porta ocupada não derruba o app e reporta `Inativa - porta em uso`; `Stop` libera a porta | A fazer |
| T-28 | Api: `ExceptionHandler` global + envelope `{erro:{codigo,mensagem}}`, tabela exceção -> HTTP/código do SDD 2.4 (400/404/409/500), 500 sem stack trace | BE | A | 2h | T-25 | T-26, T-29 | P-1 | Exceção de cada tipo mapeada ao código correto; corpo 500 não contém texto da exceção; detalhe vai ao log | A fazer |
| T-29 | Api: `CorrelationIdHandler` (gera/propaga id por requisição, header `X-Correlation-Id` de resposta) alimenta `ICorrelationContext` | BE | A | 1h | T-25 | T-26, T-28 | — | Toda resposta traz `X-Correlation-Id`; o mesmo id aparece no histórico da venda gravada | A fazer |

### Lote 7 — Endpoints (Dia 3, **marco: API utilizável pelo Vendas**) [A]

> Antecipar T-30 e T-31 para o meio do Dia 3 (quitação e status primeiro).

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-30 | `POST /api/vendas/quitacao`: DTOs + controller fino | BE | A | 2h | T-18, T-25, T-26, T-28, T-29 | T-31, T-32 | contrato v1.1, P-2 | curl: 200 `{status:"Quitada",dataQuitacao}`; repetição 200 com mesma data; corpo inválido 400; venda cancelada 409 `VENDA_JA_CANCELADA` | A fazer |
| T-31 | `GET /api/vendas/{vendaId}/status` | BE | A | 1h | T-24, T-25, T-26, T-28 | T-30, T-32 | RF-03 | 200 `{vendaId,status}` com string PascalCase; inexistente 404 `VENDA_NAO_ENCONTRADA` | A fazer |
| T-32 | `POST /api/vendas/cancelamento`: DTOs + controller fino | BE | A | 2h | T-19, T-25, T-26, T-28, T-29 | T-30, T-31 | D-06, D-08, contrato v1.1 | curl: Pendente/desconhecida 200; Quitada sem motivo 409 `MOTIVO_OBRIGATORIO`; repetição 200 | A fazer |
| T-33 | Alias `/api/v1/vendas/...` por dupla atribuição de rota nos 3 controllers (S-02) | BE | A | 1h | T-30, T-31, T-32 | T-34 | D-05 | Os 3 endpoints respondem idênticos nas duas rotas (mesma coleção, 2 bases); se D-05 rejeitada, desativar sem apagar controllers | A fazer |
| T-34 | Coleção Postman/curl de fumaça em `docs/` (marco do Dia 3): caminho feliz + 400/404/409 por endpoint | QA | A | 1,5h | T-30, T-31, T-32 | T-33 | contrato v1.1 | Coleção roda 100% verde contra o app local; salva em `docs/postman/`; **critério do marco: disponível ao Vendas até o fim do Dia 3** | A fazer |

### Lote 8 — Segurança da API e integração real (Dia 4) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-35 | `ApiKeyHandler` (S-08): `X-Api-Key`, comparação em tempo constante, isenta `GET /api/health`; ausente/inválida -> 401 `NAO_AUTORIZADO` sem revelar qual | BE | A | 1,5h | T-25, T-28, T-26 | T-36 | P-4/P-5 | Sem header e com chave errada retornam 401 idêntico; com chave correta 200; chave não aparece em log | A fazer |
| T-36 | `GET /api/health` (S-09): 200 `{status:"ok",banco:"ok"}`; falha de banco 503 `{status:"degradado",banco:"falha"}`; sem autenticação | BE | A | 1h | T-22, T-25, T-26 | T-35 | P-5 | Com banco ok 200; com `.fdb` inacessível 503; sem `X-Api-Key` responde | A fazer |
| T-37 | Validar exemplos JSON do contrato v1.1 contra a API (amostras de T-03 executadas, incluindo 401): ajustar `docs/contrato-v1.1.md` se houver divergência | QA | A | 1,5h | T-34, T-35 | T-38 | contrato v1.1, P-6 | Todos os exemplos de `docs/contrato-v1.1.md` reproduzidos byte a byte na resposta (exceto valores dinâmicos) | A fazer |
| T-38 | **O-11a** Integração real com o Vendas (Delphi): quitação e consulta de status disparadas pelo Vendas real; registrar request/response | QA | A | 2,5h | T-30, T-31, T-35, T-36, T-03 (aceite do Vendas) | T-37 | contrato v1.1 aceito, D-04 | Venda quitada no Vendas aparece na `FIN_VENDA` e no `GET status`; evidência (log/print). **Se contrato v1.1 não foi aceito**: usar v1.0 e registrar bloqueio em `BLOCKERS.md` | A fazer |
| T-39 | **O-11b** Integração real: cancelamento, reenvio após timeout (idempotência), erro 400/401/409 tratados pelo Vendas | QA | A | 2h | T-38, T-32 | — | D-06, D-08, P-2 | Cada cenário executado no Vendas real com resultado registrado; divergências viram itens de T-40 | A fazer |
| T-40 | **O-11c** Correções da integração (reserva de tempo; só divergências de T-38/T-39). Marco: **fim do Dia 4** | BE | A | 2h | T-39 | — | contrato v1.1 | Todas as divergências abertas em T-38/T-39 fechadas ou registradas em `BLOCKERS.md` com motivo; re-teste dos cenários afetados verde | A fazer |

### Lote 9 — Tela de consulta: grid e detalhe (Dia 4, paralelo ao Lote 8) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-41 | Base visual: `Formatadores` (moeda, data local, status+ícone), skin única em `DefaultLookAndFeel`, ícones SVG 16 px (check/relógio/x), cultura pt-BR | FE | A | 1,5h | T-04 | T-42 (após contrato de `Formatadores`) | UX-SPEC 3 | Testes de `Formatadores` (moeda `1.250,00`, data UTC->local, status com texto+ícone); nenhuma cor hardcoded | A fazer |
| T-42 | `FrmConsulta` (F-1): `GridControl` somente leitura, colunas do UX-SPEC 2.1, ordenação padrão, contador "N vendas", "—" e "Cancelada*" com tooltip/legenda, filtro automático DX desligado, dados via `ConsultaService.Listar` | FE | A | 3h | T-23, T-26, T-41 | — | D-10, ADR-007, ADR-008 | Com seed: lista as 3 vendas, formatos corretos, "—" nos nulos; F5 recarrega; grid sem edição | A fazer |
| T-43 | Estados de F-1: vazio (texto central via `CustomDrawEmptyForeground`), carregando (`ProgressPanel`, UI não congela), erro de banco (+ "Tentar novamente"), aviso "Mostrando as 5.000 mais recentes" | FE | A | 2h | T-42 | T-44 | ADR-008 | Os 4 estados reproduzidos: banco vazio, consulta lenta simulada, `.fdb` indisponível, 5.001 linhas; textos iguais aos do UX-SPEC 4 | A fazer |
| T-44 | `FrmDetalheVenda` (F-3, modal): cabeçalho + aba Itens (subtotal, total, texto p/ venda sem itens), `Esc` fecha, foco volta à linha; aba Histórico **não existe** (Tier C); 4 estados | FE | A | 2,5h | T-24, T-42 | T-43 | ADR-007 | Duplo clique/Enter abre; soma dos subtotais = valor da venda (seed); cancelada sem quitação mostra o texto explicativo; falha de itens mostra "Tentar novamente" sem fechar | A fazer |

### Lote 10 — Tela: status e ciclo de vida (Dia 4) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-45 | Barra de status (F-6): indicadores independentes API/Banco (ícone+texto), timer 15 s via `IHealthService` em processo, clique abre diálogo (porta, caminho `.fdb`, último erro, **Copiar**) | FE | A | 2,5h | T-22, T-27, T-42 | T-46 | D-04, ADR-004 | Derrubar porta/banco muda o indicador em <= 15 s; texto sempre presente (nunca só cor); "Copiar" coloca o erro na área de transferência | A fazer |
| T-46 | Inicialização (F-7): splash "Iniciando banco e API...", `DbInitializer` + host, mutex de instância única, falha de banco abre `FrmConsulta` em estado de erro (nunca fecha silencioso), falha de porta mostra diálogo com instruções | FE | A | 2h | T-12, T-27, T-42 | T-45 | D-04 | Segunda instância mostra aviso e sai; `.fdb` bloqueado -> tela em erro; porta ocupada -> diálogo com causa e ação | A fazer |
| T-47 | Encerramento: confirmação ao fechar ("Fechar encerra a API; o ERP Vendas não conseguirá enviar vendas.") e `ApiHost.Stop` ordenado | FE | A | 1h | T-46 | — | D-04 | Fechar pede confirmação; "Não" mantém app e API; "Sim" libera a porta e o `.fdb` (processo termina) | A fazer |

### Lote 11 — Relatório (Dia 5) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-48 | `RelatorioDataSource` (Reports): mesmo `ConsultaService` e mesmo `FiltroVendas`, **sem limite de 5.000**, "Total listado" calculado da mesma lista, nulos como 0 + contagem para nota de rodapé | BE | A | 2h | T-23 | T-49 (após DTO `LinhaRelatorio`) | D-10, ADR-008 | Com seed: Total listado = soma documentada em T-06 (CA-07.3); lista vazia -> total 0,00 | A fazer |
| T-49 | Layout FastReport `.frx` de listagem: cabeçalho (título, filtro aplicado, data de emissão), colunas, "Total listado", nota de nulos, mensagem "Nenhuma venda no período" | FE | A | 3h | T-02, T-48 | — | ADR-006 | Renderiza com seed e com lista vazia; sem cor hardcoded; PDF gerado abre | A fazer |
| T-50 | Fluxo "Emitir relatório" (F-5): botão, `WaitForm` cancelável, preview (se T-02 = tem preview) ou PDF em pasta temporária + abrir/ mostrar caminho, erros em `XtraMessageBox`, 4 estados | FE | A | 2,5h | T-42, T-49 | — | ADR-008 | Cenário com preview e cenário sem preview (forçado por configuração) funcionam; falha de arquivo mostra causa + caminho; relatório usa o filtro corrente | A fazer |

### Lote 12 — Acessibilidade e verificação (Dia 5) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-51 | Acessibilidade da tela (UX-SPEC 5): `TabIndex` explícito, `AccessibleName/Description`, `AccessKey`, atalhos F5/Enter/Esc, `AutoScaleMode=Dpi` + `SetDPIAware`, tamanho mínimo 1024x600/640x420 | FE | A | 2h | T-42, T-44, T-45, T-46, T-47, T-50 | — | UX-SPEC 5/6 | Navegação completa só por teclado; DPI 125/150% sem corte; contraste da skin conferido (>= 4,5:1) ou variante de alto contraste aplicada | A fazer |
| T-52 | Verificação manual com Narrador: checklist de 8 itens do UX-SPEC 5, resultado registrado em `docs/acessibilidade.md` | QA | A | 1h | T-51 | — | UX-SPEC 5 | 8 itens marcados ok/nok com evidência; nok crítico vira correção antes do Dia 6 | A fazer |

### Lote 13 — Entrega O-12 (Dia 5 preparação; 26-27/09 conclusão; **sem código novo**) [A]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-53 | Build Release + empacotamento: pasta com executável, DLLs Firebird (bitness de T-01), `database/*.sql`, `.fdb` de demonstração (backup do banco), `App.config.example` | DOC/OPS | A | 2h | T-40, T-50, T-52 | T-54, T-55 | RNF-14 | Pasta de entrega roda a partir de outro diretório; contém `.sql` **e** `.fdb`; sem `ApiKey` real | A fazer |
| T-54 | README: pré-requisitos, instalação dos trials (T-02), configuração (porta, connection string, ApiKey), execução, integração com o Vendas, limitações/dívidas aceitas (SDD 6), versões e licenças | DOC/OPS | A | 2h | T-02, T-03 | T-53, T-55 | D-09 | Um leitor sem contexto executa o passo a passo sem perguntas; lista de versões/licenças completa | A fazer |
| T-55 | Evidências: prints da tela (lista, detalhe, erro, indicador) e PDF de exemplo do relatório em `docs/evidencias/` | DOC/OPS | A | 1h | T-50 | T-53, T-54 | CA-10.2 | Arquivos existem e mostram dados do seed; PDF abre | A fazer |
| T-56 | Teste em **máquina limpa** seguindo apenas o README; registrar resultado; só correção de bug bloqueante (sem funcionalidade nova) | QA | A | 2h | T-53, T-54, T-55 | — | CA-10.3 | API, tela e banco sobem na máquina limpa; e cada desvio do README corrigido no README; relatório emitido | A fazer |

### Lote 14 — Tier B (só se sobrar tempo; executa no Dia 5 antes do Lote 13 fechar; **cortar de baixo para cima**) [B]

| ID | Tarefa | Dono | Tier | Est. | Dep. | Paralelizável-com | Premissa | Critério de aceite | Status |
|---|---|---|---|---|---|---|---|---|---|
| T-57 | S-06 regra: aplicar `FiltroVendas` (período, cliente exato/contém, status, combinados) em `ConsultaService`/repositório, com validação período inicial > final | BE | B | 1,5h | T-23 | T-59, T-61, T-63 | D-10 | Com seed: cada filtro isolado e combinado retorna o conjunto esperado; período invertido -> erro de validação | A fazer |
| T-58 | S-06 tela: painel de filtros de F-2 (DateEdit x2, Cliente, Status, Aplicar/Limpar, resumo na barra, estados vazio/erro) | FE | B | 2h | T-57, T-42 | T-60, T-62 | D-10 | Enter aplica; "Nenhuma venda para os filtros informados" + Limpar; período inválido mostra mensagem inline sem consultar; relatório passa a usar o filtro (T-48) | A fazer |
| T-59 | S-07 regra: totais quitado/cancelado (e pendente condicional) no `RelatorioDataSource`, nulos = 0 | BE | B | 1,5h | T-48 | T-57, T-61, T-63 | D-03 (linha pendente) | Com seed, soma por status confere; Total listado = soma dos totais por status (CA-07.3) | A fazer |
| T-60 | S-07 layout: bloco de totais no `.frx` (quitado/cancelado; linha pendente só se D-03 aceita e T-62 concluída) | FE | B | 1h | T-59, T-49 | T-58, T-62 | D-03 | PDF mostra os totais e a contagem por status; linha pendente ausente quando D-03 rejeitada | A fazer |
| T-61 | RF-04 regra: `RegistroService` (registrar Pendente, idempotente com status atual) | BE | B (condicional D-03; **cortar primeiro**) | 1,5h | T-14, T-15, T-16, T-17 | T-57, T-59, T-63 | D-03, P-9 | CA-04.1/04.2 contra `.fdb`; se D-03 rejeitada, tarefa **cancelada** com justificativa (não implementar) | A fazer |
| T-62 | RF-04 endpoint: `POST /api/vendas` (+ alias v1), DTO reutilizando o de quitação | BE | B (condicional D-03) | 1,5h | T-61, T-30 | T-58, T-60 | D-03, D-05, P-9 | curl: 200 `{status:"Pendente"}`; repetição 200 com status atual; venda Pendente depois quitada via T-30 gera histórico de quitação | A fazer |
| T-63 | S-11 reduzido: testes xUnit da máquina de estados do Domain (todas as transições válidas/inválidas/idempotentes, regra de motivo, venda desconhecida) | BE | B | 1,5h | T-09, T-10 | T-57, T-59, T-61 | D-11, RNF-08 | Suíte verde; matriz 3 estados x 2 operações coberta (>= 12 casos) | A fazer |

### Tier C — estacionado (sem tarefa; não decompor nesta rodada)
S-03 aba/tela de histórico (histórico **continua gravado** em T-18/T-19; dados via T-24 disponíveis) · S-10 Serilog/log JSON estruturado (T-21 mantém a abstração) · S-12 endpoint de histórico · S-13 estorno formal · testes unitários de `QuitacaoService`/`CancelamentoService` com Moq (resto de S-11) · rodapé de totais na grid · persistência de layout do grid.

### Resumo de contagem
**63 tarefas** = 56 Tier A (Lotes 1-13) + 7 Tier B (Lote 14). **14 lotes.** Esforço somado Tier A: dev (Lotes 1-12) ≈ 96,5 h-pessoa; entrega (Lote 13) 7 h; Tier B 10,5 h. Ver Seção 5 (capacidade).

---

## 4. Dependências e Ordem de Execução

### 4.1 Paralelismo por lote (instâncias do Executor por rodada)

"Rodada" = conjunto de tarefas cujas dependências já estão `Concluída`. Tarefas de lotes diferentes podem ser misturadas na mesma rodada real se as dependências permitirem (ex.: T-05 roda junto de T-04).

| Lote | Rodadas (tarefas em paralelo) | Máx. paralelo |
|---|---|---|
| 1 Spike e preparação | R1: T-01 ‖ T-02 ‖ T-03 | **3** |
| 2 Esqueleto e Domínio | R1: T-04 · R2: T-07 · R3: T-08 · R4: T-09 ‖ T-10 | **2** (cadeia curta; compensar com Lote 3) |
| 3 Banco | R1: T-05 · R2: T-06 ‖ T-11 ‖ T-12 | **3** |
| 4 Persistência/Application base | R1: T-13 · R2: T-14 ‖ T-17 ‖ T-21 · R3: T-15 · R4: T-16 | **3** |
| 5 Serviços de negócio | R1: T-18 ‖ T-19 ‖ T-22 ‖ T-23 ‖ T-24 · R2: T-20 | **5** |
| 6 API base/hospedagem | R1: T-25 · R2: T-26 ‖ T-28 ‖ T-29 · R3: T-27 | **3** |
| 7 Endpoints | R1: T-30 ‖ T-31 ‖ T-32 · R2: T-33 ‖ T-34 | **3** |
| 8 Segurança e integração | R1: T-35 ‖ T-36 · R2: T-37 ‖ T-38 · R3: T-39 · R4: T-40 | **2** |
| 9 Tela grid/detalhe | R1: T-41 · R2: T-42 · R3: T-43 ‖ T-44 | **2** |
| 10 Tela status/ciclo de vida | R1: T-45 ‖ T-46 · R2: T-47 | **2** |
| 11 Relatório | R1: T-48 · R2: T-49 · R3: T-50 (T-48/T-49 em paralelo se `LinhaRelatorio` for publicada primeiro) | **1** (2 com o acordo de DTO) |
| 12 Acessibilidade | R1: T-51 · R2: T-52 | **1** |
| 13 Entrega O-12 | R1: T-53 ‖ T-54 ‖ T-55 · R2: T-56 | **3** |
| 14 Tier B | R1: T-57 ‖ T-59 ‖ T-61 ‖ T-63 · R2: T-58 ‖ T-60 ‖ T-62 | **4** |

Independentes **entre lotes** (paralelismo adicional): Lote 9 (T-41 em diante) roda em paralelo aos Lotes 6-8 assim que T-23 existir; Lote 8 (T-35/T-36/T-37) roda em paralelo ao Lote 9/10.

### 4.2 Ordem por dia e caminho crítico

| Dia | Data | Lotes / marcos |
|---|---|---|
| 1 | 21/09 | L1 (**T-01 primeiro, bloqueante**), L2, L3; início L4 (T-13) |
| 2 | 22/09 | L4, L5 (T-18/T-19 + S-04) |
| 3 | 23/09 | L6, L7. **Marco: API utilizável pelo Vendas no fim do Dia 3** (T-30/T-31 no meio do dia; T-34 fecha o marco) |
| 4 | 24/09 | L8 (S-08, S-09, **O-11 T-38…T-40 até o fim do dia**) ‖ L9 + L10 |
| 5 | 25/09 | L11, L12; L14 (Tier B) se sobrar tempo; preparação de L13 (T-54/T-55, sem código). **Fim do desenvolvimento** |
| 6-7 | 26-27/09 | L13 (T-53, T-56 e ajustes de README); **sem código novo**, só correção bloqueante |

**Caminho crítico (Tier A, ~24 h em série):** T-01 -> T-04 -> T-07 -> T-08 -> T-10 -> T-19 -> T-32 -> T-38…T-40 (integração) e, em paralelo, T-05 -> T-11 -> T-14 -> T-15 -> T-16 -> T-18 -> T-30 -> T-38. Qualquer atraso em T-01 ou T-14…T-16 desloca o marco do Dia 3.

### 4.3 Pontos "A VALIDAR" -> tarefas afetadas (premissas explícitas)

| Ponto | Premissa assumida | Tarefas que dependem | Se rejeitado |
|---|---|---|---|
| D-03 / P-9 (Pendente) | `POST /api/vendas` existe | T-18 (ramo Pendente), T-59/T-60 (linha pendente), T-61, T-62 | Cancelar T-61/T-62; remover linha pendente; registrar justificativa |
| D-04 / ADR-004 (OWIN no Desktop) | API dentro do app | T-25, T-26, T-27, T-45, T-46, T-47 | Muda hospedagem: Coordenador reabre (novo ADR) antes de T-27; T-45…T-47 refeitas |
| D-05 / P-8 (alias `/api/v1`) | Alias ativo | T-33, T-62 | Desativar rota alias (1 linha) |
| D-06 (motivo p/ Quitada) | Obrigatório | T-09, T-19, T-32, T-39 | Ajustar T-09 e critérios; sem impacto estrutural |
| D-08 / ADR-007 (cancelar desconhecida) | Cria Cancelada, colunas NULL | T-05, T-07, T-10, T-11, T-19, T-42, T-44 | Voltar colunas a NOT NULL e `Cancelar` desconhecida -> 404; T-10 sem uso |
| D-10 (cliente por ID) | Sem nome | T-23, T-42, T-48, T-57, T-58 | Campo `clienteNome` aditivo: coluna nova, sem redesenho |
| D-11 (xUnit + Moq) | xUnit | T-04, T-20, T-63 | Trocar framework, sem impacto em código de produção |
| Contrato v1.1 (P-1…P-9) | Aceito pelo Vendas | T-03, T-28, T-30…T-33, T-35…T-40 | Manter v1.0; mudanças só aditivas; bloqueio em `BLOCKERS.md`; T-38 vira teste por Postman se sem acesso ao Vendas |

---

## 5. Riscos de Prazo

| # | Risco | Sev. | Ação |
|---|---|---|---|
| RP-1 | **Capacidade:** soma de esforço Tier A dev ≈ 96,5 h-pessoa contra ~49 h do VISAO 6.1. A diferença é overhead de granularidade fina e testes por tarefa, **não** escopo novo; só cabe em 5 dias com **execução paralela real** (Seção 4.1) e caminho crítico ~24 h. Com um único executor sequencial não cabe | Alta | Confirmar com o usuário quantas instâncias paralelas estão disponíveis; se < 3, cortar Tier B inteiro e simplificar T-43/T-51 |
| RP-2 | T-01 no-go: +1h de extensão e refaz T-05/T-11/T-12 (~7 h) | Alta | Timebox rígido; fallback SQL Server já decidido (ADR-002) |
| RP-3 | Contrato v1.1 não aceito / sem acesso ao código do Vendas (T-38/T-39) | Alta | Enviar T-03 no Dia 1; T-34 como contrato executável; fallback v1.0 |
| RP-4 | Trial DevExpress/FastReport (marca d'água, sem preview) | Média | T-02 no Dia 1; fluxo PDF já previsto (T-50) |
| RP-5 | OWIN/porta/firewall (T-27) | Média | Porta configurável; erro visível na barra de status |
| RP-6 | O-11 concentra dependência externa no Dia 4 | Alta | T-40 é reserva de 2 h; nenhum Tier B antes dela |
| RP-7 | Dia 5 cheio (L11+L12) sem folga real para Tier B | Média | Tier B é oportunista, cortar sem discussão |

---

## 6. Lacunas Sinalizadas e Decisões de Detalhe

**Lacunas estruturais no SDD/UX-SPEC:** nenhuma que exija novo ADR ou alteração de SDD/UX-SPEC nesta rodada.

**Decisões de detalhe tomadas na decomposição (documentadas, sem impacto de custo/prazo):**
1. **ADR-009 (resultado do spike):** ADR-002 é imutável e está "Aceito (go/no-go pendente)". O resultado do spike entra como ADR-009 novo (rascunho do Executor em T-01; ratificação do Coordenador). Se for no-go, ADR-009 supersede ADR-002 no que trata do banco.
2. **`ICorrelationContext`** em Application, implementado na Api (T-13/T-29), para o histórico gravar `CORRELATION_ID` sem Application conhecer OWIN.
3. **`TraceLogger` em Tier A (T-21)**, Serilog Tier C (SDD 2.6): mantém a abstração `IAppLogger`.
4. **Health com `banco`** e 503 em falha (SDD 2.6) fica em T-36; a tela usa `IHealthService` em processo (T-45), não o endpoint.
5. **Tests project** referencia só Domain/Application (SDD 2.1). Testes de integração com Firebird (T-20) são harness com o app **fechado** (regra de único processo dono do `.fdb`).
6. **Relatório sem limite de 5.000** e tela com aviso de truncamento (UX-SPEC 7) implementados em T-48/T-43.
7. **Tarefas inseparáveis (misturas justificadas):** nenhuma. T-46 (F-7) agrega splash + mutex + falha de banco/porta por ser um único fluxo de tela; T-28 agrega handler + tabela de códigos por ser uma única unidade (um handler).
8. **T-63 (S-11 reduzido)** cobre só o Domain; os testes dos serviços com Moq são Tier C.
9. **Datas:** estimativas não incluem o registro da linha v1.1 em VISAO 2.4, que só ocorre com aceite do Vendas (fora das tarefas; ação do usuário).

**Autocheck de granularidade — divisões realizadas (antes -> depois):**

| Item original (VISAO) | Problema no autocheck | Resultado |
|---|---|---|
| O-01 (4h) | Mistura spike + trials + contrato | T-01, T-02, T-03 |
| O-03 (5h) | Mistura SQL + entidades + regra de estado + EF + inicializador | T-05, T-06 (SQL); T-07, T-08, T-09, T-10 (domínio, incl. regras S-05 e D-08 separadas); T-11 (EF); T-12 |
| O-04 (3h) | Mistura interfaces, repositório, transação, retry | T-13, T-14, T-15, T-16 |
| O-05 (4h) + S-04 | Serviço + validação + concorrência | T-17, T-18, T-16/T-15, T-20 |
| O-06 (3h) + S-05 | Serviço + regra de negócio distinta | T-19, T-09, T-10 |
| O-07 (5h) + S-02 | 3 endpoints + envelope + alias | T-25, T-28, T-29, T-30, T-31, T-32, T-33 |
| O-08 (2h) | Composition root + host | T-26, T-27 |
| O-09 (5h) | 4 fluxos de tela | T-41, T-42, T-43, T-44, T-45, T-46, T-47 |
| O-10 (5h) | Dados + layout + fluxo de tela | T-48, T-49, T-50 |
| O-11 (5h) | Cenários distintos + correção | T-38, T-39, T-40 (+ T-37 contrato) |
| O-12 (4h) | Build + docs + evidência + teste | T-53, T-54, T-55, T-56 |
| S-06, S-07 (Tier B) | Regra + tela/layout | T-57/T-58; T-59/T-60 |
| S-08, S-09 | Cada um já é 1 endpoint/handler | T-35, T-36 |

**Canário de ~300 mil tokens:** nenhuma tarefa prevista acima disso; as maiores (T-18, T-42, T-49) tocam 3-5 arquivos e um serviço/tela cada.

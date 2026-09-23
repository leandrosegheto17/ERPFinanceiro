# SDD.md — ERP Financeiro (C#)

> **APROVADO v0.1 (usuário, 22/09/2026).** Autor: Coordenador (chapéu Software Architect). Base: `PRD-TECNICO.md` v0.2, `PRD.md`, `VISAO-PRODUTO.md`, `CTO-REVIEW.md`.
> Legenda: **[DECIDIDO]** decisão do usuário (D-01, D-02, D-07, D-09, stack) · **[A VALIDAR]** assumida com a recomendação do PRD-TECNICO (D-03, D-04, D-05, D-06, D-08, D-10, D-11, contrato v1.1 P-1…P-9). Se a validação divergir, gera novo ADR que supersede o atual.
> O registro de mudanças do contrato (VISAO-PRODUTO 2.4) segue como fonte única. Este SDD **não** o altera; a linha v1.1 só é preenchida quando P-1…P-9 forem aceitas pelo Vendas.

## 1. Visão Geral

Módulo Financeiro que expõe API REST/JSON ao ERP Vendas (Delphi), persiste o registro financeiro (venda, itens, histórico append-only) e oferece ao operador uma tela de consulta (DevExpress) e um relatório (FastReport).

**Decisão de forma:** **um único processo** (executável Desktop WinForms) que hospeda a API via OWIN self-host [A VALIDAR D-04, ADR-004] e acessa o Firebird embarcado [DECIDIDO, ADR-002]. Monólito em camadas (ADR-001), dependências para dentro.

**Princípio de acesso ao dado:** a tela **não chama a própria API por HTTP**; API e tela consomem os mesmos serviços de `Application`. Isso evita dependência de a API estar de pé para consultar e mantém o `.fdb` aberto por um único processo (R-06 do PRD).

**Restrições:** C# .NET Framework 4.8, EF6, DevExpress WinForms (trial), FastReport (trial/Open Source), custo zero, prazo dev até 25/09 (D-07), sem acesso ao código do Vendas.

## 2. Componentes e Fluxo de Dados

### 2.1 Camadas (projetos)

| Projeto | Responsabilidade | Referencia |
|---|---|---|
| `ERPFinanceiro.Domain` | Entidades `Venda`, `VendaItem`, `VendaHistorico`; enums `StatusVenda`, `Operacao`; máquina de estados (métodos `Quitar`, `Cancelar`); exceções de domínio | nada |
| `ERPFinanceiro.Application` | `QuitacaoService`, `CancelamentoService`, `RegistroService` (Pendente, D-03), `ConsultaService`; comandos/resultados; interfaces `IVendaRepository`, `IUnitOfWork`, `IClock`, `IAppLogger`; validação de payload e de total | Domain |
| `ERPFinanceiro.Infrastructure` | `FinanceiroDbContext` (EF6, mapeamento Fluent), repositórios, UoW/transação, tradução de conflito para `ConcorrenciaException`, Serilog | Application, Domain |
| `ERPFinanceiro.Api` | Web API 2 sobre OWIN: controllers finos, DTOs, `ApiKeyHandler`, `ExceptionHandler` global -> envelope de erro, `CorrelationIdHandler`, rotas | Application (+Domain enums) |
| `ERPFinanceiro.Reports` | Layouts `.frx`, `RelatorioDataSource` (mesmo `ConsultaService` + mesmo filtro), exportação PDF/HTML | Application |
| `ERPFinanceiro.Desktop` | WinForms + DevExpress; **composition root Autofac**; inicia/para o host OWIN; tela de consulta | Application, Reports, Api (só host), Infrastructure (só no composition root) |
| `ERPFinanceiro.Tests` | xUnit + Moq (D-11) | Domain, Application |

Nota: Desktop referencia Infrastructure **somente** no composition root para registrar o DI (a regra "Desktop não acessa DbContext" vale para o código de tela).

### 2.2 Fluxo de quitação (API)

```
Vendas --POST quitacao--> [CorrelationId] -> [ApiKey 401] -> Controller (DTO->comando)
  -> QuitacaoService: valida (400) -> repo.BuscarPorVendaId
     inexistente: cria Venda + itens, Quitar() | Pendente: Quitar() | Quitada: retorna original | Cancelada: 409
  -> UoW: venda+itens+historico na MESMA transação; UPDATE ... WHERE VERSAO = :lida
  -> conflito (0 linhas / violação UNIQUE): rollback, reler e reavaliar (máx. 3 tentativas)
  -> 200 {status, dataQuitacao}
```

Cancelamento segue o mesmo esqueleto (D-06: Quitada exige motivo; D-08: desconhecida cria Cancelada).

### 2.3 Fluxo de consulta/relatório (Desktop)
`Tela -> ConsultaService(filtro) -> IVendaRepository (leitura, sem tracking) -> DTO de listagem -> Grid`. Relatório: `RelatorioDataSource` chama `ConsultaService` com o **mesmo filtro** e calcula totais a partir da **mesma lista** (garante CA-07.3), alimenta o FastReport.

### 2.4 Endpoints (contrato v1.1 proposta; base v1.0 preservada)

| Método/Rota | Notas |
|---|---|
| `POST /api/vendas/quitacao`, `POST /api/vendas/cancelamento`, `GET /api/vendas/{id}/status` | v1.0 (obrigatórios) |
| `POST /api/vendas` | Registrar Pendente [A VALIDAR D-03/P-9] |
| `GET /api/health` | Sem autenticação [A VALIDAR P-5]; retorna também verificação de banco (ver 2.6) |
| alias `/api/v1/vendas/...` | Mesmo controller via dupla atribuição de rota [A VALIDAR D-05] |

Envelope de erro `{erro:{codigo,mensagem}}` [P-1]; códigos: `PAYLOAD_INVALIDO`(400), `VALOR_TOTAL_DIVERGENTE`(400), `NAO_AUTORIZADO`(401), `VENDA_NAO_ENCONTRADA`(404), `VENDA_JA_CANCELADA`(409), `MOTIVO_OBRIGATORIO`(409), `DADOS_DIVERGENTES`(409, I-04, novo código a validar), `CONFLITO_CONCORRENCIA`(409, só após esgotar releituras), `ERRO_INTERNO`(500). JSON: UTF-8, datas ISO 8601 UTC, decimais como número [P-6].

### 2.5 Concorrência e transação (RNF-04, ADR-005)
- `UNIQUE(VENDA_ID)` cobre criação simultânea; `VERSAO` cobre atualização simultânea.
- **`VERSAO` incrementada pela aplicação** (`UPDATE ... SET VERSAO = :v+1 WHERE ID=:id AND VERSAO=:v`, mapeada como `IsConcurrencyToken`), **sem trigger**, para não haver dupla incrementação nem depender do refresh de coluna computada no provider Firebird. O spike valida; se o provider falhar, alternativa é comando SQL explícito (`ExecuteSqlCommand`) na atualização.
- Escrita da venda + itens + histórico em uma transação; histórico só por `INSERT`.
- Idempotência: repetição devolve resultado original sem novo histórico (P-2).

### 2.6 Health, log e configuração
- Health: `200 {status:"ok"}` (contrato). Um campo extra aditivo `banco:"ok|falha"` é retornado (`SELECT 1`); em falha do banco, HTTP 503 `{status:"degradado"}`. A tela usa o mesmo `IHealthService` **em processo** (sem HTTP) para o indicador de status (ver UX-SPEC).
- Log: abstração `IAppLogger`; Serilog arquivo JSON com `correlationId` (S-10, **Tier C**: se cortado, fica `Trace`/arquivo texto simples, sem alterar a abstração).
- Configuração (`App.config`): porta, connection string, `ApiKey`. `ApiKey` no `.config` local, fora do código-fonte; limitação documentada (CTO-REVIEW item 5).

## 3. Stack Tecnológica

| Item | Escolha | Justificativa / alternativa |
|---|---|---|
| Runtime | .NET Framework 4.8, C# 7.3 | Obrigatório |
| ORM | Entity Framework 6.4 | Obrigatório (não EF Core) |
| Banco | **Firebird 3.0 embarcado** `.fdb` próprio [DECIDIDO] + `FirebirdSql.Data.FirebirdClient` + `EntityFramework.Firebird` | Vendas também é Firebird; zero instalação. Fallback: SQL Server Express/LocalDB (ADR-002) |
| API | ASP.NET Web API 2 + OWIN self-host (`Microsoft.Owin.Host.HttpListener`) [A VALIDAR D-04] | Sem IIS; alternativas: serviço Windows, IIS Express (ADR-004) |
| DI | Autofac (`Autofac.WebApi2`) | Composition root único |
| UI | DevExpress WinForms (trial) | Obrigatório, custo zero (D-09) |
| Relatório | FastReport .NET Trial; se não houver preview, Open Source + exportação PDF/HTML (ADR-006) | Confirmar no Dia 1 |
| JSON | Newtonsoft.Json (padrão do Web API 2) | Decimais/datas configurados por serializer settings |
| Log | Serilog (Tier C) | S-10 |
| Testes | xUnit + Moq [A VALIDAR D-11] | Compatível com net48 |
| Mapeamento DTO | Manual | Escopo pequeno, evita AutoMapper |
| Bitness | Compilar **x86 ou x64 conforme as DLLs do Firebird embarcado**, fixando `PlatformTarget` (nunca AnyCPU) | R-06; decidir no spike |

## 4. Decisões Arquiteturais (índice de ADRs)

| ADR | Título | Status |
|---|---|---|
| [001](adr/001-arquitetura-em-camadas-monolito.md) | Monólito em camadas com dependências para dentro | Aceito |
| [002](adr/002-firebird-embarcado-spike-e-fallback.md) | Firebird 3.0 embarcado, spike Dia 1 e fallback SQL Server Express | Aceito (go/no-go pendente do spike) |
| [003](adr/003-banco-proprio-sem-fk-com-vendas.md) | Banco próprio, sem FK com o Vendas | Aceito |
| [004](adr/004-owin-self-host-no-desktop.md) | API via OWIN self-host dentro do Desktop | Proposto (A VALIDAR D-04) |
| [005](adr/005-concorrencia-versao-unique.md) | Concorrência otimista por `VERSAO` + UNIQUE + transação | Aceito |
| [006](adr/006-contrato-v1-1-e-lacunas.md) | Contrato v1.1 (P-1…P-9), alias de rota, API Key, códigos de erro | Proposto (A VALIDAR) |
| [007](adr/007-venda-desconhecida-colunas-anulaveis.md) | Cancelamento de venda desconhecida e colunas anuláveis (I-03/I-03b) | Proposto (A VALIDAR D-08) |
| [008](adr/008-consulta-em-processo-e-relatorio.md) | Tela consulta em processo (sem HTTP) e relatório com fallback PDF | Aceito |

## 5. Modelo de Dados de Alto Nível

Base: VISAO-PRODUTO Seção 4 (preservada). Ajustes do SDD:

**`FIN_VENDA`**: `ID` BIGINT identity PK; `VENDA_ID` VARCHAR(50) UNIQUE NOT NULL; `CLIENTE_ID` VARCHAR(50) **NULL**; `VALOR_TOTAL` DECIMAL(18,2) **NULL**; `STATUS` SMALLINT NOT NULL (0/1/2); `DATA_RECEBIMENTO` TIMESTAMP NOT NULL (UTC); `DATA_QUITACAO`, `DATA_CANCELAMENTO` TIMESTAMP NULL; `MOTIVO_CANCELAMENTO` VARCHAR(500) NULL; `VERSAO` INTEGER DEFAULT 0 NOT NULL.
- **I-03/I-03b:** `CLIENTE_ID` e `VALOR_TOTAL` anuláveis (D-08 assumida). Regra de domínio: NOT NULL lógico para vendas nascidas por quitação/pendente; NULL apenas para venda nascida por cancelamento desconhecido. Consulta/relatório exibem "—" e **excluem NULL dos totais (tratam como 0)**, com marca visual "origem: cancelamento sem quitação". Se D-08 for rejeitada (404), reverter para NOT NULL sem alterar demais tabelas (ADR-007).
- Índices: `UNIQUE(VENDA_ID)`, `(CLIENTE_ID)`, `(STATUS)`, `(DATA_RECEBIMENTO)` (filtro por período).

**`FIN_VENDA_ITEM`**: `ID`, `VENDA_REF` FK CASCADE, `PRODUTO_ID` VARCHAR(50), `QUANTIDADE` INTEGER CHECK >0, `PRECO_UNITARIO` DECIMAL(18,4) CHECK >=0.
**`FIN_VENDA_HISTORICO`**: `ID`, `VENDA_REF` FK, `OPERACAO` SMALLINT (0 Recebida,1 Quitação,2 Cancelamento), `STATUS_ANTERIOR` (NULL na criação)/`STATUS_NOVO`, `DATA_HORA`, `MOTIVO`, `CORRELATION_ID`. Append-only: repositório expõe só `Adicionar`; opcionalmente triggers `BEFORE UPDATE/DELETE` que lançam exceção (defesa em profundidade).

Regras Firebird: identificadores <= 31 chars (nomes de constraints/índices explícitos, ex. `IDX_FIN_VENDA_CLIENTE`), DECIMAL <= 18, DDL manual em `database/01-schema.sql` + `02-seed.sql`, sem migrations; entrega `.sql` + `.fdb`. Não há tabela de clientes/produtos (RN-08, D-10: filtro por ID).

## 6. Riscos Técnicos

| # | Risco | Sev. | Mitigação / dívida aceita |
|---|---|---|---|
| RT-01 | Provider EF6 Firebird imaturo (concorrência, DECIMAL, identity/generator, `Take/Skip`) | Alta | Spike de ~2h no Dia 1 com go/no-go; fallback SQL Server; SQL explícito pontual permitido |
| RT-02 | Trial DevExpress/FastReport: marca d'água, expiração, avaliador sem trial | Alta | Confirmar no Dia 1; README; evidências (prints, PDF) |
| RT-03 | Capacidade: P0 = 43h em 5 dias | Alta | Escopo por tiers; ordem de corte do VISAO 6.2; nada de Tier B/C antes de O-11 |
| RT-04 | Contrato v1.1 não aceito pelo Vendas / sem acesso ao código | Alta | Congelar Dia 1; testar por Postman/curl; mudanças só aditivas; `POST /api/vendas` opcional |
| RT-05 | Bitness das DLLs Firebird embarcadas e acesso de dois processos ao `.fdb` | Média | Fixar plataforma; único processo abre o `.fdb` (ADR-008); detectar `.fdb` bloqueado e mostrar erro claro |
| RT-06 | OWIN `HttpListener` em porta ocupada/firewall | Média | Porta configurável; falha de bind mostrada na barra de status; fallback `127.0.0.1` |
| RT-07 | Decimal/data Delphi↔C# | Média | Serializer com `decimal`; exemplos JSON em `docs/`; teste de contrato |
| RT-08 | Multi-thread: Web API atende em thread pool enquanto a UI lê | Média | `DbContext` por requisição/operação (escopo Autofac), nunca compartilhado com a UI |
| RT-09 | Desempenho grid com muitas vendas | Baixa | Paginação/limite de 5.000 linhas + aviso; índices; leitura `AsNoTracking` |
| **Dívidas aceitas** | (a) API Key em texto simples no `.config` (contexto de desafio); (b) sem TLS (localhost); (c) sem estorno formal (S-13); (d) log estruturado e histórico na tela podem cair (Tier C) — histórico continua gravado; (e) `ClienteId` sem nome (D-10) | — | Motivo: prazo/custo zero; documentar no README |

## 7. Requisitos de Segurança (nível de arquitetura)

Requisitos de arquitetura; SAST/DAST/hardening ficam com o Validador.

- **Autenticação:** `X-Api-Key` em todos os endpoints exceto `GET /api/health` [A VALIDAR P-4/P-5]. Handler de mensagem antes do roteamento; comparação em tempo constante (`CryptographicOperations`/XOR manual em net48); chave lida de `App.config` (`appSettings`), com valor de exemplo no repositório e **sem chave real versionada**. Ausente/inválida -> 401 `NAO_AUTORIZADO`, sem revelar qual caso.
- **Autorização:** papel único (cliente Vendas com chave válida). Tela Desktop: sem login (aplicativo local do operador), documentado como limitação.
- **Exposição de rede:** bind em `http://localhost:{porta}/` (padrão) — sem admin/`netsh`. Bind em rede só por configuração explícita e documentada (aí exigir chave e recomendar TLS).
- **Entrada:** validação de payload na Application (tamanhos máximos: IDs 50, motivo 500, itens <= 1000, corpo <= 1 MB); consultas somente via EF/parâmetros (sem concatenação SQL), inclusive filtros da tela.
- **Erros:** envelope sem stack trace nem mensagem de exceção; detalhes só no log com `correlationId`.
- **Criptografia:** dados em repouso — `.fdb` sem criptografia (sem dados pessoais sensíveis, apenas IDs; LGPD baixo impacto, CTO-REVIEW item 5), permissão de pasta do usuário; em trânsito — HTTP local, sem TLS (dívida aceita).
- **Isolamento:** banco próprio, sem FK/credenciais compartilhadas com o Vendas (ADR-003); um processo dono do `.fdb`; SYSDBA/senha do embarcado por configuração (não hardcoded).
- **Integridade/auditoria:** histórico append-only, valores DECIMAL, transação atômica, correlationId por requisição.
- **Segredos/logs:** ApiKey nunca logada; payload logado sem cabeçalhos.
- **Dependências:** apenas pacotes NuGet oficiais; versões listadas no README (licenças).

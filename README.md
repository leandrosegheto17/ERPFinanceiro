# ERP Financeiro

Módulo Financeiro do desafio ERP: um aplicativo Windows (Desktop, .NET Framework 4.8) que hospeda uma API HTTP local para o ERP **Vendas** (Delphi) e uma tela de consulta com relatório. O banco é um arquivo Firebird embarcado (`.fdb`), próprio do Financeiro, sem chave estrangeira nem credencial compartilhada com o Vendas (ADR-003).

> **Estado deste README (tarefa T-54).** O documento descreve apenas o que existe hoje no repositório. Trechos marcados como **PENDENTE** dependem de tarefas ainda não concluídas (ver "Estado atual da entrega"). O passo a passo completo de execução deve ser reconfirmado em máquina limpa pela T-56.

## Sumário

1. [Estado atual da entrega](#1-estado-atual-da-entrega)
2. [Pré-requisitos](#2-pré-requisitos)
3. [Instalação dos trials (DevExpress e FastReport)](#3-instalação-dos-trials-devexpress-e-fastreport)
4. [Obter e compilar o código](#4-obter-e-compilar-o-código)
5. [Configuração](#5-configuração)
6. [Banco de dados](#6-banco-de-dados)
7. [Execução](#7-execução)
8. [Integração com o Vendas (API)](#8-integração-com-o-vendas-api)
9. [Limitações e dívidas aceitas](#9-limitações-e-dívidas-aceitas)
10. [Versões e licenças](#10-versões-e-licenças)
11. [Estrutura do repositório](#11-estrutura-do-repositório)

---

## 1. Estado atual da entrega

| Item | Situação |
|---|---|
| Domínio, Application, Infrastructure (EF6 + Firebird), esquema e seed SQL | Implementados (Lotes 1 a 6) |
| API: host OWIN, JSON, correlation id, tratamento global de erros | Implementados |
| API: endpoint `POST /api/vendas/quitacao` | Implementado (`VendasController`) |
| API: `GET /api/health` | Implementado (`HealthController`, T-36) |
| API: `POST /api/vendas/cancelamento`, `GET /api/vendas/{id}/status`, `POST /api/vendas`, alias `/api/v1` | **PENDENTE** (não existem no código nesta data) |
| API: autenticação por `X-Api-Key` | Implementada (`ApiKeyHandler`, T-35; isenta `GET /api/health`) |
| Leitura de `Api:ApiKey` do `App.config` | Implementada (`ApiHost`) |
| Leitura de `Api:Porta` do `App.config` | **PENDENTE** (T-46) |
| Executável Desktop | **PENDENTE**: `Program.Main` ainda lança `NotImplementedException` ("Esqueleto T-04"); tela, splash e início do host chegam em T-26/T-42/T-46 |
| Relatório de listagem (FastReport, PDF): `RelatorioDataSource` e `RelatorioListagem` | Implementado (T-48/T-49) |
| Tela (DevExpress) e preview do relatório | **PENDENTE** (T-50) |
| Pacote de entrega (pasta com executável, DLLs Firebird, `.sql`, `.fdb` de demonstração) | **Não aplicável**: a entrega é só o código-fonte. O script opcional `tools/T-53-empacotar/Empacotar.ps1` monta a pasta, se necessário. |
| Evidências (prints e PDF) em `docs/evidencias/` | **PENDENTE** (T-55) |
| Teste em máquina limpa seguindo este README | **PENDENTE** (T-56) |

Consequência prática: hoje é possível compilar a solução, criar o banco a partir dos scripts, gerar o relatório em PDF por código e rodar os testes automatizados; **ainda não é possível abrir o aplicativo**. As seções 5 a 8 documentam a configuração e o contrato já definidos, para uso assim que as tarefas pendentes forem entregues.

## 2. Pré-requisitos

Verificados nos arquivos `.csproj` e nos ADRs 002 e 009:

- Windows 64 bits (todos os projetos usam `PlatformTarget=x64`; `AnyCPU` não é usado porque as DLLs nativas do Firebird embarcado são x64, ADR-009).
- .NET Framework 4.8 (Runtime e Developer Pack). Todos os projetos usam `net48` com `LangVersion 7.3`.
- Para compilar: Visual Studio 2022 (ou Build Tools) com workload de desktop .NET, ou o SDK `dotnet` capaz de compilar projetos SDK-style `net48`. Os projetos usam `PackageReference`; o pacote de referência `net48` é resolvido via NuGet.
- Acesso ao NuGet oficial (nuget.org) para restaurar pacotes (regra 17 do TASK.md: só pacotes oficiais).
- Firebird 3.0.x **Win64 embarcado**: as DLLs nativas **não** vêm nos pacotes NuGet (o `FirebirdSql.Data.FirebirdClient` 10.x é só código gerenciado). Baixe o zip "sem instalar" `Firebird-3.0.12.33787-0-x64.zip` em <https://sourceforge.net/projects/firebird/files/v3.0.12/> e copie para a pasta do executável: `fbclient.dll` (e uma cópia chamada `fbembed.dll`), `ib_util.dll`, `icudt52.dll`, `icudt52l.dat`, `icuin52.dll`, `icuuc52.dll`, `msvcp100.dll`, `msvcr100.dll`, `zlib1.dll`, `firebird.conf`, `firebird.msg`, `security3.fdb`, `plugins.conf`, a pasta `plugins/` (`engine12.dll`, `legacy_auth.dll`, `legacy_usermanager.dll`, `srp.dll`) e a pasta `intl/` (`fbintl.dll`, `fbintl.conf`). A lista vem do ADR-009. O script opcional `tools/T-53-empacotar/Empacotar.ps1` baixa e monta esse conjunto; para rodar a partir do código-fonte, copie-o manualmente para a pasta de saída do Desktop.
- Para relatório e tela (quando entregues): trials descritos na seção 3.
- Ferramenta opcional: `isql` do Firebird, para aplicar os scripts SQL manualmente (o `DbInitializer` também os aplica em execução).

## 3. Instalação dos trials (DevExpress e FastReport)

Referência completa: [`docs/licencas.md`](docs/licencas.md) (tarefa T-02).

**Aviso importante:** a T-02 foi feita por pesquisa na documentação oficial; a instalação real dos trials **não foi realizada** por falta de ambiente com GUI. Os dados abaixo (duração, marca d'água) precisam ser confirmados na primeira instalação real, antes de T-49/T-50.

### DevExpress WinForms (trial)

1. Acesse <https://www.devexpress.com/products/try/> e baixe o instalador unificado (".NET & JavaScript Unified Component Installer"). Exige cadastro/login.
2. Execute o instalador e escolha os componentes WinForms.
3. Duração: **30 dias corridos** a partir da instalação, sem custo (G-2, custo zero).
4. Comportamento esperado: aviso/splash de trial ao executar o aplicativo e marca d'água em relatórios/exportações. Isso é esperado e aceito.
5. Versão exata instalada: **a registrar** (só é conhecida após o download real; anote em `docs/licencas.md`). Nenhum pacote DevExpress está referenciado nos `.csproj` atuais.

### FastReport

Há dois caminhos, ambos de custo zero (detalhes e decisão em `docs/licencas.md` e ADR-008):

- **FastReport .NET Trial** (caminho preferencial para preview dentro do WinForms): instalador oficial em fast-report.com. Tem preview "In Application" e exportação PDF nativa; espera-se marca d'água durante a avaliação; duração **a confirmar** na instalação real.
- **FastReport Open Source** (MIT, sem expiração): sem componente de preview em WinForms e com exportação PDF em plugin separado.

Hoje o projeto `ERPFinanceiro.Reports` referencia **FastReport.OpenSource 2023.3.13** e **FastReport.OpenSource.Export.PdfSimple 2023.3.13** (exportação PDF). Qual dos dois será efetivamente empacotado na entrega e o motivo: **PENDENTE** (definido em T-49/T-53; atualizar esta seção). Fluxo previsto (ADR-008): preview quando disponível, com fallback automático para exportação em PDF.

## 4. Obter e compilar o código

```
git clone <url-do-repositório>
cd ERPFinanceiro
dotnet restore ERPFinanceiro.sln
dotnet build ERPFinanceiro.sln -c Release -p:Platform=x64
```

(Alternativa: abrir `ERPFinanceiro.sln` no Visual Studio, plataforma `x64`.) Se o `dotnet build` não conseguir compilar `net48` no seu ambiente, use o Visual Studio ou `msbuild`. Esta forma de compilação não foi executada na T-54 (tarefa só de documentação); confirmar na T-56.

Testes automatizados (xUnit, projeto `src/ERPFinanceiro.Tests`):

```
dotnet test src/ERPFinanceiro.Tests/ERPFinanceiro.Tests.csproj
```

Parte dos testes de integração usa o Firebird embarcado e, portanto, precisa das DLLs nativas da seção 2 disponíveis ao processo de teste.

## 5. Configuração

Copie `src/ERPFinanceiro.Desktop/App.config.example` para `App.config` na pasta do executável (o `App.config` real está no `.gitignore`; nunca versione chave nem senha, G-6/regra 24). Chaves de `appSettings`, com os valores do exemplo:

| Chave | Valor no exemplo | Significado |
|---|---|---|
| `Api:Porta` | `5000` | Porta do host OWIN em `http://localhost:{porta}/`. *Leitura pelo código: PENDENTE (T-46).* |
| `Api:ApiKey` | `COLOQUE_UMA_CHAVE_LOCAL_AQUI` | Chave exigida no header `X-Api-Key`. Defina uma chave local sua; a mesma deve ser configurada no Vendas. |
| `Banco:CaminhoFdb` | `C:\ERPFinanceiro\data\financeiro.fdb` | Caminho do `.fdb`. Criado pelo `DbInitializer` se ausente. Lida por `ConfiguracaoBancoAppConfig`; obrigatória. |
| `Banco:Usuario` | `SYSDBA` | Usuário do Firebird embarcado. Obrigatória. |
| `Banco:Senha` | `COLOQUE_A_SENHA_LOCAL_AQUI` | Senha local. Obrigatória. |
| `Log:CaminhoArquivo` | `C:\ERPFinanceiro\data\logs\app.log` | Arquivo do `TraceLogger`. Obrigatória no composition root. |

Connection string (seção `connectionStrings`, nome `FinanceiroDbContext`, provider `FirebirdSql.Data.FirebirdClient`):

```
User=SYSDBA;Password=<senha>;Database=C:\ERPFinanceiro\data\financeiro.fdb;DataSource=localhost;Port=3050;Dialect=3;Charset=UTF8;ServerType=1;
```

`ServerType=1` significa modo **Embedded** (ADR-002/ADR-009). Mantenha `Database`, `User` e `Password` coerentes com `Banco:*`. As seções `entityFramework` e `system.data/DbProviderFactories` do exemplo são obrigatórias (registro manual do provider; o projeto usa `PackageReference`) e devem ser copiadas sem alteração.

Se a porta estiver ocupada, o `ApiHost` não derruba o aplicativo: entra no estado `InativaPortaEmUso` e guarda o erro (a exibição na barra de status é da T-45, **PENDENTE**). Escolha outra porta em `Api:Porta` e reinicie.

Diretórios de `Banco:CaminhoFdb` e `Log:CaminhoArquivo` devem ser graváveis pelo usuário que executa o aplicativo. Apenas **um processo** pode abrir o `.fdb` por vez (regra 11; ADR-008).

## 6. Banco de dados

Scripts em `database/` (Firebird 3.0, DDL manual, sem migrations):

- `01-schema.sql`: tabelas `FIN_VENDA`, `FIN_VENDA_ITEM`, `FIN_VENDA_HISTORICO` e índices.
- `02-seed.sql`: massa de demonstração (3 vendas: `V-1001` Quitada, `V-1002` Pendente, `V-1003` Cancelada sem quitação prévia, cenário D-08). Aplicar depois do schema.

Aplicação manual com `isql` (com o `.fdb` já criado):

```
isql -user SYSDBA -password <senha> "C:\ERPFinanceiro\data\financeiro.fdb" -i database\01-schema.sql
isql -user SYSDBA -password <senha> "C:\ERPFinanceiro\data\financeiro.fdb" -i database\02-seed.sql
```

Os scripts declaram em cabeçalho que também são aplicados em execução pelo `DbInitializer` (T-12) quando o `.fdb` não existe. Os cabeçalhos citam `isql -i <arquivo>` como forma de uso; a linha de comando acima com usuário/senha/banco é a sintaxe padrão do `isql` e não foi executada pela T-54. Evidências de execução dos scripts: `database/evidencia-execucao-T-05.txt` e `-T-06.txt`.

A entrega é só o código-fonte: gere o `.fdb` aplicando `database/*.sql` (ou deixe o `DbInitializer` criá-lo na primeira execução). Nenhum `.fdb` é versionado no repositório (`.gitignore`).

## 7. Execução

**PENDENTE.** Enquanto T-26/T-42/T-46/T-53 não forem concluídas o executável não sobe (`Program.Main` lança `NotImplementedException`). Quando entregue, o roteiro previsto é:

1. Compilar a solução (`ERPFinanceiro.sln`, x64) e usar a pasta de saída do Desktop (ou a pasta gerada por `tools/T-53-empacotar/Empacotar.ps1`).
2. Copiar `App.config.example` para `App.config` e preencher chave, senha e caminhos (seção 5).
3. Executar `ERPFinanceiro.Desktop.exe`. Ele abre o banco, aplica o schema se necessário e inicia a API em `http://localhost:{Api:Porta}/`.
4. Verificar com `curl http://localhost:5000/api/health` (endpoint implementado, sem `X-Api-Key`; o executável ainda não sobe).

Atualize esta seção com os passos reais na T-56 (máquina limpa: clonar, compilar, seguir este README).

## 8. Integração com o Vendas (API)

Contrato completo (proposta v1.1, **ainda não enviada nem aceita** pelo lado Vendas): [`docs/contrato-v1.1.md`](docs/contrato-v1.1.md). O contrato v1.0 segue em vigor até o aceite.

- Base: `http://localhost:{porta}/` (somente a máquina local por padrão).
- Formato: JSON UTF-8, propriedades em camelCase, decimais como número, datas ISO 8601 UTC (`...Z`). Toda resposta traz o header `X-Correlation-Id`.
- Autenticação (implementada, T-35): header `X-Api-Key` em todos os endpoints, exceto `GET /api/health`. Ausente ou inválida: `401 NAO_AUTORIZADO`.

| Endpoint | Estado no código |
|---|---|
| `POST /api/vendas/quitacao` | Implementado |
| `POST /api/vendas/cancelamento` | PENDENTE |
| `GET /api/vendas/{vendaId}/status` | PENDENTE |
| `POST /api/vendas` (registrar Pendente, aditivo, a validar) | PENDENTE |
| `GET /api/health` (sem autenticação, aditivo, a validar) | Implementado |
| Alias `/api/v1/vendas/...` (aditivo, a validar) | PENDENTE |

Exemplo (quitação), conforme o contrato:

```
curl -X POST http://localhost:5000/api/vendas/quitacao ^
  -H "Content-Type: application/json" -H "X-Api-Key: <sua-chave>" ^
  -d "{\"vendaId\":\"V-000123\",\"clienteId\":\"C-4521\",\"valorTotal\":1250.50,\"itens\":[{\"produtoId\":\"P-01\",\"quantidade\":2,\"precoUnitario\":500.00},{\"produtoId\":\"P-02\",\"quantidade\":1,\"precoUnitario\":250.50}]}"
```

Resposta 200: `{"status":"Quitada","dataQuitacao":"2026-09-22T14:05:00Z"}`. Erros usam o envelope `{"erro":{"codigo":"...","mensagem":"..."}}` com os códigos `PAYLOAD_INVALIDO` (400), `VALOR_TOTAL_DIVERGENTE` (400), `NAO_AUTORIZADO` (401), `VENDA_NAO_ENCONTRADA` (404), `VENDA_JA_CANCELADA` (409), `MOTIVO_OBRIGATORIO` (409), `DADOS_DIVERGENTES` (409), `CONFLITO_CONCORRENCIA` (409) e `ERRO_INTERNO` (500). Exemplos de todos os endpoints estão no documento do contrato.

Pendências de integração: enviar o contrato v1.1 ao lado Vendas e obter aceite ponto a ponto (Seção 5 do contrato). Sem acesso ao código do Vendas, o teste de integração é feito por curl/Postman (RT-04).

## 9. Limitações e dívidas aceitas

Da seção 6 do `.md/SDD.md`:

- **Dívidas aceitas** (motivo: prazo e custo zero):
  - a) A API Key fica em texto simples no `App.config` (contexto de desafio).
  - b) Sem TLS: HTTP em localhost.
  - c) Sem estorno formal (S-13).
  - d) Log estruturado e histórico na tela podem ficar de fora (Tier C); o histórico continua sendo gravado no banco.
  - e) `ClienteId` sem nome do cliente (D-10).
- **Riscos técnicos conhecidos:**
  - RT-01: provider EF6 Firebird imaturo (spike T-01 resultou em GO; ADR-009). Fallback previsto: SQL Server.
  - RT-02: trials DevExpress/FastReport com marca d'água e expiração (30 dias no DevExpress); instalação real ainda não confirmada.
  - RT-04: contrato v1.1 pode não ser aceito pelo Vendas; mudanças só aditivas.
  - RT-05: DLLs Firebird x64; apenas um processo pode abrir o `.fdb`.
  - RT-06: porta ocupada ou bloqueio de firewall no `HttpListener`; porta configurável.
  - RT-09: grid limitado a 5.000 linhas, com aviso.
- **Sem login na tela Desktop** (aplicativo local do operador); `.fdb` sem criptografia (apenas IDs, sem dados pessoais sensíveis).
- Bind padrão em `localhost`; expor em rede só por configuração explícita, com chave e, idealmente, TLS.
- A instalação dos trials não foi feita de fato (seção 3).

## 10. Versões e licenças

Fonte: `PackageReference` de todos os `.csproj` da solução e do spike, mais `docs/licencas.md`. A coluna "Licença" reflete o que o autor da T-54 conhece dos projetos upstream; **a confirmação contra o metadado de cada pacote no nuget.org ainda deve ser feita antes da entrega** (não havia acesso ao NuGet neste ambiente).

### Pacotes NuGet de produção

| Pacote | Versão | Projeto(s) | Licença |
|---|---|---|---|
| EntityFramework | 6.5.1 | Infrastructure | Apache-2.0 |
| EntityFramework.Firebird | 10.1.0 | Infrastructure | Initial Developer's Public License 1.0 (IDPL) |
| FirebirdSql.Data.FirebirdClient | 10.3.4 | Infrastructure, Tests | Initial Developer's Public License 1.0 (IDPL) |
| Microsoft.AspNet.WebApi.Core | 5.3.0 | Api, Desktop (transitivo) | Apache-2.0 |
| Microsoft.AspNet.WebApi.Owin | 5.3.0 | Api | Apache-2.0 |
| Microsoft.Owin.Host.HttpListener | 4.2.2 | Api | Apache-2.0 |
| Microsoft.Owin.Hosting | 4.2.2 | Desktop | Apache-2.0 |
| Autofac | 6.5.0 | Api, Desktop, Tests | MIT |
| Autofac.WebApi2 | 6.1.1 | Api, Desktop | MIT |
| Newtonsoft.Json | 13.0.3 | Api | MIT |
| FastReport.OpenSource | 2023.3.13 | Reports | MIT |
| FastReport.OpenSource.Export.PdfSimple | 2023.3.13 | Reports | MIT |

### Pacotes de teste (não vão para a entrega)

| Pacote | Versão | Licença |
|---|---|---|
| Microsoft.NET.Test.Sdk | 17.11.1 | MIT |
| xunit | 2.9.2 | Apache-2.0 |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0 |
| Moq | 4.20.72 | BSD-3-Clause |

### Componentes fora do NuGet

| Componente | Versão | Licença / condição |
|---|---|---|
| Firebird (motor embarcado, DLLs nativas x64) | 3.0.12.33787 | Initial Developer's Public License / Interbase Public License (livre, sem custo) |
| DevExpress WinForms | **A registrar** (instalador da linha 20xx.x mais recente; versão só conhecida após instalação real) | Trial de 30 dias, sem custo; aviso de trial e marca d'água |
| FastReport .NET Trial | **A registrar** | Avaliação comercial por tempo limitado (duração a confirmar), marca d'água esperada |
| .NET Framework | 4.8 | Componente do Windows / Microsoft |

Observações: o projeto do Desktop ainda não referencia DevExpress (nenhuma tela existe). A versão de cada pacote é a declarada no `.csproj`; pacotes transitivos (dependências de dependências) não estão listados aqui e devem ser incluídos por uma ferramenta de inventário (ex.: `dotnet list package --include-transitive`) na conferência final. O `spikes/T-01-firebird-spike` é descartável e não faz parte da entrega.

## 11. Estrutura do repositório

| Caminho | Conteúdo |
|---|---|
| `src/ERPFinanceiro.Domain` | Entidades e regras de negócio |
| `src/ERPFinanceiro.Application` | Casos de uso, validação, interfaces |
| `src/ERPFinanceiro.Infrastructure` | EF6 + Firebird, repositórios, log, `DbInitializer` |
| `src/ERPFinanceiro.Api` | Web API (OWIN), controllers, erros, correlation id |
| `src/ERPFinanceiro.Reports` | Relatório (FastReport) |
| `src/ERPFinanceiro.Desktop` | Executável WinForms, composition root, host OWIN, `App.config.example` |
| `src/ERPFinanceiro.Tests` | Testes xUnit |
| `database/` | `01-schema.sql`, `02-seed.sql` e evidências |
| `docs/` | `contrato-v1.1.md`, `licencas.md` |
| `spikes/T-01-firebird-spike` | Spike descartável do Firebird embarcado |
| `.md/` | PRD, SDD, UX-SPEC, TASK, GUARDRAILS e ADRs |

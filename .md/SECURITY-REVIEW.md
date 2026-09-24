# SECURITY-REVIEW.md — ERP Financeiro (C#)

> Chapéu DevSecOps do Validador. Auditoria roda **depois** do chapéu QA aprovar
> funcionalmente o mesmo lote (`QA-REPORT.md`: Lote 1 = Aprovado com ressalvas).
> Base: `SDD.md` Seção 7 (quando existir código de produção), `GUARDRAILS.md`
> (G-2, G-6).

## Lote 1 — Spike e preparação

**Veredito: Aprovado, sem débito de segurança.** Nenhum achado alto/crítico,
nenhum achado de baixa/média severidade — este lote é spike descartável +
documentação, sem código de produção, sem API/tela rodando, sem dado real de
cliente/venda.

### `static-security-analysis`

- Escopo real de código: só `spikes/T-01-firebird-spike/FirebirdSpike/Program.cs`
  e `App.config` (projeto `net48`/EF6/Firebird, descartável, fora da solução
  final — `.gitignore` do próprio spike confirma "não faz parte da solução
  final").
- Sem SQL concatenado com entrada externa (todo `CommandText` é DDL fixo/literal
  do próprio roteiro, sem parâmetro de usuário).
- Sem `AutoMapper`/`EF Core`/pacote não oficial: `.csproj` só referencia
  `EntityFramework` 6.5.1, `EntityFramework.Firebird` 10.1.0,
  `FirebirdSql.Data.FirebirdClient` 10.3.4 — todos na lista obrigatória do
  `TASK.md` Seção 1 / `GUARDRAILS.md` G-2.5.
- `PlatformTarget` fixo em `x64` (nunca `AnyCPU`) — conforme G-2.8.

### `security-requirement-validation` / `sensitive-data-exposure-check`

- **Credenciais no `Program.cs`**: `UserID = "sysdba"`, `Password = "masterkey"`
  (linhas 84-85). Verificado: são as credenciais **padrão de fábrica** do modo
  embarcado do Firebird (mesmas usadas em qualquer instalação local/spike),
  não uma credencial real de produção nem reaproveitada de outro sistema.
  Confinadas a um `.fdb` local descartável (`spikes/.../bin/Debug/net48/spike.fdb`,
  ignorado no git via `.gitignore` do spike) que não existe em produção nem é
  publicado. Não há `App.config.example` real do Desktop ainda (T-26, Lote 6,
  não iniciado) para confirmar contaminação — **confirmado que não há
  vazamento para fora do spike**: nenhuma outra referência a `sysdba`/
  `masterkey` existe fora de `spikes/T-01-firebird-spike/` (busca no
  repositório).
- **`App.config` do spike**: contém só `entityFramework`/`DbProviderFactories`
  (configuração de provider EF6/Firebird), **nenhum segredo real** — sem
  `ApiKey`, sem connection string com credencial de produção. Confirma G-6.24
  (`ApiKey`/senha do banco fora do código-fonte) não se aplica ainda a este
  lote porque não há `ApiKey` de verdade neste ponto do projeto (só surge em
  T-26).
- Busca geral por `apikey`/`senha`/`password`/`secret`/`token` em `spikes/` e
  `docs/`: únicas ocorrências são (a) `PublicKeyToken=...` (metadado padrão de
  assembly .NET, não segredo), (b) as credenciais Firebird padrão já tratadas
  acima, (c) menção ao *nome do header* `X-Api-Key` em `docs/contrato-v1.1.md`
  (parte da especificação do contrato, sem valor de chave real). Nenhum
  segredo real exposto.
- **Artefatos binários do spike** (`bin/Debug/net48/*`, incluindo o `.fdb` de
  teste, DLLs nativas do Firebird): confirmado que `spikes/T-01-firebird-spike/
  .gitignore` ignora `bin/`, `obj/` e `*.fdb` — `git status`/`git check-ignore`
  confirmam que nada dessa pasta está rastreado pelo git. Sem risco de publicar
  dado de teste ou binário desnecessário no repositório.

### `compliance-validation`

Não aplicável neste lote (sem dado pessoal/sensível de cliente, sem LGPD em
jogo — spike usa só um valor `VALOR_TOTAL` fictício, sem dado de cliente real).

### `finding-severity-classification`

Nenhum achado de segurança nesta rodada — nada a classificar. O único achado do
lote (referência a `docs/licencas-bin.md` inexistente no comentário/log do
`Program.cs`) é de qualidade documental, já registrado pelo chapéu QA em
`QA-REPORT.md` e tratado como achado simples de QA, não de segurança (não expõe
dado, não é comportamento de runtime relevante a ameaça).

### Requisitos de segurança operacional para o chapéu DevOps

Nada a definir ainda neste lote — infraestrutura real (secrets management,
firewall, hardening) só começa a fazer sentido a partir do Lote 6 (hospedagem
OWIN/`App.config` real, T-25/T-26). Registro para quando `DEPLOY.md`/infra forem
preparados: garantir que `App.config.example` do Desktop (T-26) nunca inclua
`ApiKey`/senha reais (G-6.24), e que a distribuição das DLLs nativas do Firebird
(ver nota de infraestrutura do ADR-009) não inclua o `.fdb` de teste do spike.

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado com relevância estratégica de segurança/compliance neste lote —
nada a sinalizar ao `gestor` além do fechamento de rotina do Gate.

## Fechamento estrutural do lote (checagem do próprio Validador)

1. **Todas as tarefas `Concluída`**: confirmado — T-01, T-02, T-03 (Seção 3,
   Lote 1 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 1**: T-04/T-05 (Lote 2/3)
   dependem de "T-01 (GO)" — ADR-009 registra GO explícito, gate G-2.7 do
   `GUARDRAILS.md` satisfeito, dependência não órfã. T-11/T-12 idem (dependem
   de T-05/T-01, cadeia intacta). Nenhuma tarefa do Lote 1 tem dependência
   externa não resolvida. Sem inconsistência.
3. **Nenhuma tarefa `Bloqueada`**: confirmado (`.md/BLOCKERS.md` não existe —
   nenhuma entrada aberta).
4. **Achados simples/débito baixo-médio → `Refatoração Lote-1`**: aplicável (3
   achados do `QA-REPORT.md`, RL1-01 a RL1-03). Lote **não** é reaberto por
   causa disso — tarefas novas anexadas ao final da Seção 3 do `TASK.md`.
5. **Nota de rotina, sem bloqueio**: `ADR-009` está com `Status: Rascunho do
   Executor, ratificação pendente do Coordenador`. Isso não bloqueia o gate
   G-2.7 (o próprio texto do `GUARDRAILS.md` condiciona ao "GO registrado", que
   já está registrado, com decisão explícita) nem é uma inconsistência que
   exija redesenho — é só um passo de rotina do Coordenador (ratificar o
   rascunho), fora do escopo de correção do Validador. Não vira `Refatoração
   Lote-1` (não é um achado de código/documentação incorreta) nem
   `BLOCKERS.md` (não bloqueia nada); fica registrado aqui como nota para o
   Coordenador na próxima vez que revisar ADRs.

**Conclusão: consistente, mecânico.** Nenhuma inconsistência exige redesenho de
dependência/decomposição. Lote 1 fecha **Validado (com ressalvas)**.

## Lote 2 — Esqueleto e Domínio

> Auditoria roda depois do chapéu QA aprovar funcionalmente (`QA-REPORT.md`:
> Lote 2 = Aprovado, sem ressalvas).

**Veredito: Aprovado, sem débito de segurança.** Nenhum achado alto/crítico,
nenhum achado de baixa/média severidade.

### `static-security-analysis`

- Escopo real de código: `ERPFinanceiro.sln` + 7 `.csproj` (T-04) e
  `src/ERPFinanceiro.Domain/**/*.cs` (T-07…T-10) — Domain puro, sem I/O, sem
  SQL, sem rede, sem reflexão dinâmica.
- Pacotes NuGet referenciados nos 7 `.csproj`: `EntityFramework` 6.5.1,
  `EntityFramework.Firebird` 10.1.0, `FirebirdSql.Data.FirebirdClient` 10.3.4
  (Infrastructure), `Microsoft.AspNet.WebApi.Owin`/`.Core` 5.3.0,
  `Microsoft.Owin.Host.HttpListener` 4.2.2, `Autofac`/`Autofac.WebApi2`,
  `Newtonsoft.Json` (Api), `FastReport.OpenSource` 2023.3.13 (Reports),
  `xunit`/`xunit.runner.visualstudio`/`Moq`/`Microsoft.NET.Test.Sdk` (Tests) —
  todos da lista obrigatória do `TASK.md` Seção 1/`GUARDRAILS.md` G-2.5; nenhum
  pacote proibido (`EF Core`, `AutoMapper`, pacote pago) encontrado.
  `ERPFinanceiro.Domain.csproj` e `ERPFinanceiro.Application.csproj` sem
  `PackageReference` nenhum, como exigido.
- `PlatformTarget=x64` fixo em todos os 7 `.csproj` (`Platforms=x64` também),
  `.sln` mapeia `Any CPU`/`x86` para `x64` — nunca `AnyCPU` real (G-2.8).

### Encapsulamento `ItensEf`/`HistoricoEf` (checagem específica pedida)

- `Venda.ItensEf`/`HistoricoEf` são **`internal`** (não `public`/`protected
  internal`), apontando para os mesmos backing fields privados usados pelas
  propriedades públicas somente-leitura (`Itens`/`Historico`, `IReadOnlyList`).
- `InternalsVisibleTo` declarado só para 2 assemblies em
  `ERPFinanceiro.Domain.csproj` (via `AssemblyAttribute`, não via
  `AssemblyInfo.cs` solto): `ERPFinanceiro.Infrastructure` (consumo real do
  mapeamento EF6, T-11) e `ERPFinanceiro.Tests` (teste de integração de
  concorrência, T-11) — **nenhum outro assembly da solução ganha acesso**
  (confirmado por leitura direta do `.csproj`, única ocorrência do atributo).
  `ERPFinanceiro.Api`, `ERPFinanceiro.Desktop`, `ERPFinanceiro.Reports` não
  estão na lista — não enxergam `ItensEf`/`HistoricoEf`, mesmo em tempo de
  compilação.
- API pública do agregado (`Itens`/`Historico`, `IReadOnlyList<T>`) inalterada
  — nenhum código fora de Infrastructure/Tests consegue mutar a coleção
  diretamente (só via `Quitar()`/`Cancelar()`/fábricas, que controlam a regra
  de negócio). **Sem vazamento de mutabilidade para fora da solução** (nem
  mesmo para a Api, que é a camada de maior exposição a entrada externa).

### `security-requirement-validation` / `sensitive-data-exposure-check`

- Busca por `password`/`senha`/`secret`/`apikey`/`token`/`http://`/`https://`
  em todo `src/ERPFinanceiro.Domain/`: nenhuma ocorrência (só o comentário de
  `VendaHistorico.cs` linha 9, que é texto de documentação sobre a regra de
  append-only, sem dado sensível).
- `VendaJaCanceladaException`/`MotivoObrigatorioException`: mensagens contêm
  no máximo `VendaId` (identificador de negócio, não PII) — nenhuma exceção
  de domínio expõe dado de cliente, credencial ou detalhe de infraestrutura;
  consistente com a regra "detalhe só no log com correlationId" (SDD 7),
  embora o mapeamento para o envelope de erro seja responsabilidade da Api
  (T-28, fora deste lote).
- `VendaHistorico`: construtor **internal**, sem setter público em nenhuma
  propriedade — reforça a regra 8 da Seção 1 ("histórico só por INSERT") já no
  nível de domínio, antes mesmo do repositório (T-13/T-18/T-19) existir.
- `Venda.Id`/`Versao`: `internal set` — só Infrastructure/Tests conseguem
  atribuir PK técnica e número de versão; nenhum outro assembly pode forjar
  esses valores.
- Sem SQL, sem chamada de rede, sem leitura de `App.config`/arquivo neste
  lote — nada a verificar quanto a connection string/ApiKey ainda (T-26,
  Lote 6).

### `compliance-validation`

Não aplicável neste lote (sem dado pessoal de cliente processado — `ClienteId`
é um identificador técnico opaco vindo do Vendas, sem nome/CPF/endereço no
Domain; LGPD segue como "baixo impacto" conforme SDD 7/CTO-REVIEW item 5,
inalterado por este lote).

### `finding-severity-classification`

Nenhum achado de segurança nesta rodada — nada a classificar.

### Requisitos de segurança operacional para o chapéu DevOps

Nada novo além do já registrado no Lote 1 (infraestrutura real só a partir do
Lote 6). Nota adicional deste lote: quando `FinanceiroDbContext` (T-11) for
auditado, confirmar que o mapeamento Fluent não exige tornar `ItensEf`/
`HistoricoEf` `public` (checagem já feita aqui preventivamente, permanece
`internal` em T-11 conforme lido em `Venda.cs`).

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado com relevância estratégica de segurança/compliance neste lote.

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 2

1. **Todas as tarefas `Concluída`**: confirmado — T-04, T-07, T-08, T-09, T-10
   (Seção 3, Lote 2 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 2**: T-04 dep. "T-01 (GO)" —
   satisfeita (Lote 1 Validado). T-07 dep. T-04; T-08 dep. T-07; T-09 dep.
   T-08; T-10 dep. T-08 (paralelizável com T-09) — cadeia intacta, todas
   `Concluída`, nenhuma dependência órfã. Tarefas de outros lotes que dependem
   de tarefas deste lote (T-05 dep. T-01; T-11/T-12 dep. T-04/T-05/T-07) têm
   suas dependências deste lote já satisfeitas — não há nada pendente do Lote
   2 travando essas tarefas.
3. **Nenhuma tarefa `Bloqueada`**: confirmado. T-11 (Lote 3, fora deste lote)
   está `Em andamento` — não `Bloqueada` — por causa do Bloqueio 001
   (`BLOCKERS.md`), decisão já registrada e aceita pelo usuário ("nenhuma ação
   de ambiente agora"); não é uma tarefa deste Lote 2 e não impede o fechamento
   do Lote 2. T-10, citada no Bloqueio 001 como afetada, já está `Concluída`
   neste `TASK.md` (a nota do bloqueio é histórica, da sessão em que a
   execução real ainda não tinha sido obtida; o texto atual da linha T-10
   registra execução real confirmada depois, fora do OneDrive — sem
   contradição, é a evolução cronológica da mesma tarefa).
4. **Achados simples/débito baixo-médio → `Refatoração Lote-2`**: **não
   aplicável** — zero achados neste lote, tanto do chapéu QA quanto do
   DevSecOps. Critério usado para não criar `Refatoração Lote-2`: a skill
   `task-decomposition`/`task-md-drafting` de uso restrito só cabe quando há
   um achado real a corrigir; criar um lote de refatoração vazio só para
   manter simetria com o Lote 1 seria ruído no `TASK.md` sem trabalho real
   por trás. Se um achado simples surgir em lote futuro, o critério passa a
   ser: nomear a tarefa nova como `Refatoração Lote-N` (N = lote de origem do
   achado), um lote de refatoração por lote de origem, não um único lote
   acumulador — mantém rastreabilidade "achado do Lote X -> tarefa em
   `Refatoração Lote-X`" sem ambiguidade de origem.

**Conclusão: consistente, mecânico.** Nenhuma inconsistência exige redesenho de
dependência/decomposição. Lote 2 fecha **Validado** (sem ressalvas).

## Lote 3 — Banco

> Auditoria roda depois do chapéu QA aprovar funcionalmente (`QA-REPORT.md`:
> Lote 3 = Aprovado, sem ressalvas).

**Veredito: Aprovado, sem débito de segurança.** Nenhum achado alto/crítico,
nenhum achado de baixa/média severidade.

### `static-security-analysis`

- Escopo real de código: `database/01-schema.sql`, `database/02-seed.sql`,
  `src/ERPFinanceiro.Infrastructure/Persistencia/{FinanceiroDbContext,
  DbInitializer,ConfiguracaoBancoAppConfig,BancoIndisponivelException}.cs`,
  `src/ERPFinanceiro.Application/Interfaces/IConfiguracaoBanco.cs`.
- DDL 100% estático, sem parâmetro de entrada externa — `01-schema.sql`/
  `02-seed.sql` são scripts fixos aplicados via `FbScript`/
  `FbBatchExecution`; nenhuma concatenação de string de usuário em nenhum
  `CommandText`, nem em `DbInitializer` nem em `FinanceiroDbContext`
  (que delega toda a geração de SQL parametrizado ao EF6, LINQ-to-Entities,
  regra 6 da Seção 1).
- Pacotes: sem pacote novo além dos já auditados no Lote 2 (`EntityFramework`
  6.5.1, `EntityFramework.Firebird` 10.1.0, `FirebirdSql.Data.FirebirdClient`
  10.3.4) — nenhum pacote proibido introduzido por este lote.

### `security-requirement-validation` / `sensitive-data-exposure-check`

- **Credenciais nunca hardcoded em código de produção** (checagem específica
  pedida): `DbInitializer`/`FinanceiroDbContext` recebem credenciais só via
  `IConfiguracaoBanco` (interface em Application) — implementação real
  (`ConfiguracaoBancoAppConfig`) lê de `appSettings`/`App.config`
  (`Banco:CaminhoFdb`/`Banco:Usuario`/`Banco:Senha`), lançando
  `InvalidOperationException` se a chave estiver ausente (nunca um valor
  default hardcoded no código de produção). Busca em todo `src/
  ERPFinanceiro.Infrastructure/` e `src/ERPFinanceiro.Application/` por
  `sysdba`/`masterkey`/senha literal: **nenhuma ocorrência** fora de
  comentário de documentação (`IConfiguracaoBanco.cs` linha 16, "Usuário
  Firebird (SYSDBA por padrão)" é só texto de doc, não valor de código).
- **`sysdba`/`masterkey` só em código de teste e no spike descartável**:
  confirmado por busca — as únicas ocorrências de valores literais
  `"sysdba"`/`"masterkey"` são em
  `src/ERPFinanceiro.Tests/Infrastructure/Persistencia/{DbInitializerTests,
  FinanceiroDbContextTests}.cs` (classes `ConfiguracaoBancoTeste` privadas,
  usadas só para apontar o teste a um `.fdb` de integração descartável, sem
  cenário de produção envolvido) e em `spikes/T-01-firebird-spike/` (já
  auditado no Lote 1, projeto fora da solução final). Nenhuma ocorrência em
  `ERPFinanceiro.Infrastructure`/`ERPFinanceiro.Api`/`ERPFinanceiro.Desktop`
  (código de produção).
- **`App.config.example` do Desktop** (`src/ERPFinanceiro.Desktop/
  App.config.example`, criado em T-04, ainda vigente): `Banco:Usuario` =
  `SYSDBA` (usuário padrão, não segredo) e `Banco:Senha` seria a chave
  esperada — conferido que o exemplo de connection string usa o placeholder
  `COLOQUE_A_SENHA_LOCAL_AQUI`, não uma senha real. Cumpre G-6.24
  (`ApiKey`/senha do banco fora do código-fonte, `App.config.example` sem
  segredo real).
- **`src/ERPFinanceiro.Tests/App.config`** (real, versionado): só registra
  `entityFramework`/`DbProviderFactories` (configuração de provider, não
  connection string com credencial) — nenhum segredo.
- Busca geral por `apikey`/`secret`/`token` nos 4 artefatos deste lote:
  nenhuma ocorrência — `ApiKey` ainda não existe neste ponto do projeto
  (surge em T-26/T-35, Lotes 6/8).
- Mensagens de exceção (`BancoIndisponivelException`) citam só o **caminho
  do `.fdb`** (informação operacional, não dado de cliente/segredo) —
  consistente com a regra "detalhe ao cliente nunca com stack
  trace/mensagem de exceção crua" (regra 7 da Seção 1); o consumidor final
  dessas mensagens (T-46, tela de erro) ainda não existe, fora do escopo
  deste lote, mas a mensagem em si já está adequada para log/exibição.
- **Dados sensíveis em `02-seed.sql`**: `CLI-001`/`CLI-002`/`PROD-001`…
  `PROD-004` são identificadores fictícios opacos, sem nome/CPF/e-mail/
  endereço — nenhum dado pessoal real, consistente com o Domain (T-07,
  Lote 2, já auditado: `ClienteId` é identificador técnico opaco).

### Encapsulamento `ItensEf`/`HistoricoEf` (checkpoint do Lote 2, revalidado em uso real por T-11)

- `FinanceiroDbContext.MapearVenda` mapeia `venda.HasMany(v => v.ItensEf)`/
  `HasMany(v => v.HistoricoEf)` — usa exatamente as propriedades `internal`
  já auditadas no Lote 2, sem promovê-las a `public` nem expor setter novo
  para consumir o mapeamento. `venda.Ignore(v => v.Itens)`/
  `Ignore(v => v.Historico)` confirma que a API pública (`IReadOnlyList`)
  continua fora do modelo EF6 — nenhuma regressão de visibilidade
  introduzida por T-11.

### `compliance-validation`

Não aplicável neste lote — mesmo enquadramento do Lote 2 (SDD 7/CTO-REVIEW:
`ClienteId` é identificador técnico opaco, sem PII no banco). `02-seed.sql`
não introduz dado real de cliente (`CLI-001`/`CLI-002` são fictícios).

### `finding-severity-classification`

Nenhum achado de segurança nesta rodada — nada a classificar.

### Requisitos de segurança operacional para o chapéu DevOps

- Quando a infraestrutura/CI-CD forem preparados (T-53/deploy): garantir
  que o `.fdb` de demonstração empacotado em T-53 **não** seja o mesmo
  arquivo usado nos testes de integração deste lote (`T11_*`/`T12_*.fdb`,
  gerados com GUID e apagados no `Dispose()` de cada teste — confirmado que
  não ficam presos em disco fora do diretório de teste).
- `Banco:Usuario`/`Banco:Senha` do `App.config` real do Desktop (T-26) devem
  ser tratados como segredo local de máquina (fora do git, já garantido pelo
  `.gitignore` da raiz — confirmado em T-04) — nenhuma ação nova necessária
  agora, só reforço do requisito já vigente desde o Lote 1/2.
- Backup/hardening do arquivo `.fdb` de produção (permissões de sistema de
  arquivos, backup do banco para T-53) fica como requisito operacional para
  quando o chapéu DevOps preparar o pacote de entrega — nada a bloquear
  agora, só registro para não esquecer no Lote 13.

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado com relevância estratégica de segurança/compliance neste lote.

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 3

1. **Todas as tarefas `Concluída`**: confirmado — T-05, T-06, T-11, T-12
   (Seção 3, Lote 3 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 3**: T-05 dep. "T-01 (GO)" —
   satisfeito (Lote 1 Validado); T-06 dep. T-05; T-11 dep. T-05, T-07; T-12
   dep. T-04, T-05 — todas satisfeitas, cadeia intacta. Tarefas de lotes
   futuros que dependem de tarefas deste lote (T-14 dep. T-11/T-13; T-15 dep.
   T-14; T-18/T-19/T-22/T-23/T-24 dep. transitivamente de T-11/T-12; T-26 dep.
   T-14/T-15/T-21/T-25; T-42/T-44 dep. ADR-007, já satisfeito por T-05/T-11;
   T-46 dep. T-12) têm suas dependências deste Lote 3 já satisfeitas —
   **T-14, T-22 e demais tarefas do Lote 4/5 que dependiam de T-11/T-12
   ficam corretamente liberadas** (nenhuma continua referenciada como
   bloqueada por T-11/T-12 em nenhum lugar do `TASK.md`; conferido por
   busca — as linhas de dependência da Seção 4.1/4.3 não têm nenhuma nota
   residual apontando Lote 3 como pendente).
3. **Nenhuma tarefa `Bloqueada`**: confirmado. `BLOCKERS.md` (Bloqueio 001)
   está "Resolvido (parcialmente)" — não é mais um bloqueio de tarefa, é uma
   nota de contorno operacional para testes de integração futuros nesta
   máquina (copiar `bin/Debug/net48` para fora do OneDrive antes de
   `dotnet vstest`); não impede o fechamento do Lote 3 nem de nenhuma tarefa
   futura, só documenta um passo do processo de execução de testes.
4. **Achados simples/débito baixo-médio → `Refatoração Lote-3`**: **não
   aplicável** — zero achados neste lote, tanto do chapéu QA quanto do
   DevSecOps (a observação informativa sobre `FK CASCADE`/trigger de
   append-only em `FIN_VENDA_HISTORICO`, registrada no `QA-REPORT.md`, não é
   um achado corretivo — é comportamento correto sem caminho de código que o
   exercite; não gera tarefa). `Refatoração Lote-3` **não é criada**, mesmo
   critério já usado no Lote 2 (só criar quando houver achado real a
   corrigir).

**Conclusão: consistente, mecânico.** Nenhuma inconsistência exige redesenho
de dependência/decomposição. Lote 3 fecha **Validado** (sem ressalvas).

## Lote 4 — Persistência e base da Application

> Auditoria roda depois do chapéu QA aprovar funcionalmente (`QA-REPORT.md`:
> Lote 4 = Aprovado, sem ressalvas de QA quanto ao critério de aceite
> literal; 2 achados de robustez viram `Refatoração Lote-4`).

**Veredito: Aprovado com débito registrado (severidade baixa).** Nenhum
achado alto/crítico, nenhum achado de compliance obrigatório em aberto. 1
achado de baixa severidade (tradução de conflito de concorrência dependente
de mensagem em inglês) — débito registrado, não bloqueia deploy.

### `static-security-analysis`

- Escopo real de código: `src/ERPFinanceiro.Application/{Interfaces,Commands,
  Consultas/FiltroVendas.cs,Exceptions/ConcorrenciaException.cs,Servicos/
  ExecutorComRetry.cs,Validacao/*}.cs`,
  `src/ERPFinanceiro.Infrastructure/{Persistencia/{VendaRepository,
  UnitOfWork}.cs,TraceLogger.cs}`.
- Nenhum pacote NuGet novo introduzido por este lote além dos já auditados
  (Lotes 2/3) — `ERPFinanceiro.Application.csproj` continua sem
  `PackageReference` (só `ProjectReference` a `Domain`).
- Sem SQL concatenado com entrada externa: `VendaRepository` só LINQ/EF
  (`.Include`/`.Where`/`.OrderByDescending`/`.Take`, todos parametrizados pelo
  provider), `UnitOfWork` só chama `SaveChanges()` — confirmado por leitura
  linha a linha dos dois arquivos.
- `PlatformTarget=x64` inalterado, nenhum `.csproj` novo neste lote.

### `security-requirement-validation` / `sensitive-data-exposure-check`

- **Encapsulamento `ItensEf`/`HistoricoEf` (checagem específica pedida pelo
  ponto de atenção 3 do escopo desta rodada)**: confirmado que
  `ERPFinanceiro.Domain.csproj` continua com exatamente os mesmos 2
  `InternalsVisibleTo` já auditados no Lote 2 (`ERPFinanceiro.Infrastructure`,
  `ERPFinanceiro.Tests`) — **nenhuma mudança nesta rodada**. `VendaRepository`
  (T-14) usa `Include("ItensEf")`/`Include("HistoricoEf")` só por nome de
  string (via o overload de `Include` baseado em `string`, não em expressão
  compile-time), o que **não exige elevar a visibilidade** das propriedades —
  continuam `internal`, sem vazamento para `Api`/`Desktop`/`Reports`. Achado
  do Executor **não procede** (não há vazamento nesta rodada).
- **Mensagens de exceção não expõem segredo/dado sensível**:
  `ConcorrenciaException` (mensagens fixas, sem interpolar dado de cliente) e
  `BancoIndisponivelException` (já auditada no Lote 3) — nenhuma mensagem
  inclui connection string, senha ou `ApiKey`. `UnitOfWork` captura
  `FbException`/`DbUpdateException` só para decidir o tipo a lançar, nunca
  repassa a mensagem original da exceção do provider para fora do assembly
  (a `ConcorrenciaException` tem sua própria mensagem fixa em português,
  guardando a exceção original só como `InnerException` — que fica só no log
  via `IAppLogger`, nunca no envelope de erro ao cliente, T-28, regra 7 da
  Seção 1/SDD 7 "Erros").
- **`TraceLogger` (T-21) não inspeciona/mascara conteúdo**: confirmado que
  `Registrar(mensagem, correlationId)` grava a mensagem tal como recebida, sem
  qualquer lógica de redação — a disciplina "nunca logar `ApiKey`" (SDD 7
  "Segredos/logs", G-6.24) é responsabilidade de quem chama `Registrar`
  (serviços de Application ainda não implementados, Lote 5+). Não é uma
  falha deste lote: nenhum chamador real de `IAppLogger` existe ainda para
  auditar quanto a esse ponto especificamente — revalidar quando T-18/T-19
  (Lote 5) chamarem o logger de fato.
- **Ponto de atenção do Executor avaliado (T-15, ver detalhamento completo no
  `QA-REPORT.md`)**: a tradução de violação de `UNIQUE(VENDA_ID)` depende de
  casar a mensagem em inglês da `FbException` mais o nome da constraint
  (`UQ_FIN_VENDA_VENDA_ID`). **Confirmado por reflexão direta contra o
  assembly `FirebirdSql.Data.FirebirdClient` 10.3.4** (não apenas aceito por
  afirmação do Executor): `FirebirdSql.Data.Common.IscCodes` não expõe
  nenhuma constante pública nomeada para "unique key violation" — a alegação
  do Executor procede tecnicamente. Risco de segurança/integridade real, mas
  **baixo**: (a) não é uma vulnerabilidade explorável por terceiro (não há
  caminho de entrada externa controlando o locale do servidor Firebird
  embarcado, que roda local, mesma máquina, mesmo idioma do ambiente de
  build/deploy do time); (b) o pior cenário de falha é **degradação
  silenciosa da regra de negócio de concorrência** (upsert RN-06 do Lote 5
  aceitaria uma segunda venda duplicada como erro genérico 500 em vez de 409
  `CONFLITO_CONCORRENCIA`, sem retry automático de T-16) — não é exposição de
  dado nem escalonamento de privilégio; (c) já existe alternativa mais
  robusta e locale-independente pronta para uso (`FbException.ErrorCode`/
  `SQLSTATE`, ambos numéricos), o que torna a correção de baixo esforço.
  **Classificação: severidade baixa, débito registrado** — mesma tarefa já
  criada pelo chapéu QA em `Refatoração Lote-4` (RL4-01), sem necessidade de
  duplicar; prazo sugerido: antes de T-38/T-39 (integração real com o Vendas,
  Lote 8), quando um cenário de duplicidade real passaria a ter consequência
  observável pelo sistema externo.

### `compliance-validation`

Não aplicável neste lote — mesmo enquadramento dos Lotes 2/3 (`ClienteId`/
`VendaId`/`ProdutoId` seguem identificadores técnicos opacos, sem PII; nenhum
dado pessoal novo introduzido pela camada de persistência/Application deste
lote).

### `finding-severity-classification`

| Achado | Severidade | Bloqueia deploy? | Encaminhamento |
|---|---|---|---|
| T-15: tradução de conflito por mensagem em inglês (sem uso de `ErrorCode`/`SQLSTATE`) | Baixa | Não | Débito registrado, `Refatoração Lote-4` (RL4-01), prazo antes de T-38/T-39 |
| Ausência real de `xunit.runner.json` (nota de T-23 desalinhada do repositório) | N/A — não é achado de segurança, é achado de infraestrutura de teste (QA) | Não | Já registrado pelo chapéu QA (`Refatoração Lote-4`, RL4-02); citado aqui só para não duplicar análise, sem reclassificação de severidade de segurança |

### Requisitos de segurança operacional para o chapéu DevOps

- Quando o pipeline de CI for configurado (Lote 6+): a suíte de testes de
  integração real (T-14/T-15/T-20 em diante) precisa rodar com paralelização
  de coleção do xUnit **desligada** (ver achado RL4-02) para não produzir
  falso-negativo intermitente por `AccessViolationException` do Firebird
  Embedded nativo sob concorrência — registrar isso como requisito do
  pipeline, não só do ambiente de desenvolvimento local.
- Nenhum requisito novo de secrets/rede/hardening além do já registrado nos
  Lotes 1-3 (infraestrutura real só a partir do Lote 6).

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado com relevância estratégica de segurança/compliance neste lote
— o débito de severidade baixa (RL4-01) é uma decisão técnica dentro da
autoridade do Validador (não é decisão de negócio/compliance), registrada
aqui e em `Refatoração Lote-4` conforme o guardrail de "nunca bloquear por
severidade baixa/média sem oferecer aprovação condicional".

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 4

1. **Todas as tarefas `Concluída`**: confirmado — T-13, T-14, T-15, T-16,
   T-17, T-21 (Seção 3, Lote 4 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 4**: T-13 dep. T-07 (Lote 2,
   Validado); T-14 dep. T-11 (Lote 3, Validado)/T-13; T-15 dep. T-14; T-16
   dep. T-15; T-17 dep. T-13; T-21 dep. T-13 — cadeia intacta, todas
   `Concluída`, nenhuma dependência órfã. Tarefas de lotes futuros que
   dependem deste lote: T-18 dep. T-08, T-14, T-15, T-16, T-17, T-21 — todas
   satisfeitas, **T-18 corretamente liberada**; T-19 dep. T-09, T-10, T-14,
   T-15, T-16 — todas satisfeitas, **T-19 corretamente liberada**; T-20 dep.
   T-18, T-19 (ainda `A fazer`) — **corretamente não liberada ainda**, sem
   inconsistência (dependência de tarefas do próprio Lote 5, não deste). T-22/
   T-23/T-24 (Lote 5) já estavam `Concluída` antes desta rodada (executadas
   em paralelo) e suas dependências deste Lote 4 (T-11/T-13/T-14, todas
   `Concluída`) seguem satisfeitas — sem regressão introduzida por esta
   validação.
3. **Nenhuma tarefa `Bloqueada`**: confirmado (`BLOCKERS.md` só tem o
   Bloqueio 001, já "Resolvido (parcialmente)", nota operacional de ambiente,
   não bloqueio de tarefa).
4. **Achados simples/débito baixo-médio → `Refatoração Lote-4`**: aplicável —
   2 achados (RL4-01 do chapéu DevSecOps/QA sobre T-15; RL4-02 do chapéu QA
   sobre infraestrutura de teste/T-23). Tarefas criadas ao final da Seção 3
   do `TASK.md`, lote não reaberto.

**Conclusão: consistente, sem inconsistência que exija redesenho de
dependência/decomposição.** O achado RL4-02 (flakiness de teste) é um risco
real para a **execução confiável** de T-20 (Lote 5) se não corrigido antes,
mas não é uma inconsistência de dependência/decomposição — é uma correção de
infraestrutura de teste, de baixo esforço, já com solução validada por mim
nesta sessão (ver `QA-REPORT.md`). Não exige reabrir o `coordenador`. Lote 4
fecha **Validado (com ressalvas)**.

## Lote 5 — Serviços de negócio

**Veredito: Aprovado, sem débito de segurança bloqueante.** Auditoria
disparada só após o chapéu QA aprovar funcionalmente este lote
(`QA-REPORT.md`: Lote 5 = Aprovado com ressalvas — a única ressalva é de
correção funcional sob concorrência, não de segurança, ver abaixo). Base:
`SDD.md` Seção 7, `GUARDRAILS.md` G-4/G-6.

### `static-security-analysis`

- Escopo real de código: `QuitacaoService.cs`, `CancelamentoService.cs`,
  `ExecutorComRetry.cs` (já auditado no Lote 4, sem mudança), `HealthService.cs`,
  `ConsultaService.cs` (T-23/T-24), `ResultadoQuitacao.cs`,
  `ResultadoCancelamento.cs`, `DadosDivergentesException.cs`,
  `Application.Exceptions.ValidacaoException.cs`,
  `ConcorrenciaIdempotenciaTests.cs`.
- Sem SQL concatenado em nenhum artefato: `QuitacaoService`/`CancelamentoService`
  operam só via `IVendaRepository`/`IUnitOfWork` (LINQ/EF, T-14/T-15, já
  auditados); `HealthService` usa `FbCommand` com `CommandText` fixo
  (`"SELECT 1 FROM RDB$DATABASE"`, sem parâmetro de entrada externa) — nenhuma
  interpolação de dado de usuário na query.
- Sem `AutoMapper`/`EF Core`/pacote não oficial introduzido neste lote (nenhum
  `PackageReference` novo em `Application.csproj`/`Infrastructure.csproj`
  além dos já auditados nos Lotes 3/4).
- `DadosDivergentesException`/`Application.Exceptions.ValidacaoException`
  carregam só `VendaId`/`ErroValidacao` (identificadores técnicos, sem PII) na
  mensagem — confirmado por leitura direta das duas classes; nenhuma delas
  embute payload bruto do cliente na mensagem de exceção.

### `security-requirement-validation`

- **`ApiKey` nunca logada (G-6.24)**: nenhum dos serviços deste lote
  (`QuitacaoService`, `CancelamentoService`, `HealthService`, `ConsultaService`)
  manipula `ApiKey` — a autenticação (`X-Api-Key`) só entra no Lote 8 (T-35).
  Confirmado por busca no código deste lote: nenhuma referência a
  `ApiKey`/`apikey`. `CancelamentoService._logger?.Registrar(...)` grava só
  `VendaId`/`Status` resultante — nenhum dado sensível, nenhum segredo.
- **Credenciais de banco (SYSDBA/senha)**: `HealthService`/
  `ConcorrenciaIdempotenciaTests` recebem `IConfiguracaoBanco` injetado (mesmo
  padrão já auditado no Lote 3 para `DbInitializer`) — sem hardcode de
  produção. `ConcorrenciaIdempotenciaTests.ConfiguracaoBancoTeste` usa
  `sysdba`/`masterkey` (credenciais padrão de fábrica do Firebird embarcado,
  mesmo enquadramento já aceito no Lote 1 para o spike) — confinadas à classe
  de teste, não vazam para código de produção; confirmado por busca: nenhuma
  outra referência a essas credenciais fora de `spikes/`/`Tests`.
- **`DbContext` por operação (RT-08/regra 5 da Seção 1)**: confirmado que
  `QuitacaoService`/`CancelamentoService` recebem `IVendaRepository`/
  `IUnitOfWork` já construídos (não criam conexão própria) — a política de
  "um `DbContext` por operação/requisição" é responsabilidade de quem
  compõe (T-26, ainda não implementada); `ConcorrenciaIdempotenciaTests`
  replica corretamente esse padrão no harness (um `FinanceiroDbContext` por
  chamada isolada a `Executar`/`Cancelar`). Ver achado funcional detalhado
  abaixo (RL5-01) — é um achado de **correção sob concorrência**, não uma
  violação de segurança (não há exposição de dado, escalonamento de
  privilégio, nem bypass de controle de acesso envolvido; o pior efeito
  observável é um 409 incorreto em vez de 200, sempre dentro do envelope de
  erro já sanitizado por T-28).
- **`IHealthService`/`HealthService` (P-5, SDD 2.6)**: `ObterStatus()` nunca
  lança e nunca inclui stack trace na mensagem de falha (`ex.Message` apenas)
  — confirmado no código. **Nota de atenção registrada para T-36 (endpoint
  público `GET /api/health`, ainda `A fazer`, Lote 8)**: `ResultadoHealth.
  Mensagem` (em falha) pode conter detalhe de infraestrutura (ex.: caminho de
  arquivo do `.fdb`, mensagem nativa do driver Firebird) — aceitável para o
  consumo local da tela (T-45, diagnóstico com "Copiar", D-04/ADR-004, canal
  confiável/mesma máquina do operador), mas **não deve ser ecoado
  literalmente no corpo do endpoint público** `GET /api/health`, que é isento
  de `X-Api-Key` (SDD 7) e portanto acessível sem autenticação — o corpo
  público correto já está definido em SDD 2.4/2.6 como
  `{status:"degradado",banco:"falha"}`, sem a mensagem da exceção. **Não é um
  achado deste Lote 5** (T-36 não existe ainda, nada foi exposto
  publicamente) — é um requisito a ser conferido explicitamente quando T-36
  for auditada (Lote 8).

### `compliance-validation`

Não aplicável neste lote — mesmo enquadramento dos Lotes 2/3/4 (`ClienteId`/
`VendaId`/`ProdutoId` seguem identificadores técnicos opacos, sem PII; nenhum
dado pessoal novo introduzido pelos serviços deste lote). LGPD segue baixo
impacto (CTO-REVIEW item 5, já registrado).

### `sensitive-data-exposure-check`

- Nenhum log/exceção deste lote expõe `ApiKey`, senha ou payload bruto não
  sanitizado — confirmado por leitura de `CancelamentoService.Registrar`
  (mensagem fixa com `VendaId`/`Status`), `DadosDivergentesException`/
  `Application.Exceptions.ValidacaoException` (só `VendaId`/`ErroValidacao`).
- `HealthService`/`ResultadoHealth`: ver nota de atenção para T-36 acima —
  hoje sem exposição real (consumo só em processo/local, tela T-45, ainda não
  implementada, e testes).
- Testes de integração real (`QuitacaoServiceTests`, `CancelamentoServiceTests`,
  `HealthServiceTests`, `ConsultaServiceTests`, `ConsultaServiceListarTests`,
  `ConcorrenciaIdempotenciaTests`) usam só dados sintéticos (`CLI-T20`,
  `PROD-T20-A` etc.) — nenhum dado real de cliente.

### `finding-severity-classification`

| Achado | Severidade | Bloqueia deploy? | Encaminhamento |
|---|---|---|---|
| Risco de entidade zumbi (`DbContext` reaproveitado entre tentativas de retry) causando falso 409 na corrida de criação concorrente — `QuitacaoService`/`CancelamentoService`, via `ExecutorComRetry` | N/A — não é achado de segurança, é achado de **correção funcional sob concorrência** (ver `QA-REPORT.md`, T-18/T-19/T-20) | Não | Já registrado pelo chapéu QA como `RL5-01` (`Refatoração Lote-5`); citado aqui só para registrar que não há dimensão de segurança/exposição de dado envolvida — sem reclassificação |
| `ResultadoHealth.Mensagem` pode carregar detalhe de infraestrutura se ecoada sem filtro por um futuro endpoint público (T-36, ainda não implementada) | Baixa (preventiva, sem exposição real hoje) | Não | Nota de atenção para a auditoria de T-36 (Lote 8) — sem tarefa em `Refatoração Lote-5` agora, porque T-36 nem existe; será conferido quando T-36 for auditada |

### Requisitos de segurança operacional para o chapéu DevOps

- Nenhum requisito novo além do já registrado no Lote 4 (paralelização de
  coleção do xUnit desligada no pipeline de CI, RL4-02).
- Quando T-26 (composition root, Lote 6) compuser o escopo real de
  `DbContext`/Autofac: garantir que a correção de `RL5-01` (fábrica de
  `DbContext` por tentativa de retry) não reabra a política de "um
  `DbContext` por operação/requisição, nunca compartilhado entre API e UI"
  (RT-08/regra 5) — o novo `DbContext` por tentativa deve continuar
  encapsulado dentro do escopo de uma única operação/requisição, não
  compartilhado entre requisições diferentes.

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado com relevância estratégica de segurança/compliance neste lote.
O achado RL5-01 (correção funcional sob concorrência, não segurança) é uma
decisão técnica dentro da autoridade do Validador, registrada em
`Refatoração Lote-5` com prioridade alta e prazo definido — não é decisão de
negócio/compliance, não é sinalizada ao Gestor por este canal (ver
`QA-REPORT.md` para o encaminhamento completo).

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 5

1. **Todas as tarefas `Concluída`**: confirmado — T-18, T-19, T-20, T-22,
   T-23, T-24 (Seção 3, Lote 5 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 5**: T-18 dep. T-08 (Lote 2)/
   T-14/T-15/T-16/T-17/T-21 (Lote 4) — todas `Concluída`/Validadas; T-19 dep.
   T-09/T-10 (Lote 2)/T-14/T-15/T-16 (Lote 4) — todas satisfeitas; T-20 dep.
   T-18/T-19 (mesmo lote, ambas `Concluída`) — cadeia intacta; T-22 dep.
   T-11 (Lote 3)/T-13 (Lote 4) — satisfeitas; T-23/T-24 dep. T-13/T-14 (Lote
   4) — satisfeitas. Tarefas de lotes futuros que dependem deste lote: T-30
   dep. T-18 (entre outras, Lote 7, ainda não iniciado — dependência íntegra,
   não órfã); T-31 dep. T-24; T-32 dep. T-19; T-36 dep. T-22; T-42/T-48 dep.
   T-23; T-44 dep. T-24 — todas apontam corretamente para tarefas
   `Concluída` deste lote, sem referência quebrada.
3. **Nenhuma tarefa `Bloqueada`**: confirmado (`BLOCKERS.md` só tem o
   Bloqueio 001, já "Resolvido (parcialmente)", nota operacional de
   ambiente, não bloqueio de tarefa deste lote).
4. **Achados simples/débito baixo-médio → `Refatoração Lote-5`**: aplicável
   — 1 achado (RL5-01, risco de entidade zumbi sob concorrência,
   severidade/prioridade alta mas não crítica — não reproduzida em >= 26
   corridas, não bloqueia nenhuma tarefa deste lote). Tarefa criada ao final
   da Seção 3 do `TASK.md`, lote não reaberto.

**Conclusão: consistente, sem inconsistência que exija redesenho de
dependência/decomposição.** O achado RL5-01 é um risco real de correção sob
concorrência, com causa raiz e correção já identificadas pelo próprio
Executor (fábrica de `DbContext` por tentativa de retry, a ser resolvida no
momento certo — durante/depois de T-26, composition root, que ainda não
começou) — não exige redesenho de dependência/decomposição nem decisão de
composição já tomada para reabrir (T-26 ainda está `A fazer`, é só a próxima
tarefa natural do caminho). Não exige reabrir o `coordenador`. Lote 5 fecha
**Validado (com ressalvas)**.

## Lote 6 — API base e hospedagem

**Veredito: Aprovado, sem débito de segurança.** Auditoria disparada só após
o chapéu QA aprovar funcionalmente este lote (`QA-REPORT.md`: Lote 6 =
Aprovado, sem ressalvas). Base: `SDD.md` Seção 7, `GUARDRAILS.md` G-4/G-6,
ADR-004.

### `static-security-analysis`

- Escopo real de código: `Startup.cs`, `CorrelationContext.cs`,
  `CorrelationIdHandler.cs`, `Erros/{GlobalExceptionHandler,
  ExceptionParaRespostaMapper,ErroEnvelopeDto}.cs`, `CompositionRoot.cs`,
  `ApiHost.cs`, `FabricaEscopoOperacaoEf.cs` (Infrastructure), e as edições
  em `QuitacaoService.cs`/`CancelamentoService.cs` que resolvem RL5-01.
- Sem SQL concatenado em nenhum artefato deste lote: `FabricaEscopoOperacaoEf`
  monta a connection string via `FbConnectionStringBuilder` (sem
  interpolação de string), a única query fixa do lote continua sendo a de
  `HealthService` (já auditada no Lote 5, sem mudança).
- `GlobalExceptionHandler`/`ExceptionParaRespostaMapper` conferidos linha a
  linha: o envelope ao cliente (`ErroEnvelopeDto`) nunca carrega
  `original.Message`/`StackTrace` para a categoria `default` (500
  `ERRO_INTERNO`) — só a mensagem genérica fixa
  (`"Ocorreu um erro interno inesperado..."`). O detalhe completo (tipo +
  mensagem + stack trace) vai exclusivamente para `DetalheLog`, gravado em
  `IAppLogger`, nunca na resposta HTTP — confirmado por leitura, e reforçado
  pelo teste `Mapear_ExcecaoNaoMapeada_Retorna500ErroInternoSemMensagemOriginalNoEnvelope`
  (`Assert.DoesNotContain`), reexecutado por mim (verde). Regra 7 da Seção 1
  cumprida.
- Para as demais categorias mapeadas (`VendaJaCanceladaException`,
  `MotivoObrigatorioException`, `VendaNaoEncontradaException`,
  `ValidacaoException`, `DadosDivergentesException`), o envelope usa
  `excecao.Message` diretamente — avaliado e aceito: são exceções de domínio/
  aplicação com mensagens estáveis e não sensíveis (ex.: "venda já
  cancelada", "motivo obrigatório"), nenhuma delas concatena dado bruto
  não sanitizado do payload do cliente na mensagem (confirmado por leitura
  das 5 classes) — comportamento correto de erro de negócio (400/404/409),
  distinto de erro interno (500), que é o único caso exigido a ser
  genérico pela regra 7.
- Nenhum `PackageReference` fora da lista obrigatória da Seção 1 (`Autofac`/
  `Autofac.WebApi2`, `Microsoft.AspNet.WebApi.Owin`/`.Core`,
  `Microsoft.Owin.Host.HttpListener`, `Microsoft.Owin.Hosting`,
  `Newtonsoft.Json`) introduzido neste lote — confirmado nos `.csproj` de
  `Api`/`Desktop`.

### `security-requirement-validation`

- **`ApiKey`/credenciais nunca logadas ou hardcoded (G-6.24)**: nenhum
  artefato deste lote manipula `ApiKey` diretamente (autenticação só entra
  no Lote 8, T-35) — confirmado por busca no código deste lote: nenhuma
  referência a `ApiKey`/`apikey` fora do `App.config.example`. Credenciais
  de banco (`Banco:Usuario`/`Banco:Senha`) só chegam via
  `IConfiguracaoBanco` injetado (`CompositionRoot`/`FabricaEscopoOperacaoEf`),
  nunca hardcoded em código de produção — mesmo padrão já auditado nos
  Lotes 3/5.
- **`App.config.example` sem segredo real**: conferido linha a linha —
  `Api:ApiKey` = `"COLOQUE_UMA_CHAVE_LOCAL_AQUI"`, `Banco:Senha` =
  `"COLOQUE_A_SENHA_LOCAL_AQUI"` (2 ocorrências, `appSettings` e
  `connectionStrings`), nenhum valor real. Confirmei por mim mesmo (não só
  pela nota do Executor) que `.gitignore` (linha 17) cobre `App.config`
  real com exceção explícita a `App.config.example`
  (`git check-ignore -v src/ERPFinanceiro.Desktop/App.config` confirma o
  match) e que nenhum `App.config` real existe hoje no worktree.
- **`DbContext` por operação/requisição (RT-08/regra 5 da Seção 1)**:
  auditoria completa de `CompositionRoot.cs` — `FinanceiroDbContext`/
  `IVendaRepository`/`IVendaConsultaLeitura`/`IUnitOfWork` registrados
  `InstancePerLifetimeScope` (nunca `SingleInstance`); confirmado que
  nenhum registro deste composition root usa `SingleInstance` para algo que
  carregue um `DbContext` — as únicas `SingleInstance` são
  `IConfiguracaoBanco`, `IClock`, `IAppLogger`, `IHealthService` (todos sem
  estado de `DbContext`, revisão já feita nos Lotes 3/5) e
  `IFabricaEscopoOperacao` (não guarda nenhum `DbContext`, só fabrica um
  novo por chamada — confirmado em `FabricaEscopoOperacaoEf.Abrir()`, que
  cria `FbConnection`+`FinanceiroDbContext` novos a cada invocação, com
  `Pooling = false`). **Este lote fecha RL5-01** (ver seção própria abaixo)
  sem reabrir a política RT-08: o `DbContext` por tentativa de retry
  continua encapsulado dentro do escopo de uma única
  operação/requisição — nunca compartilhado entre requisições diferentes,
  exatamente o requisito operacional que o Lote 5 tinha deixado registrado
  para este lote (ver `### Requisitos de segurança operacional` do Lote 5
  acima).
- **`GET /api/health` (nota herdada do Lote 5)**: endpoint ainda não existe
  (T-36, Lote 8) — nada a auditar aqui, nota mantida para quando T-36 for
  implementada.
- **`Startup.Container` estático (ponto de atenção pedido nesta rodada)**:
  avaliado sob a ótica de segurança (não só design) — não há exposição de
  dado nem bypass de controle de acesso possível através desse campo: ele
  carrega uma referência a um `IContainer` já montado (nenhum segredo em
  si), só lido dentro do próprio processo (`ConfigurarAutofac`), nunca
  serializado/exposto externamente. O risco teórico seria contaminação
  cruzada entre hosts/testes concorrentes atribuindo containers diferentes
  ao mesmo campo estático — descartado na prática pela garantia de processo
  único (ADR-004) em produção e pela suíte de teste rodando sequencialmente
  (`xunit.runner.json`/RL4-02, `parallelizeTestCollections: false`),
  confirmado por mim rodando a suíte 2x seguidas sem sintoma de
  contaminação. **Não é um achado de segurança** — ver avaliação completa
  (incluindo a perspectiva de design/manutenibilidade) no `QA-REPORT.md`,
  T-27.

### `compliance-validation`

Não aplicável neste lote — mesmo enquadramento dos Lotes 2-5 (`ClienteId`/
`VendaId`/`ProdutoId` seguem identificadores técnicos opacos, sem PII; nenhum
dado pessoal novo introduzido pelos artefatos deste lote, que são
infraestrutura de hospedagem/composição, não dados de negócio). LGPD segue
baixo impacto (CTO-REVIEW item 5, já registrado).

### `sensitive-data-exposure-check`

- Nenhum log/exceção/resposta HTTP deste lote expõe `ApiKey`, senha ou
  payload bruto não sanitizado — confirmado por leitura de
  `GlobalExceptionHandler`/`ExceptionParaRespostaMapper` (envelope 500
  sempre genérico) e `CorrelationIdHandler` (só ecoa um GUID/valor de
  header, nunca um dado de negócio).
- `CorrelationContext`/`CorrelationIdHandler`: o `correlationId` é um GUID
  gerado no servidor ou um valor de header ecoado — não é PII, não carrega
  dado sensível, seguro para aparecer em log/header de resposta (é
  literalmente o mecanismo de correlação de log, regra 7 da Seção 1).
- Testes deste lote (`StartupTests`, `CompositionRootTests`, `ApiHostTests`,
  `GlobalExceptionHandlerTests`, `ExceptionParaRespostaMapperTests`,
  `CorrelationIdHandlerTests`) usam só dados sintéticos/técnicos — nenhum
  dado real de cliente, nenhuma credencial real (portas efêmeras via
  `TcpListener`, containers Autofac vazios ou reais mas locais).

### `finding-severity-classification`

| Achado | Severidade | Bloqueia deploy? | Encaminhamento |
|---|---|---|---|
| Nenhum achado de segurança neste lote | — | — | — |

Nenhuma entrada nova nesta tabela. O ponto de atenção sobre `Startup.
Container` estático foi avaliado e não é um achado de segurança (ver acima);
está registrado como nota de design no `QA-REPORT.md` (T-27), sem tarefa em
`Refatoração Lote-6`.

### RL5-01 — confirmação de que a correção não reabre RT-08

**Confirmado.** O requisito operacional que o Lote 5 tinha deixado para este
lote ("o novo `DbContext` por tentativa deve continuar encapsulado dentro do
escopo de uma única operação/requisição, não compartilhado entre requisições
diferentes") está cumprido: `IFabricaEscopoOperacao.Abrir()` é chamado
**dentro** do delegate passado a `ExecutorComRetry.Executar` (que por sua vez
é chamado uma vez por invocação de `QuitacaoService.Executar`/
`CancelamentoService.Cancelar`, ou seja, uma vez por requisição/operação de
negócio) — o novo `DbContext` por tentativa nasce e morre inteiramente
dentro dos limites de uma única operação, nunca atravessa para outra
requisição. Confirmado por leitura de `QuitacaoService.cs`/
`CancelamentoService.cs`/`FabricaEscopoOperacaoEf.cs` e pelo teste
determinístico de RL5-01, reexecutado por mim. RL5-01 fecha sem introduzir
nenhum novo risco de segurança.

### Requisitos de segurança operacional para o chapéu DevOps

- Nenhum requisito novo além dos já registrados nos Lotes 3-5. Reforço: o
  self-host OWIN (`ApiHost`, T-27) escuta só em `http://localhost:{porta}/`
  (loopback local, sem TLS — limitação já aceita e documentada em
  `GUARDRAILS.md` G-6.26) — nenhuma superfície de rede nova exposta além do
  já previsto pelo SDD/GUARDRAILS.
- Quando T-35 (Lote 8, `ApiKeyHandler`) for implementada: confirmar que ela
  entra no pipeline de `Startup.Configuration` **antes** do roteamento de
  controller (mesmo padrão de `CorrelationIdHandler`/T-29 e
  `GlobalExceptionHandler`/T-28, já corretamente ordenados aqui), para que
  nenhuma rota autenticável escape da checagem de `X-Api-Key`.

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado com relevância estratégica de segurança/compliance neste lote.

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 6

1. **Todas as tarefas `Concluída`**: confirmado — T-25, T-26, T-27, T-28,
   T-29 (Seção 3, Lote 6 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 6**: T-25 dep. T-04 (Lote 2)/
   T-13 (Lote 4) — satisfeitas; T-26 dep. T-14/T-15/T-21 (Lote 4)/T-25 (mesmo
   lote) — satisfeitas; T-27 dep. T-25/T-26 (mesmo lote) — satisfeita; T-28
   dep. T-25 — satisfeita; T-29 dep. T-25 — satisfeita. RL5-01
   (`Refatoração Lote-5`) dep. T-26 — satisfeita, tarefa fecha `Resolvida`
   nesta rodada. Tarefas de lotes futuros que dependem deste lote: T-30 dep.
   T-18/T-25/T-26/T-28/T-29; T-31 dep. T-24/T-25/T-26/T-28; T-32 dep.
   T-19/T-25/T-26/T-28/T-29 (Lote 7); T-35 dep. T-25/T-28/T-26; T-36 dep.
   T-22/T-25/T-26 (Lote 8) — todas apontam corretamente para tarefas
   `Concluída` deste lote e de lotes anteriores já validados, sem referência
   quebrada, confirmado por leitura direta da Seção 3/4 do `TASK.md`.
3. **Nenhuma tarefa `Bloqueada`**: confirmado (`BLOCKERS.md` só tem o
   Bloqueio 001, já "Resolvido (parcialmente)", nota operacional de
   ambiente, sem bloqueio de tarefa deste lote).
4. **Achados simples/débito baixo-médio → `Refatoração Lote-6`**: não
   aplicável — zero achados nesta rodada (QA e DevSecOps). `Refatoração
   Lote-6` não é criado (mesmo critério já usado nos Lotes 2/3).

**Conclusão: consistente, sem inconsistência que exija redesenho de
dependência/decomposição.** RL5-01 fecha definitivamente nesta rodada, com
confirmação independente do Validador (não só a afirmação do Executor) de
que a correção resolve o risco funcional sem reabrir a política RT-08/regra
5. O ponto de atenção sobre `Startup.Container` estático foi avaliado a
fundo (design e segurança) e não constitui achado, dada a garantia
arquitetural de processo único já ratificada pelo ADR-004. Não exige reabrir
o `coordenador`. Lote 6 fecha **Validado** (sem ressalvas).

## Lote 7 — Endpoints (marco: API utilizável pelo Vendas)

**Veredito: Aprovado, sem débito de segurança.** Auditoria disparada só após
o chapéu QA aprovar funcionalmente este lote (`QA-REPORT.md`: Lote 7 =
Aprovado, sem ressalvas). Base: `SDD.md` Seção 7, `GUARDRAILS.md` G-21/G-22/
G-24, `docs/contrato-v1.1.md` Seção 3.1-3.3.

### `static-security-analysis`

- Escopo real do `git diff` do lote: `VendasController.cs` (3 actions e 6
  atributos `[Route]`), `VendaStatusResponseDto.cs`,
  `CancelamentoRequestDto.cs`, `CancelamentoResponseDto.cs`,
  `docs/postman/smoke-vendas.sh`+`README.md`, `tools/T-34-smoke-harness/`
  (harness descartável, não parte da `.sln`). `QuitacaoRequestDto.cs`/
  `QuitacaoResponseDto.cs` (T-30) não foram tocados nesta worktree — herdados
  do commit-base, mesma auditoria de superfície de código aplicada.
- Sem SQL concatenado em nenhum artefato novo: as 3 actions do controller
  não tocam banco diretamente, delegam para `QuitacaoService`/
  `ConsultaService`/`CancelamentoService` (Application, já auditados nos
  Lotes 4/5) — nenhuma query nova introduzida por este lote.
- Nenhum `PackageReference` fora da lista obrigatória da Seção 1 introduzido
  pelos projetos de produção (`Api`) neste lote — confirmado no
  `ERPFinanceiro.Api.csproj` (sem diff). O harness de T-34
  (`tools/T-34-smoke-harness/T34SmokeHarness.csproj`) é código descartável
  fora da `.sln`, mesma disciplina de `spikes/T-01-firebird-spike/`, já
  aceita em lotes anteriores — referenciado só para rodar a smoke suite, não
  compõe o build de produção nem é distribuído.
- `Cancelamento(dto)`: a exceção lançada para `vendaId` ausente/vazio
  (`Application.Exceptions.ValidacaoException`) é a mesma classe já mapeada
  por `ExceptionParaRespostaMapper` (T-28, Lote 6, não alterado neste lote) —
  confirmado que nenhuma nova branch de tratamento de exceção foi adicionada
  ao mapper; o roteamento para 400 reaproveita o `case` já existente
  (`ApplicationExceptions.ValidacaoException`, adicionado em T-18/Lote 5).

### `security-requirement-validation`

- **Envelope de erro nunca vaza stack trace/mensagem de exceção original
  (ponto de atenção pedido nesta rodada)**: confirmado que
  `GlobalExceptionHandler.cs`/`ExceptionParaRespostaMapper.cs` **não
  aparecem no `git diff` deste lote** — reaproveitados sem nenhuma
  modificação pelas 3 novas actions. Testei manualmente o pior caso possível
  introduzido por este lote: se `CancelamentoService.Cancelar` lançar
  `ArgumentException` (guarda defensiva interna, T-19/Lote 5) por algum
  caminho que escape da validação do controller, ela cai no `case default`
  do mapper -> 500 `ERRO_INTERNO` com mensagem genérica fixa, nunca
  `ex.Message`/stack trace — confirmado por leitura do `case default`, sem
  achado. Todas as respostas HTTP 400/404/409 observadas nos testes de
  integração deste lote usam mensagens de negócio estáveis e não sensíveis
  (`"O campo 'vendaId' é obrigatório."`, `"Cancelar uma venda quitada exige
  o campo 'motivo'."`, etc.), nenhuma delas ecoa dado bruto não sanitizado
  do payload do cliente.
- **`X-Api-Key`/credenciais não antecipadas incorretamente**: confirmado por
  busca no `git diff` e nos 3 arquivos novos de controller/DTOs — nenhuma
  referência a `X-Api-Key`/`ApiKey`/autenticação em `VendasController.cs`
  nem nos DTOs deste lote. As 3 actions permanecem sem autenticação,
  exatamente como o escopo do lote define (T-35 é Lote 8, ainda `A fazer`) —
  não há bypass nem antecipação incorreta de controle de acesso.
- **Validação de payload no controller para `Cancelamento` (ponto de atenção
  pedido nesta rodada), avaliada sob a ótica de segurança**: a decisão de
  validar `vendaId` ausente/vazio diretamente no controller, em vez de
  estender `ValidadorVendaCommand` (T-17), não introduz risco — é uma
  validação de presença simples (`string.IsNullOrWhiteSpace`), sem lógica de
  negócio sensível, e produz exatamente o código/mensagem do contrato (400
  `PAYLOAD_INVALIDO`). Não há caminho onde essa decisão pontual permita um
  payload malformado/malicioso passar sem validação: a dupla guarda
  (controller + `ArgumentException` defensivo no serviço) garante que
  `vendaId` nulo/vazio nunca alcança a camada de persistência. Avaliação:
  **decisão pontual aceitável, não um achado de segurança** — mesmo
  enquadramento do ponto de atenção sobre `Startup.Container` avaliado no
  Lote 6 (limitação real e contida, não um desvio de política).
- **`App.config`/credenciais de desenvolvimento fora do controle de
  versão**: confirmado por mim (não só pela nota do Executor) via `git
  status`/`git check-ignore -v`:
  `src/ERPFinanceiro.Tests/App.config` → `.gitignore:17:App.config` (match,
  ignorado); `tools/T-34-smoke-harness/App.config` → mesmo match, ignorado
  (aparece em `git status --ignored` como `!!`, nunca `??`). Os `.example`
  correspondentes (`tools/T-34-smoke-harness/App.config.example`) têm só
  placeholders (`Api:ApiKey = "CHAVE_LOCAL_SMOKE_T34"`, `Banco:Senha =
  "masterkey"` — a senha padrão de desenvolvimento do Firebird embarcado,
  já aceita como não-segredo real nos Lotes 1-6, mesmo valor usado em todo
  `App.config.example` do repositório) — nenhum segredo real versionado.
  `docs/postman/` (README + script + evidência) não contém nenhuma
  credencial, só `vendaId`s sintéticos com sufixo timestamp.

### `compliance-validation`

Não aplicável neste lote — mesmo enquadramento dos Lotes 2-6 (`vendaId`/
`clienteId`/`produtoId`/`motivo` seguem identificadores/textos técnicos
opacos nos DTOs deste lote, sem PII nova introduzida). LGPD segue baixo
impacto (CTO-REVIEW item 5, já registrado).

### `sensitive-data-exposure-check`

- Nenhuma resposta HTTP das 3 actions deste lote expõe dado além do previsto
  pelo contrato (`status`, `dataQuitacao`, `vendaId`) — confirmado por
  leitura dos 3 DTOs de saída e pelos corpos JSON observados nos testes de
  integração reexecutados por mim.
- `smoke-vendas.sh`/evidência de execução (`docs/postman/`): usam só dados
  sintéticos (`V-SMOKE-*`, `C-SMOKE-1`, produtos `P-01`/`P-02`) — nenhum
  dado real de cliente/venda, confirmado por leitura do script completo.
- `tools/T-34-smoke-harness/App.config.example`: placeholder de `ApiKey`
  claramente marcado como local/smoke (`CHAVE_LOCAL_SMOKE_T34`), não um
  valor que poderia ser confundido com produção.

### `finding-severity-classification`

| Achado | Severidade | Bloqueia deploy? | Encaminhamento |
|---|---|---|---|
| Nenhum achado de segurança neste lote | — | — | — |

Nenhuma entrada nova nesta tabela. Os dois pontos de atenção pedidos
(validação de `vendaId` no controller de `Cancelamento`; reaproveitamento do
envelope de erro sem alteração) foram avaliados e não constituem achado —
ver `security-requirement-validation` acima.

### Requisitos de segurança operacional para o chapéu DevOps

- Nenhum requisito novo além dos já registrados nos Lotes 3-6. Reforço
  específico deste lote: quando T-35 (`ApiKeyHandler`, Lote 8) entrar no
  pipeline, as 3 rotas deste lote (`quitacao`/`{vendaId}/status`/
  `cancelamento`, nas duas bases `/api/vendas` e `/api/v1/vendas`, 6 rotas
  no total) precisam estar todas cobertas pela checagem de `X-Api-Key` — o
  `DelegatingHandler` já é registrado no pipeline OWIN antes do roteamento
  de controller (mesmo padrão de `CorrelationIdHandler`/`GlobalExceptionHandler`,
  já corretamente ordenados), então isso deve valer automaticamente para as
  6 rotas sem trabalho adicional; confirmar isso explicitamente na auditoria
  do Lote 8 (T-35/T-37).

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado com relevância estratégica de segurança/compliance neste lote.

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 7

1. **Todas as tarefas `Concluída`**: confirmado — T-30, T-31, T-32, T-33,
   T-34 (Seção 3, Lote 7 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 7**: T-30 dep.
   T-18/T-25/T-26/T-28/T-29 (Lotes 5/6) — satisfeitas; T-31 dep.
   T-24/T-25/T-26/T-28 — satisfeitas; T-32 dep. T-19/T-25/T-26/T-28/T-29 —
   satisfeitas; T-33 dep. T-30/T-31/T-32 (mesmo lote) — satisfeita; T-34 dep.
   T-30/T-31/T-32 (mesmo lote) — satisfeita. Tarefas de lotes futuros que
   dependem deste lote: T-35 dep. T-25/T-28/T-26 (não deste lote, ok); T-36
   dep. T-22/T-25/T-26 (não deste lote, ok); T-37 dep. T-34/T-35 — T-34
   `Concluída`, T-35 `A fazer` (esperado, Lote 8 ainda não iniciado); T-38
   dep. T-30/T-31/T-35/T-36/T-03 — T-30/T-31/T-03 `Concluída`, T-35/T-36 `A
   fazer` (esperado); T-62 (Tier B, Lote 14) dep. T-61/T-30 — T-30
   `Concluída`, T-61 ainda não chegou (esperado, tier/lote muito posterior).
   Todas as referências apontam corretamente para tarefas `Concluída` deste
   lote e de lotes anteriores já validados, ou para tarefas futuras ainda
   `A fazer` de forma consistente com o cronograma — sem referência quebrada,
   confirmado por leitura direta da Seção 3/4 do `TASK.md`.
3. **Nenhuma tarefa `Bloqueada`**: confirmado (`BLOCKERS.md` só tem o
   Bloqueio 001, já "Resolvido (parcialmente)", nota operacional de
   ambiente — e nem sequer reproduzido nesta rodada de validação, suíte
   completa verde na 1ª tentativa).
4. **Achados simples/débito baixo-médio → `Refatoração Lote-7`**: não
   aplicável — zero achados nesta rodada (QA e DevSecOps). `Refatoração
   Lote-7` não é criado (mesmo critério já usado nos Lotes 2/3/6).

**Conclusão: consistente, sem inconsistência que exija redesenho de
dependência/decomposição.** Os dois pontos de atenção pedidos (validação de
`vendaId` no controller de `Cancelamento`; reaproveitamento do envelope de
erro sem alteração) foram avaliados a fundo e não constituem achado. Não
exige reabrir o `coordenador`. Lote 7 fecha **Validado** (sem ressalvas) —
marco "API utilizável pelo Vendas" cumprido: os 3 endpoints do contrato v1.0
(quitação, cancelamento, status) estão implementados, testados de ponta a
ponta contra Firebird real, com alias `/api/v1` e coleção de fumaça
disponível em `docs/postman/`.

## Lote 8 — Segurança da API e integração real (Dia 4)

**Veredito: Aprovado, sem débito de segurança.** Auditoria disparada só após
o chapéu QA aprovar funcionalmente este lote (`QA-REPORT.md`: Lote 8 =
Aprovado, sem ressalvas). Base: `SDD.md` Seção 7, `GUARDRAILS.md` G-6.24 (chave
nunca logada)/G-21/G-22/G-24, `docs/contrato-v1.1.md` Seções 2/3.1-3.5,
ADR-010.

### `static-security-analysis`

- Escopo real do `git diff` do lote: `ApiKeyHandler.cs` (novo),
  `HealthController.cs` (novo), `Dtos/HealthResponseDto.cs` (novo),
  `Startup.cs` (adição de `ConfigurarApiKey`), `docs/contrato-v1.1.md`
  (ajuste de 4 mensagens), `docs/postman/smoke-vendas.sh`/`README.md`
  (adição de `X-Api-Key`/cenários 401), `tools/T-38-integracao-simulada/`,
  `tools/T-39-integracao-simulada/` (harnesses descartáveis, fora da
  `.sln`), `docs/integracao-simulada/` (evidência/README/pendência).
- Nenhum `PackageReference` novo introduzido em projeto de produção
  (`Api`) — `ApiKeyHandler` usa só `System.Configuration` (já referenciado
  desde T-25/nota de camada documentada) e BCL (`System.Security.Cryptography`
  não foi usado — decisão correta, dado que a API necessária não existe no
  net48; a alternativa manual foi escolhida e revisada abaixo).
- Sem SQL concatenado em nenhum artefato novo deste lote — `HealthController`
  delega a `IHealthService` (já auditado T-22); `ApiKeyHandler` não toca
  banco.
- Harnesses `tools/T-38.../T-39...` seguem a mesma disciplina já aceita
  (`tools/T-34-smoke-harness`, Lote 7): fora da `.sln`, `App.config` real
  gitignorado, `.example` só com placeholders de desenvolvimento.

### `security-requirement-validation`

- **Comparação em tempo constante (S-08) — revisão criptográfica manual**:
  `ComparacaoEmTempoConstante` percorre sempre `Math.Max(len(esperada),
  len(recebida))` bytes, nunca retorna cedo (nem por diferença de tamanho —
  incorporada ao acumulador via XOR antes do laço —, nem por diferença de
  conteúdo — o `|=` acumula sem `break`/`return` dentro do laço). Único
  desvio de uma implementação de referência: usa XOR acumulado num `int`
  (0 se e só se todos os bytes comparados forem iguais **e** os tamanhos
  forem iguais) em vez de comparar `byte` a `byte` num único acumulador de
  `byte` — equivalente em segurança (a extensão do tamanho para `int` não
  introduz vazamento de timing adicional, o laço já é O(max(len)) fixo, sem
  branch dependente de dado dentro do loop). **Avaliação: criptograficamente
  aceitável** para o nível de ameaça deste componente (defesa contra timing
  attack de rede, não um HMAC/assinatura). Sem achado.
- **Fail-closed confirmado por leitura e por raciocínio de caso extremo**:
  `ChaveValida` retorna `false` sempre que `ObterChaveConfigurada()` é nulo
  ou vazio, **antes** de sequer olhar o header da requisição — nenhum
  caminho onde ausência de configuração vira "todas as chaves aceitas" ou
  "todas rejeitadas menos uma vazia coincidente" (o `IsNullOrEmpty` explícito
  elimina o caso "header também vazio bate com config vazia"). Comportamento
  correto de fail-closed, sem achado.
- **Chave nunca em log/exceção**: confirmado por leitura completa de
  `ApiKeyHandler.cs` — nenhuma variável que contenha a chave recebida ou
  configurada é passada a `IAppLogger.Registrar`, interpolada em mensagem,
  ou anexada a uma exceção lançada. A única mensagem de log é a constante
  fixa da classe. Sem achado.
- **401 idêntico para ausente/inválida**: confirmado — mesmo método
  `ConstruirRespostaNaoAutorizada` para os dois ramos de `SendAsync`
  (`EstaIsenta(request) || ChaveValida(request)` sendo falso cai sempre no
  mesmo caminho, sem branch que distinga "ausente" de "errada"). Reforça
  contra enumeração de credencial válida por diferença de resposta. Sem
  achado.
- **Ordem de registro no pipeline OWIN**: `ConfigurarApiKey` roda depois de
  `ConfigurarCorrelationId` e antes de `ConfigurarExceptionHandler`
  (`Startup.Configuration`) — como `DelegatingHandler`s em
  `config.MessageHandlers`, ambos rodam **antes** do roteamento de
  controller (mecanismo do próprio Web API 2), então nenhuma rota
  autenticável escapa da checagem, incluindo as 6 rotas de venda (3
  endpoints x 2 bases, alias `/api/v1`, T-33) — reforço específico pedido no
  `SECURITY-REVIEW.md` do Lote 7, **confirmado satisfeito** por leitura do
  pipeline e pelos testes de integração reexecutados (nenhuma rota de venda
  respondeu sem 401 na ausência de `X-Api-Key`, na suíte completa). Isenção
  de `/api/health` corretamente restrita a essa única rota — comparação por
  `AbsolutePath` não usa prefixo/`StartsWith`, então não isenta acidentalmente
  nenhuma rota de venda que comece com algo parecido.
- **`GET /api/health` de fato não autentica**: confirmado por leitura —
  `HealthController` não lê nenhum header, nenhuma configuração de chave;
  toda a lógica de isenção vive só no `ApiKeyHandler` (sem duplicação nem
  bypass paralelo). Comportamento esperado por P-5 (health check não deve
  exigir credencial, para monitoramento externo).

### `compliance-validation`

Não aplicável neste lote — mesmo enquadramento dos Lotes 2-7 (nenhum dado
pessoal novo introduzido; `X-Api-Key` é segredo operacional, não dado
pessoal). LGPD segue baixo impacto (CTO-REVIEW item 5, já registrado).

### `sensitive-data-exposure-check`

- Resposta 401 do `ApiKeyHandler`: corpo fixo (`{erro:{codigo:"NAO_AUTORIZADO",
  mensagem:"..."}}`, string constante) — não ecoa o header recebido nem
  nenhum dado do request. Confirmado.
- Arquivo de log real (`Log:CaminhoArquivo`) lido diretamente pelo teste
  dedicado de T-35 (`ApiHostVendasApiKeyControllerTests`) e reexecutado por
  mim como parte da suíte completa — inspecionei o teste (não o arquivo de
  log gerado, que fica fora do repositório) e confirmei que a asserção
  (`Assert.DoesNotContain`) cobre tanto a chave correta quanto a errada.
- `App.config.example` dos 2 novos harnesses (`tools/T-38-integracao-simulada/`,
  `tools/T-39-integracao-simulada/`): só placeholders de desenvolvimento
  (`CHAVE_LOCAL_SIMULADA_T38`/`T39`, `SYSDBA`/`masterkey` — senha padrão do
  Firebird embarcado, já aceita como não-segredo real em todos os lotes
  anteriores) — nenhum segredo real versionado, confirmado por leitura linha
  a linha dos 2 arquivos `.example`.
- `App.config` reais (T-35/T-38/T-39, além do já existente de T-32/Lote 7)
  confirmados fora do controle de versão por mim mesmo (não só pela nota do
  Executor): `git check-ignore -v src/ERPFinanceiro.Tests/App.config
  tools/T-38-integracao-simulada/App.config tools/T-39-integracao-simulada/App.config
  tools/T-34-smoke-harness/App.config` — os 4 batem com `.gitignore:17:App.config`;
  `git status --short` sobre `tools/`/`docs/integracao-simulada/`/
  `docs/postman/` mostra só `??` (não rastreado) nos diretórios novos, sem
  nenhum `App.config` real listado — confirma que nada de segredo local
  vazou para o índice do git.
- `docs/integracao-simulada/evidencia-execucao-T-38.txt`/`T-39.txt`:
  confirmado por leitura que os únicos "segredos" impressos são as chaves de
  desenvolvimento (`CHAVE_LOCAL_SIMULADA_T38`/`T39`, já classificadas acima
  como não-segredo real) — sem CPF/dado pessoal/credencial de produção.

### `finding-severity-classification`

| Achado | Severidade | Bloqueia deploy? | Encaminhamento |
|---|---|---|---|
| Nenhum achado de segurança neste lote | — | — | — |

Nenhuma entrada nova nesta tabela. A comparação em tempo constante manual
(ausência de `CryptographicOperations.FixedTimeEquals` no net48) foi avaliada
a fundo em `security-requirement-validation` acima e considerada
criptograficamente aceitável — não é um achado, é uma limitação de
plataforma já documentada e mitigada corretamente pelo Executor.

### Requisitos de segurança operacional para o chapéu DevOps

- `Api:ApiKey` real de produção deve ser gerada com entropia suficiente
  (não reaproveitar `CHAVE_LOCAL_TESTE_T32`/`CHAVE_LOCAL_SMOKE_T34`/
  `CHAVE_LOCAL_SIMULADA_T38`/`T39`, todas exclusivas de desenvolvimento/teste)
  e armazenada fora do `App.config` versionável, conforme já orientado nos
  Lotes 4/6 (gestão de secrets, T-53/empacotamento não deve incluir `ApiKey`
  real — já é critério de aceite explícito de T-53 na Seção 3 do `TASK.md`).
- Rotação de `Api:ApiKey`: como a comparação é simples (chave única, sem
  hashing/salting), qualquer rotação exige reiniciar o processo Desktop
  (chave lida uma vez por requisição via `ConfigurationManager`, mas o
  `App.config` só é relido se o processo reiniciar/o AppDomain recarregar) —
  registrar esse comportamento no runbook operacional se rotação em
  produção for um requisito (não coberto pelo `SDD.md` atual; sinalização
  preventiva, não um achado bloqueante deste lote).
- Observabilidade: tentativas 401 já geram uma linha de log genérica
  (`TraceLogger`/T-21) — suficiente para detectar tentativa de acesso
  indevido em volume, sem exigir infraestrutura nova.

### Relevância estratégica (sinalização ao Gestor)

Nenhum achado de segurança com relevância estratégica neste lote. A
pendência de integração real com o Vendas/Delphi (ADR-010, `BLOCKERS.md`
Bloqueio 002) já foi sinalizada ao usuário pelo Coordenador antes da
execução deste lote — não é um achado novo desta auditoria, só confirmado
como corretamente rotulado em toda a evidência produzida (ver
`QA-REPORT.md`, seção T-38/T-39).

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 8

1. **Todas as tarefas `Concluída`**: confirmado — T-35, T-36, T-37, T-38,
   T-39, T-40 (Seção 3, Lote 8 do `TASK.md`) estão `Concluída`.
2. **Dependências da Seção 4 relativas ao Lote 8**: T-35 dep.
   T-25/T-28/T-26 (Lote 6) — satisfeitas; T-36 dep. T-22/T-25/T-26 (Lotes
   4/6) — satisfeitas; T-37 dep. T-34/T-35 (Lote 7 + mesmo lote) —
   satisfeitas; T-38 dep. T-30/T-31/T-35/T-36 (Lote 7 + mesmo lote) —
   satisfeitas; T-39 dep. T-38/T-32 (mesmo lote + Lote 7) — satisfeitas;
   T-40 dep. T-39 (mesmo lote) — satisfeita. Tarefa futura que depende deste
   lote: T-53 (Lote 13) dep. T-40/T-50/T-52 — T-40 `Concluída` (T-50/T-52
   ainda não chegaram, esperado, tiers/lotes posteriores). Confirmado por
   leitura direta da Seção 4.1/4.2 do `TASK.md` (nota do Bloqueio 002/ADR-010:
   "nenhuma tarefa dos Lotes 9, 10, 11 ou 12 depende de T-38/T-39/T-40") que
   nenhum outro lote em andamento (9/10, paralelos a este) referencia
   nenhuma tarefa deste lote — sem referência quebrada.
3. **Nenhuma tarefa `Bloqueada`**: confirmado — `BLOCKERS.md` tem o Bloqueio
   001 (operacional, "Resolvido (parcialmente)", não reproduzido nesta
   validação: suíte completa 124/124 em 2 rodadas, sem `FileLoadException`)
   e o Bloqueio 002, cujo status é **"Resolvido (redesenho de escopo)"** —
   confirmado que T-38 **não está mais `Bloqueada`** no `TASK.md` (estava
   citada como bloqueada só na descrição histórica do Bloqueio 002, nunca
   chegou a ter `Status: Bloqueada` na Seção 3 — a linha da tarefa já nasceu
   redefinida e foi direto para `Concluída` após a decisão do Coordenador).
   **Checagem do `BLOCKERS.md` (pedida explicitamente para este lote)**: o
   registro do Bloqueio 002 é coerente — contexto, decisão do Coordenador
   (com referência ao ADR-010), atualização de 23/09/2026 (T-40) linkando
   `docs/integracao-simulada/PENDENCIA-INTEGRACAO-REAL.md`, e status final
   "Resolvido (redesenho de escopo) — pendência residual... permanece
   aberta" — a pendência residual está clara e não foi apagada/escondida
   pelo fechamento do bloqueio; não reaberto por esta validação (correto,
   nada de novo a acrescentar).
4. **Achados simples/débito baixo-médio → `Refatoração Lote-8`**: não
   aplicável — zero achados nesta rodada (QA e DevSecOps). `Refatoração
   Lote-8` não é criado (mesmo critério já usado nos Lotes 2/3/6/7).

**Conclusão: consistente, sem inconsistência que exija redesenho de
dependência/decomposição.** A redefinição de escopo de T-38/T-39 (ADR-010)
já foi decidida e aprovada antes desta validação — não é reaberta aqui, só
confirmada como corretamente executada e rotulada. Não exige reabrir o
`coordenador`. Lote 8 fecha **Validado** (sem ressalvas). Marco "O-11"
(Dia 4) cumprido nos termos do ADR-010: suíte de integração simulada verde;
integração real com o Vendas (Delphi) permanece pendência externa explícita,
documentada e não apresentada como concluída em nenhum artefato do lote.

## Lote 9 — Tela de consulta: grid e detalhe

**Veredito: Aprovado com débito baixo (documentação).** Nenhum achado
alto/crítico; nenhum compliance obrigatório em aberto. Auditados os arquivos
novos/alterados de `src/ERPFinanceiro.Desktop` (Formatadores, ConfiguracaoVisual,
ConsultaVendasModelo, FrmConsulta, DetalheVendaModelo, FrmDetalheVenda),
`Desktop.csproj`/`Tests.csproj` e `docs/licencas.md`, depois da aprovação do
chapéu QA (`QA-REPORT.md`, Lote 9: Aprovado com ressalvas).

### `static-security-analysis`
- Sem `.Result`/`.Wait()`, SQL, `Process.Start` ou desserialização no código de
  tela; a única entrada é o `VendaId` da linha do grid, repassado a
  `ConsultaService` (parametrizado no repositório, auditado em lotes
  anteriores).
- `DbContext` por operação: escopo Autofac novo por carga (regra 5/RT-08),
  nunca compartilhado — confirmado.
- Cor `#555555` hardcoded nos SVGs: qualidade, não segurança (RL9-01).

### `sensitive-data-exposure-check`
- **Erros na tela:** `FrmConsulta` e `FrmDetalheVenda` usam `catch (Exception)`
  que **descarta** a exceção e mostra mensagem fixa. Sem `ex.Message`,
  `ToString()` ou `StackTrace` em `src/ERPFinanceiro.Desktop` (grep). Nenhum
  caminho de `.fdb` exibido. Observação: a exceção é descartada sem log (o
  "detalhe técnico no log" fica com T-45/T-46); sem impacto de segurança.
- **Segredos:** nenhuma ApiKey, senha de banco ou connection string em código
  de tela.
- **Licença/artefatos:** nenhum `DevExpress_License.txt`/chave/`licenses.licx`
  versionado ou na árvore (só `LICENSE.txt` de skills do `.claude`,
  pré-existentes). `git status` sem artefato indevido.

### `finding-severity-classification` — dependência nova
`DevExpress.Win.Grid` 25.1.9 (nuget.org público, **trial 30 dias**, warnings
DX1000/DX1001). Origem oficial, versão fixa; risco principal é
**licenciamento/entrega**, não vulnerabilidade.
- **RL9-04 (baixo, documentação):** `docs/licencas.md` (T-02) ainda diz que o
  DevExpress "não foi possível instalar" — **desatualizado** após o Bloqueio
  003 (23/09/2026). Deve registrar: (a) pacotes vêm do **nuget.org público em
  trial de 30 dias** (início do relógio via NuGet não confirmado); (b) em modo
  evaluation vale **"Redistribution prohibited"** (aviso DX1000): o build
  compilado com o trial **não pode ser redistribuído/entregue** sem licença —
  **relevante para T-53 (empacotamento/entrega, Lote 13)**; (c) splash/marca
  d'água de trial persistem até licenciar. Prazo: antes de T-53.

### Requisitos de segurança operacional para o chapéu DevOps
Acrescentar ao checklist de T-53: confirmar licença DevExpress/FastReport
válida antes de gerar o pacote de entrega (não distribuir build trial).

### Relevância estratégica (sinalização ao Gestor)
Sim, baixa urgência: a proibição de redistribuição em modo trial e o prazo de
30 dias é decisão de custo/licenciamento (G-2 "custo zero") que pode conflitar
com a entrega final. Sinalizado ao Gestor, sem bloquear este lote.

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 9

1. **Tarefas `Concluída`:** T-41, T-42, T-43, T-44 — confirmado.
2. **Dependências da Seção 4:** T-41 dep. T-04; T-42 dep. T-23/T-26/T-41; T-43
   dep. T-42; T-44 dep. T-24/T-42 — satisfeitas. Dependentes futuros (T-45/
   T-46/T-47, T-50, T-51) apontam para T-42/T-44 `Concluída`: sem referência
   quebrada.
3. **Nenhuma tarefa `Bloqueada`:** confirmado. `BLOCKERS.md` Bloqueio 003
   "Resolvido (parcialmente)" com a verificação visual manual clara; não
   reaberto.
4. **Dependência esperada, não inconsistência:** as telas só funcionam de fato
   com o composition root (`Program.cs`, T-46); `DetalhePresenter` só é
   atribuída ali. Estava só na nota de T-44, não em T-46/T-51 -> RL9-02.
5. **`Refatoração Lote-9` criada** (RL9-01…RL9-04, fim da Seção 3 do
   `TASK.md`). Sem inconsistência que exija redesenho: sem escalonamento ao
   `coordenador`. Lote 9 fecha **Validado com ressalvas**.

## Lote 10 — Tela: status e ciclo de vida

**Veredito: Aprovado com débito baixo.** Nenhum achado alto/crítico; nenhum
compliance obrigatório em aberto. Auditados os arquivos novos/alterados de
`src/ERPFinanceiro.Desktop` (barra, inicialização, mutex, encerramento,
composição), `docs/licencas.md` e csproj, depois da aprovação do chapéu QA
(`QA-REPORT.md`, Lote 10: Aprovado com ressalvas).

### `static-security-analysis`
- Sem `.Result`/`.Wait()`, SQL, `Process.Start` ou desserialização. Sem entrada
  externa nova: a tela lê estado em processo (`IHealthService`,
  `ApiHost.Estado`); nenhum listener/porta novo (o `ApiHost` extra do
  `ComposicaoJanelaPrincipal` nunca faz `Start`).
- Mutex `Global\ERPFinanceiro.Desktop.InstanciaUnica`: nome fixo, sem segredo;
  escopo `Global\` é o desejado (um `.fdb` por máquina, regra 11). Sem ACL
  explícita: outro usuário pode receber `UnauthorizedAccessException`
  (disponibilidade/UX, RL10-03). Reserva do nome por processo local hostil
  (DoS local) fica fora do modelo de ameaça do projeto; sem tarefa.

### `sensitive-data-exposure-check`
- **Mensagens ao usuário:** splash, aviso, diálogo de API e confirmação sem
  caminho de arquivo nem stack. `Program` mostra `ex.Message` só de config
  (nome de chave, sem valor). Estados de erro de F-1/F-3 seguem com mensagem
  fixa.
- **Diálogo de detalhes:** expõe porta e caminho do `.fdb` de propósito
  (UX-SPEC). "Último erro"/Copiar usa `ex.Message` cru de `HealthService`/
  `HttpListenerException`. `HealthService` monta a connection string (com
  `Password`) mas só propaga `ex.Message`; as mensagens do FbClient (caminho,
  I/O, "user name and password are not defined") **não ecoam a senha**
  (análise de código; não provoquei todos os erros possíveis). Sem vazamento
  comprovado, mas sem barreira: débito baixo de defesa em profundidade
  (**RL10-04**).
- **`IAppLogger`:** `Program` registra `contexto + ": " + ex` (exceção
  completa) só no arquivo de log local; nenhuma ApiKey, senha ou connection
  string no código do Desktop além de `CompositionRoot` (uso legítimo, grep
  `ApiKey|Senha|Password`).
- **Licenças/artefatos:** nenhum `DevExpress_License.txt`, `licenses.licx` ou
  chave versionados; csproj só com `PackageReference` 25.1.9 do nuget.org.

### `finding-severity-classification`
| Achado | Severidade | Destino |
|---|---|---|
| "Último erro" copiável sem sanitização (sem segredo hoje) | Baixa | RL10-04 |
| Mutex sem tratamento de acesso negado / multiusuário | Baixa | RL10-03 |
| Sem handlers de exceção não tratada; falhas do encerramento perdidas | Baixa | RL10-02 |
| Intermitência de teste (Lote 8, Firebird embarcado) | Baixa (qualidade) | RL10-06 |

### RL9-04 (`docs/licencas.md`) — resolvido
Confere com os fatos: nuget.org público, `DevExpress.Win.Grid` 25.1.9, avisos
DX1000 ("Redistribution prohibited") e DX1001, trial de 30 dias com **início
do relógio no modo NuGet explicitamente "não confirmado"**, splash/marca
d'água persistentes, pendência visual RL1-03 marcada honestamente, checklist
objetivo de T-53 (licença válida ou decisão explícita do usuário antes de
empacotar) e nota ao Gestor sobre o conflito com G-2. Sem afirmação de
verificação não feita. **Fechado.**

### Requisitos de segurança operacional para o chapéu DevOps
Sem novos. Mantém o item de T-53 (licença DevExpress/FastReport válida antes
do pacote de entrega).

### Relevância estratégica (sinalização ao Gestor)
Sem novidade em relação ao Lote 9 (conflito G-2 x proibição de redistribuir o
trial; já sinalizado). Sem bloqueio.

## Fechamento estrutural do lote (checagem do próprio Validador) — Lote 10

1. **Tarefas `Concluída`:** T-45, T-46, T-47 — confirmado.
2. **Dependências da Seção 4:** T-45 dep. T-22/T-27/T-42; T-46 dep.
   T-12/T-27/T-42; T-47 dep. T-46 — satisfeitas. T-51 depende de
   T-45/T-46/T-47 (coerente). **T-50 não depende de T-45/T-46/T-47** (Dep. =
   T-42, T-49): não é inconsistência (o fluxo do relatório não usa barra nem
   inicialização); só diverge da expectativa do pedido. T-48 `Concluída`;
   T-49/T-50 `A fazer` com Dep. válidas. Lote 12 (T-51/T-52) coerente. Sem
   referência quebrada.
3. **Nenhuma tarefa `Bloqueada`:** confirmado. `BLOCKERS.md` Bloqueio 003
   segue "Resolvido (parcialmente)" com a verificação visual manual do usuário.
4. **RL9-01 `Em andamento`** (esperado); RL9-02/03/04 `Concluída` e conferidas.
5. **`Refatoração Lote-10` criada** (RL10-01…RL10-06, fim da Seção 3 do
   `TASK.md`). Sem inconsistência que exija redesenho: sem escalonamento ao
   `coordenador`. Lote 10 fecha **Validado com ressalvas**, com a conferência
   visual/ao vivo do usuário pendente.

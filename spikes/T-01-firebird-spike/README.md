# T-01 — Spike Firebird go/no-go (projeto descartável)

> Projeto de uso único, criado só para executar o roteiro de go/no-go do
> `TASK.md` (Lote 1). **Não é** o esqueleto da solução final (esse é T-04) e
> não deve ser referenciado por nenhum outro projeto.

## Resultado

**GO.** Ver `.md/adr/009-spike-firebird-resultado-go.md` para a decisão
ratificável e `evidencia-execucao.txt` para a saída completa do roteiro.

## O que o programa faz (`FirebirdSpike/Program.cs`)

1. Cria um `.fdb` novo em modo **embedded** (`FbServerType.Embedded`).
2. Aplica DDL manual mínimo (`FIN_VENDA` com `ID` BIGINT identity,
   `VALOR_TOTAL DECIMAL(18,2)`, `VERSAO INTEGER`) — sem migrations, seguindo a
   regra 10 do TASK.md Seção 1.
3. **CA-1**: grava `1250.50` via EF6 (`DbContext`/Fluent mapping) e lê de volta
   num segundo `DbContext`, confirmando que o valor volta idêntico.
4. **CA-2**: abre **dois `DbContext`/duas conexões dentro do mesmo processo**
   (limitação assumida — ver "Limitação" abaixo) apontando para a mesma
   `VENDA`, atualiza pelo primeiro (que incrementa `VERSAO` na aplicação, sem
   trigger, regra 8) e tenta atualizar pelo segundo com a `VERSAO` antiga;
   confirma que o EF6 lança `DbUpdateConcurrencyException`.
5. **CA-3**: registra a bitness do processo/DLLs usadas (x64).

## Limitação assumida (documentada, conforme instrução da tarefa)

A regra 11 do TASK.md ("um único processo abre o `.fdb`") impede testar
literalmente um *segundo processo do SO* sem o primeiro fechado. O roteiro
simula isso com **dois contextos/duas conexões dentro do mesmo processo de
teste** (duas `FbConnection` distintas, cada uma com seu `DbContext`),
suficiente para exercitar o conflito otimista por `VERSAO` no provider EF6 +
Firebird. Teste de bloqueio real de segundo processo do SO fica coberto mais
tarde por T-20 (harness de concorrência, app fechado).

## Como reproduzir

O projeto (`FirebirdSpike.csproj`, SDK-style, `net48`, `PlatformTarget=x64`)
está versionado; `bin/`, `obj/` e `*.fdb` **não** estão (`.gitignore`), porque
dependem de binários nativos de terceiros que não fazem sentido versionar
neste spike.

1. `dotnet restore && dotnet build` dentro de `FirebirdSpike/` (restaura
   `EntityFramework` 6.5.1, `EntityFramework.Firebird` 10.1.0,
   `FirebirdSql.Data.FirebirdClient` 10.3.4 — provider **puramente
   gerenciado**, sem DLL nativa embutida no pacote NuGet).
2. Baixar o pacote Windows x64 "zip" do Firebird 3.0 (usado como fonte das
   DLLs nativas do modo embarcado, **sem instalar** — é só extrair arquivos):
   `https://sourceforge.net/projects/firebird/files/v3.0.12/Firebird-3.0.12.33787-0-x64.zip/download`
3. Copiar para `FirebirdSpike/bin/Debug/net48/` (raiz do executável):
   `fbclient.dll` (copiar/duplicar **também** como `fbembed.dll` — é o nome
   que o provider procura em modo `ServerType.Embedded`), `ib_util.dll`,
   `icudt52.dll`, `icudt52l.dat`, `icuin52.dll`, `icuuc52.dll`,
   `msvcp100.dll`, `msvcr100.dll`, `zlib1.dll`, `firebird.conf`,
   `firebird.msg`, `security3.fdb`, `plugins.conf`, e as pastas `plugins/`
   (`engine12.dll`, `legacy_auth.dll`, `legacy_usermanager.dll`, `srp.dll`) e
   `intl/` (`fbintl.dll`, `fbintl.conf`).
4. Rodar `FirebirdSpike.exe` a partir dessa pasta.

Isso confirma, na prática, o que **T-53** (empacotamento) precisa levar na
pasta de entrega: DLLs do Firebird embarcado + `.sql`/`.fdb`, sem instalador
(RNF-14).

## App.config (registro do provider EF6/ADO.NET para Firebird)

Como o projeto usa `PackageReference` (SDK-style), o `install.ps1` dos pacotes
antigos não roda; o `App.config` deste projeto já registra manualmente
`DbProviderFactories` e `entityFramework/providers` — esse trecho deve ser
copiado para o `App.config` real do Desktop em T-04/T-26.

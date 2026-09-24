<#
  T-53 - Build Release + empacotamento (RNF-14). Reproduzivel: rode da raiz do repo.

    powershell -ExecutionPolicy Bypass -File tools\T-53-empacotar\Empacotar.ps1
    (opcional) -Saida dist\ERPFinanceiro-entrega  -FirebirdZip <caminho do zip Firebird x64>

  Gera a pasta de entrega com: executavel + DLLs (.NET), motor Firebird 3.0.12 x64 embarcado
  (bitness x64, ADR-009), database\*.sql, .fdb de demonstracao (criado aqui com isql a partir
  dos .sql), App.config.example. NUNCA copia App.config real. Ao final, roda Verificar.ps1.

  ANTES DE ENTREGAR (docs\licencas.md, checklist T-53): DevExpress esta em evaluation
  ("Redistribution prohibited", DX1000). A pasta gerada serve a validacao interna/T-56; a
  entrega final exige licenca valida ou decisao explicita do usuario.
#>
param(
  [string]$Saida = 'dist\ERPFinanceiro-entrega',
  [string]$FirebirdZip = '',
  [string]$FirebirdUrl = 'https://github.com/FirebirdSQL/firebird/releases/download/v3.0.12/Firebird-3.0.12.33787-0-x64.zip',
  [string]$FirebirdSha256 = 'fc0d09962fc8d18e14c0a47fcc88e6b40f34483c7758b0624e3df4e9cb5f68b4'
)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $raiz
$saidaAbs = [IO.Path]::GetFullPath((Join-Path $raiz $Saida))

# 1. Build Release (PlatformTarget x64 fixo no csproj)
Write-Host '== Build Release'
dotnet build src\ERPFinanceiro.Desktop -c Release -v q
if ($LASTEXITCODE -ne 0) { throw 'Build falhou' }
$bin = Join-Path $raiz 'src\ERPFinanceiro.Desktop\bin\Release\net48'

# 2. Motor Firebird x64 (zip oficial, sha256 conferido)
if (-not $FirebirdZip) {
  $cache = Join-Path $env:TEMP 'ERPFinanceiro-T53'
  New-Item -ItemType Directory -Force $cache | Out-Null
  $FirebirdZip = Join-Path $cache 'firebird-x64.zip'
  if (-not (Test-Path $FirebirdZip)) { Write-Host '== Baixando Firebird'; Invoke-WebRequest $FirebirdUrl -OutFile $FirebirdZip }
}
$hash = (Get-FileHash $FirebirdZip -Algorithm SHA256).Hash.ToLower()
if ($hash -ne $FirebirdSha256) { throw "SHA256 do zip Firebird difere: $hash" }
$fbDir = Join-Path $env:TEMP 'ERPFinanceiro-T53\fb'
if (Test-Path $fbDir) { Remove-Item -Recurse -Force $fbDir }
Expand-Archive $FirebirdZip $fbDir

# 3. Pasta de entrega
if (Test-Path $saidaAbs) { Remove-Item -Recurse -Force $saidaAbs }
New-Item -ItemType Directory -Force $saidaAbs | Out-Null
Get-ChildItem $bin -File | Where-Object { $_.Name -notmatch '\.pdb$' -and $_.Name -ne 'ERPFinanceiro.Desktop.exe.config' } |
  ForEach-Object { Copy-Item $_.FullName $saidaAbs }
Get-ChildItem $bin -Directory | ForEach-Object { Copy-Item $_.FullName $saidaAbs -Recurse }
# .exe.config gerado pelo build e o App.config de runtime: so vai se nao tiver segredo (o build
# a partir de App.config.example nao existe; ver verificacao). Entregamos apenas o .example.
$fbArquivos = 'fbclient.dll','ib_util.dll','icudt52.dll','icudt52l.dat','icuin52.dll','icuuc52.dll',
  'msvcp100.dll','msvcr100.dll','zlib1.dll','firebird.conf','firebird.msg','security3.fdb','plugins.conf'
foreach ($f in $fbArquivos) { Copy-Item (Join-Path $fbDir $f) $saidaAbs }
Copy-Item (Join-Path $fbDir 'fbclient.dll') (Join-Path $saidaAbs 'fbembed.dll')   # nome procurado em ServerType=Embedded (ADR-009)
Copy-Item (Join-Path $fbDir 'plugins') $saidaAbs -Recurse
Copy-Item (Join-Path $fbDir 'intl') $saidaAbs -Recurse
Copy-Item (Join-Path $fbDir 'LICENSE*') $saidaAbs -ErrorAction SilentlyContinue
$dbDir = Join-Path $saidaAbs 'database'
New-Item -ItemType Directory -Force $dbDir | Out-Null
Copy-Item database\*.sql $dbDir
Copy-Item src\ERPFinanceiro.Desktop\App.config.example $saidaAbs

# 4. .fdb de demonstracao: schema + seed via isql embarcado (sem servidor)
$fdb = Join-Path $dbDir 'financeiro-demo.fdb'
$isql = Join-Path $fbDir 'isql.exe'
$env:FIREBIRD = $fbDir
# Se outro Firebird embarcado da maquina ja mapeia outro lock dir, o motor recusa ("Wrong file for
# memory mapping"): nesse caso defina T53_FIREBIRD_LOCK com o mesmo diretorio dele (ou feche-o).
$env:FIREBIRD_LOCK = if ($env:T53_FIREBIRD_LOCK) { $env:T53_FIREBIRD_LOCK } else { Join-Path $env:TEMP 'ERPFinanceiro-T53\lock' }
New-Item -ItemType Directory -Force $env:FIREBIRD_LOCK | Out-Null
$sql = "CREATE DATABASE '$fdb' PAGE_SIZE 8192 DEFAULT CHARACTER SET UTF8;`r`n" +
       "IN '$dbDir\01-schema.sql';`r`nIN '$dbDir\02-seed.sql';`r`nCOMMIT;`r`nQUIT;`r`n"
$tmp = Join-Path $env:TEMP 'ERPFinanceiro-T53\criar.sql'
[IO.File]::WriteAllText($tmp, $sql, (New-Object Text.UTF8Encoding $false))
Write-Host '== Criando .fdb de demonstracao'
& $isql -q -i $tmp
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $fdb)) { throw 'Falha ao criar o .fdb' }

Write-Host "== Pasta gerada: $saidaAbs"
& (Join-Path $PSScriptRoot 'Verificar.ps1') -Pasta $saidaAbs
if ($LASTEXITCODE -ne 0) { throw 'Verificacao falhou' }

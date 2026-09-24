# T-55: gera as evidencias em docs/evidencias (prints reais + PDF do relatorio) sobre o seed real.
# Requer: sessao GUI interativa (CopyFromScreen), .NET SDK, e o zip/pasta do Firebird 3.0.x x64
# ja extraido em $FirebirdDir (o mesmo usado por tools/T-53-empacotar; ver Empacotar.ps1).
# Se outro Firebird embarcado da maquina bloquear ("Wrong file for memory mapping"), defina
# T55_FIREBIRD_LOCK com outro diretorio (nao encerre processos alheios).
param(
  [string]$FirebirdDir = (Join-Path $env:TEMP 'ERPFinanceiro-T53\fb'),
  [string]$Saida = (Join-Path $PSScriptRoot '..\..\docs\evidencias'),
  # -Memoria: nao usa Firebird (seed equivalente em memoria, health/API stubs). Foi o modo usado
  # quando um Firebird embarcado orfao da maquina travava o motor (ver docs/evidencias/README.md).
  [switch]$Memoria
)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$proj = Join-Path $PSScriptRoot 'T55Evidencias.csproj'
$work = Join-Path $env:TEMP 'ERPFinanceiro-T55'
New-Item -ItemType Directory -Force $work | Out-Null
$fdb = Join-Path $work 'financeiro-t55.fdb'
Remove-Item $fdb -ErrorAction SilentlyContinue

# App.config local (gitignored): mesmos valores do example, com caminhos temporarios e porta propria.
$cfg = Get-Content (Join-Path $raiz 'src\ERPFinanceiro.Desktop\App.config.example') -Raw
$cfg = $cfg.Replace('value="5000"', 'value="5055"').Replace('COLOQUE_UMA_CHAVE_LOCAL_AQUI', 'CHAVE_LOCAL_T55')
$cfg = $cfg.Replace('C:\ERPFinanceiro\data\financeiro.fdb', $fdb).Replace('C:\ERPFinanceiro\data\logs\app.log', (Join-Path $work 'app.log'))
$cfg = $cfg.Replace('COLOQUE_A_SENHA_LOCAL_AQUI', 'masterkey')
Set-Content (Join-Path $PSScriptRoot 'App.config') $cfg -Encoding UTF8

dotnet build $proj -c Release -p:Platform=x64 -o (Join-Path $work 'bin') | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'build falhou' }
$bin = Join-Path $work 'bin'

# Motor Firebird embarcado ao lado do exe (mesma convencao de T-53).
foreach ($f in 'fbclient.dll','ib_util.dll','icudt52.dll','icudt52l.dat','icuin52.dll','icuuc52.dll','msvcp100.dll','msvcr100.dll','zlib1.dll','firebird.conf','firebird.msg','security3.fdb','plugins.conf') {
  Copy-Item (Join-Path $FirebirdDir $f) $bin -Force
}
Copy-Item (Join-Path $FirebirdDir 'fbclient.dll') (Join-Path $bin 'fbembed.dll') -Force
Copy-Item (Join-Path $FirebirdDir 'plugins') $bin -Recurse -Force
Copy-Item (Join-Path $FirebirdDir 'intl') $bin -Recurse -Force

$env:FIREBIRD = $bin
$env:FIREBIRD_LOCK = if ($env:T55_FIREBIRD_LOCK) { $env:T55_FIREBIRD_LOCK } else { Join-Path $work 'lock' }
New-Item -ItemType Directory -Force $env:FIREBIRD_LOCK | Out-Null

# Banco = schema real + seed real (database/01-schema.sql, 02-seed.sql) via isql embarcado.
$sql = "CREATE DATABASE '$fdb' PAGE_SIZE 8192 DEFAULT CHARACTER SET UTF8;`r`nIN '$raiz\database\01-schema.sql';`r`nIN '$raiz\database\02-seed.sql';`r`nCOMMIT;`r`nQUIT;`r`n"
$tmp = Join-Path $work 'criar.sql'
[IO.File]::WriteAllText($tmp, $sql, (New-Object Text.UTF8Encoding $false))
if (-not $Memoria) {
  & (Join-Path $FirebirdDir 'isql.exe') -q -i $tmp
  if ($LASTEXITCODE -ne 0 -or -not (Test-Path $fdb)) { throw 'falha ao criar o .fdb com o seed' }
}

New-Item -ItemType Directory -Force $Saida | Out-Null
$exe = Join-Path $bin 'T55Evidencias.exe'
$p = Start-Process $exe -ArgumentList ('"' + (Resolve-Path $Saida).Path + '"' + $(if ($Memoria) { ' memoria' } else { '' })) -WorkingDirectory $bin -PassThru -Wait -RedirectStandardOutput (Join-Path $work 'saida.txt') -RedirectStandardError (Join-Path $work 'erro.txt')
Get-Content (Join-Path $work 'saida.txt'), (Join-Path $work 'erro.txt')
exit $p.ExitCode

<#
  T-53 - Verificacao automatizada do criterio de aceite da pasta de entrega.
  Uso: Verificar.ps1 -Pasta <pasta de entrega>. Sai com codigo 1 se algo falhar.
  Checks: (1) .sql e .fdb presentes; (2) DLLs Firebird x64 + exe x64; (3) App.config.example
  presente e App.config real ausente; (4) sem ApiKey/senha real em nenhum arquivo de texto;
  (5) .fdb abre e contem o seed a partir de OUTRO diretorio (copia para %TEMP%, isql).
#>
param([Parameter(Mandatory)][string]$Pasta)
$ErrorActionPreference = 'Stop'
$falhas = @()
function Ok($c, $m) { if ($c) { Write-Host "OK    $m" } else { Write-Host "FALHA $m"; $script:falhas += $m } }
function Maquina($p) {
  $b = [IO.File]::ReadAllBytes($p); $pe = [BitConverter]::ToInt32($b, 0x3C)
  '{0:X}' -f [BitConverter]::ToUInt16($b, $pe + 4)
}
$Pasta = (Resolve-Path $Pasta).Path

Ok ((Get-ChildItem "$Pasta\database\*.sql").Count -ge 2) 'database\*.sql presentes (01-schema, 02-seed)'
Ok ((Get-ChildItem "$Pasta\database\*.fdb").Count -ge 1) '.fdb de demonstracao presente'
Ok (Test-Path "$Pasta\ERPFinanceiro.Desktop.exe") 'executavel presente'
Ok (Test-Path "$Pasta\App.config.example") 'App.config.example presente'
Ok (-not (Test-Path "$Pasta\App.config") -and -not (Test-Path "$Pasta\ERPFinanceiro.Desktop.exe.config")) 'sem App.config real'
foreach ($d in 'fbclient.dll','fbembed.dll','plugins\engine12.dll','intl\fbintl.dll') {
  Ok ((Test-Path "$Pasta\$d") -and (Maquina "$Pasta\$d") -eq '8664') "x64: $d"
}
Ok ((Maquina "$Pasta\ERPFinanceiro.Desktop.exe") -eq '8664') 'exe PE x64 (nao AnyCPU/x86 de 32 bits)'

# Sem segredo: ApiKey diferente do placeholder, ou senha/connection string real
$txt = Get-ChildItem $Pasta -Recurse -File -Include *.config,*.example,*.sql,*.json,*.xml,*.txt,*.md,*.conf |
  Where-Object { $_.Name -ne 'firebird.conf' -and $_.FullName -notmatch '\\(plugins|intl)\\' }
$achados = @()
foreach ($f in $txt) {
  foreach ($l in (Select-String -Path $f.FullName -Pattern '(ApiKey|Senha)"[^>]*value="[^"]*"|Password=[^;"]*')) {
    if ($l.Line -notmatch 'COLOQUE_') { $achados += "$($f.Name): $($l.Line.Trim())" }
  }
}
Ok ($achados.Count -eq 0) 'sem ApiKey/senha real (todo valor e placeholder COLOQUE_*)'
$achados | ForEach-Object { Write-Host "      $_" }

# Roda de outro diretorio: copia e abre o .fdb com o motor embarcado da propria pasta
$alt = Join-Path $env:TEMP ('T53-verif-' + [guid]::NewGuid().ToString('N'))
Copy-Item $Pasta $alt -Recurse
try {
  $fdb = @(Get-ChildItem "$alt\database\*.fdb")[0].FullName
  Set-Location $env:TEMP
  $env:FIREBIRD = $alt
  $env:FIREBIRD_LOCK = if ($env:T53_FIREBIRD_LOCK) { $env:T53_FIREBIRD_LOCK } else { Join-Path $env:TEMP 'ERPFinanceiro-T53\lock' }
  New-Item -ItemType Directory -Force $env:FIREBIRD_LOCK | Out-Null
  $isqlZip = Join-Path $env:TEMP 'ERPFinanceiro-T53\fb\isql.exe'
  $q = Join-Path $env:TEMP 'ERPFinanceiro-T53\q.sql'
  [IO.File]::WriteAllText($q, "CONNECT '$fdb';`r`nSELECT COUNT(*) FROM FIN_VENDA;`r`nQUIT;`r`n")
  $saida = & $isqlZip -q -i $q 2>&1 | Out-String
  Ok ($saida -match '\b3\b') "copia em $alt abre o .fdb e FIN_VENDA tem 3 vendas do seed"
  if ($saida -notmatch '\b3\b') { Write-Host $saida }
} finally { Remove-Item -Recurse -Force $alt -ErrorAction SilentlyContinue }

if ($falhas.Count) { Write-Host "`n$($falhas.Count) falha(s)"; exit 1 }
Write-Host "`nCriterio de aceite T-53: OK"

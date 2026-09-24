# Evidencias (T-55, CA-10.2)

Prints reais da tela Desktop e PDF de exemplo do relatorio. Nada aqui e desenhado a mao: as imagens
sao capturas da tela (`CopyFromScreen`) das janelas reais (`FrmConsulta`, `FrmDetalheVenda`,
`FrmDetalhesStatus`) e o PDF e gerado pelo fluxo real `FluxoEmitirRelatorio` -> `GeradorRelatorioPdf`
(FastReport Open Source, layout `RelatorioListagem.frx`, T-49/T-50). Gerados em 24/09/2026 por
`tools/T-55-evidencias` (harness descartavel, fora da `.sln`, sem alterar codigo de producao).

## Ressalva importante: origem dos dados

Estes arquivos foram gerados no **modo `memoria`** do harness: as 3 vendas do seed (`V-1001` Quitada
500,50; `V-1002` Pendente 450,50; `V-1003` Cancelada sem cliente/valor) com os mesmos valores de
`database/02-seed.sql`, criadas pelas fabricas de dominio (como em `GeradorRelatorioPdfTests`) e
servidas pelo `ConsultaService` real a partir de um repositorio em memoria. **Nao vieram do Firebird
embarcado**: na maquina de geracao, um Firebird embarcado orfao (processo de testes) travava o motor
(o `isql` e o `DbInitializer` ficam pendurados/recusam com "Wrong file for memory mapping"), e o
processo nao foi encerrado. Consequencias, visiveis nas imagens:

- `04-indicador-barra-status.png` e `05-indicador-detalhes-status.png`: o texto "API: Ativa" e
  "Banco: OK" vem de **stubs** (`IFonteEstadoApi`/`IHealthService`), nao da API/banco reais; o caminho
  do banco no dialogo aparece como "(seed em memoria - stub)". Sao a UI real, com estado de entrada
  simulado.
- `03-erro.png`: o estado de erro F-1 e provocado por um carregador que lanca excecao (banco
  indisponivel simulado); e a UI real no estado de erro real, nao uma falha de banco verdadeira.

## Indice

| Arquivo | O que e | Origem |
|---|---|---|
| `01-lista.png` | F-1 lista com as 3 vendas do seed, botao "Emitir relatorio", barra de status | UI real, dados do seed (memoria) |
| `02-detalhe.png` | F-3 detalhe de `V-1001` (2 itens, total 500,50) | UI real, dados do seed (memoria) |
| `03-erro.png` | F-1 estado de erro + "Tentar novamente" | UI real, falha simulada |
| `04-indicador-barra-status.png` | Recorte da barra de status (indicadores API/Banco) da captura 01 | UI real, estado stub |
| `05-indicador-detalhes-status.png` | Dialogo "Detalhes do status" (clique no indicador) | UI real, estado stub |
| `relatorio-exemplo.pdf` | Relatorio financeiro: 3 linhas, Total listado R$ 951,00, nota de 1 valor nulo | Gerador real, dados do seed (memoria) |

## Verificacao do PDF

Cabecalho `%PDF-`, 352.512 bytes, 1 pagina (PdfPig). Total listado R$ 951,00 = 500,50 + 450,50 + 0,00
(nulo como 0, nota de rodape presente), conferido visualmente. O PDF do FastReport Open Source e
**raster** (pagina = JPEG 300 dpi): abre e imprime, mas o texto nao e selecionavel/pesquisavel
(achado ja registrado em T-49). Sem preview WinForms (T-50): a tela so gera o PDF e o abre.

## O que falta e como gerar (dados do Firebird real)

Falta gerar o mesmo conjunto com o **banco Firebird real** (schema + seed via `isql`, API real e health
real). Em maquina sem outro Firebird embarcado rodando:

1. Ter o Firebird 3.0.12 x64 extraido (o zip baixado por `tools/T-53-empacotar/Empacotar.ps1`).
2. `powershell -File tools/T-55-evidencias/Executar.ps1 -FirebirdDir <pasta do Firebird>`
   (se aparecer "Wrong file for memory mapping", defina `T55_FIREBIRD_LOCK` com o lock dir do outro
   Firebird indicado no `firebird.log`, ou feche esse processo).
3. Requer sessao Windows interativa (a captura usa a tela). Para repetir o modo usado aqui:
   adicione `-Memoria`.

Alternativa manual: rodar o Desktop (`ERPFinanceiro.Desktop.exe` com `App.config` local, banco criado
com `database/01-schema.sql` + `02-seed.sql`), usar Win+Shift+S nas telas F-1 (lista), F-3 (Enter na
linha), erro (apontar `Banco:CaminhoFdb` para arquivo invalido) e a barra de status, e clicar
"Emitir relatorio" para obter o PDF em `%TEMP%\ERPFinanceiro\Relatorios`.

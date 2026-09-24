# Integração simulada — T-38/T-39 (O-11a/O-11b, ADR-010)

## SIMULAÇÃO — NÃO é integração real com o Vendas (Delphi)

Este diretório e os harnesses em `tools/T-38-integracao-simulada/` (T-38, quitação/status) e
`tools/T-39-integracao-simulada/` (T-39, cancelamento/reenvio pós-timeout/erros) produzem uma
**suíte de integração simulada**: harnesses HTTP reais fazendo o papel do sistema "Vendas" contra
a API local deste projeto (`ApiHost`+`Startup`+`CompositionRoot.Construir()` reais + Firebird
embarcado real). **Não há acesso ao sistema Vendas em Delphi real neste sandbox**, nem aceite
formal do `docs/contrato-v1.1.md` pelo time Vendas — ambos permanecem pendências externas
explícitas, fora do alcance deste ambiente. Ver:

- `.md/adr/010-integracao-vendas-simulada-sem-acesso-delphi.md` (decisão e alternativas
  consideradas);
- `.md/BLOCKERS.md`, Bloqueio 002 (relato original e decisão do Coordenador que redefiniu
  T-38/T-39).

Todo documento/evidência gerado aqui carrega esse rótulo explicitamente, conforme exigido pelo
critério de aceite de T-38/T-39 e pelo ADR-010.

## T-38 — O que este harness cobre (critério de aceite de T-38)

1. **Venda nova quitada -> 200.**
2. **Repetição da mesma quitação -> idempotente, sem novo histórico (P-2).** Verificado de duas
   formas complementares:
   - resposta HTTP: `dataQuitacao` semanticamente inalterada entre a 1ª e a 2ª chamada
     (comparação por instante, com tolerância de 1 ms — ver nota de precisão TIMESTAMP abaixo,
     em `evidencia-execucao-T-38.txt`);
   - consulta direta ao banco (`VendaRepository.ObterPorVendaId`/`FinanceiroDbContext`, mesmo
     padrão de acesso usado pelo resto da aplicação): contagem de `VendaHistorico` da venda
     antes e depois da repetição — precisa continuar igual (2: `Recebida` + `Quitacao`). Esta é
     a verificação definitiva de "sem novo histórico", já que a API HTTP não expõe o histórico
     de uma venda (fora do escopo do contrato v1.1 atual).
3. **`GET status` reflete o resultado da quitação.**

Todos os 3 pontos executados **via HTTP real** contra a API local (não teste em memória, não
inspeção estática) — Diretriz 16 da Seção 1 do `TASK.md` (evidência de execução real).

## Por que um harness dedicado (não reaproveitar `smoke-vendas.sh` literalmente)

`docs/postman/smoke-vendas.sh` (T-34/T-37) já cobre caminho feliz/400/401/404/409 dos 3
endpoints via curl. T-38 pede evidência própria e, especificamente, uma prova objetiva de "sem
novo histórico" (P-2) — algo que só dá para confirmar com uma consulta direta ao banco (o
contrato HTTP não expõe histórico). Por isso o harness desta tarefa
(`tools/T-38-integracao-simulada/`) é um console C# dedicado que:

- sobe a API real pelo mesmo caminho de produção de `tools/T-34-smoke-harness/` (T-34/T-37),
  em porta dedicada (`5038`, distinta de `5034`);
- dispara as chamadas via `HttpClient` real (não curl externo) fazendo o papel do Vendas;
- consulta o Firebird diretamente entre as chamadas para contar `VendaHistorico` (mesmo
  `VendaRepository` usado pela aplicação, sem SQL solto);
- imprime request/response completos de cada cenário (evidência bruta em
  `evidencia-execucao-T-38.txt`) e sai com código 0/1 conforme o resultado.

## Como rodar

```bash
cd tools/T-38-integracao-simulada
cp App.config.example App.config   # segredo local, gitignorado (mesma convenção de T-04/T-34)
dotnet build
```

DLLs nativas do Firebird embarcado precisam estar em `bin/Debug/net48` — reaproveite as já
presentes em `src/ERPFinanceiro.Tests/bin/Debug/net48` (mesma orientação de
`docs/postman/README.md`, T-34).

Rodando de dentro do OneDrive, aplique o contorno do Bloqueio 001 (`.md/BLOCKERS.md`): copie
`bin/Debug/net48` inteiro para fora do OneDrive antes de executar (ex.: `C:\temp_erp_t38\net48`,
usado na evidência desta tarefa) e rode `T38IntegracaoSimulada.exe` de lá.

O harness é auto-contido: cria/recria o `.fdb` a cada execução, gera um `vendaId` único
(timestamp) e roda os 3 cenários sozinho, sem interação — não fica no ar esperando `parar.txt`
(diferente do harness de T-34, que existe para permitir chamadas externas via curl).

## Resultado desta execução (23/09/2026)

2 execuções seguidas, cada uma com uma instância nova e limpa do harness (banco recriado,
`vendaId` novo): **4/4 verificações OK em ambas** (venda quitada 200; repetição idempotente
confirmada por resposta HTTP + contagem de histórico em banco; `GET status` reflete o
resultado). Evidência bruta completa (request/response de cada cenário) em
`evidencia-execucao-T-38.txt`. Uma observação de precisão de timestamp (não bloqueante,
documentada na própria evidência) foi encontrada e tratada com tolerância semântica em vez de
comparação de string bruta — ver nota ao final de `evidencia-execucao-T-38.txt`.

## T-39 — O que este harness cobre (critério de aceite de T-39)

`tools/T-39-integracao-simulada/` reaproveita a mesma disciplina de `tools/T-38-integracao-
simulada/` (harness C# dedicado, não faz parte da `ERPFinanceiro.sln`, porta própria `5039`,
banco próprio `financeiro_t39_simulado.fdb`) para cobrir os cenários de **cancelamento** do
critério de aceite de T-39 (`docs/contrato-v1.1.md` Seção 3.2, D-06/D-08/P-2):

1. **Cancelamento de venda Pendente -> 200** `{status:"Cancelada"}` (motivo opcional, ausente).
2. **Cancelamento de venda desconhecida -> 200** `{status:"Cancelada"}` (cria já Cancelada, D-08).
3. **Cancelamento de venda Quitada sem motivo -> 409** `{erro:{codigo:"MOTIVO_OBRIGATORIO"}}`
   (D-06 — o próprio agregado `Venda.Cancelar` exige motivo para venda Quitada).
4. **Reenvio após timeout (idempotência, P-2):** o mesmo payload do cenário 1 é reenviado sobre a
   venda já Cancelada, simulando o "Vendas" reenviando a mesma requisição por ter sofrido timeout
   na 1ª resposta sem saber se ela processou. Verificado de duas formas complementares, mesmo
   padrão de T-38:
   - resposta HTTP: mesmo `{status:"Cancelada"}`;
   - consulta direta ao banco (`VendaRepository.ObterPorVendaId`/`FinanceiroDbContext`): contagem
     de `VendaHistorico` da venda antes e depois do reenvio — precisa continuar igual (2 registros:
     `Recebida`+`Cancelamento`, já que a venda seed é `CriarPendente`). Esta é a verificação
     definitiva de "sem duplicar histórico", já que o HTTP por si só não expõe histórico.
5. **Erros 400/401 exercitados de fato:**
   - `5a` — payload inválido (`vendaId` ausente) -> 400 `PAYLOAD_INVALIDO`;
   - `5b` — sem header `X-Api-Key` -> 401 `NAO_AUTORIZADO`;
   - `5c` — `X-Api-Key` errada -> 401 `NAO_AUTORIZADO`, corpo **idêntico** ao de `5b` (T-35:
     nenhuma distinção entre "ausente" e "inválida" é revelada ao chamador).

Todos os 8 pontos executados **via HTTP real** contra a API local (não teste em memória, não
inspeção estática) — Diretriz 16 da Seção 1 do `TASK.md`. Vendas Pendente/Quitada usadas nos
cenários 1/3/4 são semeadas diretamente no banco real antes de subir a API (mesma técnica de
`ApiHostVendasCancelamentoControllerTests`, T-32 — não há endpoint público de "registrar
Pendente" implementado nesta sessão, `POST /api/vendas` é T-62/Tier B condicional).

### Como rodar (T-39)

```bash
cd tools/T-39-integracao-simulada
cp App.config.example App.config   # segredo local, gitignorado (mesma convenção de T-04/T-34/T-38)
dotnet build
```

Mesmo contorno de DLLs nativas do Firebird e do Bloqueio 001 do harness de T-38 (copiar
`bin/Debug/net48` para fora do OneDrive, ex.: `C:\temp_erp_t39\net48`, e rodar
`T39IntegracaoSimulada.exe` de lá). O harness é auto-contido: apaga/recria o `.fdb` a cada
execução, gera `vendaId`s únicos (timestamp) e roda os 8 cenários sozinho, sem interação.

### Resultado desta execução (23/09/2026)

2 execuções seguidas, cada uma com uma instância nova e limpa do harness (banco recriado,
`vendaId`s novos): **8/8 verificações OK em ambas**. Nenhuma divergência encontrada em relação a
`docs/contrato-v1.1.md` Seção 3.2 nem ao comportamento já coberto por
`ApiHostVendasCancelamentoControllerTests` (T-32) — nada a registrar em T-40 por esta tarefa.
Evidência bruta completa (request/response de cada cenário, 2 execuções) em
`evidencia-execucao-T-39.txt`.

## Pendência que esta suíte NÃO resolve

Integração real com o sistema Vendas em Delphi (acesso ao ambiente + aceite formal do
`docs/contrato-v1.1.md` pelo time Vendas) continua **em aberto**, registrada em
`.md/BLOCKERS.md` Bloqueio 002 e no `TASK.md` Seção 6. Deve ser retomada fora deste sandbox,
quando houver acesso ao sistema Delphi real. Isto vale tanto para T-38 quanto para T-39.

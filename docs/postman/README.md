# Coleção de fumaça — T-34 (revisada por T-37)

Autor: Executor (chapéu QA), tarefas T-34 e T-37. Base: `docs/contrato-v1.1.md`
(Seção 2, 3.1-3.3), `TASK.md` Seção 1 (diretriz 16 — evidência de execução real).

## O que é

`smoke-vendas.sh`: coleção de fumaça em **curl real** (não Postman GUI) cobrindo caminho
feliz + 400/401/404/409 dos 3 endpoints do contrato v1.1 já implementados:

- `POST /api/vendas/quitacao` (T-30)
- `POST /api/vendas/cancelamento` (T-32)
- `GET /api/vendas/{vendaId}/status` (T-31)

9 cenários no total (T-34: 7 cenários, 2-3 por endpoint; T-37: +2 cenários `401`):
caminho feliz, `400 PAYLOAD_INVALIDO`, `404 VENDA_NAO_ENCONTRADA`,
`409 VENDA_JA_CANCELADA`, `409 MOTIVO_OBRIGATORIO`, `401 NAO_AUTORIZADO`
(chave ausente / chave inválida).

## Por que curl e não uma coleção `.postman_collection.json`

Este ambiente de execução (sandbox) não tem GUI/Postman instalado nem acesso interativo para
importar/rodar uma coleção Postman. `curl` é o único formato que dava para **efetivamente
executar e confirmar "100% verde contra o app local"** nesta sessão, em vez de produzir só
documentação estática (Diretriz 16 da Seção 1 do `TASK.md` exige evidência de execução real,
não só inspeção). Ver `evidencia-execucao-T-34.txt`/`evidencia-execucao-T-37.txt` para a saída
real capturada. Se o time do Vendas preferir Postman GUI, os mesmos 9 requests podem ser
recriados 1:1 a partir deste script (método, path, headers, corpo) — não foi produzida uma
versão `.json` paralela porque não haveria como confirmar sua execução real aqui.

## Como rodar

Contra a API real (Desktop com `ApiHost` ativo, ou o harness descartável de
`tools/T-34-smoke-harness/`, ver abaixo):

```bash
BASE_URL=http://localhost:5000/api/vendas API_KEY=SUA_CHAVE_LOCAL ./smoke-vendas.sh   # 5000 = porta padrão do App.config.example
```

`BASE_URL` é opcional (default `http://localhost:5000/api/vendas`); `API_KEY` também é
opcional (default `CHAVE_LOCAL_SMOKE_T34` — ajuste para o valor real de `Api:ApiKey` do
seu `App.config` local). O script:
- não precisa de nenhum estado pré-semeado no banco — os cenários `409` são produzidos
  encadeando os próprios endpoints do contrato (ex.: cancelar uma venda desconhecida cria+
  cancela, D-08; depois tentar quitar essa mesma venda dá `409 VENDA_JA_CANCELADA`);
- usa `vendaId` único por execução (sufixo timestamp), então pode ser rodado quantas vezes
  for preciso sem colidir com execuções anteriores (confirmado por 2 execuções seguidas em
  T-34, 7/7 verde, e novamente 2 execuções seguidas em T-37, 9/9 verde — ver
  `evidencia-execucao-T-34.txt` e `evidencia-execucao-T-37.txt`);
- sai com código 0 se os 9 cenários baterem com o esperado, código 1 caso contrário (uso em
  pipeline/CI se algum dia for automatizado — fora do escopo desta tarefa).

## Autenticação (X-Api-Key)

Desde T-35 (Lote 8, `ApiKeyHandler`), a API exige o header `X-Api-Key` em todo
endpoint de vendas (isento só `GET /api/health`, T-36). **T-37** revisitou este
script (originalmente sem cenário `401`, quando T-35 ainda não existia) para:
- adicionar o header `X-Api-Key` (variável `API_KEY`, default
  `CHAVE_LOCAL_SMOKE_T34` — mesmo valor do `App.config` do harness abaixo) às
  9 chamadas de sucesso/400/404/409 — sem o header, todas passariam a dar 401
  depois de T-35;
- adicionar os cenários **8** e **9**, cobrindo os 2 casos de
  `401 NAO_AUTORIZADO` do contrato v1.1 Seção 2 (`docs/contrato-v1.1.md`):
  chave ausente e chave inválida — resposta idêntica nos dois casos, como o
  contrato exige.

Coleção agora com **9 cenários** (7 originais de T-34 + 2 novos de T-37),
confirmada 9/9 verde (duas execuções seguidas) contra a API real — ver
`evidencia-execucao-T-37.txt`.

## Harness descartável (`tools/T-34-smoke-harness/`)

Como este sandbox não tem o Desktop/DevExpress rodando de verdade, foi criado um pequeno
console descartável em `tools/T-34-smoke-harness/` (mesma disciplina de
`spikes/T-01-firebird-spike/` — não faz parte da `ERPFinanceiro.sln`, não é código de
produção) que sobe a API real pelo mesmo caminho de produção (`ApiHost` + `Startup` +
`CompositionRoot.Construir()`, T-26/T-27) contra um Firebird embarcado real, numa porta fixa
(`5034`), e fica no ar até o arquivo `parar.txt` aparecer na pasta de saída. Foi contra essa
instância real que `smoke-vendas.sh` foi executado e confirmado 7/7 verde (duas vezes seguidas)
em `evidencia-execucao-T-34.txt`. Reaproveitável por QA/Executor em sessões futuras para rodar
smoke tests sem precisar do Desktop/DevExpress de pé; não precisa ser mantido sincronizado com
o `.sln` principal.

Para rodar o harness numa sessão nova: `App.config` real é gitignorado (mesma regra global do
`.gitignore` de T-04, "só o `.example` é versionado") — copie
`tools/T-34-smoke-harness/App.config.example` para `tools/T-34-smoke-harness/App.config`
antes de `dotnet build` (sem segredo real: só porta/caminho de `.fdb` local/chave-placeholder,
mesmo padrão de `ERPFinanceiro.Desktop/App.config.example`). DLLs nativas do Firebird
embarcado (`fbclient.dll`/`fbembed.dll`/`security3.fdb`/`plugins/`/`intl/`) precisam estar na
pasta de saída (`bin/Debug/net48`) — reaproveite as já presentes em
`src/ERPFinanceiro.Tests/bin/Debug/net48` (worktrees onde T-30/T-31/T-32 já rodaram) ou repita
o contorno documentado em `spikes/T-01-firebird-spike/README.md`. Rodando de dentro do
OneDrive, aplique o mesmo contorno do Bloqueio 001 (`BLOCKERS.md`): copie
`bin/Debug/net48` para fora do OneDrive antes de executar.

## Marco do Dia 3

Este documento e o script ficam disponíveis em `docs/postman/` a partir desta tarefa (T-34),
cumprindo o critério do marco "API utilizável pelo Vendas até o fim do Dia 3" (Lote 7).
Entrega ao lado Vendas (fora do código): pendência do usuário/orquestrador, mesma situação já
registrada para o envio do `docs/contrato-v1.1.md` em si (T-03).

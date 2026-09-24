# Contrato v1.1 (proposto) — API Financeiro <-> ERP Vendas

> **Status deste documento: PROPOSTA, pronta para envio ao lado Vendas. Ainda NÃO enviada e NÃO aceita.**
> Contrato v1.0 (base, Seção 2.1 da `VISAO-PRODUTO.md`) permanece em vigor até aceite explícito.
> Conforme `GUARDRAILS.md` (regra 20/G-5.20): o congelamento desta v1.1 é **interno** — a linha "1.1" da
> tabela de versões em `VISAO-PRODUTO.md` Seção 2.4 só é preenchida quando o lado Vendas aceitar
> ponto a ponto os itens da seção "Pontos A VALIDAR" abaixo. Este documento não fabrica esse aceite.
> Todas as mudanças propostas em relação à v1.0 são **aditivas** (novos campos/endpoints opcionais,
> nunca remoção/alteração de contrato já em uso).

Autor: Executor (chapéu DOC/OPS), tarefa T-03. Base: `SDD.md` (2.4-2.6, 4.3), `PRD-TECNICO.md`
(RF-01..RF-09, RN-01..RN-08), `VISAO-PRODUTO.md` (2.1, 2.4, Seção "P-1…P-9"), `GUARDRAILS.md` (regra 20).

---

## 1. Envio ao lado Vendas — status (pendência aberta)

| Item | Status |
|---|---|
| Envio deste documento ao lado Vendas (Delphi) | **Pendente** — ação do usuário/orquestrador, não realizada neste ambiente (sem acesso ao lado Vendas) |
| Data de envio | — (a preencher quando ocorrer) |
| Resposta ponto a ponto (aceita/rejeitada por item) | **Pendente** — não simulada; nenhuma aceitação foi fabricada |
| Linha "v1.1" em `VISAO-PRODUTO.md` 2.4 | **Não preenchida** (correto conforme G-5.20; só preencher após aceite) |

Se, quando o envio ocorrer, algum ponto vier **rejeitado**, isso vira entrada em `.md/BLOCKERS.md`
referenciando o ponto rejeitado e a tarefa afetada (ver Seção 4 abaixo) — está fora do escopo desta
tarefa (T-03), que é produzir e deixar pronta a proposta.

---

## 2. Envelope de resposta

**Sucesso:** corpo específico do endpoint (ver Seção 3), sem envelope adicional.

**Erro** — envelope único em todos os endpoints:

```json
{
  "erro": {
    "codigo": "VENDA_JA_CANCELADA",
    "mensagem": "A venda já está cancelada e não pode ser quitada."
  }
}
```

- Nunca inclui stack trace ou mensagem bruta de exceção (`GUARDRAILS.md`, diretriz 7 do `TASK.md` Seção 1).
- Detalhe de diagnóstico fica só no log interno (`IAppLogger`), correlacionado por `correlationId`.
- JSON sempre UTF-8; toda resposta inclui o header `X-Correlation-Id` (o mesmo valor é gravado no
  histórico da venda, quando aplicável).

### Códigos de erro

| Código | HTTP | Quando |
|---|---|---|
| `PAYLOAD_INVALIDO` | 400 | `vendaId`/`clienteId` ausente/vazio, `itens` vazio, `quantidade` <= 0, `precoUnitario` < 0 |
| `VALOR_TOTAL_DIVERGENTE` | 400 | `\|valorTotal - Σ(quantidade × precoUnitario)\| > 0,01` |
| `NAO_AUTORIZADO` | 401 | `X-Api-Key` ausente ou inválida (resposta idêntica nos dois casos) |
| `VENDA_NAO_ENCONTRADA` | 404 | Consulta de status para `vendaId` inexistente |
| `VENDA_JA_CANCELADA` | 409 | Tentativa de quitar venda em estado `Cancelada` |
| `MOTIVO_OBRIGATORIO` | 409 | Cancelar venda `Quitada` sem `motivo` |
| `DADOS_DIVERGENTES` | 409 | Quitar venda `Pendente` com payload que diverge do registrado (I-04 do `PRD-TECNICO.md`) — **novo código, a validar** |
| `CONFLITO_CONCORRENCIA` | 409 | Conflito de `VERSAO` não resolvido após 3 tentativas de releitura |
| `ERRO_INTERNO` | 500 | Falha não tratada; corpo não revela detalhe da exceção |

**409 — conflito de concorrência** (aplicável a `POST /api/vendas/quitacao` e
`POST /api/vendas/cancelamento`; mensagem conforme
`ExceptionParaRespostaMapper.MensagemConflitoConcorrencia`, RL1-01a):
```json
{
  "erro": {
    "codigo": "CONFLITO_CONCORRENCIA",
    "mensagem": "Conflito de concorrência ao processar a operação; tente novamente."
  }
}
```

**500 — erro interno** (aplicável a qualquer endpoint autenticado; mensagem
genérica, sem detalhe da exceção original — regra 7 do `TASK.md` Seção 1;
conforme `ExceptionParaRespostaMapper.MensagemGenericaErroInterno`):
```json
{
  "erro": {
    "codigo": "ERRO_INTERNO",
    "mensagem": "Ocorreu um erro interno inesperado. Consulte o suporte informando o horário da operação."
  }
}
```

---

## 3. Endpoints

Base v1.0 (já acordada, preservada sem alteração de comportamento):
- `POST /api/vendas/quitacao`
- `POST /api/vendas/cancelamento`
- `GET /api/vendas/{vendaId}/status`

Aditivos propostos na v1.1 (dependem de pontos A VALIDAR, ver Seção 4):
- `POST /api/vendas` (registrar Pendente — D-03/P-9)
- `GET /api/health` (sem autenticação — P-5)
- alias `/api/v1/vendas/...` para os três endpoints acima (D-05/P-8)

Autenticação: header `X-Api-Key` obrigatório em todos os endpoints exceto `GET /api/health` (P-4/P-5).

### 3.1 `POST /api/vendas/quitacao`

Entrada:
```json
{
  "vendaId": "V-000123",
  "clienteId": "C-4521",
  "valorTotal": 1250.50,
  "itens": [
    { "produtoId": "P-01", "quantidade": 2, "precoUnitario": 500.00 },
    { "produtoId": "P-02", "quantidade": 1, "precoUnitario": 250.50 }
  ]
}
```

**200 — sucesso (venda inexistente, criada e quitada / Pendente quitada / já Quitada idempotente):**
```json
{
  "status": "Quitada",
  "dataQuitacao": "2026-09-22T14:05:00Z"
}
```

**400 — payload inválido** (`itens` vazio):
```json
{
  "erro": {
    "codigo": "PAYLOAD_INVALIDO",
    "mensagem": "O campo 'itens' não pode ser vazio."
  }
}
```

**400 — valor total divergente:**
```json
{
  "erro": {
    "codigo": "VALOR_TOTAL_DIVERGENTE",
    "mensagem": "valorTotal (1250.50) diverge da soma dos itens (1240.00) além da tolerância de 0,01."
  }
}
```

> **Nota (T-37, validado contra a API real):** os números dentro desta
> mensagem são formatados com a *culture* corrente do processo da Api
> (`ValidadorVendaCommand`, interpolação de string simples), não com
> `CultureInfo.InvariantCulture`. Em host com *culture* pt-BR — caso
> confirmado nesta validação — a mensagem real sai com vírgula decimal
> (ex.: `"valorTotal (1250,50) diverge da soma dos itens (1240,00) além da
> tolerância de 0,01."`), diferente do separador `.` usado no exemplo acima
> e do formato de número JSON exigido pelo P-6 para os *campos* do corpo
> (este texto é mensagem legível, não um campo JSON tipado — P-6 não se
> aplica a ele). Como o separador depende da *culture* do host de produção
> (não determinístico entre ambientes), o lado Vendas **não deve fazer
> parsing** desse texto — só do campo `codigo`. Ajuste de código (forçar
> `CultureInfo.InvariantCulture` em `ValidadorVendaCommand`) é uma
> correção pontual de implementação, fora do escopo de T-37 (que só
> valida/ajusta este documento); sinalizado aqui para follow-up.

**401 — sem `X-Api-Key` ou chave inválida:**
```json
{
  "erro": {
    "codigo": "NAO_AUTORIZADO",
    "mensagem": "Credencial de acesso ausente ou inválida."
  }
}
```

**409 — venda já cancelada** (mensagem ajustada por T-37 — ver nota abaixo):
```json
{
  "erro": {
    "codigo": "VENDA_JA_CANCELADA",
    "mensagem": "Venda 'V-000123' já está cancelada; Cancelada é um estado terminal."
  }
}
```

**409 — dados divergentes (quitar Pendente com payload diferente do registrado; A VALIDAR D-03/I-04)** (mensagem ajustada por T-37 — ver nota abaixo):
```json
{
  "erro": {
    "codigo": "DADOS_DIVERGENTES",
    "mensagem": "O payload de quitação diverge dos dados registrados na venda Pendente 'V-000123'."
  }
}
```

### 3.2 `POST /api/vendas/cancelamento`

Entrada:
```json
{
  "vendaId": "V-000123",
  "motivo": "Cliente desistiu da compra"
}
```

**200 — sucesso (Pendente cancelada / já Cancelada idempotente / desconhecida criada já Cancelada, D-08):**
```json
{
  "status": "Cancelada"
}
```

**400 — payload inválido** (`vendaId` ausente):
```json
{
  "erro": {
    "codigo": "PAYLOAD_INVALIDO",
    "mensagem": "O campo 'vendaId' é obrigatório."
  }
}
```

**401 — chave inválida:** igual ao exemplo da Seção 3.1.

**404 — alternativa a D-08, se venda desconhecida for rejeitada como "cria Cancelada"** (mensagem ajustada por T-37 — ver nota abaixo):
```json
{
  "erro": {
    "codigo": "VENDA_NAO_ENCONTRADA",
    "mensagem": "Venda 'V-000123' não encontrada."
  }
}
```

**409 — cancelar Quitada sem motivo** (mensagem ajustada por T-37 — ver nota abaixo):
```json
{
  "erro": {
    "codigo": "MOTIVO_OBRIGATORIO",
    "mensagem": "Motivo do cancelamento é obrigatório."
  }
}
```

### 3.3 `GET /api/vendas/{vendaId}/status`

**200 — sucesso:**
```json
{
  "vendaId": "V-000123",
  "status": "Quitada"
}
```

Valores possíveis de `status` (string, PascalCase — P-3): `"Pendente"`, `"Quitada"`, `"Cancelada"`.

**401 — chave inválida:** igual ao exemplo da Seção 3.1.

**404 — venda inexistente** (mensagem ajustada por T-37 — ver nota abaixo):
```json
{
  "erro": {
    "codigo": "VENDA_NAO_ENCONTRADA",
    "mensagem": "Venda 'V-000999' não encontrada."
  }
}
```

### 3.4 `POST /api/vendas` (aditivo — registrar Pendente; A VALIDAR D-03/P-9)

Entrada: mesmo payload de `POST /api/vendas/quitacao`.

**200 — sucesso (criada Pendente / já existente, idempotente):**
```json
{
  "status": "Pendente"
}
```

**400/401:** mesmos exemplos e códigos das Seções 3.1/3.3.

### 3.5 `GET /api/health` (aditivo — sem autenticação; A VALIDAR P-5)

**200 — banco acessível:**
```json
{
  "status": "ok",
  "banco": "ok"
}
```

**503 — banco inacessível (campo `banco` é aditivo em relação ao contrato-base P-5):**
```json
{
  "status": "degradado",
  "banco": "falha"
}
```

Este endpoint não usa o envelope `{erro:{...}}` (não é um erro de requisição, é um status de saúde).

### 3.6 Alias `/api/v1/vendas/...` (aditivo — A VALIDAR D-05/P-8)

Cada rota acima também responde de forma idêntica prefixada por `/api/v1`
(ex.: `POST /api/v1/vendas/quitacao`), mapeada ao mesmo controller. Rota canônica
`/api/vendas/...` é a acordada na v1.0 e não muda de comportamento.

### 3.7 Validação dos exemplos contra a API real (T-37)

Autor: Executor (chapéu QA), tarefa T-37. Base: `docs/postman/smoke-vendas.sh`
(T-34, revisado por esta tarefa) rodado contra a Api real via
`tools/T-34-smoke-harness/` (mesmo caminho de produção `ApiHost` + `Startup` +
`CompositionRoot.Construir()`), Firebird embarcado real. Evidência bruta em
`docs/postman/evidencia-execucao-T-37.txt`.

Todos os exemplos JSON das Seções 2, 3.1, 3.2, 3.3 e 3.5 foram reproduzidos e
comparados campo a campo, código a código, com a resposta real da Api
(exceto valores dinâmicos: `dataQuitacao`, `vendaId` gerado por execução,
timestamps). Resultado:

- **Corpo de sucesso** (200 quitação/cancelamento/status, 200/503 health) e
  **envelope de erro** (`{erro:{codigo,mensagem}}`), **401** (Seção 2/3.1,
  as duas variantes "ausente"/"inválida") e **400** `PAYLOAD_INVALIDO`
  (`itens` vazio e `vendaId` ausente) e `VALOR_TOTAL_DIVERGENTE` (formato):
  batem **byte a byte** com a API real (à parte da nota sobre separador
  decimal em `VALOR_TOTAL_DIVERGENTE`, Seção 3.1 acima).
- **Divergências de texto de `mensagem` encontradas e corrigidas neste
  documento** (código de erro e HTTP status batiam; só o texto divergia —
  a API é a fonte de verdade agora que está implementada, não a proposta
  escrita em T-03 antes da implementação):
  - `409 VENDA_JA_CANCELADA` (Seção 3.1): mensagem real usa aspas simples ao
    redor do `vendaId` e frase diferente ("... é um estado terminal.", em vez
    de "... e não pode ser quitada.").
  - `409 DADOS_DIVERGENTES` (Seção 3.1): mensagem real usa aspas simples ao
    redor do `vendaId`.
  - `404 VENDA_NAO_ENCONTRADA` (Seções 3.2 e 3.3): mensagem real usa aspas
    simples ao redor do `vendaId` (contrato não tinha aspas).
  - `409 MOTIVO_OBRIGATORIO` (Seção 3.2): mensagem real é mais curta e não
    cita o campo `motivo` nem o `vendaId` ("Motivo do cancelamento é
    obrigatório.").
  - `400 PAYLOAD_INVALIDO`/`vendaId` obrigatório (Seção 3.2) e `401`
    (Seções 2/3.1) **já batiam exatamente**, sem ajuste necessário.
- Endpoint aditivo `POST /api/vendas` (Seção 3.4, D-03/P-9) **não foi
  validado** nesta tarefa: ainda não implementado (T-62, Tier B
  condicional) — os exemplos dessa seção continuam sendo só a proposta,
  sem confirmação contra API real.
- Alias `/api/v1/vendas/...` (Seção 3.6): confirmado espelhando exatamente
  o comportamento das rotas canônicas (`quitacao` e `status` testados).

---

## 4. Campos e formatos aditivos (compatíveis com v1.0, sem quebra)

| Campo/formato | Regra | Ponto |
|---|---|---|
| `banco` em `GET /api/health` | Aditivo, campo extra no corpo 200/503 | P-5 |
| `DADOS_DIVERGENTES` | Novo código de erro 409, não existia na v1.0 | I-04, ligado a D-03 |
| Datas | ISO 8601 UTC, ex. `2026-09-22T14:05:00Z` | P-6 |
| Decimais | Número JSON com ponto, 2 casas em `valorTotal`, 4 em `precoUnitario` | P-6 |
| `status` | String PascalCase (`Pendente`/`Quitada`/`Cancelada`) | P-3 |
| `X-Correlation-Id` | Header de resposta em toda chamada, aditivo, não obrigatório de leitura pelo Vendas | — |

---

## 5. Pontos A VALIDAR (aceite pendente do lado Vendas)

Cada linha lista a premissa assumida nesta proposta (conforme `SDD.md` 4.3) e o impacto se o lado
Vendas rejeitar o ponto.

| Ponto | Descrição | Premissa assumida nesta proposta | Impacto se rejeitado |
|---|---|---|---|
| **D-03 / P-9** | Existência do endpoint `POST /api/vendas` (registrar Pendente) e upsert na quitação | Endpoint existe; quitação faz upsert quando a venda já está Pendente | Remover `POST /api/vendas` do contrato; remover linha "pendente" do relatório (T-59/T-60); cancelar T-61/T-62; justificativa registrada |
| **D-04 / ADR-004** | API hospedada via OWIN self-host dentro do próprio Desktop (não afeta o formato JSON, mas afeta disponibilidade/porta) | API roda embutida no app Desktop, porta configurável | Não é ponto de aceite do Vendas propriamente (decisão interna de hospedagem); mantido aqui só por rastreabilidade — mudança de hospedagem reabre ADR-004 com o Coordenador, sem impacto no formato JSON deste contrato |
| **D-05 / P-8** | Alias `/api/v1/vendas/...` mapeado aos mesmos controllers | Alias ativo e espelha 100% o comportamento da rota canônica | Desativar alias (mudança de 1 linha de roteamento); rota canônica `/api/vendas/...` seguiria sendo a única |
| **D-06** | Cancelar venda `Quitada` exige `motivo` não vazio (409 `MOTIVO_OBRIGATORIO` se ausente) | Motivo obrigatório nesse caso específico | Ajustar regra (ex.: motivo opcional) sem impacto estrutural; muda só validação e critério de aceite de T-09/T-19/T-32/T-39 |
| **D-08** | Cancelamento de `vendaId` desconhecido cria o registro já `Cancelada` (200), com `clienteId`/`valorTotal` nulos, em vez de `404 VENDA_NAO_ENCONTRADA` | Cria Cancelada; colunas `CLIENTE_ID`/`VALOR_TOTAL` anuláveis no schema | Reverter para 404 `VENDA_NAO_ENCONTRADA`; colunas voltam a `NOT NULL`; função `Venda.CriarCancelada` (T-10) fica sem uso |
| **P-1** | Envelope de erro `{erro:{codigo,mensagem}}` e mapeamento de exceção para HTTP 400/401/404/409/500 | Formato aceito como está | Ajustar formato do envelope conforme o padrão que o Vendas já consome de outras integrações, se houver |
| **P-2** | Idempotência: quitação/cancelamento repetidos devolvem 200 com o resultado original, sem novo histórico; quitar Cancelada é sempre 409 | Comportamento idempotente aceito | Ajustar semântica de repetição (ex.: Vendas preferir erro em vez de 200 idempotente); revisão pontual dos serviços de quitação/cancelamento |
| **P-3** | Valores de `status` como string PascalCase (`Pendente`/`Quitada`/`Cancelada`) | Formato aceito | Trocar para outro formato (ex. minúsculas, código numérico) é mudança pontual no serializer/DTO |
| **P-4** | Autenticação por header `X-Api-Key` em todos os endpoints exceto health | Mecanismo aceito pelo Vendas | Trocar mecanismo de autenticação (ex. outro header/esquema) — revisão do `ApiKeyHandler` |
| **P-5** | `GET /api/health` sem autenticação, incluindo campo aditivo `banco` | Sem autenticação; campo `banco` aceito como aditivo | Se exigir autenticação, ajuste simples no handler; campo `banco` pode ser removido do corpo sem quebra caso não seja aceito |
| **P-6** | Datas ISO 8601 UTC; decimais como número JSON (não string) | Formato aceito | Mudança de formato de serialização, pontual no `JsonSerializerSettings` |
| **P-7** | Tolerância de R$ 0,01 na validação `valorTotal ≈ Σ(quantidade × precoUnitario)` | Tolerância de 0,01 aceita | Ajustar constante de tolerância; sem impacto estrutural |
| **P-8** | Ver D-05 acima (mesmo ponto) | — | — |
| **P-9** | Ver D-03 acima (mesmo ponto) | — | — |

Observação: P-8 é o mesmo ponto de D-05 e P-9 é o mesmo ponto de D-03 na nomenclatura da
`VISAO-PRODUTO.md` (2.1) — listados separadamente aqui só para cobrir a nomenclatura "P-1…P-9"
citada no critério de aceite desta tarefa.

---

## 6. Regra de congelamento (G-5.20)

- Este documento é a **proposta interna** de contrato v1.1, pronta para envio.
- Envio ao lado Vendas: pendência do usuário/orquestrador (fora do alcance deste ambiente de execução).
- Enquanto não houver aceite ponto a ponto, a v1.1 **não é vigente** — continua valendo o contrato
  v1.0 (Seção 2.1 da `VISAO-PRODUTO.md`) para o que já está implementado.
- A linha "1.1" da tabela de versionamento em `VISAO-PRODUTO.md` Seção 2.4 permanece em branco até
  esse aceite (não foi preenchida por esta tarefa).
- Se, após o envio real, algum ponto vier rejeitado: abrir entrada em `.md/BLOCKERS.md` referenciando
  o ponto e a(s) tarefa(s) afetada(s) (tabela da Seção 4 acima espelha a tabela `SDD.md` 4.3), fora do
  escopo desta tarefa.

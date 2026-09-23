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

**401 — sem `X-Api-Key` ou chave inválida:**
```json
{
  "erro": {
    "codigo": "NAO_AUTORIZADO",
    "mensagem": "Credencial de acesso ausente ou inválida."
  }
}
```

**409 — venda já cancelada:**
```json
{
  "erro": {
    "codigo": "VENDA_JA_CANCELADA",
    "mensagem": "A venda V-000123 já está cancelada e não pode ser quitada."
  }
}
```

**409 — dados divergentes (quitar Pendente com payload diferente do registrado; A VALIDAR D-03/I-04):**
```json
{
  "erro": {
    "codigo": "DADOS_DIVERGENTES",
    "mensagem": "O payload de quitação diverge dos dados registrados na venda Pendente V-000123."
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

**404 — alternativa a D-08, se venda desconhecida for rejeitada como "cria Cancelada":**
```json
{
  "erro": {
    "codigo": "VENDA_NAO_ENCONTRADA",
    "mensagem": "Venda V-000123 não encontrada."
  }
}
```

**409 — cancelar Quitada sem motivo:**
```json
{
  "erro": {
    "codigo": "MOTIVO_OBRIGATORIO",
    "mensagem": "Cancelar uma venda quitada exige o campo 'motivo'."
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

**404 — venda inexistente:**
```json
{
  "erro": {
    "codigo": "VENDA_NAO_ENCONTRADA",
    "mensagem": "Venda V-000999 não encontrada."
  }
}
```

### 3.3.1 Erros transversais (qualquer endpoint)

**409 — conflito de concorrência** (`VERSAO` não resolvida após 3 tentativas de releitura; o cliente pode repetir a requisição):
```json
{
  "erro": {
    "codigo": "CONFLITO_CONCORRENCIA",
    "mensagem": "Conflito de concorrência ao atualizar a venda V-000123. Tente novamente."
  }
}
```

**500 — erro interno** (mensagem genérica; nunca inclui stack trace nem detalhe da exceção):
```json
{
  "erro": {
    "codigo": "ERRO_INTERNO",
    "mensagem": "Erro interno. Tente novamente mais tarde."
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

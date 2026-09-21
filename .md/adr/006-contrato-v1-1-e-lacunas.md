# ADR-006 — Contrato v1.1 (P-1…P-9), rotas, API Key e códigos de erro

- **Status:** Proposto (A VALIDAR com o Vendas: P-1…P-9, D-03, D-05, D-06) · **Data:** 21/09/2026
- **Contexto:** Contrato v1.0 (3 endpoints) tem lacunas de erro, idempotência, pendente, auth, formatos. Não há acesso ao código do Vendas. O registro 2.4 do VISAO-PRODUTO é a fonte única.
- **Decisão (assumida):** adotar P-1…P-9 como aditivas: envelope de erro; idempotência (P-2); status PascalCase; `X-Api-Key`; `GET /api/health`; datas UTC ISO 8601 e decimais como número; tolerância 0,01 (`VALOR_TOTAL_DIVERGENTE`); rota canônica `/api/vendas` com alias `/api/v1/vendas` (duas rotas para o mesmo controller); `POST /api/vendas` = Pendente e quitação como upsert (D-03); cancelar Quitada só com motivo (D-06, `MOTIVO_OBRIGATORIO`); código extra `DADOS_DIVERGENTES` (I-04) e `CONFLITO_CONCORRENCIA`.
- **Compatibilidade:** o Vendas só precisa ignorar campos e endpoints que não usa; o comportamento v1.0 (3 endpoints e campos) não é alterado. Única mudança visível de comportamento: erros passam a ter envelope e quitação repetida é 200.
- **Plano de contingência:** se o Vendas rejeitar algum item, ele é desligado por regra localizada (ex.: D-03 rejeitada remove `RegistroService`, o endpoint `POST /api/vendas` e "pendente" do relatório, ADR novo).
- **Procedimento:** este ADR **não** escreve a linha v1.1 em VISAO-PRODUTO 2.4; o Gestor/usuário a preenche após aceite do Vendas. Exemplos JSON reais entram em `docs/contrato/`.
- **Consequências:** (+) contrato robusto e testável. (-) dependente de aceite externo até o marco do Dia 3.

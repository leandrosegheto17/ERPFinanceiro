# ADR-005 — Concorrência otimista por `VERSAO` + UNIQUE(VENDA_ID) + transação

- **Status:** Aceito · **Data:** 21/09/2026
- **Contexto:** Vendas pode reenviar por timeout e disparar requisições simultâneas (S-04, CA-01.7, CA-05.3). Firebird não tem `rowversion`.
- **Alternativas:** trigger `BEFORE UPDATE` incrementando `VERSAO` (depende do EF reler coluna computada no provider Firebird, risco de dupla incrementação); lock pessimista (`SELECT ... WITH LOCK`, mais frágil no EF6); incremento pela aplicação com `WHERE VERSAO = :lida` (escolhida).
- **Decisão:** `VERSAO` INTEGER NOT NULL DEFAULT 0, mapeada como `IsConcurrencyToken`, incrementada **pela aplicação** no mesmo `UPDATE`; sem trigger. Criação concorrente barrada por `UNIQUE(VENDA_ID)`. Venda + itens + histórico numa transação. Perdedor: rollback, relê, reavalia (idempotente ou 409); até 3 tentativas, depois 409 `CONFLITO_CONCORRENCIA`. Se o spike (ADR-002) mostrar que o EF não detecta o conflito, usar `UPDATE` explícito via `ExecuteSqlCommand` e checar linhas afetadas.
- **Consequências:** (+) simples, testável (teste com duas tasks). (-) qualquer escrita fora da aplicação (script manual) não incrementa `VERSAO`; documentar.

# ADR-007 — Cancelamento de venda desconhecida e colunas anuláveis (I-03/I-03b)

- **Status:** Proposto (A VALIDAR D-08; assumida a recomendação) · **Data:** 21/09/2026
- **Contexto:** Cancelamento traz só `vendaId`/`motivo`. Criar registro Cancelada exige `CLIENTE_ID` e `VALOR_TOTAL` vazios, conflitando com NOT NULL do modelo.
- **Alternativas:** (a) 404 `VENDA_NAO_ENCONTRADA` (mais simples, Vendas trata); (b) criar Cancelada com placeholders (`""`/0, dado falso); (c) criar Cancelada com colunas anuláveis (escolhida).
- **Decisão:** `FIN_VENDA.CLIENTE_ID` e `VALOR_TOTAL` passam a NULL. Sem itens. Histórico com `STATUS_ANTERIOR` NULL, operação Cancelamento. Totais do relatório tratam NULL como 0; a tela mostra "—" e marca a origem. Quitação posterior desta venda é 409 (Cancelada é terminal).
- **Reversão:** se D-08 for rejeitada, voltar colunas a NOT NULL e responder 404 (ADR novo); impacto: DDL, uma regra do `CancelamentoService` e um teste.
- **Consequências:** (+) o Vendas não precisa tratar 404. (-) registros incompletos em consulta; validação "NOT NULL lógico" na aplicação para quitação/pendente.

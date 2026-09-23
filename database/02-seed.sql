-- ERP Financeiro — database/02-seed.sql
-- Massa conhecida para demo e teste do relatório (T-06). Depende de 01-schema.sql
-- já aplicado no mesmo .fdb. Aplicado por: isql -i 02-seed.sql (após 01-schema.sql),
-- ou em runtime por FbScript/FbBatchExecution (FirebirdSql.Data.Isql) — mesmo
-- mecanismo do DbInitializer (T-12).
--
-- 3 vendas, cada uma coerente com sua trajetória de estado (histórico append-only,
-- regra 8 do TASK.md Seção 1):
--
-- 1) V-1001 — Quitada. Cliente CLI-001. Itens: PROD-001 (2 x 150.0000 = 300.00) +
--    PROD-002 (1 x 200.5000 = 200.50). VALOR_TOTAL = 500.50 (bate com a soma dos
--    itens). Histórico: Recebida (status 0) -> Quitação (status 0 -> 1).
--
-- 2) V-1002 — Pendente. Cliente CLI-002. Itens: PROD-003 (3 x 100.0000 = 300.00) +
--    PROD-004 (2 x 75.2500 = 150.50). VALOR_TOTAL = 450.50 (bate com a soma dos
--    itens). Histórico: só Recebida (status NULL -> 0), sem quitação/cancelamento.
--
-- 3) V-1003 — Cancelada sem quitação prévia, cenário D-08 "venda desconhecida"
--    (ADR-007): CLIENTE_ID e VALOR_TOTAL NULL. SEM itens (decisão explícita do
--    ADR-007: "Sem itens"). Histórico com uma única entrada Cancelamento
--    (STATUS_ANTERIOR NULL, STATUS_NOVO 2) — não há Recebida prévia porque o
--    cancelamento chega para uma venda nunca antes registrada neste sistema
--    (só recebemos vendaId/motivo do cancelamento). DATA_RECEBIMENTO é
--    NOT NULL no schema; como não há evento de recebimento conhecido, o script
--    usa a própria data/hora do cancelamento (interpretação documentada aqui,
--    já que o registro só passa a existir neste sistema nesse instante).
--
-- Nota de interpretação (desvio pequeno de leitura da tarefa, resolvido e
-- documentado pelo Executor/chapéu DB, conforme guardrail de "desvio pequeno"):
-- a linha T-06 do TASK.md ilustra genericamente "Cancelada-sem-quitação tem só
-- Recebida->Cancelamento", mas o cenário concretamente pedido na mesma linha é
-- explicitamente o D-08 (CLIENTE_ID/VALOR_TOTAL nulos "nessa última"), que é
-- justamente o caso "venda desconhecida" do ADR-007 — cuja decisão já tomada
-- e documentada é SEM itens e histórico só com Cancelamento (STATUS_ANTERIOR
-- NULL), sem Recebida prévia. Este script segue o ADR-007 (decisão já
-- ratificada) em vez do exemplo ilustrativo genérico da linha da tarefa, para
-- não contradizer uma decisão de arquitetura já tomada.
--
-- Soma da massa (insumo de CA-07.3, PRD-TECNICO.md: "Total listado" = soma dos
-- VALOR_TOTAL da lista, nulos tratados como 0 — SDD.md 2.1/T-48):
--   V-1001 (Quitada):    500.50
--   V-1002 (Pendente):   450.50
--   V-1003 (Cancelada):  NULL -> tratado como 0.00
--   TOTAL DA MASSA (CA-07.3): 951.00
--
-- Datas em UTC (regra 4 do TASK.md Seção 1). VENDA_REF resolvido por subselect
-- em VENDA_ID (evita depender do valor gerado pelo GENERATED AS IDENTITY).

-- =====================================================================
-- V-1001 — Quitada
-- =====================================================================
INSERT INTO FIN_VENDA (VENDA_ID, CLIENTE_ID, VALOR_TOTAL, STATUS, DATA_RECEBIMENTO, DATA_QUITACAO, DATA_CANCELAMENTO, MOTIVO_CANCELAMENTO, VERSAO)
VALUES ('V-1001', 'CLI-001', 500.50, 1, TIMESTAMP '2026-09-01 10:00:00', TIMESTAMP '2026-09-03 14:30:00', NULL, NULL, 1);

INSERT INTO FIN_VENDA_ITEM (VENDA_REF, PRODUTO_ID, QUANTIDADE, PRECO_UNITARIO)
SELECT ID, 'PROD-001', 2, 150.0000 FROM FIN_VENDA WHERE VENDA_ID = 'V-1001';

INSERT INTO FIN_VENDA_ITEM (VENDA_REF, PRODUTO_ID, QUANTIDADE, PRECO_UNITARIO)
SELECT ID, 'PROD-002', 1, 200.5000 FROM FIN_VENDA WHERE VENDA_ID = 'V-1001';

INSERT INTO FIN_VENDA_HISTORICO (VENDA_REF, OPERACAO, STATUS_ANTERIOR, STATUS_NOVO, DATA_HORA, MOTIVO, CORRELATION_ID)
SELECT ID, 0, NULL, 0, TIMESTAMP '2026-09-01 10:00:00', NULL, 'seed-v1001-recebida' FROM FIN_VENDA WHERE VENDA_ID = 'V-1001';

INSERT INTO FIN_VENDA_HISTORICO (VENDA_REF, OPERACAO, STATUS_ANTERIOR, STATUS_NOVO, DATA_HORA, MOTIVO, CORRELATION_ID)
SELECT ID, 1, 0, 1, TIMESTAMP '2026-09-03 14:30:00', NULL, 'seed-v1001-quitacao' FROM FIN_VENDA WHERE VENDA_ID = 'V-1001';

-- =====================================================================
-- V-1002 — Pendente
-- =====================================================================
INSERT INTO FIN_VENDA (VENDA_ID, CLIENTE_ID, VALOR_TOTAL, STATUS, DATA_RECEBIMENTO, DATA_QUITACAO, DATA_CANCELAMENTO, MOTIVO_CANCELAMENTO, VERSAO)
VALUES ('V-1002', 'CLI-002', 450.50, 0, TIMESTAMP '2026-09-05 09:15:00', NULL, NULL, NULL, 0);

INSERT INTO FIN_VENDA_ITEM (VENDA_REF, PRODUTO_ID, QUANTIDADE, PRECO_UNITARIO)
SELECT ID, 'PROD-003', 3, 100.0000 FROM FIN_VENDA WHERE VENDA_ID = 'V-1002';

INSERT INTO FIN_VENDA_ITEM (VENDA_REF, PRODUTO_ID, QUANTIDADE, PRECO_UNITARIO)
SELECT ID, 'PROD-004', 2, 75.2500 FROM FIN_VENDA WHERE VENDA_ID = 'V-1002';

INSERT INTO FIN_VENDA_HISTORICO (VENDA_REF, OPERACAO, STATUS_ANTERIOR, STATUS_NOVO, DATA_HORA, MOTIVO, CORRELATION_ID)
SELECT ID, 0, NULL, 0, TIMESTAMP '2026-09-05 09:15:00', NULL, 'seed-v1002-recebida' FROM FIN_VENDA WHERE VENDA_ID = 'V-1002';

-- =====================================================================
-- V-1003 — Cancelada sem quitação prévia (D-08, "venda desconhecida" — ADR-007)
-- =====================================================================
INSERT INTO FIN_VENDA (VENDA_ID, CLIENTE_ID, VALOR_TOTAL, STATUS, DATA_RECEBIMENTO, DATA_QUITACAO, DATA_CANCELAMENTO, MOTIVO_CANCELAMENTO, VERSAO)
VALUES ('V-1003', NULL, NULL, 2, TIMESTAMP '2026-09-07 16:45:00', NULL, TIMESTAMP '2026-09-07 16:45:00', 'Venda nao localizada no sistema de origem (D-08)', 0);

INSERT INTO FIN_VENDA_HISTORICO (VENDA_REF, OPERACAO, STATUS_ANTERIOR, STATUS_NOVO, DATA_HORA, MOTIVO, CORRELATION_ID)
SELECT ID, 2, NULL, 2, TIMESTAMP '2026-09-07 16:45:00', 'Venda nao localizada no sistema de origem (D-08)', 'seed-v1003-cancelamento' FROM FIN_VENDA WHERE VENDA_ID = 'V-1003';

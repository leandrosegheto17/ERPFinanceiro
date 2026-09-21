# ADR-003 — Banco próprio, sem FK com o Vendas

- **Status:** Aceito (D-02, mantida pelo usuário) · **Data:** 21/09/2026
- **Contexto:** Módulos separados que só se integram por API; Vendas também usa Firebird.
- **Alternativas:** banco compartilhado com FK (acopla schemas, exige acesso ao banco do Vendas que não temos); banco próprio (escolhida).
- **Decisão:** `.fdb` exclusivo do Financeiro. `VENDA_ID`, `CLIENTE_ID`, `PRODUTO_ID` são referências externas sem integridade referencial; Financeiro não valida existência (RN-08).
- **Consequências:** (+) desacoplamento, comportamento realista. (-) dados órfãos/inconsistentes possíveis; reconciliação via `GET status` e idempotência; cliente exibido por ID (D-10).

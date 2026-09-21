# ADR-002 — Firebird 3.0 embarcado, spike do Dia 1 e fallback SQL Server Express

- **Status:** Aceito (D-01 decidida pelo usuário). **Go/no-go do spike: PENDENTE** — registrar o resultado ao final do spike, em nota de rodapé "Resultado do spike" neste ADR (é o único trecho editável; mudança de decisão gera ADR novo que supersede este).
- **Data:** 21/09/2026
- **Contexto:** Vendas usa Firebird 3.0; custo zero; provider EF6 Firebird é menos maduro, sem migrations e sem `rowversion`.
- **Alternativas:** SQL Server 2022 Express/LocalDB (nativo no EF6, `rowversion`, migrations; exige instalação); Firebird servidor (instalação); Firebird embarcado (escolhida).
- **Decisão:** Firebird 3.0 embarcado, `.fdb` próprio, `EntityFramework.Firebird` + `FirebirdSql.Data.FirebirdClient`, DDL manual em `database/`, `VERSAO` INT, identificadores <= 31, DECIMAL <= 18.
- **Spike (~2h, primeira tarefa do Dia 1), critérios objetivos:**
  1. Abrir `.fdb` embarcado e criar schema pelo DDL, com DLLs na bitness escolhida;
  2. EF6 grava `FIN_VENDA` + itens; lê `DECIMAL(18,2)` e `(18,4)` sem perda;
  3. Identity/generator retorna o ID ao EF;
  4. Duas escritas concorrentes na mesma venda: a segunda falha com `DbUpdateConcurrencyException` por `VERSAO`;
  5. Violação de `UNIQUE(VENDA_ID)` é distinguível (exceção mapeável);
  6. Filtro por período/status com `Skip/Take` funciona.
  **Go:** 1-5 passam. **No-go:** qualquer um de 1-5 falha e não se resolve em +1h (limite total ~3h; até 4h em O-01 com instalação de trials).
- **Fallback:** SQL Server 2022 Express/LocalDB. Impacto limitado a Infrastructure e ao DDL (`ROWVERSION` no lugar de `VERSAO`, tipos nativos, migrations opcionais); Domain/Application/Api não mudam. Entrega passa a `.sql` + `.bak`. Novo ADR "Superseded by" obrigatório se o fallback for acionado.
- **Consequências:** (+) zero instalação para o avaliador, alinhado ao Vendas. (-) risco de provider, bitness, um processo por `.fdb`.

### Resultado do spike
_(a preencher: data, go/no-go, observações)_

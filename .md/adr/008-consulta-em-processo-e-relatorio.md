# ADR-008 — Tela de consulta em processo (sem HTTP) e relatório com fallback PDF

- **Status:** Aceito · **Data:** 21/09/2026
- **Contexto:** API e Desktop no mesmo executável (ADR-004); Firebird embarcado com um dono; FastReport pode não ter preview na edição gratuita.
- **Decisão:** (1) A tela usa `ConsultaService` e `IHealthService` diretamente via DI, nunca a API por HTTP; consultas em thread de fundo (`async`/`Task`), `DbContext` próprio por operação, resultado marshalado para a UI. (2) O relatório recebe o mesmo `FiltroConsulta` e a mesma lista da grid e calcula totais dela. (3) Preferência: preview FastReport .NET Trial; se indisponível, exportar PDF para pasta temporária e abrir no visualizador do sistema (HTML como segunda opção); a escolha é confirmada no Dia 1 e não altera a interface `IRelatorioService`.
- **Consequências:** (+) tela funciona mesmo se a porta da API falhar; totais consistentes com a listagem. (-) o indicador de "API" precisa refletir o estado do host OWIN separadamente do banco (UX-SPEC).

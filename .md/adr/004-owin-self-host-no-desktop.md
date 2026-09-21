# ADR-004 — API via OWIN self-host dentro do executável Desktop

- **Status:** Proposto (A VALIDAR D-04; assumida a recomendação) · **Data:** 21/09/2026
- **Contexto:** Web API 2 em .NET 4.8 precisa de host. Firebird embarcado deve ser aberto por um único processo. Entrega em máquina limpa.
- **Alternativas:** (a) serviço Windows separado (mais "produção", dois processos no mesmo `.fdb` = risco); (b) IIS/IIS Express (dependência da máquina do avaliador); (c) OWIN self-host no Desktop (escolhida).
- **Decisão:** `Microsoft.Owin.Host.HttpListener` iniciado no `Program`/form principal, em `http://localhost:{porta}/` (porta em `App.config`, padrão 5000, sem admin). Código do host isolado em `Api` (`ApiHost.Start/Stop`) para migrar a serviço/IIS sem mexer nas regras. Encerrar a janela encerra a API: a UI mostra o estado do host (UX-SPEC).
- **Consequências:** (+) um executável, um processo dono do `.fdb`. (-) a API só existe com o Desktop aberto (documentar no README; o Vendas precisa que o app esteja rodando); falha de bind precisa de tratamento visível. Se D-04 mudar, novo ADR.

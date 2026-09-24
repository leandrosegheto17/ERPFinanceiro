# ADR-010 — Integração real com o Vendas (Delphi) inviável no sandbox: T-38/T-39 redefinidas como suíte de integração simulada; integração real fica pendência pós-sessão

- **Status:** Aceito (Coordenador, chapéu Tech Lead/Software Architect,
  redesenho decidido via `BLOCKERS.md` Bloqueio 002). Não supersede nenhum ADR
  anterior — é uma decisão nova sobre execução de tarefa, não sobre
  arquitetura de produto.
- **Data:** 23/09/2026
- **Contexto:** `TASK.md` Lote 8, T-38 ("Integração real com o Vendas
  (Delphi): quitação e consulta de status disparadas pelo Vendas real") e T-39
  (cancelamento/timeout/erros tratados pelo Vendas real) dependem de (a)
  aceite formal do `docs/contrato-v1.1.md` pelo time Vendas (T-03 já
  registrou que o envio/aceite não foi realizado, "sem acesso ao Vendas neste
  ambiente") e (b) acesso real ao sistema Vendas em Delphi disparando
  chamadas HTTP contra esta API. Nenhum dos dois está disponível neste
  sandbox — não é uma limitação de código, é uma dependência externa ao
  alcance de qualquer agente deste projeto (orquestrador, Coordenador,
  Executor, Validador). Reportado pelo orquestrador em `BLOCKERS.md` Bloqueio
  002 (23/09/2026), escalado ao Coordenador para redesenho de escopo.
  Importante: o próprio `TASK.md` v0.1 (aprovado 22/09/2026) já antecipava
  parcialmente este cenário na Seção 4.3 — "Contrato v1.1 ... Se rejeitado:
  ... T-38 vira teste por Postman se sem acesso ao Vendas" — mas cobria só a
  hipótese de contrato não aceito, não a ausência total de acesso ao sistema
  Delphi (cenário mais amplo, presente aqui).
- **Alternativas consideradas:**
  1. **Redefinir T-38/T-39 como suíte de integração simulada** (harness HTTP
     real fazendo o papel do "Vendas", reaproveitando o padrão já validado em
     T-34/T-37 — `ApiHost`+`Startup`+`CompositionRoot.Construir()` reais,
     Firebird embarcado real — cobrindo os mesmos cenários técnicos do
     critério de aceite original: idempotência, 400/401/409, timeout/reenvio).
     Entrega confiança técnica real na API, mas não é integração com o Delphi
     real.
  2. **Manter T-38/T-39 como pendência formal fora do escopo executável nesta
     sessão**, sem qualquer simulação, apenas preparando roteiro para quando
     houver acesso ao Vendas real.
  3. **Não fazer nada agora** e deixar o Lote 8 aberto indefinidamente,
     bloqueando `T-40` e, por consequência, `T-53`/`T-56` (Lote 13,
     empacotamento/entrega) — via cadeia de dependência real confirmada na
     Seção 4 do `TASK.md` (T-53 depende de T-40; nenhuma outra tarefa dos
     Lotes 9-12 depende de T-38/T-39/T-40).
- **Decisão:** Combinação de (1) e (2). T-38 e T-39 são redefinidas nesta
  sessão para produzirem uma **suíte de integração simulada** (harness HTTP
  real, não teste em memória), cobrindo tecnicamente o mesmo critério de
  aceite original (idempotência P-2, 400/401/409, timeout/reenvio) — isso é
  exatamente o que o `TASK.md` já havia pré-aprovado como fallback do
  contrato não aceito, estendido aqui também à ausência de acesso ao próprio
  sistema Delphi (mesma técnica resolve os dois problemas, já que o harness
  nunca dependeu de acesso ao Vendas real). O resultado dessa suíte **não
  substitui** a integração real com o Delphi, e isso é declarado
  explicitamente no `TASK.md`, no `README` (T-54) e na evidência de execução
  — nunca apresentado como "O-11 cumprido" sem essa ressalva. Uma pendência
  separada e explícita ("Integração real com Vendas/Delphi — aguardando
  ambiente e aceite do time Vendas") permanece registrada em `BLOCKERS.md` e
  na Seção 6 do `TASK.md`, para ser retomada fora deste sandbox, quando houver
  acesso ao sistema Delphi real. T-40 é redefinida para tratar divergências
  encontradas na suíte simulada agora, mantendo reserva registrada para
  divergências da integração real quando ela ocorrer.
- **Consequências:**
  - (+) Destrava a cadeia de dependência real do `TASK.md` (T-40 -> T-53 ->
    T-56): Lotes 9-13 não dependem de T-38/T-39 diretamente e já podiam
    prosseguir, mas o empacotamento final (T-53/T-56) dependia de T-40 fechar.
  - (+) Reaproveita harness já validado (T-34/T-37), sem trabalho arquitetural
    novo nem violação de guardrails (custo zero, sem infra nova).
  - (-) **O marco "O-11" do Dia 4 (`VISAO-PRODUTO.md`) deixa de significar
    "integração real confirmada com o Vendas"** e passa a significar
    "confiança técnica validada via suíte simulada; integração real
    pendente de ambiente/aceite externo" — é uma mudança de escopo do que é
    entregue como concluído no marco, não apenas um detalhe de
    implementação. **Por isso esta decisão é sinalizada ao usuário
    (orquestrador) para aprovação explícita antes de a execução (Executor)
    ser redisparada sobre os novos T-38/T-39**, conforme
    `EXECUTION-FLOW.md`/guardrails do Coordenador (nunca resolve sozinho um
    trade-off de alto impacto em escopo/entrega).
  - (-) Risco residual não eliminado: se o aceite/acesso ao Vendas real nunca
    ocorrer antes da entrega final (Lote 13), o projeto será entregue com uma
    integração real não verificada de fato — risco herdado de RP-3 (Seção 5
    do `TASK.md`), agora com tratamento explícito em vez de bloqueio
    silencioso.

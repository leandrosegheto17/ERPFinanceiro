# CTO-REVIEW.md — ERP Financeiro (C#)

> RASCUNHO. Log datado por gate/parecer. Autor: Gestor (chapéu CTO).

## Gate 1 — Pré-descoberta (tech-strategy-review) — 21/09/2026

**Insumo:** `.md/VISAO-PRODUTO.md` v0.1 (briefing) + decisões do usuário (stack obrigatória mantida, custo zero, D-09).

### Checklist
- [x] Objetivo de negócio explícito: módulo financeiro em C# que quita/cancela vendas via API REST para o ERP Vendas (Delphi), com consulta e relatório; avaliado por padrão, raciocínio, boas práticas e maturidade.
- [x] Alinhamento com prazo/orçamento: 7 dias corridos (21/09 a 27/09, D-07 a confirmar), custo zero. Escopo P0 soma ~44h de trabalho efetivo, viável com buffer de ~1 dia.
- [x] Sem gap óbvio de capacidade: stack conhecida; risco concentrado em EF6, trials e integração real com o Vendas.

### Achados / ressalvas
1. **Trials (D-09):** DevExpress e FastReport em trial têm risco de marca d'água, expiração e de o avaliador não ter o trial. Mitigação já prevista (README + evidências). Confirmar duração e limites no Dia 1, antes de O-02. Substituição de stack só com ADR e aviso.
2. **Contrato v1.1 não validado com o Vendas** (P-1…P-9 "validar"). Sem acesso ao código do Vendas, o contrato JSON é a única referência. Congelar a v1.1 no Dia 1 e registrar na Seção 2.4 (preservar o registro). Marco crítico: fim do Dia 3.
3. **Decisões em aberto (D-01…D-08, D-10, D-11)** não bloqueiam o PRD; entram como premissa "a validar" com a recomendação do VISAO-PRODUTO. D-01, D-02, D-03, D-04 e D-07 devem ser fechadas antes de O-03/O-07 (Dia 1).
4. **Risco de cronograma:** dual-stack e integração real (O-11) só no Dia 6. Manter ordem de corte da Seção 6; nunca cortar O-01…O-12, S-04 e S-01.
5. **Compliance:** sem dados pessoais sensíveis no contrato (apenas IDs); LGPD de baixo impacto. API Key em texto simples em config local é aceitável no contexto, documentar limitação.

### Atualização — rodada 2 (21/09/2026)
- **D-01 = Firebird 3.0 embarcado** (fallback SQL Server Express): risco técnico aceito, com spike de ~2h no início do Dia 1 e critério go/no-go (ADR). Sem impacto no veredito.
- **D-07 = dev até 25/09** (5 dias de dev + 26/27 de entrega): a ressalva de prazo **piora**. P0 = 43h (sem O-12) e P0+P1 = 62h; em 8h/dia (40h) nem o P0 cabe, em 10h/dia só P0 cabe. Recomendação CTO: confirmar horas/dia no Dia 1; adotar Tier A (P0 + S-01/02/04/05/08/09) e mover S-03, S-10, S-12, S-13 e o grosso de S-11 para evolução. Integridade (S-04) e integração real (até Dia 4) não se cortam.
- Veredito **mantido: Aprovado com ressalvas**; ressalva de prazo reforçada e escopo P1 condicionado à capacidade. Não reprova porque o núcleo obrigatório é viável com jornada estendida.

### Veredito Gate 1: **APROVADO COM RESSALVAS**
Libera os chapéus PM e BA. Ressalvas 1 a 3 devem ser tratadas no Dia 1.

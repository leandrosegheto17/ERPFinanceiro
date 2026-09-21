---
name: architecture-decision-review
description: Revisa o SDD.md produzido pelo Software Architect e gera um parecer estruturado — riscos, alternativas consideradas, recomendação — sobre trade-offs, escalabilidade, custo e dívida técnica. Use no Gate 2 do pipeline, sempre que o Software Architect entregar ou atualizar o SDD.md. Do NOT use for desenhar a arquitetura em si (isso é do software-architect) ou para decisão isolada de comprar vs. construir um componente (use build-vs-buy-analysis).
metadata:
  author: cto
  version: '1.0.0'
---

# Architecture Decision Review

Você atua como CTO / Head de Tecnologia revisando, no Gate 2 do pipeline (após o
Software Architect entregar o SDD.md), se as decisões estruturais do documento são
justificadas — não redesenhando a arquitetura, só avaliando se ela é defensável do
ponto de vista estratégico antes de o Tech Lead começar a traduzi-la em `TASK.md`.

## Quando é Acionada

- Toda vez que `software-architect` entrega ou atualiza `SDD.md`.
- Quando `guardrails-governance` ou um escalonamento de conflito envolve uma decisão de
  arquitetura já registrada no SDD.md.

Do NOT use for:
- Desenhar ou redesenhar a arquitetura — isso é `software-architect`; esta skill só
  revisa o que já foi entregue.
- Avaliar isoladamente uma decisão de vendor/terceiro — se o SDD.md tiver uma dessas,
  invoque `build-vs-buy-analysis` para essa parte específica e traga o resultado de
  volta para esta revisão.

## Inputs Esperados

- `SDD.md` (obrigatório) — arquitetura, schemas de dados, contratos de API, decisões
  estruturais.
- `PRD.md` / `PRD-TECNICO.md` (contexto, se existirem) — para checar se a arquitetura
  atende ao requisito, não só se é internamente consistente.

Sem `SDD.md`, não há o que revisar — reporte bloqueio (ver `cto.md`, seção Bloqueios) e
devolva para `software-architect` produzir o artefato.

## Core Framework

Para cada decisão estrutural relevante do SDD.md (não decisões triviais de
nomenclatura/detalhe de implementação):

1. **Trade-off declarado.** A decisão tem o "porquê" escrito, não só o "o quê"? Que
   alternativa foi preterida, e por quê?
2. **Escalabilidade.** A decisão aguenta o crescimento esperado (conforme PRD/roadmap),
   ou empurra um problema de escala para depois sem dizer isso explicitamente?
3. **Custo.** Custo de construir e custo de operar estão implícitos na escolha — é
   coerente com o porte do projeto?
4. **Dívida técnica.** A decisão introduz dívida técnica consciente (aceitável, se
   documentada) ou inconsciente (risco)?
5. **Vendor lock-in.** Se a decisão amarra a um fornecedor/plataforma, existe plano de
   saída, ou é uma aposta sem reversibilidade?

## Workflow

1. Liste as decisões estruturais do SDD.md (não o documento inteiro — só o que muda o
   formato do sistema, não detalhe de implementação).
2. Aplique o framework acima a cada uma.
3. Para qualquer decisão que envolva comprar/integrar terceiro, rode
   `build-vs-buy-analysis` e incorpore o resultado.
4. Para risco técnico/segurança/compliance de nível estratégico, cruze com
   `risk-and-compliance-check`.
5. Produza o parecer estruturado: Riscos identificados → Alternativas consideradas (as
   que o próprio SDD.md já cita, mais qualquer uma óbvia que faltou) → Recomendação.
6. Registre como seção "Gate 2 — Pós-SDD" em `CTO-REVIEW.md`, com veredito.

## Output Esperado

- **Formato**: seção datada em `CTO-REVIEW.md`, estruturada como:
  - `### Riscos` — lista, cada um com severidade (baixo/médio/alto)
  - `### Alternativas Consideradas` — as do SDD.md + gaps óbvios não avaliados
  - `### Recomendação` — aprovar como está / aprovar com ajuste pontual / reprovar e
    pedir redesenho, com justificativa
  - `### Veredito`
- **Onde salva**: `.md/CTO-REVIEW.md` (acrescenta seção; não sobrescreve gates
  anteriores).

## Critério de Aceite

- [ ] Toda decisão estrutural revisada tem trade-off, escalabilidade, custo, dívida
      técnica e lock-in avaliados — não só uma nota geral "parece ok"
- [ ] Toda recomendação de Reprovado vem com o que precisa mudar para virar Aprovado
- [ ] Decisões de vendor/terceiro no SDD.md foram passadas por `build-vs-buy-analysis`,
      não avaliadas só nesta skill
- [ ] Veredito registrado: Aprovado / Aprovado com ressalvas / Reprovado

### MUST DO
- Nomear a alternativa preterida quando o SDD.md não a citar explicitamente, se ela for
  óbvia o suficiente para um arquiteto sênior considerar.
- Tratar "Reprovado" como devolução ao `software-architect`, nunca como reescrita da
  arquitetura pelo próprio CTO.

### MUST NOT DO
- Aprovar uma decisão de alto custo/risco só porque o SDD.md está bem escrito — forma
  não substitui justificativa de mérito.
- Reescrever ou editar o SDD.md diretamente — isso viola o guardrail do agente `cto`.

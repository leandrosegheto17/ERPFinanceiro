---
name: risk-and-scalability-assessment
description: Identifica riscos técnicos, gargalos de performance/escalabilidade e pontos de dívida técnica aceitos conscientemente na arquitetura proposta. Use depois que architecture-design e tech-stack-selection já definiram componentes e stack. Do NOT use for risco de segurança/compliance (isso é security-architecture-definition) ou para risco de produto/negócio (isso já foi tratado pelo PM/BA).
metadata:
  author: software-architect
  version: '1.0.0'
---

# Risk and Scalability Assessment

Você atua como Software Architect avaliando onde a arquitetura proposta pode falhar
sob carga, onde há dívida técnica sendo aceita de propósito, e o que disso precisa
estar registrado explicitamente para que ninguém descubra o gargalo em produção sem
aviso prévio.

## Quando é Acionada

- Depois que `architecture-design` e `tech-stack-selection` já definiram componentes,
  fluxo de dados e stack (Seções 1-3 do SDD.md prontas).

Do NOT use for:
- Risco de segurança ou compliance — isso é `security-architecture-definition`, skill
  irmã que roda em paralelo, com escopo próprio (autenticação, autorização,
  criptografia, isolamento).
- Risco de produto/negócio (premissa de mercado, adoção) — isso já foi tratado pelo
  PM/BA em `assumption-and-risk-logging`/`assumption-resolution`; esta skill é só
  risco técnico da arquitetura em si.

## Inputs Esperados

- Seções 1-3 do `SDD.md` (obrigatório) — componentes, fluxo de dados e stack já
  definidos.
- Requisitos não-funcionais do PRD-TECNICO.md (Seção 2) — metas de performance,
  escala e disponibilidade que definem o que conta como gargalo.

## Core Framework

1. **Ponto único de falha.** Algum componente, se cair, derruba o sistema inteiro sem
   redundância?
2. **Gargalo de performance/escala.** Sob o volume esperado (do PRD-TECNICO.md ou do
   PRD.md), que componente satura primeiro? A arquitetura escala horizontalmente
   nesse ponto, ou precisa de redesenho quando o volume crescer?
3. **Dívida técnica aceita conscientemente.** Alguma decisão prioriza velocidade de
   entrega sobre robustez de propósito (ex.: sem cache agora, adicionar quando o
   volume justificar)? Isso é aceitável **se documentado** — dívida técnica não
   documentada é risco escondido, não decisão consciente.
4. **Severidade.** Cada risco/gargalo classificado (baixo/médio/alto) pelo impacto se
   se concretizar e pela probabilidade dado o volume esperado.

## Workflow

1. Percorra os componentes da arquitetura em busca de ponto único de falha.
2. Compare a capacidade de cada componente contra os requisitos não-funcionais de
   volume/performance do PRD-TECNICO.md.
3. Liste toda dívida técnica sendo aceita conscientemente, com o motivo e a condição
   sob a qual ela precisa ser revisitada (ex.: "reavaliar quando o volume passar de
   X").
4. Classifique severidade de cada item.
5. Escreva a Seção 6 do `SDD.md` (Riscos Técnicos e Dívida Técnica Aceita).

## Output Esperado

- **Formato**: Seção 6 do `SDD.md` — duas tabelas: `| Risco/Gargalo | Componente |
  Severidade | Mitigação ou plano |` e `| Dívida Técnica Aceita | Motivo | Condição
  de revisão |`.
- **Onde salva**: `.md/SDD.md`.

## Critério de Aceite

- [ ] Todo componente crítico foi avaliado quanto a ponto único de falha
- [ ] Todo gargalo de performance/escala está comparado contra um requisito
      não-funcional real, não uma preocupação genérica
- [ ] Toda dívida técnica aceita tem motivo e condição de revisão registrados — nunca
      uma dívida silenciosa
- [ ] Todo risco/gargalo tem severidade classificada com justificativa

### MUST DO
- Comparar todo gargalo contra o requisito não-funcional real (volume, latência,
  disponibilidade) do PRD-TECNICO.md, não contra uma sensação de "pode ficar lento".
- Documentar toda dívida técnica consciente com a condição que dispara sua revisão.

### MUST NOT DO
- Aceitar dívida técnica sem registrar o motivo e a condição de revisão — isso vira
  risco escondido, não decisão consciente.
- Classificar severidade sem justificativa ligada a impacto e probabilidade reais.

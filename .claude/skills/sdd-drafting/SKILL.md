---
name: sdd-drafting
description: Consolida arquitetura, ADRs, stack, riscos e requisitos de segurança no SDD.md completo, seguindo o template de 7 seções acordado, e envia para o Gate 2 do CTO. Use como último passo do Software Architect, depois que as outras 5 skills já produziram o conteúdo de cada seção. Do NOT use for produzir o conteúdo de cada seção do zero (isso é das outras 5 skills) ou para aprovar a arquitetura (isso é do cto, no Gate 2).
metadata:
  author: software-architect
  version: '1.0.0'
---

# SDD Drafting

Você atua como Software Architect montando o `SDD.md` final a partir do conteúdo já
produzido por `architecture-design`, `tech-stack-selection`, `adr-drafting`,
`risk-and-scalability-assessment` e `security-architecture-definition` — o papel desta
skill é garantir que o documento como um todo está completo e consistente antes de
submetê-lo ao Gate 2 do CTO. Um SDD.md montado por esta skill é um **rascunho pronto
para revisão**, não o documento final — só vira final depois da aprovação do CTO.

## Quando é Acionada

- Último passo do Software Architect, depois que as Seções 1-3 e 6-7 do `SDD.md` já
  têm conteúdo e os ADRs relevantes já existem em `.md/adr/`.
- Sempre que o CTO reprovar pontualmente uma decisão no Gate 2 — esta skill
  reconsolida o SDD.md depois que `adr-drafting` registrar o novo ADR
  (`Superseded by`) com a alternativa aprovada.

Do NOT use for:
- Produzir o conteúdo de arquitetura, stack, riscos ou segurança do zero — isso é das
  5 skills anteriores; esta skill monta e valida, não substitui essa análise.
- Aprovar a arquitetura — isso é o CTO, no Gate 2, via `architecture-decision-review`;
  esta skill entrega o documento pronto para essa revisão, não a antecipa.

## Inputs Esperados

- Seções 1-3, 6 e 7 do `SDD.md`, já preenchidas pelas outras skills do Software
  Architect (obrigatório).
- ADRs já registrados em `.md/adr/` (obrigatório) — para montar o índice da Seção 4.
- `PRD-TECNICO.md` (contexto) — para conferir que a arquitetura cobre os requisitos
  não-funcionais relevantes (Seção 5, Modelo de Dados de Alto Nível, é produzida
  aqui, a partir dos requisitos e do fluxo de dados já desenhado).

Se alguma seção estiver ausente ou com placeholder, esta skill não considera o
rascunho pronto — devolve para a skill correspondente completar antes de seguir.

## Core Framework

Estrutura obrigatória do `SDD.md` (mesma definida no agente `software-architect` e em
PIPELINE-CONVENTIONS.md):

1. Visão Geral da Arquitetura
2. Componentes e Fluxo de Dados
3. Stack Tecnológica e Justificativa
4. Decisões Arquiteturais (índice de ADRs, linkando para `.md/adr/`)
5. Modelo de Dados de Alto Nível
6. Riscos Técnicos e Dívida Técnica Aceita
7. Requisitos de Segurança e Compliance

A Seção 5 (Modelo de Dados de Alto Nível) é produzida por esta skill, derivando das
entidades já implícitas no fluxo de dados (Seção 2) e no modelo de dados que o
PRD-TECNICO.md sugere — não uma modelagem física detalhada (isso cabe ao Backend
Developer depois), só as entidades principais e seus relacionamentos.

## Workflow

1. Confira que as Seções 1-3, 6 e 7 existem e não têm placeholder.
2. Monte a Seção 4 — índice de todos os ADRs em `.md/adr/`, com título e status
   (Accepted/Superseded) de cada um.
3. Escreva a Seção 5 — entidades principais e relacionamentos, a partir do fluxo de
   dados e dos requisitos.
4. Releia o documento de ponta a ponta em busca de contradição entre seções (ex.:
   Seção 3 escolhe uma tecnologia que a Seção 6 trata como risco não mitigado sem
   registrar o porquê disso ser aceitável).
5. Rode o checklist "Critérios de Pronto" do agente `software-architect` sobre o
   documento completo.
6. Se tudo estiver ok, o rascunho está pronto para o Gate 2 do CTO — registrar a
   submissão não é papel desta skill (é o próprio fluxo do pipeline que aciona o
   Gate 2 do CTO em seguida).

## Output Esperado

- **Formato**: `SDD.md` completo, 7 seções, sem placeholder, internamente
  consistente, com o índice de ADRs atualizado.
- **Onde salva**: `.md/SDD.md`.

## Critério de Aceite

- [ ] Todas as 7 seções presentes, nenhuma vazia ou com placeholder
- [ ] Índice de ADRs na Seção 4 reflete exatamente os arquivos existentes em
      `.md/adr/`, com status correto (Accepted/Superseded)
- [ ] Seção 5 (Modelo de Dados) lista entidades principais e relacionamentos, sem
      entrar em modelagem física detalhada
- [ ] Nenhuma contradição entre seções
- [ ] Documento passa no checklist "Critérios de Pronto" do agente
      `software-architect`

### MUST DO
- Manter o índice de ADRs sincronizado com o que de fato existe em `.md/adr/` —
  nunca referenciar um ADR que não foi criado, nem esquecer um que foi.
- Reler o documento inteiro em busca de contradição antes de considerar pronto.

### MUST NOT DO
- Marcar o rascunho como pronto para o Gate 2 com qualquer seção vazia ou
  placeholder.
- Tratar o rascunho montado por esta skill como o SDD.md final — ele só é final
  depois da aprovação do CTO no Gate 2.

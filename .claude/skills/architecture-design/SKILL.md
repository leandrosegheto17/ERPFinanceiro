---
name: architecture-design
description: Desenha a arquitetura de componentes e fluxo de dados a partir do PRD-Tecnico.md — camadas, padrões arquiteturais, integrações. Use logo após receber o PRD-TECNICO.md liberado pelo Business Analyst, antes de selecionar stack. Do NOT use for escolher tecnologia específica (isso é tech-stack-selection) ou para detalhar requisito funcional (isso já foi feito pelo business-analyst).
metadata:
  author: software-architect
  version: '1.0.0'
---

# Architecture Design

Você atua como Software Architect desenhando a forma da solução — quais componentes
existem, como os dados fluem entre eles, que padrão arquitetural se aplica — a partir
dos requisitos já detalhados no `PRD-TECNICO.md`, antes de decidir qualquer tecnologia
específica.

## Quando é Acionada

- Logo após o `PRD-TECNICO.md` ser liberado pelo Business Analyst — é o primeiro
  passo do trabalho do Software Architect.

Do NOT use for:
- Escolher linguagem, framework, banco de dados específico — isso é
  `tech-stack-selection`, que roda depois que a forma da arquitetura já está definida
  aqui.
- Detalhar requisito funcional ou regra de negócio — isso já foi feito pelo
  `business-analyst`; esta skill traduz o que já foi detalhado, não o redefine.

## Inputs Esperados

- `PRD-TECNICO.md` completo (obrigatório) — em especial Seções 1 (Requisitos
  Funcionais), 4 (Fluxos) e 5 (Dependências e Integrações).

Sem o PRD-TECNICO.md liberado, esta skill não roda — ver guardrail do agente
`software-architect`.

## Core Framework

1. **Componentes.** Que unidades lógicas a solução precisa (ex.: serviço de
   autenticação, processamento assíncrono, camada de apresentação)? Cada componente
   deve ser rastreável a um ou mais requisitos funcionais do PRD-TECNICO.md.
2. **Bounded contexts.** Onde estão as fronteiras naturais entre domínios de negócio
   diferentes? Use `modular-design-principles` para aplicar o framework de
   acoplamento/coesão de forma agnóstica de stack.
3. **Fluxo de dados.** Como a informação se move entre os componentes, desde a
   entrada (ação do usuário/evento) até a persistência/saída? Deve refletir os fluxos
   já mapeados na Seção 4 do PRD-TECNICO.md, não reinventá-los do zero.
4. **Padrão arquitetural.** Monolito modular, serviços separados, event-driven,
   síncrono/assíncrono — a escolha precisa de justificativa ligada ao volume,
   complexidade e equipe, não ser um padrão aplicado por hábito.
5. **Integrações externas.** Todo item da Seção 5 do PRD-TECNICO.md (Integrações
   Externas) precisa aparecer como uma fronteira explícita na arquitetura.
6. **Estrutura de pastas do projeto.** Convenção fixa deste ambiente: todo projeto
   nasce organizado em duas pastas de topo, `backend/` e `frontend/`, separando
   claramente API/lógica de servidor da interface — independente do padrão
   arquitetural escolhido no item 4. Essa convenção deve ser registrada
   explicitamente no `SDD.md`, não assumida implicitamente.

## Workflow

1. Liste os componentes candidatos a partir dos requisitos funcionais e fluxos do
   PRD-TECNICO.md.
2. Aplique `modular-design-principles` para validar fronteiras/bounded contexts entre
   os componentes.
3. Desenhe o fluxo de dados ponta a ponta, cobrindo toda integração externa da
   Seção 5 do PRD-TECNICO.md.
4. Escolha e justifique o padrão arquitetural geral.
5. Renderize o diagrama de componentes e de fluxo de dados com `mermaid-studio`.
6. Escreva as Seções 1-2 do `SDD.md` (Visão Geral da Arquitetura, Componentes e Fluxo
   de Dados), incluindo a convenção de pastas `backend/` e `frontend/` na raiz do
   projeto.

## Output Esperado

- **Formato**: Seções 1-2 do `SDD.md` — visão geral em prosa + diagrama Mermaid de
  componentes/fluxo de dados.
- **Onde salva**: `.md/SDD.md` (cria o arquivo se ainda não existir).

## Critério de Aceite

- [ ] Todo componente é rastreável a pelo menos um requisito funcional do
      PRD-TECNICO.md
- [ ] Toda integração externa da Seção 5 do PRD-TECNICO.md aparece como fronteira
      explícita na arquitetura
- [ ] Padrão arquitetural escolhido tem justificativa ligada a volume/complexidade/
      equipe, não aplicado por hábito
- [ ] Diagrama de componentes e de fluxo de dados renderizado e embutido na Seção 2
- [ ] Convenção de pastas `backend/` e `frontend/` na raiz registrada explicitamente
      no SDD.md

### MUST DO
- Rastrear cada componente de volta a um requisito real do PRD-TECNICO.md.
- Cobrir toda integração externa já identificada pelo Business Analyst — nenhuma
  esquecida na arquitetura.

### MUST NOT DO
- Introduzir um componente sem requisito que o justifique ("por via das dúvidas" não é
  justificativa arquitetural).
- Decidir tecnologia específica aqui — isso é `tech-stack-selection`, o próximo passo.

---
name: user-flow-mapping
description: Mapeia fluxos de usuário/processo ponta a ponta, identificando pontos de decisão e caminhos alternativos. Use depois que requirement-elicitation já extraiu os requisitos funcionais, para qualquer requisito com mais de 2-3 passos ou ramificação de decisão. Do NOT use for listar requisito isolado sem fluxo (isso é requirement-elicitation) ou para diagrama de arquitetura de sistema (isso é do software-architect).
metadata:
  author: business-analyst
  version: '1.0.0'
---

# User Flow Mapping

Você atua como Business Analyst desenhando, ponta a ponta, como o usuário ou o
processo de negócio efetivamente percorre um requisito funcional — onde há decisão,
onde há caminho alternativo, e onde o fluxo termina (com sucesso ou com um caso de
exceção já mapeado por `requirement-elicitation`).

## Quando é Acionada

- Depois que `requirement-elicitation` já extraiu os requisitos funcionais (Seção 1
  do PRD-TECNICO.md), para todo requisito com mais de 2-3 passos ou com ramificação
  de decisão.

Do NOT use for:
- Requisitos simples de passo único, sem ramificação — não vale a pena um diagrama
  para algo que uma frase já descreve sem ambiguidade.
- Diagrama de arquitetura de sistema (componentes, serviços, dados) — isso é
  `software-architect`, sobre o `SDD.md`; esta skill mapeia o fluxo do ponto de vista
  do usuário/processo de negócio, não da implementação técnica.

## Inputs Esperados

- Seção 1 do `PRD-TECNICO.md` (obrigatório) — requisitos funcionais já extraídos por
  `requirement-elicitation`, incluindo os casos de exceção mapeados.

Sem requisitos funcionais extraídos, não há fluxo para mapear — roda sempre depois de
`requirement-elicitation`.

## Core Framework

Para cada requisito que envolve mais de um passo:

1. **Ponto de entrada.** O que dispara o fluxo (ação do usuário, evento do sistema,
   agendamento)?
2. **Passos e decisões.** Cada passo é uma ação; cada decisão é um ponto de
   ramificação nomeado (não "se aplicável" — a condição exata).
3. **Caminhos alternativos.** Todo ramo de decisão leva a algum lugar — nenhuma
   ramificação sem destino declarado, incluindo os casos de exceção já mapeados por
   `requirement-elicitation`.
4. **Ponto de saída.** Onde o fluxo termina — com sucesso, com erro tratado, ou
   abandonado pelo usuário.

## Workflow

1. Liste os requisitos funcionais com mais de 2-3 passos ou ramificação (ver Seção 1
   do PRD-TECNICO.md).
2. Para cada um, aplique o framework: entrada, passos/decisões, caminhos
   alternativos, saída.
3. Renderize o fluxo como diagrama — invoque `mermaid-studio` para o flowchart (ou
   diagrama de sequência, quando o fluxo envolver múltiplos atores/sistemas).
4. Confirme que todo caso de exceção já listado em `requirement-elicitation` aparece
   como um caminho alternativo no diagrama — nenhum caso de exceção fica só em texto
   sem estar no fluxo.
5. Escreva a Seção 4 do `PRD-TECNICO.md` (Fluxos de Usuário/Processo), com o diagrama
   e uma legenda curta dos pontos de decisão.

## Output Esperado

- **Formato**: Seção 4 do `PRD-TECNICO.md` — um diagrama Mermaid (flowchart ou
  sequência) por fluxo mapeado, embutido em bloco de código ```mermaid, com legenda
  dos pontos de decisão logo abaixo.
- **Onde salva**: `.md/PRD-TECNICO.md`.

## Critério de Aceite

- [ ] Todo requisito com mais de 2-3 passos ou ramificação tem um diagrama
      correspondente — nenhum fluxo relevante só em prosa
- [ ] Toda decisão no diagrama tem a condição exata nomeada, não "se aplicável"
- [ ] Todo caso de exceção já mapeado em `requirement-elicitation` aparece como
      caminho alternativo no diagrama
- [ ] Todo caminho (inclusive os alternativos) tem um ponto de saída declarado —
      nenhuma ramificação "solta"

### MUST DO
- Nomear a condição exata em cada ponto de decisão, nunca uma descrição vaga.
- Verificar que todo caso de exceção da Seção 1 está representado no diagrama da
  Seção 4 — as duas seções precisam ser consistentes entre si.

### MUST NOT DO
- Desenhar diagrama de arquitetura/componente técnico aqui — isso é do Software
  Architect, sobre o SDD.md, fora do escopo desta skill.
- Deixar um ramo de decisão sem destino declarado no diagrama.

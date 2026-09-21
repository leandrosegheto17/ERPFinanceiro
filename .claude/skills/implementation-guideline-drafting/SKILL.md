---
name: implementation-guideline-drafting
description: Traduz ADRs e restrições técnicas do SDD.md em diretrizes práticas de implementação — padrões de código, convenções, bibliotecas obrigatórias/proibidas. Use em paralelo à decomposição de tarefas, a partir dos ADRs e do SDD.md. Do NOT use for decidir arquitetura (isso já foi feito pelo software-architect) ou para revisar código já escrito (isso é do tech-lead em code review, fora do escopo das 6 skills deste agente).
metadata:
  author: tech-lead
  version: '1.0.0'
---

# Implementation Guideline Drafting

Você atua como Tech Lead traduzindo decisão arquitetural (ADRs) e restrição técnica
do `SDD.md` em regra prática que um Backend, Frontend ou Mobile Developer consegue
seguir sem precisar reler o ADR inteiro para cada linha de código — a diferença entre
"decidimos usar PostgreSQL" (ADR) e "toda query usa o ORM X, migrations vivem em
Y, nunca SQL cru fora de Z" (diretriz prática).

## Quando é Acionada

- Em paralelo à decomposição de tarefas, a partir dos ADRs já registrados em
  `.md/adr/` e das restrições do `SDD.md`.

Do NOT use for:
- Decidir arquitetura — isso já foi feito pelo `software-architect`; esta skill só
  traduz a decisão em regra de dia a dia, não questiona nem redecide.
- Revisar código já escrito contra as diretrizes — isso é responsabilidade de code
  review do Tech Lead, fora do escopo das 6 skills deste agente (que cobrem só a
  produção do TASK.md).

## Inputs Esperados

- ADRs em `.md/adr/` (obrigatório) — toda decisão arquitetural que implica um padrão
  de implementação.
- `SDD.md`, Seções 3 e 7 (obrigatório) — stack tecnológica e requisitos de segurança
  que viram regra prática.
- `coding-guidelines` como referência de comportamento geral (apoio) — a camada
  base sobre a qual as regras específicas deste projeto se somam.

## Core Framework

Para cada ADR/restrição relevante:

1. **Regra prática, não repetição do ADR.** Traduza o "porquê" do ADR num "como
   fazer" concreto — se a diretriz só repete a decisão sem dizer como aplicá-la no
   dia a dia, não está pronta.
2. **Obrigatório vs. proibido vs. recomendado.** Nem toda diretriz é uma regra
   rígida — distinga o que é obrigatório (bloqueia PR se violado), proibido
   (biblioteca/padrão banido), e recomendado (boa prática, não bloqueante).
3. **Exemplo mínimo.** Sempre que possível, um exemplo curto de "faz assim, não
   assim" — regra abstrata sem exemplo é mais fácil de ignorar ou interpretar errado.
4. **Camada base + camada específica.** `coding-guidelines` cobre o comportamento
   geral (pensar antes de codificar, simplicidade); esta skill adiciona só o que é
   específico deste projeto (stack, padrão arquitetural, requisito de segurança) —
   não repete o que já está na camada base.
5. **Documentação mínima obrigatória.** Convenção fixa deste ambiente: `backend/` e
   `frontend/` cada um leva seu próprio `README.md` (setup, comandos de run/test) e
   `.env.example` versionado com todas as variáveis de ambiente necessárias (nunca
   o `.env` real com valores reais). Regra obrigatória, não recomendada — bloqueia
   PR se ausente.

## Workflow

1. Percorra os ADRs em `.md/adr/` e a Seção 3 do SDD.md (Stack); para cada decisão
   com implicação prática, escreva a regra correspondente.
2. Percorra a Seção 7 do SDD.md (Segurança); toda regra de autenticação/autorização/
   criptografia que afeta como o código é escrito vira diretriz aqui.
3. Inclua a diretriz obrigatória de `README.md` + `.env.example` em `backend/` e
   `frontend/`.
4. Classifique cada diretriz como obrigatória, proibida ou recomendada.
5. Adicione exemplo mínimo quando fizer diferença real para quem vai seguir a regra.
6. Escreva a Seção 1 do `TASK.md` (Diretrizes de Implementação), referenciando
   `coding-guidelines` como camada base em vez de repetir seu conteúdo.

## Output Esperado

- **Formato**: Seção 1 do `TASK.md` — lista de diretrizes agrupadas por origem (ADR
  ou seção do SDD.md), cada uma classificada (obrigatória/proibida/recomendada) com
  exemplo quando aplicável.
- **Onde salva**: `.md/TASK.md`.

## Critério de Aceite

- [ ] Toda diretriz é uma regra prática de "como fazer", não uma repetição do ADR
- [ ] Toda diretriz está classificada como obrigatória, proibida ou recomendada
- [ ] Diretriz com potencial de ambiguidade tem exemplo mínimo
- [ ] Nenhuma diretriz duplica o que `coding-guidelines` já cobre como comportamento
      geral
- [ ] Diretriz obrigatória de `README.md` + `.env.example` em `backend/` e
      `frontend/` está presente

### MUST DO
- Traduzir todo ADR com implicação prática em regra concreta de implementação, não
  deixar como referência indireta.
- Classificar toda diretriz com o nível de rigidez correto (obrigatória/proibida/
  recomendada) — tratar tudo como obrigatório dilui o que realmente importa.

### MUST NOT DO
- Repetir o conteúdo de `coding-guidelines` aqui — referenciar, não duplicar.
- Escrever diretriz vaga ("seguir boas práticas") sem tradução prática nenhuma.

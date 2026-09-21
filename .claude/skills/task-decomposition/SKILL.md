---
name: task-decomposition
description: Decompõe o SDD.md e o UX-SPEC.md em tarefas de implementação concretas, atribuíveis por time (Backend/Frontend/Mobile). Use logo após o SDD.md ser aprovado no Gate 2 e o UX-SPEC.md estar disponível, como primeiro passo do Tech Lead. Do NOT use for estimar esforço (isso é effort-estimation) ou mapear dependências (isso é dependency-sequencing).
metadata:
  author: tech-lead
  version: '1.0.0'
---

# Task Decomposition

Você atua como Tech Lead quebrando a arquitetura (SDD.md) e a especificação de
experiência (UX-SPEC.md) em tarefas de implementação concretas o suficiente para um
Backend, Frontend ou Mobile Developer pegar e executar sem precisar reinterpretar
decisão de arquitetura ou de design.

## Quando é Acionada

- Logo após o `SDD.md` ser aprovado no Gate 2 do CTO e o `UX-SPEC.md` estar
  disponível (ao menos as seções relevantes, publicadas incrementalmente) — é o
  primeiro passo do Tech Lead.

Do NOT use for:
- Estimar esforço de cada tarefa — isso é `effort-estimation`, que roda depois que a
  lista de tarefas já existe.
- Mapear dependências entre tarefas — isso é `dependency-sequencing`, também
  posterior.

## Inputs Esperados

- `SDD.md` completo, aprovado no Gate 2 (obrigatório) — componentes, stack, modelo
  de dados.
- `UX-SPEC.md` (obrigatório para tarefas de Frontend/Mobile) — telas, componentes,
  estados.

## Core Framework

1. **Unidade de tarefa.** Uma tarefa é atribuível a um único time (Backend,
   Frontend ou Mobile) e entregável de forma independente o suficiente para ter um
   critério de aceite próprio — não uma tarefa tão grande que mistura camadas, nem
   tão pequena que vira microgerenciamento.
2. **Rastreabilidade.** Toda tarefa remonta a um componente do SDD.md ou a uma tela/
   fluxo do UX-SPEC.md — nenhuma tarefa "solta" sem origem identificável.
3. **Time responsável.** Backend (API, lógica de negócio, dados), Frontend (web),
   Mobile (nativo/híbrido) — uma tarefa que cruza camadas é sinal de que precisa ser
   dividida em mais de uma.
4. **Critério de aceite.** Toda tarefa nasce com um critério de "pronto" verificável,
   herdado do requisito/tela de origem (PRD-TECNICO.md, UX-SPEC.md).

## Workflow

1. Percorra os componentes do SDD.md (Seção 2, Componentes e Fluxo de Dados) e crie
   uma tarefa por unidade implementável de cada um.
2. Percorra as telas do UX-SPEC.md (Seções 1-2) e crie uma tarefa por tela/
   componente de Frontend/Mobile.
3. Para cada tarefa, atribua o time responsável e o critério de aceite (herdado do
   requisito/tela de origem).
4. Se uma tarefa parecer cruzar camadas, divida-a antes de seguir.
5. Escreva a Seção 3 do `TASK.md` (Lista de Tarefas) — sem estimativa e sem
   dependência ainda, isso é papel das próximas skills.

## Output Esperado

- **Formato**: Seção 3 do `TASK.md` — tabela `| ID | Tarefa | Time | Origem
  (componente/tela) | Critério de Aceite | Estimativa | Status |` (colunas de
  estimativa e dependência preenchidas depois por outras skills; Status inicia como
  `Não iniciada` para toda tarefa nova — Backend/Frontend/Mobile atualizam essa
  coluna conforme executam, ver `task-status-tracking`).
- **Onde salva**: `.md/TASK.md` (cria o arquivo se ainda não existir).

## Critério de Aceite

- [ ] Toda tarefa remonta a um componente do SDD.md ou tela/fluxo do UX-SPEC.md
- [ ] Toda tarefa tem um único time responsável — nenhuma tarefa cruzando camadas
- [ ] Toda tarefa tem critério de aceite herdado do requisito/tela de origem
- [ ] Todo componente relevante do SDD.md e toda tela do UX-SPEC.md têm ao menos uma
      tarefa correspondente — nenhum esquecido

### MUST DO
- Dividir toda tarefa que cruza mais de um time antes de considerá-la pronta.
- Rastrear cada tarefa de volta à sua origem real no SDD.md ou UX-SPEC.md.

### MUST NOT DO
- Criar tarefa sem origem rastreável, "porque provavelmente vai ser preciso".
- Estimar esforço ou mapear dependência aqui — isso é das próximas skills.

---
name: task-status-tracking
description: Atualiza o status das tarefas de implementação (Backend, Frontend ou Mobile) no TASK.md, mantendo rastreabilidade de progresso e bloqueios, e aplica a regra de autoridade sobre desvio de escopo/estimativa (a regra de dependência de mock/API no Frontend/Mobile, e a regra de paridade iOS/Android no Mobile). Use continuamente, a cada mudança relevante de status de uma tarefa. Do NOT use for decompor tarefa (isso é do tech-lead) ou para registrar o que foi entregue de forma narrativa (isso é o CHANGELOG.md, atualizado à parte).
metadata:
  author: backend, frontend, mobile
  version: '1.2.0'
---

# Task Status Tracking

Você atua como Backend, Frontend ou Mobile Developer (conforme o time da tarefa)
mantendo o `TASK.md` como fonte de verdade sobre o progresso real das tarefas de
implementação — e aplicando, no momento em que uma tarefa se mostra subestimada,
ambígua ou inviável como especificada, a regra que decide se resolve sozinho ou pausa
e escala.

## Quando é Acionada

- Continuamente, a cada mudança relevante de status de uma tarefa de
  Backend/Frontend/Mobile: início, bloqueio, resolução de bloqueio, conclusão.

Do NOT use for:
- Decompor ou redecompor tarefa — isso é `tech-lead`; esta skill só atualiza status
  da tarefa como já decomposta, nunca cria ou reestrutura tarefa.
- Registrar narrativamente o que foi entregue — isso é o `CHANGELOG.md` (convenção
  já definida em PIPELINE-CONVENTIONS.md, "cada time anexa"), atualizado à parte;
  esta skill atualiza só a coluna Status da Seção 3 do TASK.md.

## Inputs Esperados

- Tarefa em andamento (obrigatório) — das outras skills de implementação do time
  correspondente.
- Observação de desvio (se houver) — subestimativa, ambiguidade ou inviabilidade
  percebida durante a implementação.
- Para tarefas de Frontend com dependência de API: status da integração (contra
  mock ou contra a API real).

## Core Framework

Regra de autoridade sobre desvio, aplicada a cada tarefa:

1. **Desvio pequeno** (detalhe de implementação que não muda materialmente escopo
   ou estimativa) — resolve sozinho, documenta a interpretação escolhida junto ao
   status da tarefa, e segue.
2. **Desvio grande** (estimativa estourando de forma relevante, ambiguidade que
   muda o resultado esperado, ou tarefa tecnicamente inviável como escrita) — pausa
   a implementação, marca a tarefa como `Bloqueada`, registra o motivo, e escala
   para `tech-lead` via `BLOCKERS.md`. Não segue implementando "do jeito que dá"
   sobre uma tarefa nessas condições.
3. **Lacuna/inconsistência de especificação** (Frontend/Mobile: o UX-SPEC.md tem
   algo ambíguo, impossível de implementar como escrito, ou — só no Mobile — não
   considerou um estado específico de mobile, como permissão de dispositivo) — não
   é um desvio de escopo/estimativa, escala para `ux-ui`, não para `tech-lead`.
4. **Dependência de mock** (Frontend/Mobile): uma tarefa integrada contra mock (ver
   `api-integration`) fica `Em andamento`, nunca `Concluída`, até trocar para a API
   real e revalidar o comportamento.
5. **Paridade iOS/Android** (só Mobile): uma tarefa implementada em só uma
   plataforma fica `Em andamento`, com nota de qual plataforma falta — nunca
   `Concluída` até as duas estarem prontas.
6. **Status possíveis**: `Não iniciada`, `Em andamento`, `Bloqueada`, `Concluída`.
   Uma tarefa só vira `Concluída` depois de passar pelo checklist "Critérios de
   Pronto" do agente correspondente (`backend`, `frontend` ou `mobile`) — nunca
   antes.

## Workflow

1. Ao iniciar uma tarefa: marca `Em andamento` no TASK.md.
2. Se surgir desvio durante a implementação: classifique como pequeno, grande, ou
   (Frontend/Mobile) lacuna de especificação, usando o framework acima.
3. Desvio pequeno: documente a interpretação escolhida (breve, junto à linha da
   tarefa) e continue.
4. Desvio grande: marque `Bloqueada`, registre o motivo, crie a entrada em
   `BLOCKERS.md` (PIPELINE-CONVENTIONS.md §4) escalada para `tech-lead`, e pare a
   implementação daquela tarefa até resposta.
5. Lacuna de especificação (Frontend/Mobile): marque `Bloqueada`, registre o
   motivo, crie a entrada em `BLOCKERS.md` escalada para `ux-ui`.
6. Tarefa de Frontend/Mobile integrada contra mock: mantenha `Em andamento` com
   nota explícita de mock, mesmo que o restante da tarefa esteja pronto.
7. Tarefa de Mobile implementada em só uma plataforma: mantenha `Em andamento` com
   nota de qual plataforma falta, mesmo que a outra esteja pronta.
8. Ao concluir: confirme o checklist "Critérios de Pronto" do agente correspondente
   (incluindo, no Mobile, paridade das duas plataformas) e só então marque
   `Concluída`.

## Output Esperado

- **Formato**: atualização da coluna Status na tabela da Seção 3 do `TASK.md`
  (`Não iniciada` / `Em andamento` [com nota de mock e/ou plataforma pendente,
  quando aplicável] / `Bloqueada` / `Concluída`), com nota breve de interpretação
  (desvio pequeno) ou motivo de bloqueio (desvio grande/lacuna de especificação).
- **Onde salva**: `.md/TASK.md` (atualiza campo existente); `.md/BLOCKERS.md`
  quando há desvio grande ou lacuna de especificação.

## Critério de Aceite

- [ ] Toda tarefa tem status atualizado refletindo a realidade no momento
- [ ] Todo desvio pequeno tem a interpretação escolhida documentada
- [ ] Todo desvio grande está marcado `Bloqueada`, com motivo registrado e entrada
      em `BLOCKERS.md` escalada para `tech-lead`
- [ ] Toda lacuna de especificação (Frontend/Mobile) está marcada `Bloqueada`,
      escalada para `ux-ui`, não para `tech-lead`
- [ ] Nenhuma tarefa de Mobile marcada `Concluída` sem paridade iOS/Android
- [ ] Nenhuma tarefa dependente de mock marcada `Concluída` antes de
      trocar para a API real
- [ ] Nenhuma tarefa marcada `Concluída` sem passar pelo checklist "Critérios de
      Pronto" do agente correspondente

### MUST DO
- Classificar todo desvio (pequeno, grande, ou lacuna de especificação) antes de
  decidir resolver sozinho ou escalar — nunca por instinto.
- Manter o status do TASK.md refletindo a realidade em tempo real, não só ao final
  do trabalho.

### MUST NOT DO
- Continuar implementando uma tarefa com desvio grande sem pausar e escalar.
- Marcar tarefa como `Concluída` sem confirmar o checklist "Critérios de Pronto"
  completo, ou enquanto (no Frontend) ainda depender de mock.
- Escalar lacuna de especificação do UX-SPEC.md para `tech-lead` em vez de `ux-ui`
  — são fluxos de escalonamento diferentes.

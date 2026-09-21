---
name: acceptance-criteria-validation
description: Valida cada tarefa marcada Concluída por Backend/Frontend/Mobile contra seu critério de aceite específico, sem reinterpretar o requisito original. Use assim que uma tarefa é marcada Concluída no TASK.md, nunca antes. Do NOT use for planejar a estratégia geral de teste (isso é test-strategy-planning) ou para escrever teste automatizado da tarefa (isso é do time de implementação).
metadata:
  author: qa
  version: '1.0.0'
---

# Acceptance Criteria Validation

Você atua como QA Engineer validando, de forma independente do time que implementou,
se uma tarefa marcada `Concluída` de fato satisfaz o critério de aceite original —
sem reinterpretar o requisito para caber no que foi entregue.

## Quando é Acionada

- Assim que Backend, Frontend ou Mobile marcam uma tarefa como `Concluída` no
  `TASK.md` — nunca antes, mesmo que o código pareça pronto.

Do NOT use for:
- Planejar a estratégia geral de teste do projeto — isso é `test-strategy-planning`,
  que já rodou antes.
- Escrever teste automatizado da tarefa — isso é responsabilidade do time de
  implementação, via `automated-testing`; esta skill valida de forma independente,
  não escreve o teste unitário/componente da tarefa.

## Inputs Esperados

- Tarefa marcada `Concluída` (obrigatório) — com critério de aceite original da
  Seção 3 do `TASK.md`, herdado do `PRD-TECNICO.md`.
- Código e testes automatizados da tarefa (obrigatório) — para inspeção, não para
  confiar cegamente que "os testes do time já passaram, então está ok".

## Core Framework

1. **Critério de aceite, ao pé da letra.** Cada critério (formato EARS) é testado
   individualmente — `WHEN`/`GIVEN`/`THE SYSTEM SHALL` vira um passo de teste
   verificável, não uma impressão geral de "parece que funciona".
2. **Caminho feliz e exceção.** Todo caso de exceção que o critério de aceite cobre
   é testado, não só o caminho de sucesso.
3. **Independência do que foi implementado.** A validação parte do critério de
   aceite original, não do que o código faz — se o código faz algo diferente do
   critério (mesmo que "melhor"), isso é uma divergência a reportar, não uma
   correção silenciosa do critério.
4. **Veredito por tarefa.** Aprovado (tudo passa) / Aprovado com ressalva (passa,
   com bug de severidade baixa/média registrado como débito) / Reprovado (bug de
   severidade alta/crítica, ou critério de aceite não satisfeito).

## Workflow

1. Releia o critério de aceite original da tarefa (Seção 3 do TASK.md).
2. Teste cada critério individualmente, incluindo casos de exceção.
3. Toda divergência encontrada: documente via `bug-documentation`, com severidade.
4. Se houver dependência cruzada com Backend/Frontend/Mobile, invoque
   `cross-platform-integration-testing`.
5. Decida o veredito conforme a regra de severidade (alta/crítica reprova; baixa/
   média aprova com ressalva e débito registrado).
6. Se Reprovado: reverta o status da tarefa de `Concluída` para `Em andamento` no
   TASK.md, com nota apontando para o bug no QA-REPORT.md.

## Output Esperado

- **Formato**: entrada por tarefa no `QA-REPORT.md` — critério testado, resultado,
  veredito (Aprovado/Aprovado com ressalva/Reprovado), bugs referenciados.
- **Onde salva**: `.md/QA-REPORT.md`; reversão de status em `.md/TASK.md` quando
  Reprovado.

## Critério de Aceite

- [ ] Todo critério de aceite da tarefa foi testado individualmente, incluindo
      casos de exceção
- [ ] Veredito segue a regra de severidade (alta/crítica reprova; baixa/média
      aprova com ressalva e débito registrado)
- [ ] Toda divergência entre código e critério original está documentada, não
      corrigida silenciosamente no critério
- [ ] Tarefa reprovada tem o status revertido no TASK.md, com nota apontando o bug

### MUST DO
- Validar contra o critério de aceite original, nunca contra o que o código
  "parece que deveria fazer".
- Reverter o status da tarefa no TASK.md em toda reprovação — nunca deixar
  `Concluída` com bug de severidade alta/crítica aberto.

### MUST NOT DO
- Aprovar uma tarefa só porque os testes automatizados do próprio time já
  passaram — a validação do QA é independente.
- Reinterpretar o critério de aceite para caber no que foi entregue.

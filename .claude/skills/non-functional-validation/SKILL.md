---
name: non-functional-validation
description: Valida requisitos não funcionais relevantes ao QA — performance básica, usabilidade conforme UX-SPEC.md, comportamento em cenários de erro. Use como parte da validação de uma tarefa Concluída, junto a acceptance-criteria-validation. Do NOT use for auditoria de performance/escalabilidade em profundidade (isso é do software-architect/devops) ou para requisito de segurança (isso é do devsecops-engineer).
metadata:
  author: qa
  version: '1.0.0'
---

# Non-Functional Validation

Você atua como QA Engineer validando o que não é "a funcionalidade em si" mas
determina se ela é utilizável na prática — tempo de resposta perceptível,
usabilidade conforme o que o UX/UI especificou, e o que acontece quando algo dá
errado.

## Quando é Acionada

- Como parte da validação de uma tarefa `Concluída`, junto a
  `acceptance-criteria-validation`.

Do NOT use for:
- Auditoria de performance/escalabilidade em profundidade (carga, stress test,
  capacidade de infraestrutura) — isso é `software-architect`
  (`risk-and-scalability-assessment`) em nível de arquitetura, ou `devops` em nível
  de infraestrutura real; esta skill valida performance básica percebida pelo
  usuário, não faz teste de carga.
- Requisito de segurança — isso é `devsecops-engineer`; esta skill não avalia
  autenticação, autorização ou exposição de dado.

## Inputs Esperados

- Tarefa `Concluída` (obrigatório).
- `UX-SPEC.md`, Seções 2 e 4 (obrigatório) — layout e estados de tela, para
  validar usabilidade e cenário de erro conforme especificado.
- Requisito não funcional relevante do `PRD-TECNICO.md`, Seção 2, se houver
  (contexto — número real como referência, quando existir).

## Core Framework

1. **Performance básica percebida.** O tempo de resposta da interação é aceitável
   do ponto de vista do usuário (não uma medição de carga, uma checagem de que a
   tela não trava/demora de forma perceptível no uso normal)?
2. **Usabilidade conforme UX-SPEC.md.** A tela implementada é de fato usável como
   o UX/UI especificou — não só visualmente igual, mas a interação funciona como
   pretendido (ex.: o fluxo de decisão do UX-SPEC.md realmente guia o usuário).
3. **Cenário de erro.** Todo estado de erro especificado no UX-SPEC.md (Seção 4)
   se comporta como esperado quando forçado (não só existe no código, realmente
   aparece e comunica o problema).
4. **Requisito não funcional explícito.** Se o PRD-TECNICO.md tem um número real
   (ex.: "responde em até 2s"), a validação confere contra esse número, não contra
   uma sensação geral.

## Workflow

1. Para a tarefa em validação, releia a especificação de usabilidade e estados de
   erro no UX-SPEC.md.
2. Verifique performance percebida no uso normal.
3. Force cada cenário de erro especificado e confirme o comportamento.
4. Se houver requisito não funcional explícito no PRD-TECNICO.md, confira contra
   o número real.
5. Toda divergência encontrada: documente via `bug-documentation`.

## Output Esperado

- **Formato**: entrada no `QA-REPORT.md` — requisito não funcional validado,
  resultado, bugs referenciados quando aplicável.
- **Onde salva**: `.md/QA-REPORT.md`.

## Critério de Aceite

- [ ] Performance percebida no uso normal validada
- [ ] Toda usabilidade especificada no UX-SPEC.md confirmada na prática, não só
      visualmente
- [ ] Todo cenário de erro especificado foi forçado e o comportamento confirmado
- [ ] Requisito não funcional explícito (quando existir) validado contra o número
      real, não uma impressão geral

### MUST DO
- Forçar cada cenário de erro especificado, não assumir que "deve funcionar".
- Validar contra o número real quando o PRD-TECNICO.md especifica um.

### MUST NOT DO
- Fazer teste de carga/stress aqui — isso é fora do escopo desta skill.
- Aprovar usabilidade só porque a tela "parece" com o UX-SPEC.md sem testar a
  interação de fato.

---
name: non-functional-requirement-validation
description: Valida que a infraestrutura provisionada suporta os requisitos não funcionais (performance, disponibilidade) definidos pelo Software Architect no SDD.md. Use antes do deploy em produção, depois que a infraestrutura já está provisionada e o pipeline configurado. Do NOT use for validação funcional/usabilidade (isso é do qa-engineer) ou para desenhar o requisito de escalabilidade em si (isso já foi feito pelo software-architect).
metadata:
  author: devops
  version: '1.0.0'
---

# Non-Functional Requirement Validation

Você atua como DevOps Engineer confirmando, na infraestrutura real, que os
requisitos de performance e disponibilidade que o Software Architect definiu no
SDD.md são de fato suportados — não assumidos porque "o provisionamento seguiu a
Seção 3 à risca".

## Quando é Acionada

- Antes do deploy em produção, depois que a infraestrutura já está provisionada
  (`infrastructure-as-code-provisioning`) e o pipeline configurado
  (`cicd-pipeline-configuration`).

Do NOT use for:
- Validação funcional ou de usabilidade — isso é `qa-engineer`; esta skill valida
  capacidade de infraestrutura, não comportamento funcional.
- Desenhar o requisito de escalabilidade em si — isso já foi feito pelo
  `software-architect`, na Seção 6 do SDD.md; esta skill confirma que a
  infraestrutura real suporta o que foi desenhado, não redefine o requisito.

## Inputs Esperados

- `SDD.md`, Seção 6 (Riscos Técnicos e Dívida Técnica Aceita) (obrigatório) —
  requisito de performance/disponibilidade e o volume esperado.
- Infraestrutura já provisionada (obrigatório).

## Core Framework

1. **Performance sob carga esperada.** A infraestrutura suporta o volume que o
   SDD.md sinalizou como esperado, testado de fato (teste de carga básico), não
   assumido pelo dimensionamento.
2. **Disponibilidade.** A infraestrutura tem redundância suficiente para o nível
   de disponibilidade que o projeto exige (se o SDD.md/PRD-TECNICO.md especifica
   um SLA, testar contra ele; se não, confirmar que não há ponto único de falha
   óbvio já sinalizado na Seção 6).
3. **Comportamento sob o gargalo já identificado.** Se o SDD.md já sinalizou um
   gargalo esperado (Seção 6), confirma que a infraestrutura se comporta de forma
   aceitável quando esse gargalo é atingido (degrada graciosamente, não cai
   inteira).

## Workflow

1. Releia a Seção 6 do SDD.md para volume esperado, gargalos e requisito de
   disponibilidade.
2. Execute teste de carga básico contra o volume esperado, em staging.
3. Confirme redundância/disponibilidade conforme o requisito.
4. Force o gargalo já identificado (se aplicável) e confirme degradação aceitável,
   não queda total.
5. Toda divergência encontrada: sinaliza para `software-architect` (a
   infraestrutura real revela algo que o SDD.md não previu corretamente).

## Output Esperado

- **Formato**: resultado do teste de carga/disponibilidade contra o requisito do
  SDD.md, registrado no `DEPLOY.md`.
- **Onde salva**: `.md/DEPLOY.md`.

## Critério de Aceite

- [ ] Teste de carga executado contra o volume esperado do SDD.md, não assumido
- [ ] Disponibilidade confirmada conforme o requisito (SLA quando existir)
- [ ] Gargalo já identificado no SDD.md testado, com degradação aceitável
      confirmada
- [ ] Toda divergência encontrada sinalizada ao Software Architect

### MUST DO
- Testar de fato (carga real, ao menos básica), nunca assumir que o
  dimensionamento é suficiente só porque seguiu a especificação.
- Sinalizar ao Software Architect qualquer divergência entre o requisito e o
  comportamento real da infraestrutura.

### MUST NOT DO
- Aprovar a infraestrutura para produção sem testar contra o volume esperado.
- Redefinir o requisito de escalabilidade por conta própria quando a
  infraestrutura real não bate com o SDD.md — isso volta para o Software
  Architect.

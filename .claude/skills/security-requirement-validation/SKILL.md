---
name: security-requirement-validation
description: Audita o código contra os requisitos de segurança definidos no SDD.md pelo Software Architect — autenticação, autorização, criptografia, isolamento multi-tenant. Use como parte da auditoria final, depois que o QA aprovar o build. Do NOT use for definir o requisito de segurança em si (isso já foi feito pelo software-architect) ou para varredura genérica de código (isso é static-security-analysis).
metadata:
  author: devsecops
  version: '1.0.0'
---

# Security Requirement Validation

Você atua como DevSecOps Engineer confirmando que a implementação de fato cumpre o
que o Software Architect definiu como requisito de segurança em nível de
arquitetura — a diferença entre o SDD.md dizer "autenticação obrigatória em toda
rota sensível" e o código de fato aplicar isso em toda rota, não só nas óbvias.

## Quando é Acionada

- Como parte da auditoria final, depois que o QA aprova (Aprovado ou Aprovado com
  ressalvas) o build.

Do NOT use for:
- Definir o requisito de segurança em si — isso já foi feito pelo
  `software-architect`, na Seção 7 do SDD.md; esta skill audita o cumprimento, não
  redefine o requisito.
- Varredura genérica de padrão de código inseguro sem ligação a um requisito
  específico do SDD.md — isso é `static-security-analysis`.

## Inputs Esperados

- `SDD.md`, Seção 7 (obrigatório) — autenticação, autorização, criptografia,
  isolamento multi-tenant já definidos.
- Código-fonte do build aprovado pelo QA (obrigatório).
- Achados já registrados por `security-implementation-check` do Backend/Frontend/
  Mobile (contexto — o que os times já checaram durante a implementação, para
  auditar de forma independente, não confiar cegamente).

## Core Framework

Auditoria independente por item da Seção 7 do SDD.md:

1. **Autenticação.** Toda rota/operação que o SDD.md marca como exigindo
   autenticação de fato exige — testado tentando acessar sem credencial.
2. **Autorização.** Toda ação que exige permissão específica de fato verifica —
   testado tentando executar com permissão insuficiente.
3. **Criptografia.** Dado sensível identificado no SDD.md está de fato
   criptografado em trânsito (TLS confirmado) e em repouso (campo no banco/
   armazenamento confirmado, não só "deveria estar").
4. **Isolamento multi-tenant.** Se aplicável, testado tentando acessar dado de
   outro tenant — confirma que o isolamento é real, não só assumido pelo desenho.

## Workflow

1. Percorra a Seção 7 do SDD.md item a item.
2. Para cada item, teste ativamente (não só leia o código) — tente acessar sem
   autenticação, com permissão insuficiente, tente ver dado de outro tenant.
3. Toda falha encontrada: registre como achado, com severidade.
4. Compare contra o que `security-implementation-check` do time de implementação já
   havia checado — divergência entre o que foi declarado como feito e o que a
   auditoria independente encontrou é ela mesma um achado (processo, não só
   técnico).

## Output Esperado

- **Formato**: checklist da Seção 7 do SDD.md, item a item, com resultado
  (Confirmado / Falhou, com achado referenciado) — consolidado em
  `SECURITY-REVIEW.md`.
- **Onde salva**: `.md/SECURITY-REVIEW.md`.

## Critério de Aceite

- [ ] Todo item da Seção 7 do SDD.md foi testado ativamente, não só lido no código
- [ ] Toda falha de autenticação/autorização/criptografia/isolamento está
      registrada como achado com severidade
- [ ] Divergência entre o que o time de implementação declarou (via
      `security-implementation-check`) e o que a auditoria encontrou está registrada

### MUST DO
- Testar ativamente cada requisito (tentar acessar sem autenticação, sem
  permissão, dado de outro tenant), não só ler o código e assumir.
- Comparar contra o que o próprio time já havia declarado, registrando
  divergência.

### MUST NOT DO
- Confiar que um requisito está cumprido só porque o código "parece" implementá-lo,
  sem teste ativo.
- Redefinir o requisito de segurança do SDD.md durante a auditoria — se o requisito
  em si parecer insuficiente, isso é sinal para o Software Architect revisar, não
  para o DevSecOps mudar o padrão.

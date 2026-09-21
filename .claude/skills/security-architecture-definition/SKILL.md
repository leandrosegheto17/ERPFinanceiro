---
name: security-architecture-definition
description: Define requisitos de segurança e compliance em nível de arquitetura — autenticação, autorização, criptografia, isolamento multi-tenant se aplicável — como insumo para o DevSecOps. Use em paralelo a risk-and-scalability-assessment, depois que a arquitetura de componentes já está definida. Do NOT use for SAST/DAST, scanner de segredos ou hardening tático de código (isso é do devsecops-engineer) ou para risco de compliance em nível estratégico de negócio (isso é risk-and-compliance-check, do cto).
metadata:
  author: software-architect
  version: '1.0.0'
---

# Security Architecture Definition

Você atua como Software Architect definindo, em nível de arquitetura, como o sistema
autentica, autoriza, protege dado em trânsito/repouso e isola inquilinos (quando
aplicável) — o requisito que o DevSecOps vai usar como base para o hardening tático
mais adiante, não a implementação nem a auditoria de segurança em si.

## Quando é Acionada

- Em paralelo a `risk-and-scalability-assessment`, depois que `architecture-design`
  já definiu os componentes.

Do NOT use for:
- SAST/DAST, scanner de segredos, hardening de código — isso é `devsecops-engineer`,
  etapa 13 do pipeline de implementação; esta skill define requisito de arquitetura,
  não executa análise sobre código (que ainda não existe nesta fase).
- Risco técnico/segurança/compliance em nível estratégico de negócio — isso é
  `risk-and-compliance-check`, do agente `cto`, no Gate 2; esta skill fornece o
  requisito de arquitetura que alimenta essa revisão, não a substitui.

## Inputs Esperados

- Seções 1-2 do `SDD.md` (obrigatório) — componentes e fluxo de dados já definidos.
- `PRD-TECNICO.md`, requisitos não-funcionais e regras de negócio (contexto) — para
  saber que dado sensível circula e que nível de isolamento é necessário.

## Core Framework

1. **Autenticação.** Como o sistema confirma identidade — de usuário final, de
   serviço a serviço, de integração externa? Qual mecanismo (não a biblioteca
   específica — isso é `tech-stack-selection` — mas o requisito: MFA obrigatório,
   token de vida curta, etc.).
2. **Autorização.** Que modelo de controle de acesso é necessário (RBAC, ABAC,
   ownership simples)? Que ação exige que nível de permissão?
3. **Criptografia.** Que dado precisa de criptografia em trânsito (TLS em toda
   borda externa, no mínimo) e em repouso (dado sensível identificado no
   PRD-TECNICO.md)?
4. **Isolamento multi-tenant** (se aplicável). Se o sistema atende múltiplos
   clientes/organizações, como os dados de um inquilino ficam isolados dos outros —
   isolamento lógico (linha com tenant_id) ou físico (banco/schema separado)?
5. **Superfície de exposição.** Que componente da arquitetura fica exposto à
   internet, e qual o requisito mínimo de proteção dessa borda?

## Workflow

1. Percorra os componentes da arquitetura identificando toda borda que precisa de
   autenticação (usuário, serviço, integração externa).
2. Defina o modelo de autorização necessário, ligado às regras de negócio do
   PRD-TECNICO.md (Seção 3 de lá).
3. Identifique todo dado sensível (do PRD-TECNICO.md) e defina requisito de
   criptografia em trânsito e em repouso.
4. Se o produto for multi-tenant, defina o requisito de isolamento.
5. Escreva a Seção 7 do `SDD.md` (Requisitos de Segurança e Compliance).

## Output Esperado

- **Formato**: Seção 7 do `SDD.md`, com subtítulos "Autenticação", "Autorização",
  "Criptografia", "Isolamento Multi-Tenant" (ou "Não aplicável — sistema
  single-tenant", explicitamente) e "Superfície de Exposição".
- **Onde salva**: `.md/SDD.md`.

## Critério de Aceite

- [ ] Toda borda de autenticação identificada (usuário, serviço, integração externa)
- [ ] Modelo de autorização definido e ligado às regras de negócio do PRD-TECNICO.md
- [ ] Todo dado sensível identificado tem requisito de criptografia em trânsito e em
      repouso declarado
- [ ] Isolamento multi-tenant definido, ou explicitamente marcado como não aplicável
- [ ] Nenhum item da seção é genérico ("seguir boas práticas de segurança") sem
      requisito concreto

### MUST DO
- Ligar o requisito de autorização às regras de negócio reais do PRD-TECNICO.md, não
  um modelo genérico copiado.
- Marcar explicitamente "não aplicável" quando isolamento multi-tenant não se aplica,
  em vez de omitir a subseção.

### MUST NOT DO
- Escrever requisito de segurança genérico sem ligação a um componente/dado real da
  arquitetura.
- Especificar biblioteca ou implementação técnica aqui — isso é `tech-stack-selection`;
  esta skill define o requisito, não a ferramenta que o implementa.

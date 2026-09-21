---
name: static-security-analysis
description: Executa/revisa análise estática de código (SAST) e checagem de vulnerabilidades em dependências de terceiros. Use em paralelo à implementação — não espera build completo, roda sobre o código conforme ele é escrito. Do NOT use for auditar requisito de segurança de arquitetura específico (isso é security-requirement-validation) ou para threat model completo do repositório (isso é a skill de apoio security-threat-model, invocada quando necessário).
metadata:
  author: devsecops
  version: '1.0.0'
---

# Static Security Analysis

Você atua como DevSecOps Engineer rodando varredura estática de código e checagem
de dependência de terceiros — a camada de segurança que não depende de esperar o
build inteiro terminar, porque escaneia o que já existe a qualquer momento.

## Quando é Acionada

- Em paralelo à implementação de Backend/Frontend/Mobile — roda continuamente
  sobre o código conforme ele é escrito, não espera um build completo.

Do NOT use for:
- Auditar um requisito de segurança de arquitetura específico (autenticação,
  autorização, criptografia) contra o SDD.md — isso é `security-requirement-validation`.
- Threat model completo e ancorado no repositório — isso é a skill de apoio
  `security-threat-model`, invocada dentro desta skill quando um achado exige
  modelagem mais profunda do que um scanner direto resolve.

## Inputs Esperados

- Código-fonte do repositório (obrigatório) — conforme vai sendo escrito por
  Backend/Frontend/Mobile.
- Lockfile/manifesto de dependências (obrigatório) — para checagem de
  vulnerabilidade conhecida em pacote de terceiro.
- `security-best-practices` como referência por linguagem/framework (apoio).

## Core Framework

1. **SAST.** Padrões de código inseguro (injeção, uso de função perigosa, segredo
   hardcoded, deserialização insegura) — usa `security-best-practices` para o
   detalhe específico de linguagem/framework.
2. **Dependência vulnerável.** Toda dependência de terceiro com CVE conhecido —
   severidade da CVE, se há atualização disponível que corrige.
3. **Segredo exposto no código.** Chave de API, senha, token — nunca deveria estar
   commitado; qualquer achado desses é automaticamente severidade alta/crítica
   (ver `finding-severity-classification`).
4. **Achado que exige modelagem mais profunda.** Quando um padrão suspeito não é
   claramente resolvido por um scanner direto (ex.: fluxo de dado sensível
   atravessando múltiplos componentes), invoca `security-threat-model` para
   analisar o trust boundary específico.

## Workflow

1. Rode a varredura estática sobre o código disponível (não espera build
   completo).
2. Rode a checagem de dependências contra CVEs conhecidos.
3. Para achado que precisa de análise mais profunda de fluxo/trust boundary,
   invoque `security-threat-model`.
4. Registre todo achado (mesmo antes da auditoria final pós-QA) — a lista
   acumulada alimenta `finding-severity-classification` quando a auditoria formal
   rodar.

## Output Esperado

- **Formato**: lista de achados (arquivo/linha, tipo, CVE se aplicável) — rascunho
  contínuo, consolidado depois em `SECURITY-REVIEW.md` por
  `security-report-drafting`.
- **Onde salva**: anotações contínuas; consolidação final em
  `.md/SECURITY-REVIEW.md`.

## Critério de Aceite

- [ ] Toda dependência com CVE conhecido está listada com a versão que corrige,
      quando existir
- [ ] Todo segredo hardcoded encontrado está marcado como severidade alta/crítica
- [ ] Achado que exige análise de fluxo/trust boundary foi passado por
      `security-threat-model`, não deixado como "suspeito" sem investigação

### MUST DO
- Rodar a varredura continuamente, não só uma vez no final.
- Marcar automaticamente segredo exposto como severidade alta/crítica.

### MUST NOT DO
- Esperar o build completo para começar a escanear.
- Ignorar CVE de dependência só porque "provavelmente não é explorável neste
  contexto" sem registrar a avaliação.

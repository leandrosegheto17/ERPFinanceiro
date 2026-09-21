---
name: sensitive-data-exposure-check
description: Identifica exposição de dados sensíveis em logs, mensagens de erro, armazenamento local (mobile) ou payloads de API. Use como parte da auditoria final, verificando toda superfície onde dado pode vazar sem ser um bug funcional óbvio. Do NOT use for auditar requisito de criptografia definido no SDD.md em si (isso é security-requirement-validation) ou para achado de compliance regulatório (isso é compliance-validation).
metadata:
  author: devsecops
  version: '1.0.0'
---

# Sensitive Data Exposure Check

Você atua como DevSecOps Engineer procurando dado sensível vazando por onde ninguém
está olhando de propósito — log verboso demais, mensagem de erro que devolve
detalhe interno, dado guardado sem proteção no dispositivo, ou payload de API que
retorna mais campo do que o cliente precisa.

## Quando é Acionada

- Como parte da auditoria final, percorrendo toda superfície onde dado sensível
  pode vazar sem ser um bug funcional óbvio (não quebra a funcionalidade, só expõe
  informação).

Do NOT use for:
- Auditar o requisito de criptografia definido no SDD.md em si — isso é
  `security-requirement-validation`; esta skill procura vazamento incidental, não
  audita o requisito formal de criptografia.
- Achado de compliance regulatório (base legal, consentimento) — isso é
  `compliance-validation`; esta skill é técnica (onde o dado vaza), não regulatória
  (se o tratamento do dado tem base legal).

## Inputs Esperados

- Código-fonte do build aprovado pelo QA (obrigatório) — em especial camada de
  log, tratamento de erro, e (no Mobile) armazenamento local.
- `API-CONTRACT.yaml` (obrigatório) — para comparar o que o payload documentado
  promete vs. o que a resposta real inclui.
- Lista de campos sensíveis identificados no PRD-TECNICO.md/SDD.md (contexto).

## Core Framework

1. **Log verboso.** Log de aplicação/erro inclui campo sensível (senha, token,
   dado pessoal completo) em texto plano? Log deveria mascarar ou omitir.
2. **Mensagem de erro reveladora.** Erro devolvido ao cliente expõe detalhe
   interno (stack trace, query SQL, estrutura de banco) que ajuda um atacante, em
   vez de uma mensagem genérica?
3. **Armazenamento local (Mobile).** Dado sensível fica em armazenamento
   desprotegido no dispositivo (preferências não criptografadas, arquivo de cache
   sem proteção) quando deveria estar no keychain/keystore seguro da plataforma?
4. **Payload de API além do necessário.** A resposta de um endpoint inclui campo
   que o cliente não precisa e que aumenta a superfície de exposição (ex.: devolver
   o objeto de usuário inteiro quando só o nome é necessário)?

## Workflow

1. Percorra a camada de log e tratamento de erro em busca de dado sensível exposto.
2. No Mobile, percorra o armazenamento local em busca de dado sensível
   desprotegido.
3. Compare cada payload de resposta de API contra o que o cliente de fato consome,
   procurando campo além do necessário.
4. Toda exposição encontrada: registre como achado, com severidade.

## Output Esperado

- **Formato**: lista de achados — `| Superfície (log/erro/storage/payload) |
  Campo exposto | Onde | Severidade |` — consolidado em `SECURITY-REVIEW.md`.
- **Onde salva**: `.md/SECURITY-REVIEW.md`.

## Critério de Aceite

- [ ] Camada de log/erro revisada em busca de dado sensível exposto
- [ ] No Mobile, armazenamento local revisado quanto à proteção de dado sensível
- [ ] Todo payload de API comparado contra o consumo real do cliente, campo por
      campo relevante
- [ ] Toda exposição encontrada tem severidade classificada

### MUST DO
- Comparar payload de API contra o consumo real do cliente, não só contra o
  contrato documentado (o contrato pode já estar superexposto por design).
- Verificar armazenamento local no Mobile especificamente — é uma superfície que
  Backend/Frontend não têm.

### MUST NOT DO
- Ignorar exposição em log/erro por não ser um "bug funcional" — vazamento de
  dado é achado de segurança independente de quebrar ou não a funcionalidade.
- Assumir que o armazenamento local está protegido sem verificar a API de
  segurança da plataforma efetivamente usada no código.

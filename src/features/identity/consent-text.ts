/**
 * TASK-036 — texto e versão do consentimento específico e destacado para
 * dado pessoal sensível (RNF-05, CA-10.2, migration `consent` de TASK-027).
 *
 * `CONSENT_TEXT_VERSION` é gravado literalmente em `consent.version` — é o
 * mesmo valor exibido pelo `BlocoConsentimento` (nunca duas fontes de
 * verdade para a versão aceita). `CONSENT_TEXT_SHA256` é o hash do texto
 * completo abaixo, gravado em `consent.text_sha256`; o teste
 * (`consent-text.test.ts`) recalcula o hash a partir do texto literal e
 * confirma que bate com esta constante — qualquer edição do texto sem
 * atualizar `CONSENT_TEXT_VERSION`/`CONSENT_TEXT_SHA256` junto falha o
 * teste, em vez de silenciosamente gravar uma versão desatualizada.
 *
 * Alterar `CONSENT_TEXT_FULL` sem também mudar `CONSENT_TEXT_VERSION` é
 * defeito: a tabela `consent` usa `version` como identificador do texto
 * aceito (RNF-05 — "registro do consentimento... versão do texto aceito").
 */

export const CONSENT_TEXT_VERSION = "2026-09-08.v1";

/** Rótulo curto ao lado do `Alternador`, dentro do `BlocoConsentimento`. */
export const CONSENT_TEXT_SUMMARY =
  "Concordo que o app guarde meu progresso de leitura, minhas preferências " +
  "e meus esboços. Esses dados revelam convicção religiosa e são tratados " +
  "como dado pessoal sensível.";

/** Texto completo, alcançável via "ler o texto completo" (UX-SPEC T-08). */
export const CONSENT_TEXT_FULL =
  "Concordo que o app guarde meu progresso de leitura, minhas preferências " +
  "e meus esboços. Esses dados revelam convicção religiosa e são tratados " +
  "como dado pessoal sensível, conforme a Lei Geral de Proteção de Dados " +
  "(LGPD, art. 5º, II). Você pode revogar este consentimento a qualquer " +
  "momento nas configurações da conta, e pode solicitar a exclusão " +
  "completa dos seus dados, atendida em até 15 dias corridos.";

/** SHA-256 (hex) de `CONSENT_TEXT_FULL`, gravado em `consent.text_sha256`. */
export const CONSENT_TEXT_SHA256 =
  "9033c7736f963b928c820d6a2b5476e958956ff5530bb21f71dfa2aed8f1d2f9";

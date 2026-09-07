/**
 * Validação de schema de `CorpusManifest` (critério de aceite de TASK-008:
 * "manifest.json validado contra schema"). Sem Ajv/Zod no projeto (ver
 * `package.json`): validador TypeScript explícito, campo a campo, que lança
 * `ManifestSchemaError` com o motivo exato da rejeição em vez de retornar
 * `false` silenciosamente.
 */

import type { CorpusManifest } from "./manifest-types";

export class ManifestSchemaError extends Error {}

const SHA256_HEX_PATTERN = /^[0-9a-f]{64}$/;

function fail(reason: string): never {
  throw new ManifestSchemaError(`Manifesto inválido: ${reason}`);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.length > 0;
}

function isIsoDate(value: unknown): value is string {
  if (typeof value !== "string" || value.length === 0) {
    return false;
  }
  const parsed = new Date(value);
  return !Number.isNaN(parsed.getTime());
}

function isValidUrl(value: unknown): value is string {
  if (typeof value !== "string") {
    return false;
  }
  try {
    new URL(value);
    return true;
  } catch {
    return false;
  }
}

/**
 * Lança `ManifestSchemaError` descrevendo o campo inválido; retorna
 * normalmente (sem valor) quando `value` é um `CorpusManifest` válido — o
 * type predicate permite usar o resultado como guarda de tipo depois da
 * chamada não lançar.
 */
export function validateManifestSchema(value: unknown): asserts value is CorpusManifest {
  if (typeof value !== "object" || value === null) {
    fail("valor raiz deve ser um objeto");
  }

  const manifest = value as Record<string, unknown>;

  if (!isNonEmptyString(manifest.sourceId)) {
    fail("campo obrigatório 'sourceId' ausente ou não é uma string não vazia");
  }

  if (!isIsoDate(manifest.sourceVersionDate)) {
    fail("campo obrigatório 'sourceVersionDate' ausente ou não é uma data ISO 8601 válida");
  }

  if (!isIsoDate(manifest.importedAt)) {
    fail("campo obrigatório 'importedAt' ausente ou não é uma data ISO 8601 válida");
  }

  if (!isNonEmptyString(manifest.license)) {
    fail("campo obrigatório 'license' ausente ou não é uma string não vazia");
  }

  if (!isValidUrl(manifest.licenseUrl)) {
    fail("campo obrigatório 'licenseUrl' ausente ou não é uma URL válida");
  }

  if (
    !Array.isArray(manifest.holders) ||
    manifest.holders.length === 0 ||
    !manifest.holders.every((holder) => isNonEmptyString(holder))
  ) {
    fail("campo obrigatório 'holders' ausente, vazio, ou contém entrada que não é string não vazia");
  }

  if (manifest.modifications !== "none") {
    fail("campo obrigatório 'modifications' deve ser exatamente o literal 'none' (exigência CC BY 4.0)");
  }

  if (typeof manifest.sha256 !== "object" || manifest.sha256 === null || Array.isArray(manifest.sha256)) {
    fail("campo obrigatório 'sha256' ausente ou não é um mapa nome-do-arquivo → hash");
  }

  const sha256Entries = Object.entries(manifest.sha256 as Record<string, unknown>);
  if (sha256Entries.length === 0) {
    fail("campo obrigatório 'sha256' não pode ser um mapa vazio");
  }

  for (const [fileName, hash] of sha256Entries) {
    if (typeof hash !== "string" || !SHA256_HEX_PATTERN.test(hash)) {
      fail(
        `entrada 'sha256["${fileName}"]' não é um hash SHA-256 válido (esperado 64 caracteres hexadecimais)`,
      );
    }
  }
}

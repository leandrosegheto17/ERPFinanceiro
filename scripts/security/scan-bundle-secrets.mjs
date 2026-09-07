#!/usr/bin/env node
// scripts/security/scan-bundle-secrets.mjs
//
// Varre um diretório (o bundle publicado do cliente) em busca de segredos de
// serviço que nunca deveriam aparecer ali (GUARDRAILS.md G-10 / TASK.md
// DI-09: chave de service role, chave VAPID privada, connection string
// privilegiada do Postgres).
//
// Usado como:
//   - biblioteca, pelo teste automatizado
//     (tests/security/bundle-secrets.test.mjs, TASK-005)
//   - CLI standalone em CI: `node scripts/security/scan-bundle-secrets.mjs dist`
//     (gate de release, TASK-042)
//
// TASK-005 (placeholder, sem bundle real de app ainda) e TASK-042 (gate de
// release real) compartilham exatamente esta mesma lógica de detecção — não
// duas implementações divergentes.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join } from 'node:path';
import { pathToFileURL } from 'node:url';

const TEXT_EXTENSIONS = new Set(['.js', '.mjs', '.cjs', '.css', '.html', '.map', '.json', '.txt']);

const JWT_PATTERN = /eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*/g;
const POSTGRES_CREDENTIAL_PATTERN = /postgres(?:ql)?:\/\/[^:\s"'`/]+:[^@\s"'`]+@/gi;

/**
 * Decodifica o payload (segunda parte) de um JWT, sem validar assinatura —
 * aqui só nos interessa saber se a claim `role` é `service_role`.
 * @param {string} value
 * @returns {Record<string, unknown> | null}
 */
function decodeJwtPayload(value) {
  const parts = value.split('.');
  if (parts.length !== 3) return null;
  try {
    const json = Buffer.from(parts[1].replace(/-/g, '+').replace(/_/g, '/'), 'base64').toString('utf8');
    return JSON.parse(json);
  } catch {
    return null;
  }
}

/**
 * @typedef {{ filePath: string, reason: string, match: string }} SecretFinding
 */

/**
 * @param {string} content
 * @param {string} filePath
 * @returns {SecretFinding[]}
 */
export function findSecretsInContent(content, filePath) {
  /** @type {SecretFinding[]} */
  const findings = [];

  for (const match of content.matchAll(JWT_PATTERN)) {
    const payload = decodeJwtPayload(match[0]);
    if (payload && payload.role === 'service_role') {
      findings.push({
        filePath,
        reason: 'JWT com claim role=service_role (chave de service role do Supabase)',
        match: match[0],
      });
    }
  }

  for (const match of content.matchAll(POSTGRES_CREDENTIAL_PATTERN)) {
    findings.push({
      filePath,
      reason: 'connection string do Postgres com credencial embutida',
      match: match[0],
    });
  }

  const envSecrets = [
    ['SUPABASE_SERVICE_ROLE_KEY', process.env.SUPABASE_SERVICE_ROLE_KEY],
    ['VAPID_PRIVATE_KEY', process.env.VAPID_PRIVATE_KEY],
  ].filter(([, value]) => typeof value === 'string' && value.length > 0);

  for (const [name, secret] of envSecrets) {
    if (content.includes(secret)) {
      findings.push({
        filePath,
        reason: `valor literal da variável de ambiente de segredo ${name}`,
        match: secret,
      });
    }
  }

  return findings;
}

/**
 * @param {string} dir
 * @returns {string[]}
 */
function listFilesRecursively(dir) {
  const entries = readdirSync(dir);
  /** @type {string[]} */
  const files = [];
  for (const entry of entries) {
    const fullPath = join(dir, entry);
    const stats = statSync(fullPath);
    if (stats.isDirectory()) {
      files.push(...listFilesRecursively(fullPath));
    } else if (TEXT_EXTENSIONS.has(extname(entry))) {
      files.push(fullPath);
    }
  }
  return files;
}

/**
 * @param {string} bundleDir
 * @returns {SecretFinding[]}
 */
export function scanBundleForSecrets(bundleDir) {
  const files = listFilesRecursively(bundleDir);
  /** @type {SecretFinding[]} */
  const findings = [];
  for (const filePath of files) {
    const content = readFileSync(filePath, 'utf8');
    findings.push(...findSecretsInContent(content, filePath));
  }
  return findings;
}

const isMainModule = process.argv[1] !== undefined && import.meta.url === pathToFileURL(process.argv[1]).href;

if (isMainModule) {
  const target = process.argv[2];
  if (!target) {
    console.error('Uso: node scripts/security/scan-bundle-secrets.mjs <diretorio-do-bundle>');
    process.exit(2);
  }
  const findings = scanBundleForSecrets(target);
  if (findings.length > 0) {
    console.error(`Encontrado(s) ${findings.length} segredo(s) potencial(is) no bundle:`);
    for (const finding of findings) {
      console.error(`  - ${finding.filePath}: ${finding.reason}`);
    }
    process.exit(1);
  }
  console.log(`OK: nenhum segredo encontrado em ${target}`);
}

#!/usr/bin/env node
// scripts/security/scan-bundle-forbidden-strings.mjs
//
// Varre um diretório (o bundle publicado do cliente) em busca das strings
// proibidas por DI-05 / GUARDRAILS.md G-01: o produto usa o corpus BLIVRE
// (`porbr2018`, CC BY 4.0) e nunca pode se confundir com, ou citar por nome,
// outras traduções de licenciamento distinto — "Almeida Atualizada",
// "Almeida Revista e Atualizada", "ARA", "ARC" — em nenhuma tela, commit ou
// texto de acervo (CTO-REVIEW R-02, RN-06).
//
// Usado como:
//   - biblioteca, pelo teste automatizado
//     (tests/security/bundle-forbidden-strings.test.mjs, TASK-011)
//   - CLI standalone em CI: `node scripts/security/scan-bundle-forbidden-strings.mjs dist`
//     (gate de release, TASK-091)
//
// Decisão de matching (evitar falso positivo sem enfraquecer a proteção):
//   - "Almeida Atualizada" / "Almeida Revista e Atualizada" são frases longas
//     e específicas — comparação case-insensitive direta já é segura, sem
//     risco relevante de colisão com outro texto legítimo.
//   - "ARA" e "ARC" são siglas de 3 letras: comparadas sem borda de palavra,
//     colidiriam com substrings legítimas frequentes em português
//     (ex.: "ARARA", "ARCO", "PARAR", "MARCOS", "ARCA", nomes de variável
//     como "chart"). Por isso exigimos borda de token nos dois lados — a
//     ocorrência não pode ser flanqueada por letra (`[A-Za-zÀ-ÿ]`) — e
//     comparamos sempre em maiúsculas: sigla bíblica é convencionalmente
//     grafada em maiúsculas (ARA/ARC), então exigir exatamente essa forma
//     (não `ara`/`arc` minúsculo, que é comum em outras palavras/tokens)
//     reduz ruído sem abrir brecha real — quem for citar a tradução por
//     nome/marca o faz na forma convencional em maiúsculas.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join } from 'node:path';
import { pathToFileURL } from 'node:url';

const TEXT_EXTENSIONS = new Set(['.js', '.mjs', '.cjs', '.css', '.html', '.map', '.json', '.txt']);

const PHRASE_PATTERNS = [
  { reason: 'string proibida "Almeida Revista e Atualizada" (DI-05)', pattern: /almeida\s+revista\s+e\s+atualizada/gi },
  { reason: 'string proibida "Almeida Atualizada" (DI-05)', pattern: /almeida\s+atualizada/gi },
];

// Borda de token: não flanqueada por letra (acentuada inclusive) nos dois
// lados, para não confundir com substring de outra palavra (ver comentário
// de topo do arquivo).
const ACRONYM_PATTERNS = [
  { reason: 'string proibida "ARA" (sigla da Almeida Revista e Atualizada, DI-05)', pattern: /(?<![A-Za-zÀ-ÿ])ARA(?![A-Za-zÀ-ÿ])/g },
  { reason: 'string proibida "ARC" (sigla da Almeida Revista e Corrigida, DI-05)', pattern: /(?<![A-Za-zÀ-ÿ])ARC(?![A-Za-zÀ-ÿ])/g },
];

/**
 * @typedef {{ filePath: string, reason: string, match: string }} ForbiddenStringFinding
 */

/**
 * @param {string} content
 * @param {string} filePath
 * @returns {ForbiddenStringFinding[]}
 */
export function findForbiddenStringsInContent(content, filePath) {
  /** @type {ForbiddenStringFinding[]} */
  const findings = [];

  for (const { reason, pattern } of PHRASE_PATTERNS) {
    for (const match of content.matchAll(pattern)) {
      findings.push({ filePath, reason, match: match[0] });
    }
  }

  for (const { reason, pattern } of ACRONYM_PATTERNS) {
    for (const match of content.matchAll(pattern)) {
      findings.push({ filePath, reason, match: match[0] });
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
 * @returns {ForbiddenStringFinding[]}
 */
export function scanBundleForForbiddenStrings(bundleDir) {
  const files = listFilesRecursively(bundleDir);
  /** @type {ForbiddenStringFinding[]} */
  const findings = [];
  for (const filePath of files) {
    const content = readFileSync(filePath, 'utf8');
    findings.push(...findForbiddenStringsInContent(content, filePath));
  }
  return findings;
}

const isMainModule = process.argv[1] !== undefined && import.meta.url === pathToFileURL(process.argv[1]).href;

if (isMainModule) {
  const target = process.argv[2];
  if (!target) {
    console.error('Uso: node scripts/security/scan-bundle-forbidden-strings.mjs <diretorio-do-bundle>');
    process.exit(2);
  }
  const findings = scanBundleForForbiddenStrings(target);
  if (findings.length > 0) {
    console.error(`Encontrada(s) ${findings.length} string(s) proibida(s) no bundle:`);
    for (const finding of findings) {
      console.error(`  - ${finding.filePath}: ${finding.reason} (match: "${finding.match}")`);
    }
    process.exit(1);
  }
  console.log(`OK: nenhuma string proibida encontrada em ${target}`);
}

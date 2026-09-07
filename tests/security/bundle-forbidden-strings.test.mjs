// tests/security/bundle-forbidden-strings.test.mjs
//
// TASK-011 (Lote 1 — Núcleo: Corpus e Importação): teste anti-string proibida
// exigido por DI-05 / GUARDRAILS.md G-01. O corpus usado é BLIVRE
// (`porbr2018`, CC BY 4.0) — o produto nunca pode se confundir com, ou citar
// por nome, outras traduções ("Almeida Atualizada", "Almeida Revista e
// Atualizada", "ARA", "ARC"), por razão de licenciamento/marca.
//
// Mesmo padrão de tests/security/bundle-secrets.test.mjs (TASK-005):
//   1. Prova que o detector (scripts/security/scan-bundle-forbidden-strings.mjs)
//      de fato encontra cada uma das 4 strings proibidas quando injetadas
//      numa fixture sintética — não é asserção vazia por lógica ausente.
//   2. Prova que o detector NÃO aciona falso positivo em texto legítimo que
//      colide por substring com as siglas curtas (ARARA, ARCO, PARAR,
//      MARCOS, ARCA) — ver decisão de matching documentada no próprio
//      scanner.
//   3. Roda o mesmo detector contra `dist/` sempre que ele existir. Enquanto
//      `dist/` não existe nesta sessão de teste, o teste correspondente fica
//      explicitamente `skipped` (nunca `passed` silencioso).
//
// TASK-091 (Lote consolidação final) reusa esta mesma lógica de detecção
// como gate de release formal, junto com os demais checks de DI-05.
//
// Rodar com: node --test tests/security/bundle-forbidden-strings.test.mjs

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync, rmSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { scanBundleForForbiddenStrings } from '../../scripts/security/scan-bundle-forbidden-strings.mjs';

const PROJECT_ROOT = fileURLToPath(new URL('../../', import.meta.url));
const BUNDLE_DIR = join(PROJECT_ROOT, 'dist');

function withFixture(prefix, content, run) {
  const fixtureDir = mkdtempSync(join(tmpdir(), prefix));
  try {
    writeFileSync(join(fixtureDir, 'chunk.js'), content);
    return run(fixtureDir);
  } finally {
    rmSync(fixtureDir, { recursive: true, force: true });
  }
}

test('detector aciona ao encontrar "Almeida Atualizada" injetada de propósito (fixture sintética)', () => {
  withFixture('bundle-forbidden-fixture-', 'const t = "texto da Almeida Atualizada aqui";', (dir) => {
    const findings = scanBundleForForbiddenStrings(dir);
    assert.ok(findings.length > 0, 'deveria encontrar "Almeida Atualizada" injetada');
    assert.match(findings[0].reason, /Almeida Atualizada/);
  });
});

test('detector aciona ao encontrar "Almeida Revista e Atualizada" injetada de propósito (fixture sintética)', () => {
  withFixture('bundle-forbidden-fixture-', 'const t = "texto da Almeida Revista e Atualizada aqui";', (dir) => {
    const findings = scanBundleForForbiddenStrings(dir);
    assert.ok(findings.length > 0, 'deveria encontrar "Almeida Revista e Atualizada" injetada');
    assert.match(findings[0].reason, /Almeida Revista e Atualizada/);
  });
});

test('detector aciona ao encontrar a sigla "ARA" injetada de propósito (fixture sintética)', () => {
  withFixture('bundle-forbidden-fixture-', 'const t = "consulte a ARA para comparar";', (dir) => {
    const findings = scanBundleForForbiddenStrings(dir);
    assert.ok(findings.length > 0, 'deveria encontrar a sigla ARA injetada');
    assert.match(findings[0].reason, /ARA/);
  });
});

test('detector aciona ao encontrar a sigla "ARC" injetada de propósito (fixture sintética)', () => {
  withFixture('bundle-forbidden-fixture-', 'const t = "consulte a ARC para comparar";', (dir) => {
    const findings = scanBundleForForbiddenStrings(dir);
    assert.ok(findings.length > 0, 'deveria encontrar a sigla ARC injetada');
    assert.match(findings[0].reason, /ARC/);
  });
});

test('detector não sinaliza falso positivo em texto legítimo que colide por substring com as siglas', () => {
  withFixture(
    'bundle-forbidden-fixture-clean-',
    [
      'console.log("hello world");',
      'export const livro = "Evangelho segundo Marcos";',
      'export const arara = "ararinha-azul";',
      'export const arco = "arco-íris";',
      'export const arca = "arca de Noé";',
      'if (!pararExecucao) { continue; }',
      'export const bookId = "GEN";',
    ].join('\n'),
    (dir) => {
      const findings = scanBundleForForbiddenStrings(dir);
      assert.deepEqual(findings, []);
    },
  );
});

test('detector não sinaliza falso positivo num bundle sem nenhuma string proibida', () => {
  withFixture(
    'bundle-forbidden-fixture-clean-',
    'export const corpus = "porbr2018"; export const license = "CC BY 4.0";',
    (dir) => {
      const findings = scanBundleForForbiddenStrings(dir);
      assert.deepEqual(findings, []);
    },
  );
});

test(
  'bundle real do cliente (dist/) não contém nenhuma string proibida por DI-05',
  {
    skip: existsSync(BUNDLE_DIR)
      ? false
      : 'dist/ ainda não existe — nenhum build real rodou ainda nesta sessão de teste ' +
        '(este check passa a rodar de verdade a partir do primeiro `npm run build`; ' +
        'gate formal de release é TASK-091)',
  },
  () => {
    const findings = scanBundleForForbiddenStrings(BUNDLE_DIR);
    assert.deepEqual(
      findings,
      [],
      `String(s) proibida(s) encontrada(s) no bundle publicado: ${JSON.stringify(findings, null, 2)}`,
    );
  },
);

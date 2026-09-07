// tests/security/bundle-secrets.test.mjs
//
// TASK-005 (Lote 0 — Fundação e CI): placeholder do teste de grep do bundle
// do cliente exigido por DI-09 / GUARDRAILS.md G-10. Nenhuma tarefa de build
// de app rodou ainda neste repositório — não existe `dist/` real.
//
// Este arquivo evita o problema descrito no critério de aceite ("o teste
// deve ser escrito para não falsamente passar quando o bundle existir de
// verdade"):
//
//   1. Prova que o detector (scripts/security/scan-bundle-secrets.mjs) de
//      fato encontra um segredo injetado numa fixture sintética — não é uma
//      asserção vazia por lógica ausente.
//   2. Roda o mesmo detector contra `dist/` sempre que ele existir. Assim
//      que as próximas tarefas gerarem o bundle real, este mesmo teste
//      passa a cobrir o caso real automaticamente — sem precisar ser
//      reescrito. Enquanto `dist/` não existe, o teste correspondente fica
//      explicitamente `skipped` (nunca `passed` silencioso), com o motivo
//      registrado no relatório do runner.
//
// TASK-042 (Lote 7) reusa esta mesma lógica de detecção como gate de
// release formal.
//
// Rodar com: node --test tests/security/bundle-secrets.test.mjs

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync, rmSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { scanBundleForSecrets } from '../../scripts/security/scan-bundle-secrets.mjs';

const PROJECT_ROOT = fileURLToPath(new URL('../../', import.meta.url));
const BUNDLE_DIR = join(PROJECT_ROOT, 'dist');

test('detector aciona ao encontrar um segredo de service_role injetado (fixture sintética)', () => {
  const fixtureDir = mkdtempSync(join(tmpdir(), 'bundle-secret-fixture-'));
  try {
    const fakePayload = Buffer.from(
      JSON.stringify({ iss: 'supabase-demo', role: 'service_role', exp: 9999999999 }),
    ).toString('base64url');
    const fakeServiceRoleJwt = `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.${fakePayload}.assinatura-fake-nao-e-jwt-valido`;

    writeFileSync(
      join(fixtureDir, 'chunk.js'),
      `console.log("segredo inserido de propósito para o teste: ${fakeServiceRoleJwt}");`,
    );

    const findings = scanBundleForSecrets(fixtureDir);

    assert.ok(
      findings.length > 0,
      'o detector deveria encontrar o JWT com claim role=service_role injetado na fixture',
    );
    assert.match(findings[0].reason, /service_role/);
  } finally {
    rmSync(fixtureDir, { recursive: true, force: true });
  }
});

test('detector aciona ao encontrar connection string do Postgres com credencial embutida (fixture sintética)', () => {
  const fixtureDir = mkdtempSync(join(tmpdir(), 'bundle-secret-fixture-'));
  try {
    writeFileSync(
      join(fixtureDir, 'chunk.js'),
      'const url = "postgresql://postgres:senha-fake-de-teste@db.exemplo.com:5432/postgres";',
    );

    const findings = scanBundleForSecrets(fixtureDir);

    assert.ok(findings.length > 0, 'o detector deveria encontrar a connection string com credencial');
    assert.match(findings[0].reason, /connection string/);
  } finally {
    rmSync(fixtureDir, { recursive: true, force: true });
  }
});

test('detector não sinaliza falso positivo num bundle sem nenhum segredo', () => {
  const fixtureDir = mkdtempSync(join(tmpdir(), 'bundle-secret-fixture-clean-'));
  try {
    writeFileSync(
      join(fixtureDir, 'chunk.js'),
      'console.log("hello world"); export const anonKey = "chave-anonima-publica-sem-problema";',
    );

    const findings = scanBundleForSecrets(fixtureDir);

    assert.deepEqual(findings, []);
  } finally {
    rmSync(fixtureDir, { recursive: true, force: true });
  }
});

test(
  'bundle real do cliente (dist/) não contém segredo de serviço',
  {
    skip: existsSync(BUNDLE_DIR)
      ? false
      : 'dist/ ainda não existe — nenhuma tarefa de build de app rodou ainda neste repositório ' +
        '(scaffold é TASK-001; este check passa a rodar de verdade a partir do primeiro build real; ' +
        'gate formal de release é TASK-042)',
  },
  () => {
    const findings = scanBundleForSecrets(BUNDLE_DIR);
    assert.deepEqual(
      findings,
      [],
      `Segredo(s) encontrado(s) no bundle publicado: ${JSON.stringify(findings, null, 2)}`,
    );
  },
);

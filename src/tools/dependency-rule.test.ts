import { mkdtempSync, writeFileSync, mkdirSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { cruise } from "dependency-cruiser";
import { describe, expect, it, afterEach } from "vitest";

/**
 * TASK-001 — critério de aceite: "lint falha se `core` importar de
 * `features`". Este teste cobre as duas pontas:
 *  1. o `src` real do projeto passa limpo pela regra (build/lint limpos);
 *  2. uma fixture que força `core` a importar de `features` é reprovada pela
 *     mesma regra (`no-features-from-core`), confirmando que o lint
 *     efetivamente falha nesse caso.
 *
 * A regra em si é a mesma de `.dependency-cruiser.cjs` (usada por
 * `npm run lint:deps`); duplicada aqui via API do dependency-cruiser para não
 * depender de spawnar um processo CLI.
 */

const noFeaturesFromCoreRuleSet = {
  forbidden: [
    {
      name: "no-features-from-core",
      severity: "error" as const,
      from: { path: "(^|[\\\\/])core[\\\\/]" },
      to: { path: "(^|[\\\\/])features[\\\\/]" },
    },
  ],
  options: {
    tsPreCompilationDeps: true,
  },
};

async function cruiseWithRule(target: string) {
  const result = await cruise([target], {
    validate: true,
    ruleSet: noFeaturesFromCoreRuleSet,
  });

  if (result.output === undefined || typeof result.output === "string") {
    throw new Error("Saída inesperada do dependency-cruiser");
  }

  return result.output;
}

describe("DI-02 — dependência unidirecional core -> features", () => {
  let fixtureDir: string | undefined;

  afterEach(() => {
    if (fixtureDir) {
      rmSync(fixtureDir, { recursive: true, force: true });
      fixtureDir = undefined;
    }
  });

  it("não reporta violação para o src real do projeto (build/lint limpos)", async () => {
    const output = await cruiseWithRule("src");

    const violations = output.summary.violations.filter(
      (violation) => violation.rule.name === "no-features-from-core",
    );

    expect(violations).toHaveLength(0);
    expect(output.summary.error).toBe(0);
  });

  it("reporta violação quando core importa de features", async () => {
    const currentFixtureDir = mkdtempSync(join(tmpdir(), "di-02-fixture-"));
    fixtureDir = currentFixtureDir;

    const coreDir = join(currentFixtureDir, "core");
    const featuresDir = join(currentFixtureDir, "features");
    mkdirSync(coreDir, { recursive: true });
    mkdirSync(featuresDir, { recursive: true });

    writeFileSync(
      join(featuresDir, "algum-componente.ts"),
      "export const algumValor = 1;\n",
    );
    writeFileSync(
      join(coreDir, "regra-invalida.ts"),
      'import { algumValor } from "../features/algum-componente";\nexport const usoInvalido = algumValor;\n',
    );

    const output = await cruiseWithRule(currentFixtureDir);

    const violations = output.summary.violations.filter(
      (violation) => violation.rule.name === "no-features-from-core",
    );

    expect(violations.length).toBeGreaterThan(0);
    expect(output.summary.error).toBeGreaterThan(0);
  });
});

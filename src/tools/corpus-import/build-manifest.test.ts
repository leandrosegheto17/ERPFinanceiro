import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { describe, expect, it } from "vitest";
import { buildManifest } from "./build-manifest";
import { ManifestSchemaError, validateManifestSchema } from "./validate-manifest-schema";
import type { CorpusManifest } from "./manifest-types";

const __dirname = dirname(fileURLToPath(import.meta.url));
const fixturePath = join(__dirname, "__fixtures__", "porbr2018-sample.txt");
const fixtureContent = readFileSync(fixturePath, "utf8");

function buildValidManifest(): CorpusManifest {
  return buildManifest({
    sourceId: "porbr2018",
    sourceVersionDate: "2018-01-01",
    importedAt: "2026-09-07T12:00:00.000Z",
    license: "CC BY 4.0",
    licenseUrl: "https://creativecommons.org/licenses/by/4.0/",
    holders: ["BLIVRE"],
    artifacts: [{ fileName: "porbr2018-sample.txt", content: fixtureContent }],
  });
}

describe("buildManifest + validateManifestSchema — TASK-008 (critério de aceite: manifest.json validado contra schema)", () => {
  it("gera um manifesto que passa na validação de schema", () => {
    const manifest = buildValidManifest();

    expect(() => validateManifestSchema(manifest)).not.toThrow();
    expect(manifest.sourceId).toBe("porbr2018");
    expect(manifest.modifications).toBe("none");
    expect(manifest.sha256["porbr2018-sample.txt"]).toMatch(/^[0-9a-f]{64}$/);
  });

  it("calcula o SHA-256 real do conteúdo do artefato (determinístico)", () => {
    const manifestA = buildValidManifest();
    const manifestB = buildValidManifest();

    expect(manifestA.sha256["porbr2018-sample.txt"]).toBe(
      manifestB.sha256["porbr2018-sample.txt"],
    );
  });

  it("rejeita manifesto com campo obrigatório faltando", () => {
    const manifest = buildValidManifest();
    const withoutLicense: Record<string, unknown> = { ...manifest };
    delete withoutLicense.license;

    expect(() => validateManifestSchema(withoutLicense)).toThrow(ManifestSchemaError);
    expect(() => validateManifestSchema(withoutLicense)).toThrow(/license/);
  });

  it("rejeita manifesto com hash em formato inválido", () => {
    const manifest = buildValidManifest();
    const corrupted = {
      ...manifest,
      sha256: { ...manifest.sha256, "porbr2018-sample.txt": "not-a-valid-hash" },
    };

    expect(() => validateManifestSchema(corrupted)).toThrow(ManifestSchemaError);
    expect(() => validateManifestSchema(corrupted)).toThrow(/hash SHA-256 válido/);
  });

  it("rejeita manifesto com sourceVersionDate malformada", () => {
    const manifest = buildValidManifest();
    const corrupted = { ...manifest, sourceVersionDate: "not-a-date" };

    expect(() => validateManifestSchema(corrupted)).toThrow(ManifestSchemaError);
    expect(() => validateManifestSchema(corrupted)).toThrow(/sourceVersionDate/);
  });

  it("rejeita manifesto com sha256 vazio", () => {
    const manifest = buildValidManifest();
    const corrupted = { ...manifest, sha256: {} };

    expect(() => validateManifestSchema(corrupted)).toThrow(ManifestSchemaError);
    expect(() => validateManifestSchema(corrupted)).toThrow(/vazio/);
  });

  it("rejeita manifesto com modifications diferente do literal 'none'", () => {
    const manifest = buildValidManifest();
    const corrupted = { ...manifest, modifications: "reformatted" };

    expect(() => validateManifestSchema(corrupted)).toThrow(ManifestSchemaError);
    expect(() => validateManifestSchema(corrupted)).toThrow(/modifications/);
  });
});

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { describe, expect, it } from "vitest";
import { parseSnapshot } from "./parse-snapshot";
import { CorpusIntegrityError, verifyCorpusIntegrity } from "./verify-integrity";

const __dirname = dirname(fileURLToPath(import.meta.url));
const fixturePath = join(__dirname, "__fixtures__", "porbr2018-sample.txt");
const fixtureContent = readFileSync(fixturePath, "utf8");

describe("verifyCorpusIntegrity — TASK-007 (critério de aceite: falha com 1 caractere corrompido)", () => {
  it("não lança erro quando o snapshot está íntegro", () => {
    const corpus = parseSnapshot(fixtureContent);

    expect(() => verifyCorpusIntegrity(fixtureContent, corpus)).not.toThrow();
  });

  it("lança CorpusIntegrityError quando 1 único caractere do snapshot bruto é corrompido", () => {
    const corpus = parseSnapshot(fixtureContent);
    const targetIndex = fixtureContent.indexOf(
      "Texto de exemplo (fixture) do primeiro versiculo de GEN.",
    );
    const corruptedContent =
      fixtureContent.slice(0, targetIndex) +
      "X" +
      fixtureContent.slice(targetIndex + 1);

    expect(corruptedContent).not.toBe(fixtureContent);
    expect(corruptedContent).toHaveLength(fixtureContent.length);
    expect(() => verifyCorpusIntegrity(corruptedContent, corpus)).toThrow(
      CorpusIntegrityError,
    );
    expect(() => verifyCorpusIntegrity(corruptedContent, corpus)).toThrow(
      /Divergência de integridade byte a byte/,
    );
  });

  it("lança CorpusIntegrityError quando o número de linhas significativas diverge", () => {
    const corpus = parseSnapshot(fixtureContent);
    const truncatedContent = fixtureContent.slice(
      0,
      fixtureContent.lastIndexOf("REV\t1\t2"),
    );

    expect(() => verifyCorpusIntegrity(truncatedContent, corpus)).toThrow(
      CorpusIntegrityError,
    );
  });
});

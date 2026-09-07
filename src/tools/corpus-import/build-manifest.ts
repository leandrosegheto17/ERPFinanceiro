/**
 * Constrói o `CorpusManifest` (ADR-003 etapa 5): calcula o SHA-256 de cada
 * artefato gerado pelo pipeline (`node:crypto`, sem dependência nova) e
 * combina com os dados de proveniência informados pelo chamador.
 */

import { createHash } from "node:crypto";
import type { CorpusManifest } from "./manifest-types";

export interface CorpusManifestArtifact {
  readonly fileName: string;
  readonly content: string | Buffer;
}

export interface BuildManifestInput {
  readonly sourceId: string;
  readonly sourceVersionDate: string;
  readonly importedAt: string;
  readonly license: string;
  readonly licenseUrl: string;
  readonly holders: readonly string[];
  readonly artifacts: readonly CorpusManifestArtifact[];
}

function sha256Of(content: string | Buffer): string {
  return createHash("sha256").update(content).digest("hex");
}

export function buildManifest(input: BuildManifestInput): CorpusManifest {
  const sha256: Record<string, string> = {};
  for (const artifact of input.artifacts) {
    sha256[artifact.fileName] = sha256Of(artifact.content);
  }

  return {
    sourceId: input.sourceId,
    sourceVersionDate: input.sourceVersionDate,
    importedAt: input.importedAt,
    license: input.license,
    licenseUrl: input.licenseUrl,
    holders: input.holders,
    modifications: "none",
    sha256,
  };
}

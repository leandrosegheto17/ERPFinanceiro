import { createHash } from "node:crypto";

import { describe, expect, it } from "vitest";

import { CONSENT_TEXT_FULL, CONSENT_TEXT_SHA256, CONSENT_TEXT_VERSION } from "./consent-text";

describe("consent-text (TASK-036)", () => {
  it("CONSENT_TEXT_SHA256 é o hash real de CONSENT_TEXT_FULL (evita drift entre texto e hash gravado)", () => {
    const recalculated = createHash("sha256").update(CONSENT_TEXT_FULL, "utf8").digest("hex");
    expect(CONSENT_TEXT_SHA256).toBe(recalculated);
  });

  it("versão e hash não são strings vazias", () => {
    expect(CONSENT_TEXT_VERSION.length).toBeGreaterThan(0);
    expect(CONSENT_TEXT_SHA256).toMatch(/^[0-9a-f]{64}$/);
  });
});

/**
 * TASK-017 — orçamento de quota (RNF-03, RT-08). `evaluateStorageUsage`
 * cobre o cálculo de "estourou o teto"; o fallback sem Storage API é coberto
 * aqui porque o ambiente de teste (`environment: "node"`, vitest.config.ts)
 * não tem `navigator.storage`.
 */
import { describe, expect, it } from "vitest";

import { estimateStorageUsage, isOverBudget, STORAGE_BUDGET_BYTES } from "./quota";

describe("core/storage/quota (TASK-017)", () => {
  it("devolve uso zero e quota infinita quando a Storage API não está disponível", async () => {
    const estimate = await estimateStorageUsage();

    expect(estimate.usageBytes).toBe(0);
    expect(estimate.quotaBytes).toBe(Number.POSITIVE_INFINITY);
    // Sem informação real de quota, nunca considera estourado (RNF-03: nunca
    // descarta dado do usuário "no escuro").
    expect(isOverBudget(estimate)).toBe(false);
  });

  it("considera estourado quando o uso alcança o teto de 50 MB (RNF-03)", () => {
    expect(isOverBudget({ usageBytes: STORAGE_BUDGET_BYTES, quotaBytes: Number.POSITIVE_INFINITY })).toBe(true);
    expect(isOverBudget({ usageBytes: STORAGE_BUDGET_BYTES - 1, quotaBytes: Number.POSITIVE_INFINITY })).toBe(false);
  });
});

/**
 * Orçamento de armazenamento local (RNF-03, RT-08, TASK-017). Camada
 * `core/*`: TypeScript puro, sem React nem DOM (DI-02) — o único acoplamento
 * de plataforma é a leitura opcional de `navigator.storage.estimate()`, que é
 * a Storage API do navegador citada explicitamente em RT-08 ("Uso de quota
 * reportado por `storage.estimate()`") como sinal de monitoramento.
 */

/** Estimativa de uso/quota de armazenamento, normalizada para bytes. */
export interface StorageUsageEstimate {
  readonly usageBytes: number;
  readonly quotaBytes: number;
}

/** Função injetável de estimativa, para tornar o expurgo testável sem navegador real. */
export type StorageEstimator = () => Promise<StorageUsageEstimate>;

/**
 * Teto de armazenamento local declarado em RNF-03 (PRD-TECNICO.md): conteúdo
 * mantido localmente para operação offline deve caber em ≤ 50 MB por usuário.
 */
export const STORAGE_BUDGET_BYTES = 50 * 1024 * 1024;

/**
 * Estimador padrão, via Storage API do navegador. Ambientes sem suporte (SSR,
 * navegador antigo, execução de teste sem mock) devolvem quota infinita e uso
 * zero — nunca disparam expurgo por falta de informação real: RNF-03/ADR-006
 * proíbem descartar dado do usuário "no escuro", e um falso positivo de
 * estouro de quota teria exatamente esse efeito.
 */
/**
 * Formato mínimo de `navigator.storage`, declarado localmente (em vez de
 * depender do tipo `Navigator` de `lib.dom`) para que este módulo compile
 * igual sob a config de app (com DOM) e a config de teste/Node
 * (`tsconfig.node.json`, sem DOM) — DI-02 (core/* é TypeScript puro).
 */
interface MinimalStorageManager {
  estimate?: () => Promise<{ usage?: number; quota?: number }>;
}

function readNavigatorStorage(): MinimalStorageManager | undefined {
  const globalNavigator = (globalThis as { navigator?: { storage?: MinimalStorageManager } }).navigator;
  return globalNavigator?.storage;
}

export const estimateStorageUsage: StorageEstimator = async () => {
  const storage = readNavigatorStorage();
  if (!storage?.estimate) {
    return { usageBytes: 0, quotaBytes: Number.POSITIVE_INFINITY };
  }

  const { usage, quota } = await storage.estimate();
  return {
    usageBytes: usage ?? 0,
    quotaBytes: quota ?? Number.POSITIVE_INFINITY,
  };
};

/**
 * A quota efetiva é o menor entre o teto do produto (RNF-03, 50 MB) e a quota
 * real do navegador reportada por `storage.estimate()` (RT-08) — o que
 * apertar primeiro manda o expurgo.
 */
export function isOverBudget(
  estimate: StorageUsageEstimate,
  budgetBytes: number = STORAGE_BUDGET_BYTES,
): boolean {
  const effectiveBudget = Math.min(estimate.quotaBytes, budgetBytes);
  return estimate.usageBytes >= effectiveBudget;
}

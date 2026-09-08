/**
 * Expurgo por orçamento de quota (RNF-03, RT-08, TASK-017). Ordem de
 * prioridade declarada em `ADR-006` ("Orçamento e expurgo local"):
 *
 *   1. `corpusChapters` — cache oportunista de capítulos lidos; expurgado por
 *      LRU (`lastAccessedAt` mais antigo primeiro), sem aviso ao usuário —
 *      é puro cache, reconstituível a qualquer momento a partir do corpus
 *      (SDD.md §5.2: "Não sincroniza, é cache de estático").
 *   2. `corpusBundle` — corpus integral do Módulo 2, expurgado só se o
 *      Módulo 2 não for usado há mais de 60 dias (ADR-006). Módulo 2 só
 *      existe na Fase 2 (TASK-070, ADR-012/DI-13) e este módulo não tem,
 *      ainda, como medir "uso do Módulo 2" — por isso esta etapa é um no-op
 *      documentado até lá, nunca removendo `corpusBundle` sem esse sinal
 *      real (mesmo espírito de "nunca descarta em silêncio" de RNF-03).
 *   3. Versões perdedoras de esboço já expiradas — Módulo 2/Fase 2, fora de
 *      escopo: a store nem existe neste schema (`db.ts`, TASK-016).
 *
 * Snapshot de esboço materializado nunca é descartado silenciosamente
 * (ADR-006) — e, por não existir na Fase 1, nunca é sequer candidato aqui.
 *
 * Qualquer outra store deste schema — `contentBundle`, `anonProgress`,
 * `progress`, `preferences`, `outbox`, `eventQueue` — nunca é tocada por este
 * expurgo: é dado do titular ou fila de sincronização pendente, não cache
 * oportunista, e ADR-006/RNF-03 são explícitos que dado do titular nunca é
 * descartado em silêncio.
 */
import type { AppDatabase } from "./db";
import {
  estimateStorageUsage,
  isOverBudget,
  STORAGE_BUDGET_BYTES,
  type StorageEstimator,
} from "./quota";

/** Um registro efetivamente removido pelo expurgo, para teste/observabilidade. */
export interface PurgedCorpusChapter {
  readonly store: "corpusChapters";
  readonly bookId: string;
  readonly chapter: number;
  readonly lastAccessedAt: string;
}

export interface PurgeOptions {
  /** Estimador de uso/quota injetável; padrão é `navigator.storage.estimate()`. */
  readonly estimator?: StorageEstimator;
  /** Teto de armazenamento; padrão é `STORAGE_BUDGET_BYTES` (RNF-03, 50 MB). */
  readonly budgetBytes?: number;
  /** Máximo de registros removidos numa chamada, para não zerar o cache de um só golpe. */
  readonly maxRecordsPerRun?: number;
}

/**
 * Roda o expurgo se, e somente se, a quota estiver estourada (RT-08). Devolve
 * a lista de registros removidos, na ordem em que foram removidos — o
 * critério de aceite de TASK-017 ("teste de quota excedida confirma ordem de
 * expurgo declarada") depende dessa ordem ser estável e verificável.
 */
export async function purgeIfOverBudget(
  db: AppDatabase,
  options: PurgeOptions = {},
): Promise<readonly PurgedCorpusChapter[]> {
  const estimator = options.estimator ?? estimateStorageUsage;
  const budgetBytes = options.budgetBytes ?? STORAGE_BUDGET_BYTES;

  const estimate = await estimator();
  if (!isOverBudget(estimate, budgetBytes)) {
    return [];
  }

  // Prioridade 1 (única acionável em Fase 1, ver cabeçalho do arquivo):
  // `corpusChapters` por LRU. Prioridades 2 e 3 do ADR-006 não têm, ainda,
  // dado disponível nesta camada para serem avaliadas com segurança — por
  // isso não removem nada (nunca descarte "no escuro", RNF-03/ADR-006).
  return purgeCorpusChaptersLru(db, options.maxRecordsPerRun ?? Number.POSITIVE_INFINITY);
}

async function purgeCorpusChaptersLru(
  db: AppDatabase,
  maxRecords: number,
): Promise<readonly PurgedCorpusChapter[]> {
  const oldestFirst = await db.corpusChapters.orderBy("lastAccessedAt").toArray();
  const toPurge = oldestFirst.slice(0, Math.min(maxRecords, oldestFirst.length));

  const purged: PurgedCorpusChapter[] = [];
  for (const record of toPurge) {
    // `delete()` tipa a chave primária pelos nomes das props (`"bookId" |
    // "chapter"`, ver db.ts), não como tupla composta — por isso a exclusão
    // usa o índice composto declarado no schema (`[bookId+chapter]`,
    // `db.ts`) em vez de `delete()` diretamente.
    await db.corpusChapters
      .where("[bookId+chapter]")
      .equals([record.bookId, record.chapter])
      .delete();
    purged.push({
      store: "corpusChapters",
      bookId: record.bookId,
      chapter: record.chapter,
      lastAccessedAt: record.lastAccessedAt,
    });
  }
  return purged;
}

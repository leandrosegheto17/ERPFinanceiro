/**
 * Tipos de registro das stores locais (IndexedDB via Dexie, TASK-016,
 * SDD.md §5.2). Cobre só as stores de **Fase 1** — `outlines`,
 * `outlineSnapshots` e `presentationState` são Módulo 2 (Fase 2, TASK-070) e
 * ficam fora deste módulo até lá (ADR-012, DI-13).
 *
 * DI-10: esta camada é IndexedDB puro. Nenhum destes tipos, nem o `db.ts`
 * que os declara como stores, grava nada em `localStorage`.
 */

/** `corpusChapters` — cache oportunista de capítulos lidos, com expurgo LRU (RT-08, TASK-017). */
export interface CorpusChapterRecord {
  readonly bookId: string;
  readonly chapter: number;
  readonly verses: ReadonlyArray<{ readonly number: number; readonly text: string }>;
  /** Usado pelo expurgo LRU de TASK-017; atualizado a cada leitura do capítulo. */
  readonly lastAccessedAt: string;
}

/** `corpusBundle` — corpus bíblico integral, store singleton (chave fixa `"default"`). */
export interface CorpusBundleRecord {
  readonly id: "default";
  readonly manifestVersion: string;
  readonly books: unknown;
  readonly importedAt: string;
}

/** `contentBundle` — plano + perícopes + notas + ganchos, store singleton (chave fixa `"default"`). */
export interface ContentBundleRecord {
  readonly id: "default";
  readonly bundle: unknown;
  readonly importedAt: string;
}

/** `anonProgress` — dias concluídos sem conta (RN-07); migra uma vez ao criar conta (TASK-022). */
export interface AnonProgressRecord {
  readonly id?: number;
  readonly planId: string;
  readonly dayNumber: number;
  readonly completedAt: string;
}

/** `progress` — dias concluídos do titular; sincroniza por união monotônica (RN-08). */
export interface ProgressRecord {
  readonly id?: number;
  readonly planId: string;
  readonly dayNumber: number;
  readonly completedAt: string;
  readonly syncedAt?: string;
}

/**
 * `preferences` — preferências do usuário (horário de lembrete, tema); sincroniza por LWW.
 *
 * `receivedAt` (TASK-021, RT-06) é opcional e **não é indexado** (sem migração
 * de schema — mesmo padrão aditivo de `PlanDay.atWordCount`/`atWordCount` em
 * TASK-013): quando o registro já passou por um round-trip de servidor, marca
 * o instante em que o servidor recebeu a mutação (relógio confiável,
 * independente do dispositivo que escreveu). Ausente em registro só local
 * (ainda não sincronizado) ou gravado antes desta migração aditiva — nesse
 * caso a conciliação usa só `updatedAt`, como antes de TASK-021.
 */
export interface PreferenceRecord {
  readonly key: string;
  readonly value: unknown;
  readonly updatedAt: string;
  readonly receivedAt?: string;
}

/** `outbox` — mutações pendentes de sincronização, com id próprio e `baseRev` (ADR-007). */
export interface OutboxRecord {
  readonly id: string;
  readonly entityType: string;
  readonly entityId: string;
  readonly operation: string;
  readonly payload: unknown;
  readonly baseRev: number | null;
  readonly createdAt: string;
  readonly status: "pending" | "sent" | "failed";
  /**
   * Contagem de tentativas de envio. Introduzido na migração de schema v1 → v2
   * (`db.ts`); ausente em registros gravados sob v1, mas a própria migração
   * back-preenche `0` em toda mutação já existente ao subir de versão — nunca
   * fica indefinido depois que a migração roda.
   */
  readonly retryCount: number;
}

/** `eventQueue` — eventos de analytics ainda não enviados (inclusive pré-conta), RF-18. */
export interface EventQueueRecord {
  readonly id?: number;
  readonly type: string;
  readonly payload: unknown;
  readonly occurredAt: string;
  readonly sentAt?: string;
}

/**
 * `authSession` — armazenamento chave/valor genérico (string) usado pelo
 * adaptador de storage do cliente Supabase Auth (TASK-038, `features/identity`,
 * DI-10/G-11): o SDK do Supabase, por padrão, persiste a sessão de
 * autenticação (`access_token`/`refresh_token`) em `localStorage`; esta store
 * existe para que essa persistência aconteça em IndexedDB (Dexie) em vez
 * disso — nunca em `localStorage` — mesma camada usada para todo o resto do
 * conteúdo do usuário. `key`/`value` são deliberadamente opacos (o formato
 * interno é definido pelo Supabase Auth, não por este schema) — este módulo
 * (`core/storage`) só provê o meio de persistência, sem conhecer o conteúdo.
 */
export interface AuthSessionRecord {
  readonly key: string;
  readonly value: string;
  readonly updatedAt: string;
}

import type { AppDatabase } from "../storage/db";
import type { PreferenceRecord, ProgressRecord } from "../storage/types";

/**
 * TASK-020 — `core/sync`: conciliação por entidade (ADR-007, SDD.md §5.2/§5.3,
 * diagrama de sequência linhas ~240-250, nó `rules{"Regra da entidade"}`).
 *
 * Implementa, para as duas entidades de Fase 1 que passam pela outbox com
 * regra de conciliação distinta (`progress` e `preferences` — `types.ts`
 * documenta isso por store), exatamente a política que ADR-007 já fixa no
 * nível de schema:
 *
 * - **`progress`**: união monotônica. "Nunca reduzir o número de dias
 *   concluídos" (CA-11.3) é garantia estrutural aqui — o conjunto resultante é
 *   sempre a união dos dias concluídos em cada lado, nunca a interseção nem um
 *   subconjunto de nenhum dos dois. Quando o mesmo dia (`planId` + `dayNumber`)
 *   aparece nos dois lados, o `completed_at` **mais antigo** vence (ADR-007,
 *   tabela "Progresso do plano": "preservando o `completed_at` mais antigo") —
 *   a data em que o usuário *de fato* concluiu o dia pela primeira vez, não a
 *   data de chegada mais recente ao servidor/outro dispositivo.
 * - **`preferences`**: Last-Write-Wins por `updatedAt` (relógio do
 *   dispositivo, ADR-007: "escrita aplicada só se `updated_at` recebido >
 *   `updated_at` armazenado"). O lado perdedor é descartado — ao contrário do
 *   esboço (Módulo 2 / TASK-071, fora de escopo aqui), preferência não tem
 *   versão recuperável.
 *
 * Esboço (`outlines`) fica fora deste módulo: é conciliação de Fase 2
 * (TASK-071, ADR-007 "Esboço" — LWW no nível do esboço com versão perdedora
 * preservada), e `presentationState` nunca sai do dispositivo (RN-14) —
 * nenhum dos dois passa pela outbox de Fase 1.
 *
 * TASK-021 (RT-06) estende `reconcilePreferences` para não deixar o LWW de
 * `preferences` ser "sequestrado" por um dispositivo com relógio local
 * incorreto — ver comentário da função para o algoritmo.
 */

/** Chave de identidade de um dia de progresso: mesmo dia do mesmo plano. */
function progressKey(record: ProgressRecord): string {
  return `${record.planId}::${record.dayNumber}`;
}

/** Remove o `id` (autoincremento local, nunca significativo entre réplicas) de um `ProgressRecord`. */
function withoutId(record: ProgressRecord): Omit<ProgressRecord, "id"> {
  const clone: Record<string, unknown> = { ...record };
  delete clone.id;
  return clone as Omit<ProgressRecord, "id">;
}

/**
 * Concilia dois conjuntos de `ProgressRecord` (ex.: dois dispositivos) por
 * união monotônica: todo dia concluído em qualquer um dos lados está no
 * resultado; nenhum dia é removido. Quando o mesmo dia existe nos dois lados,
 * mantém o registro com o `completedAt` mais antigo (ADR-007).
 *
 * O `id` (autoincremento local do Dexie, `types.ts`) nunca é significativo
 * entre dispositivos — cada réplica tem sua própria sequência de ids. O
 * resultado por isso nunca carrega `id`: quem grava de volta na store local
 * (`applyReconciledProgress`) deixa o Dexie atribuir um novo.
 */
export function reconcileProgress(
  left: readonly ProgressRecord[],
  right: readonly ProgressRecord[],
): Array<Omit<ProgressRecord, "id">> {
  const merged = new Map<string, Omit<ProgressRecord, "id">>();

  for (const record of [...left, ...right]) {
    const key = progressKey(record);
    const existing = merged.get(key);
    const candidate = withoutId(record);
    if (!existing) {
      merged.set(key, candidate);
      continue;
    }
    // Mesmo dia nos dois lados: o `completed_at` mais antigo vence — é a
    // primeira vez, real, em que o usuário concluiu o dia (ADR-007).
    if (candidate.completedAt < existing.completedAt) {
      merged.set(key, candidate);
    }
  }

  return [...merged.values()];
}

/**
 * Timestamp efetivo de um `PreferenceRecord` para fins de comparação de LWW
 * (TASK-021, RT-06: "relógio do dispositivo incorreto afeta LWW de
 * preferências"; mitigação de SDD.md: "servidor grava `received_at` além do
 * `updated_at`; apuração descarta divergência absurda; nunca corrige dado do
 * usuário em silêncio", ADR-007).
 *
 * `updatedAt` é o relógio do **dispositivo** — não confiável (aparelho com
 * data errada, RT-06). `receivedAt`, quando presente, é o instante em que o
 * servidor recebeu a mutação — relógio confiável, independente de qual
 * dispositivo escreveu. Um `updatedAt` no futuro em relação ao `receivedAt`
 * do mesmo registro é fisicamente implausível (a mutação não pode ter sido
 * "atualizada" depois de o servidor já tê-la recebido) e é sinal direto de
 * relógio do dispositivo adiantado.
 *
 * A mitigação escolhida é **clampar apenas a comparação**, nunca o dado
 * armazenado: quando `updatedAt > receivedAt`, o timestamp efetivo usado
 * pelo LWW é o `receivedAt` (o menor dos dois, sempre o mais conservador).
 * Isso garante que um dispositivo com relógio adiantado em 1 ano não
 * "sequestre" a conciliação só por causa do erro de relógio — o dano fica
 * limitado à janela real entre a escrita e o instante em que o servidor de
 * fato a recebeu, não a um ano inteiro no futuro. O `updatedAt` gravado no
 * registro em si nunca é reescrito por este clamp (ADR-007: "nunca corrige
 * dado do usuário em silêncio") — é só o critério de desempate que muda.
 *
 * Registro sem `receivedAt` (ainda não passou por round-trip de servidor,
 * ou foi gravado antes desta migração aditiva de TASK-021) usa `updatedAt`
 * puro, exatamente como antes de TASK-021 — comportamento de TASK-020
 * preservado por padrão.
 */
function effectivePreferenceTimestamp(record: PreferenceRecord): string {
  if (record.receivedAt === undefined) {
    return record.updatedAt;
  }
  return record.updatedAt > record.receivedAt ? record.receivedAt : record.updatedAt;
}

/**
 * Concilia dois conjuntos de `PreferenceRecord` por Last-Write-Wins: para
 * cada `key` de preferência, vence o registro com o timestamp efetivo mais
 * recente (`effectivePreferenceTimestamp` — `updatedAt`, clampado contra
 * `receivedAt` quando presente, TASK-021/RT-06). O lado perdedor é
 * descartado (sem versão recuperável, diferente do esboço).
 *
 * Empate de timestamp efetivo (relógio dos dois dispositivos idêntico até o
 * milissegundo, ou os dois clampados para o mesmo `receivedAt`) resolve
 * mantendo o valor do lado `right` — escolha arbitrária mas determinística;
 * não há sinal adicional (como `rev`, no caso do esboço) para desempatar
 * preferência sob ADR-007.
 */
export function reconcilePreferences(
  left: readonly PreferenceRecord[],
  right: readonly PreferenceRecord[],
): PreferenceRecord[] {
  const merged = new Map<string, PreferenceRecord>();

  for (const record of left) {
    merged.set(record.key, record);
  }
  for (const record of right) {
    const existing = merged.get(record.key);
    if (!existing || effectivePreferenceTimestamp(record) >= effectivePreferenceTimestamp(existing)) {
      merged.set(record.key, record);
    }
  }

  return [...merged.values()];
}

/** Estado conciliado de `progress` e `preferences`, pronto para aplicar em cada réplica. */
export interface ReconciledState {
  readonly progress: ReadonlyArray<Omit<ProgressRecord, "id">>;
  readonly preferences: readonly PreferenceRecord[];
}

/**
 * Grava o estado conciliado numa réplica local (`AppDatabase`), substituindo
 * por completo o conteúdo das stores `progress`/`preferences` pelo resultado
 * da conciliação. Usado nos dois lados de uma sincronização simulada
 * (`reconcileDevices`) para que ambos os dispositivos convirjam para o mesmo
 * estado final.
 */
export async function applyReconciledState(db: AppDatabase, state: ReconciledState): Promise<void> {
  await db.progress.clear();
  if (state.progress.length > 0) {
    await db.progress.bulkAdd([...state.progress]);
  }

  await db.preferences.clear();
  if (state.preferences.length > 0) {
    await db.preferences.bulkPut([...state.preferences]);
  }
}

/**
 * Concilia dois dispositivos (`AppDatabase` já abertos) de ponta a ponta:
 * lê `progress`/`preferences` de cada um, aplica a regra por entidade
 * (`reconcileProgress`/`reconcilePreferences`) e grava o mesmo resultado
 * conciliado de volta nos dois — simulando a etapa "Regra da entidade" do
 * diagrama de sequência de SDD.md §2.6 acontecendo, na prática, entre dois
 * dispositivos que sincronizaram com o servidor e agora reconciliam.
 *
 * Devolve o estado conciliado para inspeção direta em teste.
 */
export async function reconcileDevices(deviceA: AppDatabase, deviceB: AppDatabase): Promise<ReconciledState> {
  const [progressA, progressB, preferencesA, preferencesB] = await Promise.all([
    deviceA.progress.toArray(),
    deviceB.progress.toArray(),
    deviceA.preferences.toArray(),
    deviceB.preferences.toArray(),
  ]);

  const state: ReconciledState = {
    progress: reconcileProgress(progressA, progressB),
    preferences: reconcilePreferences(preferencesA, preferencesB),
  };

  await Promise.all([applyReconciledState(deviceA, state), applyReconciledState(deviceB, state)]);

  return state;
}

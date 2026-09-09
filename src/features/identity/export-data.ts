import type { AppDatabase } from "../../core/storage";

/**
 * TASK-039 (Lote 6 — Identidade), Tela T-13 "Conta e dados" (UX-SPEC T-13,
 * CA-10.4 — portabilidade de dados / LGPD): coleta os dados locais do
 * titular logado (`core/storage`, Dexie) e monta o objeto exportável em
 * JSON. Esta é só a parte de **exportação** de T-13 — o fluxo de exclusão
 * ("EXCLUIR", `erasure_request`) é TASK-040, implementada em paralelo por
 * outra instância do Executor sobre o mesmo `UX-SPEC.md`; nenhum código de
 * exclusão é tocado aqui.
 *
 * Escopo de dados: `progress` (dias concluídos) e `preferences` (horário de
 * lembrete, tema) já existem em `core/storage` (Lote 3, TASK-016). `esbocos`
 * (Módulo 2, `outlines`/`outlineSnapshots`) **não existe** no schema local
 * ainda — é Fase 2 (Lote 14, TASK-070) — então este módulo exporta sempre um
 * array vazio para o campo, junto com uma nota textual (`notas.esbocos`)
 * explicando o motivo dentro do próprio arquivo, para que o titular que abrir
 * o JSON entenda por que o campo está vazio em vez de supor perda de dado.
 * Quando `core/storage` ganhar as stores de esboço, esta função passa a
 * populá-lo sem quebrar o formato do arquivo (o campo `esbocos` já existe
 * hoje, só troca de sempre-vazio para populado).
 */

export interface DataExportProgressEntry {
  readonly planId: string;
  readonly dayNumber: number;
  readonly completedAt: string;
  readonly syncedAt?: string;
}

export interface DataExportPreferenceEntry {
  readonly key: string;
  readonly value: unknown;
  readonly updatedAt: string;
}

export interface DataExportPayload {
  /** Versão do formato do arquivo exportado — permite evoluir sem quebrar leitores antigos. */
  readonly schemaVersion: 1;
  /** Instante (ISO 8601) em que a exportação foi gerada. */
  readonly exportadoEm: string;
  readonly progresso: readonly DataExportProgressEntry[];
  readonly preferencias: readonly DataExportPreferenceEntry[];
  /**
   * Sempre `[]` nesta fase — ver nota de escopo no cabeçalho do arquivo e em
   * `notas.esbocos`. Tipado como `readonly never[]` deliberadamente: força um
   * erro de compilação neste arquivo (não silencioso) no dia em que alguém
   * tentar popular o campo sem primeiro atualizar o tipo, lembrando de também
   * revisar `notas.esbocos`.
   */
  readonly esbocos: readonly never[];
  readonly notas: {
    readonly esbocos: string;
  };
}

const NOTA_ESBOCOS =
  "Esboços ainda não são armazenados neste aparelho nesta fase do produto " +
  "(Módulo 2 / Fase 2, Lote 14 — stores `outlines`/`outlineSnapshots` de " +
  "core/storage ainda não existem). Este campo virá preenchido assim que " +
  "essa fase for implementada; um array vazio aqui não significa perda de " +
  "esboço nenhum.";

/**
 * Lê `progress`/`preferences` de `core/storage` (Dexie) e monta o payload
 * exportável. Não lê nem escreve nada relacionado a sessão/autenticação —
 * o `db` já é escopado ao titular logado neste aparelho (a mesma premissa já
 * usada pelo resto de `core/storage`, ver TASK-016/038).
 */
export async function collectDataExportPayload(db: AppDatabase): Promise<DataExportPayload> {
  const [progressRecords, preferenceRecords] = await Promise.all([
    db.progress.toArray(),
    db.preferences.toArray(),
  ]);

  return {
    schemaVersion: 1,
    exportadoEm: new Date().toISOString(),
    progresso: progressRecords.map((record) => ({
      planId: record.planId,
      dayNumber: record.dayNumber,
      completedAt: record.completedAt,
      ...(record.syncedAt ? { syncedAt: record.syncedAt } : {}),
    })),
    preferencias: preferenceRecords.map((record) => ({
      key: record.key,
      value: record.value,
      updatedAt: record.updatedAt,
    })),
    esbocos: [],
    notas: {
      esbocos: NOTA_ESBOCOS,
    },
  };
}

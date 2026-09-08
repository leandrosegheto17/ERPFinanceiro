/**
 * TASK-020 — `core/sync`: conciliação por entidade (união monotônica + LWW).
 *
 * Critério de aceite: "Dois dispositivos simulados convergem conforme a
 * regra por entidade". Cada dispositivo é um `AppDatabase`/Dexie próprio
 * (nome de banco distinto, via `fake-indexeddb/auto`), que evolui offline de
 * forma independente antes de "sincronizar" — aqui, `reconcileDevices`
 * aplicando a regra certa por entidade e regravando o resultado nos dois.
 */
import "fake-indexeddb/auto";

import { afterEach, describe, expect, it } from "vitest";

import { AppDatabase } from "../storage/db";
import type { PreferenceRecord, ProgressRecord } from "../storage/types";
import {
  applyReconciledState,
  reconcileDevices,
  reconcilePreferences,
  reconcileProgress,
} from "./reconcile-entity";

describe("reconcile-entity — conciliação por entidade (TASK-020)", () => {
  const openDatabases: AppDatabase[] = [];

  function open(name: string): AppDatabase {
    const db = new AppDatabase(name);
    openDatabases.push(db);
    return db;
  }

  afterEach(async () => {
    while (openDatabases.length > 0) {
      const db = openDatabases.pop();
      db?.close();
      if (db) {
        await db.delete();
      }
    }
  });

  function stripId(
    records: ReadonlyArray<ProgressRecord | Omit<ProgressRecord, "id">>,
  ): Array<Omit<ProgressRecord, "id">> {
    return records
      .map((record) => {
        const clone: Record<string, unknown> = { ...record };
        delete clone.id;
        return clone as Omit<ProgressRecord, "id">;
      })
      .sort((a, b) => a.planId.localeCompare(b.planId) || a.dayNumber - b.dayNumber);
  }

  function sortPreferences(records: readonly PreferenceRecord[]): PreferenceRecord[] {
    return [...records].sort((a, b) => a.key.localeCompare(b.key));
  }

  describe("progress — união monotônica (RN-08, CA-11.3)", () => {
    it("dias diferentes concluídos em cada dispositivo: a união preserva ambos, nenhum é removido", () => {
      const left: ProgressRecord[] = [{ planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" }];
      const right: ProgressRecord[] = [{ planId: "plan-1", dayNumber: 2, completedAt: "2026-09-02T08:00:00.000Z" }];

      const merged = reconcileProgress(left, right);

      expect(stripId(merged)).toEqual([
        { planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" },
        { planId: "plan-1", dayNumber: 2, completedAt: "2026-09-02T08:00:00.000Z" },
      ]);
    });

    it("nunca reduz progresso: dia presente só à esquerda continua no resultado mesmo com a direita vazia", () => {
      const left: ProgressRecord[] = [{ planId: "plan-1", dayNumber: 5, completedAt: "2026-09-01T08:00:00.000Z" }];

      const merged = reconcileProgress(left, []);

      expect(merged).toEqual([{ planId: "plan-1", dayNumber: 5, completedAt: "2026-09-01T08:00:00.000Z" }]);
    });

    it("mesmo dia concluído nos dois lados: preserva o completedAt mais antigo (ADR-007)", () => {
      const left: ProgressRecord[] = [{ planId: "plan-1", dayNumber: 3, completedAt: "2026-09-05T10:00:00.000Z" }];
      const right: ProgressRecord[] = [{ planId: "plan-1", dayNumber: 3, completedAt: "2026-09-01T09:00:00.000Z" }];

      const merged = reconcileProgress(left, right);

      expect(merged).toEqual([{ planId: "plan-1", dayNumber: 3, completedAt: "2026-09-01T09:00:00.000Z" }]);
    });
  });

  describe("preferences — Last-Write-Wins por updatedAt", () => {
    it("mesma preferência alterada nos dois dispositivos em momentos diferentes: o valor mais recente vence", () => {
      const left: PreferenceRecord[] = [{ key: "theme", value: "light", updatedAt: "2026-09-01T08:00:00.000Z" }];
      const right: PreferenceRecord[] = [{ key: "theme", value: "dark", updatedAt: "2026-09-02T08:00:00.000Z" }];

      const mergedLeftRight = reconcilePreferences(left, right);
      const mergedRightLeft = reconcilePreferences(right, left);

      // Comutativo: não importa qual lado é passado primeiro, o mais recente vence.
      expect(mergedLeftRight).toEqual([{ key: "theme", value: "dark", updatedAt: "2026-09-02T08:00:00.000Z" }]);
      expect(mergedRightLeft).toEqual([{ key: "theme", value: "dark", updatedAt: "2026-09-02T08:00:00.000Z" }]);
    });

    it("preferências distintas em cada lado: ambas sobrevivem (não é união monotônica, mas também não há conflito de chave)", () => {
      const left: PreferenceRecord[] = [{ key: "theme", value: "light", updatedAt: "2026-09-01T08:00:00.000Z" }];
      const right: PreferenceRecord[] = [
        { key: "reminderTime", value: "07:00", updatedAt: "2026-09-01T08:00:00.000Z" },
      ];

      const merged = sortPreferences(reconcilePreferences(left, right));

      expect(merged).toEqual([
        { key: "reminderTime", value: "07:00", updatedAt: "2026-09-01T08:00:00.000Z" },
        { key: "theme", value: "light", updatedAt: "2026-09-01T08:00:00.000Z" },
      ]);
    });
  });

  describe("preferences — RT-06, relógio incorreto (TASK-021)", () => {
    it("relógio adiantado 1 ano não corrompe LWW além do aceitável: receivedAt limita o dano (critério de aceite)", () => {
      // Dispositivo A tem o relógio adiantado em 1 ano: grava `theme=light`
      // com `updatedAt` no "futuro", mas o servidor de fato recebeu a
      // mutação em 2026-09-01T08:00 (receivedAt, relógio confiável).
      const left: PreferenceRecord[] = [
        {
          key: "theme",
          value: "light",
          updatedAt: "2027-09-01T08:00:00.000Z",
          receivedAt: "2026-09-01T08:00:00.000Z",
        },
      ];
      // Dispositivo B tem o relógio correto e grava `theme=dark` de fato
      // mais tarde na realidade (09:00 do mesmo dia, uma hora depois de A).
      const right: PreferenceRecord[] = [
        {
          key: "theme",
          value: "dark",
          updatedAt: "2026-09-01T09:00:00.000Z",
          receivedAt: "2026-09-01T09:00:00.000Z",
        },
      ];

      const mergedLeftRight = reconcilePreferences(left, right);
      const mergedRightLeft = reconcilePreferences(right, left);

      // Sem a mitigação, a comparação ingênua por `updatedAt` faria o
      // dispositivo A (2027) vencer e o valor ficaria "preso" no futuro por
      // quase um ano inteiro, até o relógio real alcançar 2027 — a
      // corrupção que RT-06 descreve. Com `receivedAt` limitando o dano, o
      // timestamp efetivo de A é clampado para 08:00 (2026), que é anterior
      // ao de B (09:00, sem clamp) — o valor real e mais recente (B) vence.
      expect(mergedLeftRight).toEqual([right[0]]);
      expect(mergedRightLeft).toEqual([right[0]]);
      // O `updatedAt` do lado perdedor nunca é reescrito em silêncio — o
      // clamp afeta só o critério de desempate, não o dado do usuário
      // (ADR-007). Como o registro perde a conciliação, ele nem aparece no
      // resultado, mas se A tivesse vencido em outro cenário, seu
      // `updatedAt` armazenado continuaria "2027-09-01..." intacto.
    });

    it("registro sem receivedAt usa updatedAt puro (comportamento de TASK-020 preservado por padrão)", () => {
      const left: PreferenceRecord[] = [{ key: "theme", value: "light", updatedAt: "2027-09-01T08:00:00.000Z" }];
      const right: PreferenceRecord[] = [{ key: "theme", value: "dark", updatedAt: "2026-09-01T09:00:00.000Z" }];

      // Sem `receivedAt` em nenhum dos dois lados, não há como detectar
      // relógio implausível: o `updatedAt` cru decide, como antes de
      // TASK-021 — A (2027, sem receivedAt para clampar) vence.
      const merged = reconcilePreferences(left, right);

      expect(merged).toEqual([{ key: "theme", value: "light", updatedAt: "2027-09-01T08:00:00.000Z" }]);
    });

    it("receivedAt igual ou anterior ao updatedAt (relógio plausível) não altera o resultado do LWW", () => {
      const left: PreferenceRecord[] = [
        {
          key: "theme",
          value: "light",
          updatedAt: "2026-09-01T08:00:00.000Z",
          receivedAt: "2026-09-01T08:00:01.000Z",
        },
      ];
      const right: PreferenceRecord[] = [
        {
          key: "theme",
          value: "dark",
          updatedAt: "2026-09-02T08:00:00.000Z",
          receivedAt: "2026-09-02T08:00:01.000Z",
        },
      ];

      const merged = reconcilePreferences(left, right);

      expect(merged).toEqual([right[0]]);
    });
  });

  describe("convergência de dois dispositivos simulados (critério de aceite)", () => {
    it("progress: dispositivos que concluem dias diferentes offline convergem para a união de ambos", async () => {
      const deviceA = open(`estudobiblico-reconcile-progress-a-${crypto.randomUUID()}`);
      const deviceB = open(`estudobiblico-reconcile-progress-b-${crypto.randomUUID()}`);
      await deviceA.open();
      await deviceB.open();

      // Cada dispositivo progride offline, de forma independente.
      await deviceA.progress.add({ planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" });
      await deviceA.progress.add({ planId: "plan-1", dayNumber: 2, completedAt: "2026-09-02T08:00:00.000Z" });
      await deviceB.progress.add({ planId: "plan-1", dayNumber: 3, completedAt: "2026-09-03T08:00:00.000Z" });

      await reconcileDevices(deviceA, deviceB);

      const stateA = stripId(await deviceA.progress.toArray());
      const stateB = stripId(await deviceB.progress.toArray());

      const expected = [
        { planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" },
        { planId: "plan-1", dayNumber: 2, completedAt: "2026-09-02T08:00:00.000Z" },
        { planId: "plan-1", dayNumber: 3, completedAt: "2026-09-03T08:00:00.000Z" },
      ];

      // Os dois dispositivos convergem para o mesmo estado final...
      expect(stateA).toEqual(expected);
      expect(stateB).toEqual(expected);
      // ...e nenhum dia concluído em qualquer um dos lados foi removido.
      expect(stateA).toHaveLength(3);
    });

    it("preferences: dispositivos que mudam a mesma preferência em momentos diferentes convergem para a escrita mais recente (LWW)", async () => {
      const deviceA = open(`estudobiblico-reconcile-preferences-a-${crypto.randomUUID()}`);
      const deviceB = open(`estudobiblico-reconcile-preferences-b-${crypto.randomUUID()}`);
      await deviceA.open();
      await deviceB.open();

      // Dispositivo A muda o tema primeiro, offline...
      await deviceA.preferences.put({ key: "theme", value: "light", updatedAt: "2026-09-01T08:00:00.000Z" });
      // ...dispositivo B muda a mesma preferência depois, também offline.
      await deviceB.preferences.put({ key: "theme", value: "dark", updatedAt: "2026-09-02T08:00:00.000Z" });

      await reconcileDevices(deviceA, deviceB);

      const stateA = await deviceA.preferences.toArray();
      const stateB = await deviceB.preferences.toArray();

      const expected = [{ key: "theme", value: "dark", updatedAt: "2026-09-02T08:00:00.000Z" }];

      // Os dois convergem para o mesmo valor: o mais recente (dispositivo B) vence.
      expect(stateA).toEqual(expected);
      expect(stateB).toEqual(expected);
    });

    it("preferences: relógio de um dispositivo adiantado 1 ano não corrompe a convergência ponta a ponta (RT-06, TASK-021)", async () => {
      const deviceA = open(`estudobiblico-reconcile-rt06-a-${crypto.randomUUID()}`);
      const deviceB = open(`estudobiblico-reconcile-rt06-b-${crypto.randomUUID()}`);
      await deviceA.open();
      await deviceB.open();

      // Dispositivo A: relógio adiantado 1 ano. O servidor recebeu a
      // mutação de fato em 2026-09-01T08:00 (receivedAt).
      await deviceA.preferences.put({
        key: "theme",
        value: "light",
        updatedAt: "2027-09-01T08:00:00.000Z",
        receivedAt: "2026-09-01T08:00:00.000Z",
      });
      // Dispositivo B: relógio correto, muda a mesma preferência de fato
      // depois (uma hora mais tarde, na realidade).
      await deviceB.preferences.put({
        key: "theme",
        value: "dark",
        updatedAt: "2026-09-01T09:00:00.000Z",
        receivedAt: "2026-09-01T09:00:00.000Z",
      });

      await reconcileDevices(deviceA, deviceB);

      const stateA = await deviceA.preferences.toArray();
      const stateB = await deviceB.preferences.toArray();

      const expected = [
        {
          key: "theme",
          value: "dark",
          updatedAt: "2026-09-01T09:00:00.000Z",
          receivedAt: "2026-09-01T09:00:00.000Z",
        },
      ];

      // Os dois dispositivos convergem para o valor real mais recente (B) —
      // não para o valor de A, que só "parecia" mais recente por causa do
      // relógio local incorreto. O dano fica limitado à janela real entre
      // as duas escritas (1 hora), não a um ano inteiro no futuro.
      expect(stateA).toEqual(expected);
      expect(stateB).toEqual(expected);
    });

    it("progress e preferences juntos, cada um com sua regra, convergem no mesmo passo de conciliação", async () => {
      const deviceA = open(`estudobiblico-reconcile-mixed-a-${crypto.randomUUID()}`);
      const deviceB = open(`estudobiblico-reconcile-mixed-b-${crypto.randomUUID()}`);
      await deviceA.open();
      await deviceB.open();

      await deviceA.progress.add({ planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" });
      await deviceB.progress.add({ planId: "plan-1", dayNumber: 2, completedAt: "2026-09-02T08:00:00.000Z" });
      await deviceA.preferences.put({
        key: "reminderTime",
        value: "07:00",
        updatedAt: "2026-09-03T08:00:00.000Z",
      });
      await deviceB.preferences.put({
        key: "reminderTime",
        value: "19:00",
        updatedAt: "2026-09-01T08:00:00.000Z",
      });

      const result = await reconcileDevices(deviceA, deviceB);

      expect(stripId(result.progress)).toEqual([
        { planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" },
        { planId: "plan-1", dayNumber: 2, completedAt: "2026-09-02T08:00:00.000Z" },
      ]);
      // A alteração mais antiga (dispositivo B, 09-01) perde para a mais
      // recente (dispositivo A, 09-03) — LWW, não união.
      expect(result.preferences).toEqual([
        { key: "reminderTime", value: "07:00", updatedAt: "2026-09-03T08:00:00.000Z" },
      ]);

      const progressA = stripId(await deviceA.progress.toArray());
      const progressB = stripId(await deviceB.progress.toArray());
      expect(progressA).toEqual(progressB);

      const preferencesA = await deviceA.preferences.toArray();
      const preferencesB = await deviceB.preferences.toArray();
      expect(preferencesA).toEqual(preferencesB);
    });
  });

  describe("applyReconciledState", () => {
    it("substitui por completo o conteúdo de progress/preferences pelo estado conciliado", async () => {
      const db = open(`estudobiblico-reconcile-apply-${crypto.randomUUID()}`);
      await db.open();

      await db.progress.add({ planId: "plan-1", dayNumber: 9, completedAt: "2026-01-01T00:00:00.000Z" });
      await db.preferences.put({ key: "stale", value: "x", updatedAt: "2026-01-01T00:00:00.000Z" });

      await applyReconciledState(db, {
        progress: [{ planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" }],
        preferences: [{ key: "theme", value: "dark", updatedAt: "2026-09-01T08:00:00.000Z" }],
      });

      expect(stripId(await db.progress.toArray())).toEqual([
        { planId: "plan-1", dayNumber: 1, completedAt: "2026-09-01T08:00:00.000Z" },
      ]);
      expect(await db.preferences.toArray()).toEqual([
        { key: "theme", value: "dark", updatedAt: "2026-09-01T08:00:00.000Z" },
      ]);
    });
  });
});

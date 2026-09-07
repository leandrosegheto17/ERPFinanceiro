/**
 * Loader do bundle editorial (TASK-015): dado um `ContentBundle` já pronto/
 * validado (produzido por `tools/content-validate`, TASK-013/014, e consumido
 * aqui como dado estático de runtime do navegador — `core/content` não importa
 * de `tools/*`, regra `no-tools-from-core`/DI-02), resolve `PlanDay` por
 * `dayNumber`. Mesmo padrão de `core/corpus/get-chapter.ts` (TASK-009):
 * índice `Map` para lookup O(1) em vez de O(n) por acesso, `undefined` para
 * "não encontrado" (não exceção — ausência de dia/perícope/nota é resultado
 * normal de navegação, não violação de invariante), barrel de exports.
 *
 * Também expõe lookup de `Pericope`/`Note` por id, já que `PlanDay.noteIds`
 * referencia notas por id e cada nota referencia uma perícope por id — sem
 * isso, quem consome `PlanDay` (TASK-044, TASK-048) teria que reconstruir o
 * mesmo índice por conta própria.
 */

import type { ContentBundle, Note, Pericope, PlanDay } from "./types";

/** Índices por id/número do conteúdo editorial, para lookup O(1) em vez de O(n) por acesso. */
export interface ContentIndex {
  readonly days: ReadonlyMap<number, PlanDay>;
  readonly pericopes: ReadonlyMap<string, Pericope>;
  readonly notes: ReadonlyMap<string, Note>;
}

export function buildContentIndex(bundle: ContentBundle): ContentIndex {
  const days = new Map<number, PlanDay>();
  for (const day of bundle.days) {
    days.set(day.dayNumber, day);
  }

  const pericopes = new Map<string, Pericope>();
  for (const pericope of bundle.pericopes) {
    pericopes.set(pericope.id, pericope);
  }

  const notes = new Map<string, Note>();
  for (const note of bundle.notes) {
    notes.set(note.id, note);
  }

  return { days, pericopes, notes };
}

export function getPlanDay(bundle: ContentBundle, dayNumber: number): PlanDay | undefined {
  const index = buildContentIndex(bundle);
  return getPlanDayFromIndex(index, dayNumber);
}

export function getPlanDayFromIndex(
  index: ContentIndex,
  dayNumber: number,
): PlanDay | undefined {
  return index.days.get(dayNumber);
}

export function getPericopeFromIndex(
  index: ContentIndex,
  pericopeId: string,
): Pericope | undefined {
  return index.pericopes.get(pericopeId);
}

export function getNoteFromIndex(index: ContentIndex, noteId: string): Note | undefined {
  return index.notes.get(noteId);
}

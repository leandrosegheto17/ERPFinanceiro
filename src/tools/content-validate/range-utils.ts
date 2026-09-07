/**
 * Utilitários de comparação de `ScriptureRange` (TASK-013), compartilhados
 * pelas regras C2/C6/C7/C8/C9 de RN-01. Puros, sem dependência de corpus real
 * — operam só sobre as coordenadas (livro/capítulo/versículo) já presentes no
 * `ContentBundle`.
 */

import type { ScriptureRange } from "../../core/content";

export interface ChapterVerse {
  readonly chapter: number;
  readonly verse: number;
}

/** -1 se `a` vem antes de `b`, 0 se igual, 1 se `a` vem depois de `b`. */
export function compareChapterVerse(a: ChapterVerse, b: ChapterVerse): number {
  if (a.chapter !== b.chapter) {
    return a.chapter < b.chapter ? -1 : 1;
  }
  if (a.verse !== b.verse) {
    return a.verse < b.verse ? -1 : 1;
  }
  return 0;
}

function start(range: ScriptureRange): ChapterVerse {
  return { chapter: range.startChapter, verse: range.startVerse };
}

function end(range: ScriptureRange): ChapterVerse {
  return { chapter: range.endChapter, verse: range.endVerse };
}

/** `outer` contém `inner` por completo: mesmo livro, início ≤ início, fim ≥ fim. */
export function rangeContains(outer: ScriptureRange, inner: ScriptureRange): boolean {
  if (outer.bookId !== inner.bookId) {
    return false;
  }
  return (
    compareChapterVerse(start(outer), start(inner)) <= 0 &&
    compareChapterVerse(end(outer), end(inner)) >= 0
  );
}

/** `a` e `b` se sobrepõem em algum ponto (mesmo livro, intervalos não disjuntos). */
export function rangesOverlap(a: ScriptureRange, b: ScriptureRange): boolean {
  if (a.bookId !== b.bookId) {
    return false;
  }
  const aBeforeB = compareChapterVerse(end(a), start(b)) < 0;
  const bBeforeA = compareChapterVerse(end(b), start(a)) < 0;
  return !aBeforeB && !bBeforeA;
}

/**
 * `nextStart` é imediatamente posterior a `prevEnd`: mesmo capítulo com
 * versículo seguinte, ou o próximo capítulo começando no versículo 1. Não
 * exige conhecer o último versículo real do capítulo anterior — condição
 * suficiente para as porções terminarem em fronteira de capítulo (C6).
 */
export function isImmediatelyAfter(prevEnd: ChapterVerse, nextStart: ChapterVerse): boolean {
  if (prevEnd.chapter === nextStart.chapter) {
    return nextStart.verse === prevEnd.verse + 1;
  }
  return nextStart.chapter === prevEnd.chapter + 1 && nextStart.verse === 1;
}

export function rangeStart(range: ScriptureRange): ChapterVerse {
  return start(range);
}

export function rangeEnd(range: ScriptureRange): ChapterVerse {
  return end(range);
}

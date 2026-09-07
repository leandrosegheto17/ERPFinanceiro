/**
 * Acesso tipado ao corpus local (TASK-009): dado um bundle já em memória e
 * uma referência estruturada, localiza o capítulo correspondente.
 * `undefined` para "não encontrado" (não exceção): alinhado ao restante do
 * projeto, que não usa exceções para representar ausência esperada de dado
 * (só para violação de invariante/formato, ex. `CorpusParseError`,
 * `CorpusIntegrityError`, `ManifestSchemaError` em `tools/corpus-import`) —
 * "referência não existe no bundle" é um resultado normal de navegação, não
 * um erro de programa.
 */

import type { Book, ChapterReference, Chapter, CorpusBundle } from "./types";

/** Índice de livros por `bookId`, para lookup O(1) em vez de O(n) por acesso. */
export type CorpusIndex = ReadonlyMap<string, Book>;

export function buildCorpusIndex(bundle: CorpusBundle): CorpusIndex {
  const index = new Map<string, Book>();
  for (const book of bundle) {
    index.set(book.bookId, book);
  }
  return index;
}

export function getChapter(
  bundle: CorpusBundle,
  reference: ChapterReference,
): Chapter | undefined {
  const index = buildCorpusIndex(bundle);
  return getChapterFromIndex(index, reference);
}

export function getChapterFromIndex(
  index: CorpusIndex,
  reference: ChapterReference,
): Chapter | undefined {
  const book = index.get(reference.bookId);
  return book?.chapters.find((chapter) => chapter.number === reference.chapter);
}

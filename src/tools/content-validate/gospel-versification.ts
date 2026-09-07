/**
 * Referência de versificação padrão (não específica da BLIVRE, apenas a
 * estrutura de capítulos/versículos amplamente usada) dos 4 Evangelhos,
 * necessária só para C9 (RN-01): confirmar que a trilha de NT completa **pelo
 * menos um Evangelho integralmente** dentro dos 90 dias. Sem essa referência
 * fixa não haveria como distinguir "terminou o livro" de "só parou de
 * cobrir", já que o `ContentBundle` não carrega o texto integral do corpus
 * (só intervalos). Não depende do conteúdo real de nenhuma fonte com
 * licença — é a contagem pública de capítulos/versículos finais de cada
 * Evangelho.
 */

import type { ChapterVerse } from "./range-utils";

export const GOSPEL_BOOK_IDS = ["MAT", "MRK", "LUK", "JHN"] as const;

export type GospelBookId = (typeof GOSPEL_BOOK_IDS)[number];

export function isGospelBookId(bookId: string): bookId is GospelBookId {
  return (GOSPEL_BOOK_IDS as readonly string[]).includes(bookId);
}

export const GOSPEL_FINAL_VERSE: Readonly<Record<GospelBookId, ChapterVerse>> = {
  MAT: { chapter: 28, verse: 20 },
  MRK: { chapter: 16, verse: 20 },
  LUK: { chapter: 24, verse: 53 },
  JHN: { chapter: 21, verse: 25 },
};

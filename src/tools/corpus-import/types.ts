/**
 * Modelo canônico `livro → capítulo → versículo` (ADR-003, SDD.md §2.2/§5.3).
 * Alinhado à entidade `Book`/`Chapter`/`Verse` do SDD.md §5.3, simplificado
 * para o que o parser (TASK-006) precisa produzir; `CorpusManifest`
 * (`sourceVersionDate`, SHA-256, licença) é TASK-008 e não pertence aqui.
 */

import type { Testament } from "./canon";

export interface CanonicalVerse {
  readonly number: number;
  readonly text: string;
}

export interface CanonicalChapter {
  readonly number: number;
  readonly verses: readonly CanonicalVerse[];
}

export interface CanonicalBook {
  readonly bookId: string;
  readonly name: string;
  readonly testament: Testament;
  readonly chapters: readonly CanonicalChapter[];
}

export type CanonicalCorpus = readonly CanonicalBook[];

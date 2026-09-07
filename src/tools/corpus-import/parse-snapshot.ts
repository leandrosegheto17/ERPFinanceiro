/**
 * Parser do snapshot bruto `porbr2018` (eBible.org) → modelo canônico
 * `livro → capítulo → versículo` (ADR-003, SDD.md §2.2).
 *
 * Formato assumido do snapshot bruto (documentado também na fixture, em
 * `__fixtures__/porbr2018-sample.txt` — a suposição real será confirmada
 * quando o snapshot de produção chegar): uma linha por versículo, campos
 * separados por TAB — `<ID_LIVRO_USFM>\t<CAPÍTULO>\t<VERSÍCULO>\t<TEXTO>`.
 * Linhas vazias ou iniciadas por `#` são cabeçalho/comentário da fixture e
 * são ignoradas — não fazem parte do texto bíblico.
 *
 * O parser NUNCA normaliza o texto do versículo (acentuação, aspas, hífens,
 * espaços): o campo `<TEXTO>` é preservado byte a byte, exatamente como
 * lido da linha. Normalização é problema do render, não do parser
 * (ADR-003).
 */

import { CANONICAL_BOOKS, type CanonicalBookRef } from "./canon";
import type { CanonicalBook, CanonicalChapter, CanonicalCorpus } from "./types";

interface RawVerseLine {
  readonly bookId: string;
  readonly chapter: number;
  readonly verse: number;
  readonly text: string;
}

export class CorpusParseError extends Error {}

function parseLine(line: string, lineNumber: number): RawVerseLine | undefined {
  if (line.length === 0 || line.startsWith("#")) {
    return undefined;
  }

  const fields = line.split("\t");
  if (fields.length !== 4) {
    throw new CorpusParseError(
      `Linha ${lineNumber}: esperado 4 campos separados por TAB, encontrado ${fields.length}`,
    );
  }

  const [bookId, chapterRaw, verseRaw, text] = fields as [
    string,
    string,
    string,
    string,
  ];
  const chapter = Number(chapterRaw);
  const verse = Number(verseRaw);

  if (!Number.isInteger(chapter) || chapter < 1) {
    throw new CorpusParseError(
      `Linha ${lineNumber}: capítulo inválido "${chapterRaw}"`,
    );
  }
  if (!Number.isInteger(verse) || verse < 1) {
    throw new CorpusParseError(
      `Linha ${lineNumber}: versículo inválido "${verseRaw}"`,
    );
  }

  return { bookId, chapter, verse, text };
}

/**
 * Recebe o conteúdo bruto do snapshot (já lido do disco, sem transformação)
 * e devolve o modelo canônico, na ordem canônica dos 66 livros — não na
 * ordem em que os livros aparecem no snapshot.
 */
export function parseSnapshot(rawContent: string): CanonicalCorpus {
  const linesByBook = new Map<string, RawVerseLine[]>();

  const lines = rawContent.split(/\r\n|\n/);
  for (let i = 0; i < lines.length; i += 1) {
    const parsed = parseLine(lines[i] ?? "", i + 1);
    if (parsed === undefined) {
      continue;
    }

    const bucket = linesByBook.get(parsed.bookId);
    if (bucket === undefined) {
      linesByBook.set(parsed.bookId, [parsed]);
    } else {
      bucket.push(parsed);
    }
  }

  const knownIds = new Set(CANONICAL_BOOKS.map((book) => book.id));
  for (const bookId of linesByBook.keys()) {
    if (!knownIds.has(bookId)) {
      throw new CorpusParseError(
        `Livro "${bookId}" não pertence ao cânon protestante (39 AT + 27 NT)`,
      );
    }
  }

  return CANONICAL_BOOKS.map((bookRef) => buildBook(bookRef, linesByBook.get(bookRef.id)));
}

function buildBook(
  bookRef: CanonicalBookRef,
  lines: RawVerseLine[] | undefined,
): CanonicalBook {
  if (lines === undefined || lines.length === 0) {
    throw new CorpusParseError(
      `Livro "${bookRef.id}" (${bookRef.name}) ausente do snapshot`,
    );
  }

  const versesByChapter = new Map<number, RawVerseLine[]>();
  for (const line of lines) {
    const bucket = versesByChapter.get(line.chapter);
    if (bucket === undefined) {
      versesByChapter.set(line.chapter, [line]);
    } else {
      bucket.push(line);
    }
  }

  const chapterNumbers = [...versesByChapter.keys()].sort((a, b) => a - b);
  const chapters: CanonicalChapter[] = chapterNumbers.map((chapterNumber) => {
    const verseLines = versesByChapter.get(chapterNumber) ?? [];
    const verses = [...verseLines].sort((a, b) => a.verse - b.verse);
    return {
      number: chapterNumber,
      verses: verses.map((verse) => ({ number: verse.verse, text: verse.text })),
    };
  });

  return {
    bookId: bookRef.id,
    name: bookRef.name,
    testament: bookRef.testament,
    chapters,
  };
}

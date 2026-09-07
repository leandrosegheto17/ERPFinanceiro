/**
 * Reconhecimento de referência bíblica em pt-BR (ADR-005, SPIKE-03).
 * Módulo puro: nenhuma chamada de rede/armazenamento, nenhuma dependência de
 * React/DOM. Nunca lança exceção — toda entrada malformada ou ambígua vira
 * `Unresolved { reason }`, nunca "melhor palpite" (regra de ouro de ADR-005 —
 * falso positivo é catastrófico, falso negativo é só um aviso na tela).
 *
 * Estratégia de parsing (por que não é um único regex monolítico): a forma
 * mais direta — um regex que casa `<prefixo?><nome> <capítulo>[...]` de
 * ponta a ponta — cria uma ambiguidade real quando o prefixo numérico
 * romano/arábico pode colar sem espaço no nome (SPIKE-03 §1, exemplo
 * `1Co 13:4`): um grupo opcional `(1|2|3|i|ii|iii)\s?` greedy consome o "i"
 * inicial de livros como "Isaías" antes do resto do nome ser testado,
 * produzindo uma leitura errada sem chance de backtrack para a leitura
 * correta (sem prefixo). A resolução aqui usa 3 passos determinísticos que
 * eliminam essa ambiguidade por construção:
 *   1. Divide a entrada normalizada no ÚLTIMO espaço (a "referência
 *      capítulo[:versículo[-versículo]]" é sempre o último token,
 *      SPIKE-03 §1 — exatamente um espaço entre o token do livro e o
 *      capítulo).
 *   2. Extrai um prefixo numérico do início do token de livro só quando
 *      inequívoco: forma romana/arábica sempre exige espaço explícito
 *      depois (`"i "`, `"1 "`, ...); a forma arábica colada
 *      (`"1co"`) só é reconhecida quando o primeiro caractere é
 *      literalmente um dígito — nunca uma letra —, o que não pode colidir
 *      com nome nenhum da tabela.
 *   3. Valida a forma estrutural do restante (só letras/espaços) antes de
 *      consultar a tabela — garante que "formato-nao-reconhecido" nunca é
 *      confundido com "livro-desconhecido" (ex. `Rm 8:28; 12:2`, `v. 28`).
 */

import { buildCorpusIndex } from "../corpus/get-chapter";
import type { CorpusBundle } from "../corpus/types";
import { BOOK_NAME_TABLE, type BookNameEntry } from "./book-names";
import { normalizeReferenceInput } from "./normalize";
import type { ReferenceResolution } from "./types";

type NumberedPrefix = "1" | "2" | "3";

const CHAPTER_VERSE_PATTERN = /^(\d+)(?:[:.](\d+)(?:[-–](\d+))?)?$/;

/** Prefixos que exigem espaço explícito antes do resto do nome do livro (mais longos primeiro, para "iii "/"ii " não perderem para "i "). */
const SPACED_NUMBERED_PREFIXES: ReadonlyArray<readonly [string, NumberedPrefix]> = [
  ["iii ", "3"],
  ["ii ", "2"],
  ["i ", "1"],
  ["1 ", "1"],
  ["2 ", "2"],
  ["3 ", "3"],
];

const BOOK_TOKEN_SHAPE_PATTERN = /^[a-z]+( [a-z]+)*$/;

function extractNumberedPrefix(bookPart: string): {
  readonly prefix?: NumberedPrefix;
  readonly remainder: string;
} {
  for (const [token, prefix] of SPACED_NUMBERED_PREFIXES) {
    if (bookPart.startsWith(token)) {
      return { prefix, remainder: bookPart.slice(token.length) };
    }
  }

  // Forma colada (sem espaço) só é reconhecida via dígito arábico literal no
  // início — uma letra nunca dispara esta ramificação, o que evita a
  // ambiguidade com nomes de livro que começam com "i" (Isaías).
  const firstChar = bookPart.charAt(0);
  if (firstChar === "1" || firstChar === "2" || firstChar === "3") {
    const rest = bookPart.slice(1);
    if (/^[a-z]+$/.test(rest)) {
      return { prefix: firstChar, remainder: rest };
    }
  }

  return { remainder: bookPart };
}

function matchesEntry(
  entry: BookNameEntry,
  prefix: NumberedPrefix | undefined,
  remainder: string,
): boolean {
  if ((entry.numberedPrefix ?? undefined) !== (prefix ?? undefined)) {
    return false;
  }
  if (normalizeReferenceInput(entry.canonicalName) === remainder) {
    return true;
  }
  if (entry.abbreviation && normalizeReferenceInput(entry.abbreviation) === remainder) {
    return true;
  }
  if (entry.aliases?.some((alias) => normalizeReferenceInput(alias) === remainder)) {
    return true;
  }
  return false;
}

function displayName(entry: BookNameEntry): string {
  return entry.numberedPrefix ? `${entry.numberedPrefix} ${entry.canonicalName}` : entry.canonicalName;
}

export function resolveReference(
  input: string,
  bundle: CorpusBundle,
  table: readonly BookNameEntry[] = BOOK_NAME_TABLE,
): ReferenceResolution {
  const normalized = normalizeReferenceInput(input);
  const splitIndex = normalized.lastIndexOf(" ");
  if (splitIndex <= 0 || splitIndex === normalized.length - 1) {
    return { kind: "unresolved", reason: "formato-nao-reconhecido" };
  }

  const bookPart = normalized.slice(0, splitIndex);
  const chapterVersePart = normalized.slice(splitIndex + 1);

  const chapterVerseMatch = CHAPTER_VERSE_PATTERN.exec(chapterVersePart);
  if (!chapterVerseMatch) {
    return { kind: "unresolved", reason: "formato-nao-reconhecido" };
  }

  const { prefix, remainder } = extractNumberedPrefix(bookPart);
  if (!BOOK_TOKEN_SHAPE_PATTERN.test(remainder)) {
    return { kind: "unresolved", reason: "formato-nao-reconhecido" };
  }

  const matches = table.filter((entry) => matchesEntry(entry, prefix, remainder));
  if (matches.length === 0) {
    return { kind: "unresolved", reason: "livro-desconhecido" };
  }
  if (matches.length > 1) {
    return { kind: "unresolved", reason: "ambiguo" };
  }

  const entry = matches[0]!;
  const index = buildCorpusIndex(bundle);
  const book = index.get(entry.bookId);
  // Livro reconhecido na tabela mas ausente do bundle fornecido: do ponto de
  // vista de "o que este corpus consegue resolver", é indistinguível de
  // livro desconhecido — nunca acontece em produção (o bundle real sempre
  // tem os 66 livros, TASK-006/007/008), mas fixtures de teste podem ser
  // parciais de propósito (ver `resolve-reference.test.ts`).
  if (!book) {
    return { kind: "unresolved", reason: "livro-desconhecido" };
  }

  const chapterNumber = Number(chapterVerseMatch[1]);
  const chapter = book.chapters.find((c) => c.number === chapterNumber);
  if (!chapter) {
    return { kind: "unresolved", reason: "capitulo-inexistente" };
  }

  const verseGroup = chapterVerseMatch[2];
  const verseEndGroup = chapterVerseMatch[3];
  const chapterOnly = verseGroup === undefined;

  let verseStart: number;
  let verseEnd: number;
  if (chapterOnly) {
    verseStart = 1;
    verseEnd = chapter.verses.reduce((max, verse) => Math.max(max, verse.number), 0);
  } else if (verseEndGroup === undefined) {
    verseStart = Number(verseGroup);
    verseEnd = verseStart;
  } else {
    verseStart = Number(verseGroup);
    verseEnd = Number(verseEndGroup);
  }

  if (!chapterOnly) {
    const hasVerseStart = chapter.verses.some((verse) => verse.number === verseStart);
    if (!hasVerseStart) {
      return { kind: "unresolved", reason: "versiculo-inexistente" };
    }
    if (verseEndGroup !== undefined) {
      const hasVerseEnd = chapter.verses.some((verse) => verse.number === verseEnd);
      if (verseEnd < verseStart || !hasVerseEnd) {
        return { kind: "unresolved", reason: "intervalo-invalido" };
      }
    }
  }

  const label =
    chapterOnly || verseStart === verseEnd
      ? `${displayName(entry)} ${chapterNumber}${chapterOnly ? "" : `:${verseStart}`}`
      : `${displayName(entry)} ${chapterNumber}:${verseStart}-${verseEnd}`;

  return {
    kind: "resolved",
    bookId: entry.bookId,
    chapter: chapterNumber,
    verseStart,
    verseEnd,
    normalizedLabel: label,
  };
}

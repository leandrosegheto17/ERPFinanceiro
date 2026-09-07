/**
 * Verificação de integridade do corpus (ADR-003, RNF-10): reconstitui o texto
 * plano a partir do modelo canônico (saída de `parseSnapshot`, TASK-006) e
 * compara byte a byte contra o snapshot bruto original. Qualquer divergência
 * falha o build — não é aviso, é erro que interrompe o pipeline.
 *
 * A reconstituição usa o mesmo formato de linha do snapshot bruto
 * (`<ID_LIVRO_USFM>\t<CAPÍTULO>\t<VERSÍCULO>\t<TEXTO>`), na ordem de
 * travessia do modelo canônico. O snapshot original é comparado após remover
 * apenas linhas em branco/comentário (mesmo critério estrutural do parser,
 * TASK-006) — o texto de cada linha significativa não é normalizado.
 */

import type { CanonicalCorpus } from "./types";

export class CorpusIntegrityError extends Error {}

function extractSignificantLines(rawContent: string): readonly string[] {
  return rawContent
    .split(/\r\n|\n/)
    .filter((line) => line.length > 0 && !line.startsWith("#"));
}

function reconstructLines(corpus: CanonicalCorpus): readonly string[] {
  const lines: string[] = [];
  for (const book of corpus) {
    for (const chapter of book.chapters) {
      for (const verse of chapter.verses) {
        lines.push(`${book.bookId}\t${chapter.number}\t${verse.number}\t${verse.text}`);
      }
    }
  }
  return lines;
}

/**
 * Lança `CorpusIntegrityError` se a reconstituição do `corpus` divergir, em
 * qualquer byte, do `rawContent` original. Não retorna `false` silenciosamente.
 */
export function verifyCorpusIntegrity(rawContent: string, corpus: CanonicalCorpus): void {
  const original = extractSignificantLines(rawContent);
  const reconstructed = reconstructLines(corpus);

  if (original.length !== reconstructed.length) {
    throw new CorpusIntegrityError(
      `Divergência de integridade: ${original.length} linha(s) no snapshot original vs ${reconstructed.length} reconstituída(s) a partir do modelo canônico`,
    );
  }

  for (let i = 0; i < original.length; i += 1) {
    if (original[i] !== reconstructed[i]) {
      throw new CorpusIntegrityError(
        `Divergência de integridade byte a byte na linha ${i + 1}: reconstituição a partir do modelo canônico não bate com o snapshot bruto original`,
      );
    }
  }
}

import { describe, expect, it } from "vitest";
import type { CorpusBundle, Chapter, Verse } from "../corpus/types";
import type { BookNameEntry } from "./book-names";
import { resolveReference } from "./resolve-reference";

/**
 * Suíte simétrica (ADR-005/SPIKE-03 §4): tabela positiva (formatos que
 * DEVEM resolver) + tabela negativa (formatos/entradas que NÃO PODEM
 * resolver, transcrita literalmente da Seção 4 do spike). Uma regressão em
 * qualquer caso da tabela negativa é severidade máxima (ADR-005, mesmo nível
 * de M-04) — não é "só mais um teste".
 */

function makeChapter(number: number, verseCount: number): Chapter {
  const verses: Verse[] = [];
  for (let i = 1; i <= verseCount; i += 1) {
    verses.push({ number: i, text: `Texto do versículo ${i}.` });
  }
  return { number, verses };
}

// Fixture deliberadamente parcial: cobre só os livros citados nos exemplos
// de SPIKE-03 §1/§4 (Romanos, 1/2 Coríntios, Gálatas, Salmos). Jó e João
// (evangelho) são deliberadamente OMITIDOS — é o que faz o caso "Jo 3:16"
// (SPIKE-03 §4) resolver como `livro-desconhecido`: mesmo que "jo" normalize
// igual a "Jó" (SPIKE-03 §3), o livro não está neste corpus, e
// `resolveReference` trata "livro ausente do bundle" e "livro desconhecido"
// da mesma forma (ver comentário em `resolve-reference.ts`).
const fixtureBundle: CorpusBundle = [
  {
    bookId: "ROM",
    name: "Romanos",
    chapters: [makeChapter(8, 30)],
  },
  {
    bookId: "1CO",
    name: "1 Coríntios",
    chapters: [makeChapter(13, 13)],
  },
  {
    bookId: "2CO",
    name: "2 Coríntios",
    chapters: [makeChapter(1, 5)],
  },
  {
    bookId: "GAL",
    name: "Gálatas",
    chapters: [1, 2, 3, 4, 5, 6].map((n) => makeChapter(n, 3)),
  },
  {
    bookId: "PSA",
    name: "Salmos",
    chapters: [makeChapter(1, 3), makeChapter(2, 3), makeChapter(3, 3)],
  },
];

describe("resolveReference — TASK-010 (critério de aceite: suíte simétrica 100% verde)", () => {
  describe("tabela positiva — formatos de SPIKE-03 §1 devem resolver", () => {
    it("livro + capítulo + versículo (nome canônico completo)", () => {
      const result = resolveReference("Romanos 8:28", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "ROM",
        chapter: 8,
        verseStart: 28,
        verseEnd: 28,
        normalizedLabel: "Romanos 8:28",
      });
    });

    it("livro + capítulo + versículo (abreviação, separador ponto)", () => {
      const result = resolveReference("Rm 8.28", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "ROM",
        chapter: 8,
        verseStart: 28,
        verseEnd: 28,
        normalizedLabel: "Romanos 8:28",
      });
    });

    it("livro + capítulo + intervalo de versículos, mesmo capítulo (hífen)", () => {
      const result = resolveReference("Romanos 8:28-30", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "ROM",
        chapter: 8,
        verseStart: 28,
        verseEnd: 30,
        normalizedLabel: "Romanos 8:28-30",
      });
    });

    it("livro + capítulo + intervalo de versículos (separador ponto, travessão)", () => {
      const result = resolveReference("Rm 8.28–30", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "ROM",
        chapter: 8,
        verseStart: 28,
        verseEnd: 30,
        normalizedLabel: "Romanos 8:28-30",
      });
    });

    it("livro + capítulo inteiro (nome canônico)", () => {
      const result = resolveReference("Romanos 8", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "ROM",
        chapter: 8,
        verseStart: 1,
        verseEnd: 30,
        normalizedLabel: "Romanos 8",
      });
    });

    it("livro + capítulo inteiro (abreviação)", () => {
      const result = resolveReference("Rm 8", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "ROM",
        chapter: 8,
        verseStart: 1,
        verseEnd: 30,
        normalizedLabel: "Romanos 8",
      });
    });

    it("livro numerado, forma arábica com espaço (nome canônico)", () => {
      const result = resolveReference("1 Coríntios 13:4", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "1CO",
        chapter: 13,
        verseStart: 4,
        verseEnd: 4,
        normalizedLabel: "1 Coríntios 13:4",
      });
    });

    it("livro numerado, forma arábica colada (abreviação, sem espaço entre número e abreviação)", () => {
      const result = resolveReference("1Co 13:4", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "1CO",
        chapter: 13,
        verseStart: 4,
        verseEnd: 4,
        normalizedLabel: "1 Coríntios 13:4",
      });
    });

    it("livro numerado, forma romana (equivalente ao arábico, SPIKE-03 §2)", () => {
      const result = resolveReference("I Coríntios 13:4", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "1CO",
        chapter: 13,
        verseStart: 4,
        verseEnd: 4,
        normalizedLabel: "1 Coríntios 13:4",
      });
    });

    it("segundo livro numerado (2 Coríntios) distinto do primeiro (1 Coríntios)", () => {
      const result = resolveReference("2 Coríntios 1:1", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "2CO",
        chapter: 1,
        verseStart: 1,
        verseEnd: 1,
        normalizedLabel: "2 Coríntios 1:1",
      });
    });

    it("segundo livro numerado, forma romana", () => {
      const result = resolveReference("II Coríntios 1:1", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "2CO",
        chapter: 1,
        verseStart: 1,
        verseEnd: 1,
        normalizedLabel: "2 Coríntios 1:1",
      });
    });

    it("alias singular 'Salmo' de Salmos (SPIKE-03 §2)", () => {
      const result = resolveReference("Salmo 1:1", fixtureBundle);
      expect(result).toEqual({
        kind: "resolved",
        bookId: "PSA",
        chapter: 1,
        verseStart: 1,
        verseEnd: 1,
        normalizedLabel: "Salmos 1:1",
      });
    });

    it("insensível a maiúsculas/acentos (RN-05)", () => {
      const result = resolveReference("ROMANOS 8:28", fixtureBundle);
      expect(result.kind).toBe("resolved");
    });
  });

  describe("tabela negativa — SPIKE-03 §4, transcrição literal e obrigatória (severidade máxima em regressão)", () => {
    it.each([
      ["Romanos 8:28-9:2", "formato-nao-reconhecido"],
      ["Rm 8:28; 12:2", "formato-nao-reconhecido"],
      ["v. 28", "formato-nao-reconhecido"],
      ["Rm8:28", "formato-nao-reconhecido"],
      ["Romamos 8:28", "livro-desconhecido"],
      ["Gálatas 7", "capitulo-inexistente"],
      ["Romanos 8:99", "versiculo-inexistente"],
      ["Salmo 151", "capitulo-inexistente"],
      ["Jo 3:16", "livro-desconhecido"],
      ["Livro Fantasma 1:1", "livro-desconhecido"],
    ] as const)("%s → %s", (input, reason) => {
      const result = resolveReference(input, fixtureBundle);
      expect(result).toEqual({ kind: "unresolved", reason });
    });
  });

  describe("intervalo inválido (verseStart > verseEnd, ou verseEnd inexistente)", () => {
    it("verseStart > verseEnd", () => {
      const result = resolveReference("Romanos 8:30-28", fixtureBundle);
      expect(result).toEqual({ kind: "unresolved", reason: "intervalo-invalido" });
    });

    it("verseEnd inexistente no capítulo", () => {
      const result = resolveReference("Romanos 8:28-99", fixtureBundle);
      expect(result).toEqual({ kind: "unresolved", reason: "intervalo-invalido" });
    });
  });

  describe("ambiguidade (SPIKE-03 §3: sem caso real no cânon — testado via tabela injetada artificialmente)", () => {
    it("entrada que casa com mais de um livro de uma tabela sintética vira 'ambiguo'", () => {
      const collidingTable: readonly BookNameEntry[] = [
        { bookId: "AAA", canonicalName: "Teste" },
        { bookId: "BBB", canonicalName: "Teste" },
      ];

      const result = resolveReference("Teste 1:1", fixtureBundle, collidingTable);
      expect(result).toEqual({ kind: "unresolved", reason: "ambiguo" });
    });

    it("tabela de produção real não produz nenhum 'ambiguo' para os casos testados acima (SPIKE-03 §3)", () => {
      const result = resolveReference("Romanos 8:28", fixtureBundle);
      expect(result.kind).not.toBe("unresolved");
    });
  });

  describe("nunca lança exceção", () => {
    it.each(["", "   ", "1:1", "8:28", "Romanos", "Romanos:", "-", "12345"])(
      "entrada malformada %j não lança e retorna Unresolved",
      (input) => {
        expect(() => resolveReference(input, fixtureBundle)).not.toThrow();
        const result = resolveReference(input, fixtureBundle);
        expect(result.kind).toBe("unresolved");
      },
    );
  });
});

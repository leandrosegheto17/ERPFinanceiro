import { describe, expect, it } from "vitest";
import { buildCorpusIndex, getChapter, getChapterFromIndex } from "./get-chapter";
import type { CorpusBundle } from "./types";

const fixtureBundle: CorpusBundle = [
  {
    bookId: "GEN",
    name: "Gênesis",
    chapters: [
      {
        number: 1,
        verses: [
          { number: 1, text: "No princípio..." },
          { number: 2, text: "E a terra era sem forma..." },
        ],
      },
      {
        number: 2,
        verses: [{ number: 1, text: "Assim, pois, foram acabados os céus..." }],
      },
    ],
  },
  {
    bookId: "EXO",
    name: "Êxodo",
    chapters: [
      {
        number: 1,
        verses: [{ number: 1, text: "São estes os nomes dos filhos de Israel..." }],
      },
    ],
  },
];

describe("getChapter — TASK-009 (critério de aceite: retorna capítulo por referência estruturada)", () => {
  it("retorna o capítulo correto dado bookId e chapter válidos", () => {
    const chapter = getChapter(fixtureBundle, { bookId: "GEN", chapter: 2 });

    expect(chapter?.number).toBe(2);
    expect(chapter?.verses).toHaveLength(1);
    expect(chapter?.verses[0]?.text).toBe(
      "Assim, pois, foram acabados os céus...",
    );
  });

  it("retorna o capítulo correto de um segundo livro do mesmo bundle", () => {
    const chapter = getChapter(fixtureBundle, { bookId: "EXO", chapter: 1 });

    expect(chapter?.number).toBe(1);
    expect(chapter?.verses[0]?.text).toBe(
      "São estes os nomes dos filhos de Israel...",
    );
  });

  it("retorna undefined quando o bookId não existe no bundle", () => {
    const chapter = getChapter(fixtureBundle, { bookId: "LEV", chapter: 1 });

    expect(chapter).toBeUndefined();
  });

  it("retorna undefined quando o capítulo não existe no livro", () => {
    const chapter = getChapter(fixtureBundle, { bookId: "GEN", chapter: 99 });

    expect(chapter).toBeUndefined();
  });

  it("buildCorpusIndex + getChapterFromIndex produzem o mesmo resultado que getChapter", () => {
    const index = buildCorpusIndex(fixtureBundle);

    expect(getChapterFromIndex(index, { bookId: "GEN", chapter: 1 })).toEqual(
      getChapter(fixtureBundle, { bookId: "GEN", chapter: 1 }),
    );
  });
});

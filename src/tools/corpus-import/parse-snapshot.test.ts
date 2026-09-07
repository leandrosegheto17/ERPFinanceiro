import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { describe, expect, it } from "vitest";
import { CANONICAL_BOOK_IDS } from "./canon";
import { CorpusParseError, parseSnapshot } from "./parse-snapshot";

const __dirname = dirname(fileURLToPath(import.meta.url));
const fixturePath = join(__dirname, "__fixtures__", "porbr2018-sample.txt");
const fixtureContent = readFileSync(fixturePath, "utf8");

describe("parseSnapshot — TASK-006 (critério de aceite: 66 livros)", () => {
  it("gera estrutura com exatamente 66 livros a partir da fixture", () => {
    const corpus = parseSnapshot(fixtureContent);

    expect(corpus).toHaveLength(66);
  });

  it("mantém a ordem canônica (Gênesis...Malaquias, Mateus...Apocalipse)", () => {
    const corpus = parseSnapshot(fixtureContent);

    expect(corpus.map((book) => book.bookId)).toEqual(CANONICAL_BOOK_IDS);
    expect(corpus[0]?.name).toBe("Gênesis");
    expect(corpus[38]?.name).toBe("Malaquias");
    expect(corpus[39]?.name).toBe("Mateus");
    expect(corpus[65]?.name).toBe("Apocalipse");
  });

  it("separa AT (39) e NT (27) corretamente", () => {
    const corpus = parseSnapshot(fixtureContent);

    expect(corpus.filter((book) => book.testament === "AT")).toHaveLength(39);
    expect(corpus.filter((book) => book.testament === "NT")).toHaveLength(27);
  });

  it("preserva o texto do versículo sem nenhuma normalização", () => {
    const corpus = parseSnapshot(fixtureContent);
    const genesis = corpus.find((book) => book.bookId === "GEN");

    expect(genesis?.chapters[0]?.verses[0]?.text).toBe(
      "Texto de exemplo (fixture) do primeiro versiculo de GEN.",
    );
  });

  it("agrupa capítulos e versículos ordenados numericamente", () => {
    const corpus = parseSnapshot(fixtureContent);
    const genesis = corpus.find((book) => book.bookId === "GEN");

    expect(genesis?.chapters).toHaveLength(1);
    expect(genesis?.chapters[0]?.number).toBe(1);
    expect(genesis?.chapters[0]?.verses.map((verse) => verse.number)).toEqual([
      1, 2,
    ]);
  });

  it("ignora linhas em branco e comentários (cabeçalho da fixture)", () => {
    const withExtraComment = `# outro comentario\n\n${fixtureContent}`;

    expect(() => parseSnapshot(withExtraComment)).not.toThrow();
    expect(parseSnapshot(withExtraComment)).toHaveLength(66);
  });

  it("falha com CorpusParseError se um livro do cânon estiver ausente do snapshot", () => {
    const withoutRevelation = fixtureContent
      .split("\n")
      .filter((line) => !line.startsWith("REV\t"))
      .join("\n");

    expect(() => parseSnapshot(withoutRevelation)).toThrow(CorpusParseError);
    expect(() => parseSnapshot(withoutRevelation)).toThrow(/Apocalipse/);
  });

  it("falha com CorpusParseError se um livro fora do cânon aparecer no snapshot", () => {
    const withUnknownBook = `${fixtureContent}\nXXX\t1\t1\tTexto invalido.\n`;

    expect(() => parseSnapshot(withUnknownBook)).toThrow(CorpusParseError);
  });

  it("falha com CorpusParseError se uma linha tiver número de campos errado", () => {
    expect(() => parseSnapshot("GEN\t1\t1\n")).toThrow(CorpusParseError);
  });
});

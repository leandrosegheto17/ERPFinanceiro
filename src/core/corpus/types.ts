/**
 * Tipos mínimos de runtime do corpus local (TASK-009, SDD.md §2.2/§5.3).
 * Deliberadamente não reimporta `CanonicalBook`/`CanonicalChapter`/`CanonicalVerse`
 * de `src/tools/corpus-import/types.ts`: `tools/*` é código de build/CI (roda em
 * Node, nunca no bundle do cliente), enquanto `core/*` é runtime do app no
 * navegador do usuário — são camadas diferentes mesmo com *shape* de dados
 * parecido (DI-02, ADR-001, ADR-012). O *shape* aqui é o subconjunto que o
 * acesso ao bundle local precisa, não uma cópia 1:1.
 */

export interface Verse {
  readonly number: number;
  readonly text: string;
}

export interface Chapter {
  readonly number: number;
  readonly verses: readonly Verse[];
}

export interface Book {
  readonly bookId: string;
  readonly name: string;
  readonly chapters: readonly Chapter[];
}

/**
 * Corpus local já em memória (carregamento de rede/fetch dos artefatos
 * estáticos gerados por `tools/corpus-import` — `corpus/index.json`,
 * `corpus/{livro}/{capitulo}.json` — é escopo de outra tarefa/camada, fora
 * deste módulo). Array simples: a ordem canônica já é uma garantia produzida
 * por `tools/corpus-import` (TASK-006) e o volume é fixo e pequeno (66
 * livros), então o índice por `bookId` usado em `getChapter` é construído sob
 * demanda em vez de exigir um `Record` já indexado do chamador.
 */
export type CorpusBundle = readonly Book[];

/** Referência estruturada a um capítulo (não string livre — isso é `core/reference`, TASK-010). */
export interface ChapterReference {
  readonly bookId: string;
  readonly chapter: number;
}

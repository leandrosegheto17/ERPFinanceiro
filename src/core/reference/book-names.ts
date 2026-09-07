/**
 * Tabela de nomes/abreviações dos 66 livros do cânon (dado, não código —
 * ADR-005: "toda entrada da tabela é dado, não código"). Transcrição literal
 * da Seção 2 do `SPIKE-03-resultado.md`.
 *
 * `canonicalName`/`abbreviation` armazenam a forma BASE do livro, sem o
 * prefixo numérico (ex. "Samuel", não "1 Samuel") — o prefixo é modelado
 * separadamente em `numberedPrefix`, presente apenas nos livros numerados
 * (1/2 Samuel, 1/2 Reis, 1/2 Crônicas, 1/2 Coríntios, 1/2 Tessalonicenses,
 * 1/2 Timóteo, 1/2 Pedro, 1/2/3 João epistolar). Isso permite representar as
 * duas entradas de um mesmo nome-base (ex. "Samuel") como registros
 * distintos, um por `bookId`, sem duplicar a lógica de reconhecimento do
 * prefixo em cada entrada.
 */

export interface BookNameEntry {
  readonly bookId: string;
  /** Nome canônico, sem prefixo numérico (id USFM, cânon protestante). */
  readonly canonicalName: string;
  /** Abreviação de uso corrente no Brasil, sem prefixo numérico; `undefined` = sem abreviação curta (Jó, João — SPIKE-03 §3). */
  readonly abbreviation?: string;
  /** Aliases adicionais de nome completo (ex. "Salmo", forma singular de Salmos). */
  readonly aliases?: readonly string[];
  /** Presente só nos livros numerados; distingue as entradas que compartilham o mesmo nome-base. */
  readonly numberedPrefix?: "1" | "2" | "3";
}

export const BOOK_NAME_TABLE: readonly BookNameEntry[] = [
  // Antigo Testamento (39)
  { bookId: "GEN", canonicalName: "Gênesis", abbreviation: "Gn" },
  { bookId: "EXO", canonicalName: "Êxodo", abbreviation: "Ex" },
  { bookId: "LEV", canonicalName: "Levítico", abbreviation: "Lv" },
  { bookId: "NUM", canonicalName: "Números", abbreviation: "Nm" },
  { bookId: "DEU", canonicalName: "Deuteronômio", abbreviation: "Dt" },
  { bookId: "JOS", canonicalName: "Josué", abbreviation: "Js" },
  { bookId: "JDG", canonicalName: "Juízes", abbreviation: "Jz" },
  { bookId: "RUT", canonicalName: "Rute", abbreviation: "Rt" },
  { bookId: "1SA", canonicalName: "Samuel", abbreviation: "Sm", numberedPrefix: "1" },
  { bookId: "2SA", canonicalName: "Samuel", abbreviation: "Sm", numberedPrefix: "2" },
  { bookId: "1KI", canonicalName: "Reis", abbreviation: "Rs", numberedPrefix: "1" },
  { bookId: "2KI", canonicalName: "Reis", abbreviation: "Rs", numberedPrefix: "2" },
  { bookId: "1CH", canonicalName: "Crônicas", abbreviation: "Cr", numberedPrefix: "1" },
  { bookId: "2CH", canonicalName: "Crônicas", abbreviation: "Cr", numberedPrefix: "2" },
  { bookId: "EZR", canonicalName: "Esdras", abbreviation: "Ed" },
  { bookId: "NEH", canonicalName: "Neemias", abbreviation: "Ne" },
  { bookId: "EST", canonicalName: "Ester", abbreviation: "Et" },
  // Jó: sem abreviação (SPIKE-03 §3 — evita colisão normalizada com "Jo").
  { bookId: "JOB", canonicalName: "Jó" },
  {
    bookId: "PSA",
    canonicalName: "Salmos",
    abbreviation: "Sl",
    aliases: ["Salmo"],
  },
  { bookId: "PRO", canonicalName: "Provérbios", abbreviation: "Pv" },
  { bookId: "ECC", canonicalName: "Eclesiastes", abbreviation: "Ec" },
  { bookId: "SNG", canonicalName: "Cânticos", abbreviation: "Ct" },
  { bookId: "ISA", canonicalName: "Isaías", abbreviation: "Is" },
  { bookId: "JER", canonicalName: "Jeremias", abbreviation: "Jr" },
  { bookId: "LAM", canonicalName: "Lamentações", abbreviation: "Lm" },
  { bookId: "EZK", canonicalName: "Ezequiel", abbreviation: "Ez" },
  { bookId: "DAN", canonicalName: "Daniel", abbreviation: "Dn" },
  { bookId: "HOS", canonicalName: "Oséias", abbreviation: "Os" },
  { bookId: "JOL", canonicalName: "Joel", abbreviation: "Jl" },
  { bookId: "AMO", canonicalName: "Amós", abbreviation: "Am" },
  { bookId: "OBA", canonicalName: "Obadias", abbreviation: "Ob" },
  { bookId: "JON", canonicalName: "Jonas", abbreviation: "Jn" },
  { bookId: "MIC", canonicalName: "Miquéias", abbreviation: "Mq" },
  { bookId: "NAM", canonicalName: "Naum", abbreviation: "Na" },
  { bookId: "HAB", canonicalName: "Habacuque", abbreviation: "Hc" },
  { bookId: "ZEP", canonicalName: "Sofonias", abbreviation: "Sf" },
  { bookId: "HAG", canonicalName: "Ageu", abbreviation: "Ag" },
  { bookId: "ZEC", canonicalName: "Zacarias", abbreviation: "Zc" },
  { bookId: "MAL", canonicalName: "Malaquias", abbreviation: "Ml" },

  // Novo Testamento (27)
  { bookId: "MAT", canonicalName: "Mateus", abbreviation: "Mt" },
  { bookId: "MRK", canonicalName: "Marcos", abbreviation: "Mc" },
  { bookId: "LUK", canonicalName: "Lucas", abbreviation: "Lc" },
  // João (evangelho): sem abreviação curta (SPIKE-03 §3 — decisão que evita a colisão com Jó).
  { bookId: "JHN", canonicalName: "João" },
  { bookId: "ACT", canonicalName: "Atos", abbreviation: "At" },
  { bookId: "ROM", canonicalName: "Romanos", abbreviation: "Rm" },
  { bookId: "1CO", canonicalName: "Coríntios", abbreviation: "Co", numberedPrefix: "1" },
  { bookId: "2CO", canonicalName: "Coríntios", abbreviation: "Co", numberedPrefix: "2" },
  { bookId: "GAL", canonicalName: "Gálatas", abbreviation: "Gl" },
  { bookId: "EPH", canonicalName: "Efésios", abbreviation: "Ef" },
  { bookId: "PHP", canonicalName: "Filipenses", abbreviation: "Fp" },
  { bookId: "COL", canonicalName: "Colossenses", abbreviation: "Cl" },
  { bookId: "1TH", canonicalName: "Tessalonicenses", abbreviation: "Ts", numberedPrefix: "1" },
  { bookId: "2TH", canonicalName: "Tessalonicenses", abbreviation: "Ts", numberedPrefix: "2" },
  { bookId: "1TI", canonicalName: "Timóteo", abbreviation: "Tm", numberedPrefix: "1" },
  { bookId: "2TI", canonicalName: "Timóteo", abbreviation: "Tm", numberedPrefix: "2" },
  { bookId: "TIT", canonicalName: "Tito", abbreviation: "Tt" },
  { bookId: "PHM", canonicalName: "Filemom", abbreviation: "Fm" },
  { bookId: "HEB", canonicalName: "Hebreus", abbreviation: "Hb" },
  { bookId: "JAS", canonicalName: "Tiago", abbreviation: "Tg" },
  { bookId: "1PE", canonicalName: "Pedro", abbreviation: "Pe", numberedPrefix: "1" },
  { bookId: "2PE", canonicalName: "Pedro", abbreviation: "Pe", numberedPrefix: "2" },
  { bookId: "1JN", canonicalName: "João", abbreviation: "Jo", numberedPrefix: "1" },
  { bookId: "2JN", canonicalName: "João", abbreviation: "Jo", numberedPrefix: "2" },
  { bookId: "3JN", canonicalName: "João", abbreviation: "Jo", numberedPrefix: "3" },
  { bookId: "JUD", canonicalName: "Judas", abbreviation: "Jd" },
  { bookId: "REV", canonicalName: "Apocalipse", abbreviation: "Ap" },
];

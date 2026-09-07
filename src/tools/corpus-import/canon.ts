/**
 * Cânon protestante (39 AT + 27 NT), na ordem canônica usada para validar o
 * inventário de 66 livros do parser (TASK-006) e, adiante, a verificação
 * byte a byte (TASK-007). `id` segue os códigos de 3 letras do padrão USFM
 * (convenção também usada pelo eBible.org, fonte do snapshot `porbr2018`) —
 * não é a abreviação exibida ao usuário (isso é `core/reference`, TASK-010).
 */

export type Testament = "AT" | "NT";

export interface CanonicalBookRef {
  readonly id: string;
  readonly name: string;
  readonly testament: Testament;
}

export const CANONICAL_BOOKS: readonly CanonicalBookRef[] = [
  // Antigo Testamento (39)
  { id: "GEN", name: "Gênesis", testament: "AT" },
  { id: "EXO", name: "Êxodo", testament: "AT" },
  { id: "LEV", name: "Levítico", testament: "AT" },
  { id: "NUM", name: "Números", testament: "AT" },
  { id: "DEU", name: "Deuteronômio", testament: "AT" },
  { id: "JOS", name: "Josué", testament: "AT" },
  { id: "JDG", name: "Juízes", testament: "AT" },
  { id: "RUT", name: "Rute", testament: "AT" },
  { id: "1SA", name: "1 Samuel", testament: "AT" },
  { id: "2SA", name: "2 Samuel", testament: "AT" },
  { id: "1KI", name: "1 Reis", testament: "AT" },
  { id: "2KI", name: "2 Reis", testament: "AT" },
  { id: "1CH", name: "1 Crônicas", testament: "AT" },
  { id: "2CH", name: "2 Crônicas", testament: "AT" },
  { id: "EZR", name: "Esdras", testament: "AT" },
  { id: "NEH", name: "Neemias", testament: "AT" },
  { id: "EST", name: "Ester", testament: "AT" },
  { id: "JOB", name: "Jó", testament: "AT" },
  { id: "PSA", name: "Salmos", testament: "AT" },
  { id: "PRO", name: "Provérbios", testament: "AT" },
  { id: "ECC", name: "Eclesiastes", testament: "AT" },
  { id: "SNG", name: "Cânticos", testament: "AT" },
  { id: "ISA", name: "Isaías", testament: "AT" },
  { id: "JER", name: "Jeremias", testament: "AT" },
  { id: "LAM", name: "Lamentações", testament: "AT" },
  { id: "EZK", name: "Ezequiel", testament: "AT" },
  { id: "DAN", name: "Daniel", testament: "AT" },
  { id: "HOS", name: "Oséias", testament: "AT" },
  { id: "JOL", name: "Joel", testament: "AT" },
  { id: "AMO", name: "Amós", testament: "AT" },
  { id: "OBA", name: "Obadias", testament: "AT" },
  { id: "JON", name: "Jonas", testament: "AT" },
  { id: "MIC", name: "Miquéias", testament: "AT" },
  { id: "NAM", name: "Naum", testament: "AT" },
  { id: "HAB", name: "Habacuque", testament: "AT" },
  { id: "ZEP", name: "Sofonias", testament: "AT" },
  { id: "HAG", name: "Ageu", testament: "AT" },
  { id: "ZEC", name: "Zacarias", testament: "AT" },
  { id: "MAL", name: "Malaquias", testament: "AT" },
  // Novo Testamento (27)
  { id: "MAT", name: "Mateus", testament: "NT" },
  { id: "MRK", name: "Marcos", testament: "NT" },
  { id: "LUK", name: "Lucas", testament: "NT" },
  { id: "JHN", name: "João", testament: "NT" },
  { id: "ACT", name: "Atos", testament: "NT" },
  { id: "ROM", name: "Romanos", testament: "NT" },
  { id: "1CO", name: "1 Coríntios", testament: "NT" },
  { id: "2CO", name: "2 Coríntios", testament: "NT" },
  { id: "GAL", name: "Gálatas", testament: "NT" },
  { id: "EPH", name: "Efésios", testament: "NT" },
  { id: "PHP", name: "Filipenses", testament: "NT" },
  { id: "COL", name: "Colossenses", testament: "NT" },
  { id: "1TH", name: "1 Tessalonicenses", testament: "NT" },
  { id: "2TH", name: "2 Tessalonicenses", testament: "NT" },
  { id: "1TI", name: "1 Timóteo", testament: "NT" },
  { id: "2TI", name: "2 Timóteo", testament: "NT" },
  { id: "TIT", name: "Tito", testament: "NT" },
  { id: "PHM", name: "Filemom", testament: "NT" },
  { id: "HEB", name: "Hebreus", testament: "NT" },
  { id: "JAS", name: "Tiago", testament: "NT" },
  { id: "1PE", name: "1 Pedro", testament: "NT" },
  { id: "2PE", name: "2 Pedro", testament: "NT" },
  { id: "1JN", name: "1 João", testament: "NT" },
  { id: "2JN", name: "2 João", testament: "NT" },
  { id: "3JN", name: "3 João", testament: "NT" },
  { id: "JUD", name: "Judas", testament: "NT" },
  { id: "REV", name: "Apocalipse", testament: "NT" },
] as const;

export const CANONICAL_BOOK_IDS: readonly string[] = CANONICAL_BOOKS.map(
  (book) => book.id,
);

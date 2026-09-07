/**
 * `CorpusManifest` (ADR-003 etapa 5, SDD.md §5.3): proveniência do corpus —
 * versão declarada pela fonte, licença, titulares e SHA-256 dos artefatos
 * gerados pelo pipeline. É o dado de onde a tela de atribuição (T-06,
 * TASK-045) renderiza a string de RN-06; nenhuma atribuição é digitada em
 * código.
 *
 * SDD.md §5.3 descreve o hash como `sha256[]`. Aqui é um mapa
 * `nome-do-arquivo → hash` (`Record<string, string>`), não um array: permite
 * verificar o hash de um artefato específico por nome em O(1) sem varrer a
 * lista, e evita duplicar o nome do arquivo em dois lugares (chave do mapa
 * já é o identificador). Mesma informação, formato mais verificável.
 */

export interface CorpusManifest {
  readonly sourceId: string;
  readonly sourceVersionDate: string;
  readonly importedAt: string;
  readonly license: string;
  readonly licenseUrl: string;
  readonly holders: readonly string[];
  readonly modifications: "none";
  readonly sha256: Readonly<Record<string, string>>;
}

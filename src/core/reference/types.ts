/**
 * Tipos de resultado do reconhecimento de referência bíblica (ADR-005):
 * soma de tipos `Resolved | Unresolved`, sem exceção, sem "melhor palpite".
 * `reason` é uma enumeração fechada — qualquer entrada malformada cai em um
 * destes 6 motivos, nunca em `undefined`/`null` solto.
 */

export interface ResolvedReference {
  readonly kind: "resolved";
  readonly bookId: string;
  readonly chapter: number;
  readonly verseStart: number;
  readonly verseEnd: number;
  readonly normalizedLabel: string;
}

export type UnresolvedReason =
  | "formato-nao-reconhecido"
  | "livro-desconhecido"
  | "capitulo-inexistente"
  | "versiculo-inexistente"
  | "intervalo-invalido"
  | "ambiguo";

export interface UnresolvedReference {
  readonly kind: "unresolved";
  readonly reason: UnresolvedReason;
}

export type ReferenceResolution = ResolvedReference | UnresolvedReference;

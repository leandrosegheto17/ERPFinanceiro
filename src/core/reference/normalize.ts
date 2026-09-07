/**
 * Normalização de entrada exigida por RN-05 / SPIKE-03 §1: minúsculas + NFD
 * com remoção de diacríticos + colapso de espaços múltiplos. Aplicada tanto
 * à entrada inteira do usuário quanto às strings de dado da tabela de nomes
 * (`book-names.ts`), garantindo a mesma normalização dos dois lados da
 * comparação.
 *
 * Pontos (`.`) não são removidos aqui: são o próprio separador
 * capítulo:versículo aceito por SPIKE-03 §1 (`Rm 8.28`). Um ponto que sobre
 * fora dessa posição (ex. `v. 28`) quebra a estrutura esperada do token de
 * livro e o parser resolve o caso corretamente como
 * `formato-nao-reconhecido` sem precisar de uma regra extra de remoção de
 * ponto — ver `resolve-reference.ts`.
 */
const COMBINING_DIACRITICS_PATTERN = /[\u0300-\u036f]/g;

export function normalizeReferenceInput(input: string): string {
  return input
    .normalize("NFD")
    .replace(COMBINING_DIACRITICS_PATTERN, "")
    .toLowerCase()
    .replace(/\s+/g, " ")
    .trim();
}

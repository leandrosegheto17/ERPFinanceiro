/**
 * TASK-023 — leitor do `tokens.css` para o teste de contraste.
 *
 * `tokens.css` é a fonte única de verdade dos tokens (ADR-011: CSS custom
 * properties, não objeto JS/TS). Para o teste automatizado poder recalcular
 * a razão de contraste de cada par sem duplicar valores de cor num segundo
 * lugar (o que permitiria os dois arquivos divergirem sem detecção), este
 * módulo faz um parse mínimo por regex do próprio CSS:
 *
 *  - extrai o valor hex de cada `--token: #rrggbb;` dentro de um bloco de
 *    tema (`:root`, `:root[data-theme="dark"]`, `:root[data-theme="light"]`
 *    e o fallback de `@media (prefers-color-scheme: dark)`);
 *  - extrai as anotações `CONTRAST-PAIR (tema): --a sobre --b | nível
 *    (>=x:1) | ~y:1` dos comentários, que declaram quais pares devem ser
 *    verificados e com qual nível mínimo.
 *
 * Não é um parser de CSS genérico — cobre só a sintaxe que `tokens.css`
 * usa (chaves simples, sem aninhamento além de `@media { :root { ... } }`).
 *
 * Ferramenta de teste, não código de app: usa `node:fs`, então é excluído
 * de `tsconfig.app.json` (não entra no bundle do navegador, igual a
 * `src/tools/corpus-import/**\/*.ts`) e incluído em `tsconfig.node.json`.
 */

import { readFileSync } from "node:fs";
import type { WcagLevel } from "./contrast";

export type ThemeName = "light" | "dark";

export interface DeclaredContrastPair {
  theme: ThemeName;
  textToken: string;
  bgToken: string;
  level: WcagLevel;
  minimumRatio: number;
}

export interface ParsedTokensCss {
  /** Valores de cor resolvidos por tema (nome do token, hex). */
  colorsByTheme: Record<ThemeName, Record<string, string>>;
  /** Pares de contraste declarados nos comentários `CONTRAST-PAIR`. */
  declaredPairs: DeclaredContrastPair[];
  /**
   * Cores do bloco de fallback `@media (prefers-color-scheme: dark)
   * { :root:not([data-theme]) { ... } }`, expostas à parte para o teste
   * confirmar que não divergem do override manual `[data-theme="dark"]`.
   */
  darkFallbackColors: Record<string, string>;
}

const CONTRAST_PAIR_PATTERN =
  /CONTRAST-PAIR \((light|dark)\):\s*(--[\w-]+)\s+sobre\s+(--[\w-]+)\s*\|\s*(AA-normal|AA-ui)\s*\(>=([\d.]+):1\)/g;

const COLOR_TOKEN_PATTERN = /(--color-[\w-]+):\s*(#[0-9a-fA-F]{3,8})\s*;/g;

/**
 * Extrai o conteúdo do primeiro bloco `{ ... }` que começa depois de
 * `selector`, tolerando um nível de chave aninhada (usado para o bloco
 * `@media (prefers-color-scheme: dark) { :root:not([data-theme]) { ... } }`).
 */
function extractBlock(css: string, selectorPattern: RegExp): string {
  const match = selectorPattern.exec(css);
  if (!match) {
    throw new Error(`Bloco não encontrado em tokens.css: ${selectorPattern}`);
  }

  const start = match.index + match[0].length;
  let depth = 1;
  let index = start;
  while (depth > 0 && index < css.length) {
    if (css[index] === "{") depth += 1;
    else if (css[index] === "}") depth -= 1;
    index += 1;
  }

  return css.slice(start, index - 1);
}

function extractColors(block: string): Record<string, string> {
  const colors: Record<string, string> = {};
  for (const match of block.matchAll(COLOR_TOKEN_PATTERN)) {
    const [, token, hex] = match;
    colors[token] = hex.toLowerCase();
  }
  return colors;
}

/**
 * Anotação `CONTRAST-PAIR` genérica, usada por arquivos de tema único (sem
 * variantes light/dark) como `presentation-theme.css` (TASK-024). Aceita
 * qualquer nome de tema entre parênteses e o nível AAA além dos níveis AA já
 * usados por `tokens.css`.
 */
const GENERIC_CONTRAST_PAIR_PATTERN =
  /CONTRAST-PAIR \(([\w-]+)\):\s*(--[\w-]+)\s+sobre\s+(--[\w-]+)\s*\|\s*(AA-normal|AA-ui|AAA)\s*\(>=([\d.]+):1\)/g;

/** Tokens de cor de qualquer prefixo (`--color-*`, `--apres-*`, ...). */
const GENERIC_COLOR_TOKEN_PATTERN = /(--[\w-]+):\s*(#[0-9a-fA-F]{3,8})\s*;/g;

export interface SingleThemeContrastPair {
  theme: string;
  textToken: string;
  bgToken: string;
  level: WcagLevel;
  minimumRatio: number;
}

export interface ParsedSingleThemeCss {
  /** Valores de cor resolvidos dentro do bloco do seletor informado. */
  colors: Record<string, string>;
  /** Pares de contraste declarados nos comentários `CONTRAST-PAIR`. */
  declaredPairs: SingleThemeContrastPair[];
}

/**
 * Lê um arquivo CSS com um único bloco de tema (ex.: `presentation-theme.css`,
 * TASK-024) — variante mais simples de `parseTokensCss`, sem a lógica de
 * light/dark/fallback, para arquivos que não precisam dela. Reaproveita
 * `extractBlock` para tolerar o mesmo nível de aninhamento e a mesma
 * convenção de anotação `CONTRAST-PAIR` (generalizada para aceitar AAA e
 * qualquer prefixo de token).
 */
export function parseSingleThemeTokensCss(
  cssFilePath: string,
  selectorPattern: RegExp,
): ParsedSingleThemeCss {
  const css = readFileSync(cssFilePath, "utf-8");
  const block = extractBlock(css, selectorPattern);

  const colors: Record<string, string> = {};
  for (const match of block.matchAll(GENERIC_COLOR_TOKEN_PATTERN)) {
    const [, token, hex] = match;
    colors[token] = hex.toLowerCase();
  }

  const declaredPairs: SingleThemeContrastPair[] = [];
  for (const match of css.matchAll(GENERIC_CONTRAST_PAIR_PATTERN)) {
    const [, theme, textToken, bgToken, level, minimumRatio] = match;
    declaredPairs.push({
      theme,
      textToken,
      bgToken,
      level: level as WcagLevel,
      minimumRatio: Number.parseFloat(minimumRatio),
    });
  }

  return { colors, declaredPairs };
}

export function parseTokensCss(cssFilePath: string): ParsedTokensCss {
  const css = readFileSync(cssFilePath, "utf-8");

  const rootBlock = extractBlock(css, /:root\s*\{/);
  const darkOverrideBlock = extractBlock(css, /:root\[data-theme="dark"\]\s*\{/);
  const lightOverrideBlock = extractBlock(css, /:root\[data-theme="light"\]\s*\{/);
  const darkFallbackBlock = extractBlock(
    css,
    /:root:not\(\[data-theme\]\)\s*\{/,
  );

  const colorsByTheme: Record<ThemeName, Record<string, string>> = {
    light: { ...extractColors(rootBlock), ...extractColors(lightOverrideBlock) },
    dark: extractColors(darkOverrideBlock),
  };

  const darkFallbackColors = extractColors(darkFallbackBlock);

  const declaredPairs: DeclaredContrastPair[] = [];
  for (const match of css.matchAll(CONTRAST_PAIR_PATTERN)) {
    const [, theme, textToken, bgToken, level, minimumRatio] = match;
    declaredPairs.push({
      theme: theme as ThemeName,
      textToken,
      bgToken,
      level: level as WcagLevel,
      minimumRatio: Number.parseFloat(minimumRatio),
    });
  }

  return {
    colorsByTheme,
    declaredPairs,
    darkFallbackColors,
  };
}

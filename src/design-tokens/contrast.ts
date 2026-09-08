/**
 * TASK-023 — cálculo de razão de contraste WCAG 2.x (1.4.3/1.4.11).
 *
 * Implementação própria, pequena e sem dependência nova: a fórmula é curta
 * o bastante (luminância relativa + razão) para não justificar puxar uma
 * biblioteca externa (`wcag-contrast` ou similar) só para isto — menor
 * superfície de dependência (ADR-011, Decision Drivers).
 *
 * Referência: https://www.w3.org/TR/WCAG21/#contrast-minimum
 */

export type WcagLevel = "AA-normal" | "AA-ui" | "AAA";

/**
 * Razão mínima exigida por nível, conforme WCAG 2.2 critério 1.4.3/1.4.11
 * (AA-normal/AA-ui) e 1.4.6 (AAA — usado só pelo tema exclusivo do Modo
 * Apresentação, TASK-024, que mira contraste reforçado por ser lido de
 * longe/em ambientes variados de luz).
 */
export const WCAG_MINIMUM_RATIO: Record<WcagLevel, number> = {
  "AA-normal": 4.5,
  "AA-ui": 3,
  AAA: 7,
};

const HEX_COLOR_PATTERN = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/;

export interface RgbColor {
  r: number;
  g: number;
  b: number;
}

/** Converte uma cor hex (`#fff` ou `#ffffff`) em componentes RGB (0–255). */
export function hexToRgb(hex: string): RgbColor {
  if (!HEX_COLOR_PATTERN.test(hex)) {
    throw new Error(`Cor hex inválida: "${hex}"`);
  }

  const normalized =
    hex.length === 4
      ? `#${hex[1]}${hex[1]}${hex[2]}${hex[2]}${hex[3]}${hex[3]}`
      : hex;

  const value = Number.parseInt(normalized.slice(1), 16);

  return {
    r: (value >> 16) & 0xff,
    g: (value >> 8) & 0xff,
    b: value & 0xff,
  };
}

function toLinearChannel(channel: number): number {
  const normalized = channel / 255;
  return normalized <= 0.03928
    ? normalized / 12.92
    : Math.pow((normalized + 0.055) / 1.055, 2.4);
}

/** Luminância relativa de uma cor (0 = preto, 1 = branco), fórmula WCAG. */
export function relativeLuminance(color: RgbColor): number {
  const r = toLinearChannel(color.r);
  const g = toLinearChannel(color.g);
  const b = toLinearChannel(color.b);
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/** Razão de contraste entre duas cores hex, sempre >= 1. */
export function contrastRatio(hexA: string, hexB: string): number {
  const luminanceA = relativeLuminance(hexToRgb(hexA));
  const luminanceB = relativeLuminance(hexToRgb(hexB));
  const [lighter, darker] =
    luminanceA >= luminanceB ? [luminanceA, luminanceB] : [luminanceB, luminanceA];
  return (lighter + 0.05) / (darker + 0.05);
}

/** Confere se a razão de contraste de um par atinge o alvo do nível WCAG. */
export function meetsWcagLevel(hexA: string, hexB: string, level: WcagLevel): boolean {
  return contrastRatio(hexA, hexB) >= WCAG_MINIMUM_RATIO[level];
}

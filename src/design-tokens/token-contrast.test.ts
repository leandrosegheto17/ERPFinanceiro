import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { contrastRatio, WCAG_MINIMUM_RATIO, type WcagLevel } from "./contrast";
import { parseTokensCss } from "./parse-tokens-css";

/**
 * TASK-023 — critério de aceite: "Teste de contraste passa para todos os
 * pares de token".
 *
 * Este teste é a verificação viva de UX-SPEC §3.1 ("o arquivo de tokens
 * registra o par e a razão calculada ao lado de cada combinação, e um
 * teste automatizado falha se qualquer par cair abaixo do alvo do seu
 * tema") e de DI-14 (contraste verificado por teste, nunca por inspeção
 * manual isolada).
 *
 * Não há hardcode de hex aqui: os valores vêm do parse do `tokens.css`
 * real, então uma mudança de cor no arquivo de tokens é recalculada aqui
 * automaticamente, sem precisar atualizar um segundo lugar.
 */

const tokensCssPath = fileURLToPath(new URL("./tokens.css", import.meta.url));

describe("TASK-023 — contraste AA dos tokens de cor (tema claro/escuro)", () => {
  const parsed = parseTokensCss(tokensCssPath);

  it("declara ao menos um par de contraste por tema", () => {
    const themes = new Set(parsed.declaredPairs.map((pair) => pair.theme));
    expect(themes).toEqual(new Set(["light", "dark"]));
    expect(parsed.declaredPairs.length).toBeGreaterThan(0);
  });

  it("os níveis declarados usam a razão mínima oficial do nível WCAG", () => {
    for (const pair of parsed.declaredPairs) {
      expect(pair.minimumRatio).toBe(WCAG_MINIMUM_RATIO[pair.level]);
    }
  });

  it.each(
    // Gera um caso de teste por par declarado no tokens.css, nos dois temas —
    // é aqui que o critério de aceite é verificado par a par.
    (() => {
      const cases: Array<{
        theme: "light" | "dark";
        textToken: string;
        bgToken: string;
        level: WcagLevel;
        minimumRatio: number;
      }> = [];
      for (const pair of parsed.declaredPairs) {
        cases.push(pair);
      }
      return cases;
    })(),
  )(
    "$theme: $textToken sobre $bgToken atinge $level (>=$minimumRatio:1)",
    ({ theme, textToken, bgToken, level, minimumRatio }) => {
      const colors = parsed.colorsByTheme[theme];
      const textHex = colors[textToken];
      const bgHex = colors[bgToken];

      expect(textHex, `token ${textToken} não encontrado no tema ${theme}`).toBeDefined();
      expect(bgHex, `token ${bgToken} não encontrado no tema ${theme}`).toBeDefined();

      const ratio = contrastRatio(textHex, bgHex);

      expect(ratio).toBeGreaterThanOrEqual(minimumRatio);
      expect(ratio).toBeGreaterThanOrEqual(WCAG_MINIMUM_RATIO[level]);
    },
  );

  it("o fallback de prefers-color-scheme não diverge do tema escuro explícito", () => {
    expect(parsed.darkFallbackColors).toEqual(parsed.colorsByTheme.dark);
  });
});

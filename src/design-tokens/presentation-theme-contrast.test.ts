import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { contrastRatio, WCAG_MINIMUM_RATIO } from "./contrast";
import { parseSingleThemeTokensCss } from "./parse-tokens-css";

/**
 * TASK-024 — critério de aceite: "Teste falha se qualquer par cair abaixo
 * de 7:1".
 *
 * Assim como `token-contrast.test.ts` (TASK-023), este teste não confia nos
 * valores documentados em UX-SPEC.md §3.1 — ele parseia
 * `presentation-theme.css` de verdade e recalcula a razão de contraste de
 * cada par anotado `CONTRAST-PAIR`, reaproveitando `contrastRatio` de
 * `contrast.ts` e o parser generalizado de `parse-tokens-css.ts`.
 */

const presentationThemeCssPath = fileURLToPath(
  new URL("./presentation-theme.css", import.meta.url),
);

describe("TASK-024 — contraste AAA do tema exclusivo de apresentação", () => {
  const parsed = parseSingleThemeTokensCss(
    presentationThemeCssPath,
    /\[data-theme="apresentacao"\]\s*\{/,
  );

  it("declara ao menos um par de contraste", () => {
    expect(parsed.declaredPairs.length).toBeGreaterThan(0);
  });

  it("todo par declarado usa o nível AAA com a razão mínima oficial (7:1)", () => {
    for (const pair of parsed.declaredPairs) {
      expect(pair.level).toBe("AAA");
      expect(pair.minimumRatio).toBe(WCAG_MINIMUM_RATIO.AAA);
    }
  });

  it.each(
    (() => {
      const cases: Array<{
        theme: string;
        textToken: string;
        bgToken: string;
        level: "AAA";
        minimumRatio: number;
      }> = [];
      for (const pair of parsed.declaredPairs) {
        cases.push(pair as (typeof cases)[number]);
      }
      return cases;
    })(),
  )(
    "$textToken sobre $bgToken atinge $level (>=$minimumRatio:1)",
    ({ textToken, bgToken, minimumRatio }) => {
      const textHex = parsed.colors[textToken];
      const bgHex = parsed.colors[bgToken];

      expect(textHex, `token ${textToken} não encontrado`).toBeDefined();
      expect(bgHex, `token ${bgToken} não encontrado`).toBeDefined();

      const ratio = contrastRatio(textHex, bgHex);

      // Critério de aceite literal: falha se qualquer par cair abaixo de 7:1.
      expect(ratio).toBeGreaterThanOrEqual(7);
      expect(ratio).toBeGreaterThanOrEqual(minimumRatio);
    },
  );

  it("expõe exatamente os 4 tokens literais definidos em UX-SPEC.md §3.1", () => {
    expect(parsed.colors).toEqual({
      "--apres-fundo": "#0b0b0f",
      "--apres-texto": "#f2f2f7",
      "--apres-secundario": "#a8a8b3",
      "--apres-acento": "#f0b429",
    });
  });
});

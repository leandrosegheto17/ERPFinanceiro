/**
 * TASK-025 — estilo compartilhado de alvo de toque mínimo (DI-08, UX-SPEC
 * §3.1): 44×44 px em toda a interface.
 *
 * Usa o token `--touch-target-min` (`src/design-tokens/tokens.css`,
 * TASK-023 — `2.75rem`, 44px na base de 16px) em vez de um valor duplicado
 * aqui — única fonte de verdade do tamanho continua sendo o token.
 *
 * Aplicado como `style` inline (não classe de CSS Module) deliberadamente:
 * o projeto não tem motor de layout real disponível em teste (jsdom não
 * calcula caixa/`getBoundingClientRect`, ver ADR-011/RNF-04 e o padrão já
 * usado por `src/design-tokens/parse-tokens-css.ts`), então o critério de
 * aceite "alvo de toque mínimo 44×44" é verificado na string do próprio
 * `element.style` logo após a renderização — determinístico, sem depender
 * de layout real — combinado com o teste de token já existente
 * (`token-contrast.test.ts`/`theme.test.ts`, TASK-023) que confirma que
 * `--touch-target-min` de fato resolve para 44px.
 */
import type { CSSProperties } from "react";

export const TOUCH_TARGET_MIN_STYLE: CSSProperties = {
  minWidth: "var(--touch-target-min)",
  minHeight: "var(--touch-target-min)",
};

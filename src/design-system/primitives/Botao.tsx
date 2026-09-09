/**
 * TASK-025 — `Botao`: primitivo (UX-SPEC §3.2), comportamento convencional.
 *
 * É um `<button>` HTML nativo — Tab foca, Enter e Espaço ativam sem
 * nenhum handler de teclado escrito à mão (comportamento de plataforma,
 * não reimplementado). `type="button"` por padrão para nunca submeter um
 * formulário por acidente quando usado dentro de um.
 */
import { forwardRef } from "react";
import type { ButtonHTMLAttributes } from "react";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";
import styles from "./Botao.module.css";

export type BotaoVariante = "primario" | "secundario" | "perigo";

export interface BotaoProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: BotaoVariante;
}

export const Botao = forwardRef<HTMLButtonElement, BotaoProps>(function Botao(
  { variante = "primario", className, style, type = "button", ...rest },
  ref,
) {
  const variantClass = variante === "primario" ? styles.primario : variante === "secundario" ? styles.secundario : styles.perigo;

  return (
    <button
      ref={ref}
      type={type}
      className={[styles.botao, variantClass, className].filter(Boolean).join(" ")}
      style={{ ...TOUCH_TARGET_MIN_STYLE, ...style }}
      {...rest}
    />
  );
});

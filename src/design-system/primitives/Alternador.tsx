/**
 * TASK-025 — `Alternador`: primitivo (UX-SPEC §3.2, T-11 Configurações).
 *
 * `<button role="switch" aria-checked>` — Tab foca, Enter e Espaço ativam
 * (comportamento nativo de `<button>`, sem handler de teclado escrito à
 * mão). Estado (ligado/desligado) nunca é transmitido só por cor (DI-07):
 * o rótulo textual acompanha sempre o controle, e `aria-checked` comunica o
 * estado a tecnologia assistiva independente de cor.
 */
import { forwardRef } from "react";
import type { ButtonHTMLAttributes } from "react";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";
import styles from "./Alternador.module.css";

export interface AlternadorProps
  extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, "onChange" | "type"> {
  rotulo: string;
  ligado: boolean;
  onChange: (ligado: boolean) => void;
}

export const Alternador = forwardRef<HTMLButtonElement, AlternadorProps>(
  function Alternador({ rotulo, ligado, onChange, style, className, ...rest }, ref) {
    return (
      <button
        ref={ref}
        type="button"
        role="switch"
        aria-checked={ligado}
        className={[styles.alternador, ligado ? styles.ligado : "", className]
          .filter(Boolean)
          .join(" ")}
        style={{ ...TOUCH_TARGET_MIN_STYLE, ...style }}
        onClick={() => onChange(!ligado)}
        {...rest}
      >
        <span className={styles.trilho} aria-hidden="true">
          <span className={styles.polegar} />
        </span>
        <span className={styles.rotulo}>{rotulo}</span>
      </button>
    );
  },
);

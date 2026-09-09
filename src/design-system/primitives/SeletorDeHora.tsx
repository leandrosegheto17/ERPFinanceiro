/**
 * TASK-025 — `SeletorDeHora`: primitivo (UX-SPEC §3.2, T-12 preferência de
 * horário do lembrete).
 *
 * `<input type="time">` nativo: teclado (Tab foca, dígitos/setas ajustam o
 * valor) e alvo de toque vêm da plataforma, sem widget customizado. Rótulo
 * sempre visível. Erro nunca é transmitido só por cor (DI-07).
 */
import { forwardRef, useId } from "react";
import type { InputHTMLAttributes } from "react";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";
import styles from "./SeletorDeHora.module.css";

export interface SeletorDeHoraProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, "id" | "type"> {
  rotulo: string;
  erro?: string;
  id?: string;
}

export const SeletorDeHora = forwardRef<HTMLInputElement, SeletorDeHoraProps>(
  function SeletorDeHora({ rotulo, erro, id, style, className, ...rest }, ref) {
    const generatedId = useId();
    const inputId = id ?? generatedId;
    const erroId = erro ? `${inputId}-erro` : undefined;

    return (
      <div className={styles.campo}>
        <label htmlFor={inputId} className={styles.rotulo}>
          {rotulo}
        </label>
        <input
          ref={ref}
          id={inputId}
          type="time"
          className={[styles.input, className].filter(Boolean).join(" ")}
          style={{ ...TOUCH_TARGET_MIN_STYLE, ...style }}
          aria-invalid={erro ? true : undefined}
          aria-describedby={erroId}
          {...rest}
        />
        {erro && (
          <p id={erroId} className={styles.erro} role="alert">
            <span aria-hidden="true" className={styles.erroIcone}>
              ⚠
            </span>
            <span>{erro}</span>
          </p>
        )}
      </div>
    );
  },
);

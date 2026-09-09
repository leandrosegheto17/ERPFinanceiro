/**
 * TASK-025 — `CampoDeTexto`: primitivo (UX-SPEC §3.2, T-08/T-09/T-12).
 *
 * Rótulo sempre visível (nunca só placeholder — UX-SPEC §3.2). Erro nunca é
 * transmitido só por cor (DI-07): o texto de erro sempre vem com ícone **e**
 * texto, associado ao campo por `aria-describedby` e anunciado por
 * `role="alert"`.
 *
 * É um `<input>` HTML nativo — Tab foca e a digitação funciona sem nenhum
 * handler de teclado escrito à mão.
 */
import { forwardRef, useId } from "react";
import type { InputHTMLAttributes } from "react";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";
import styles from "./CampoDeTexto.module.css";

export interface CampoDeTextoProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, "id"> {
  /** Rótulo visível do campo — nunca substituído por placeholder. */
  rotulo: string;
  /** Mensagem de erro. Quando presente, some o texto de ajuda. */
  erro?: string;
  textoDeAjuda?: string;
  id?: string;
}

export const CampoDeTexto = forwardRef<HTMLInputElement, CampoDeTextoProps>(
  function CampoDeTexto(
    { rotulo, erro, textoDeAjuda, id, style, className, ...rest },
    ref,
  ) {
    const generatedId = useId();
    const inputId = id ?? generatedId;
    const erroId = erro ? `${inputId}-erro` : undefined;
    const ajudaId = textoDeAjuda && !erro ? `${inputId}-ajuda` : undefined;
    const describedBy = [ajudaId, erroId].filter(Boolean).join(" ") || undefined;

    return (
      <div className={styles.campo}>
        <label htmlFor={inputId} className={styles.rotulo}>
          {rotulo}
        </label>
        <input
          ref={ref}
          id={inputId}
          className={[styles.input, className].filter(Boolean).join(" ")}
          style={{ ...TOUCH_TARGET_MIN_STYLE, ...style }}
          aria-invalid={erro ? true : undefined}
          aria-describedby={describedBy}
          {...rest}
        />
        {ajudaId && (
          <p id={ajudaId} className={styles.ajuda}>
            {textoDeAjuda}
          </p>
        )}
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

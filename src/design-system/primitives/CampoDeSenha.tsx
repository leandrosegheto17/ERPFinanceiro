/**
 * TASK-025 — `CampoDeSenha`: primitivo (UX-SPEC §3.2, T-08/T-09).
 *
 * Diferenças específicas exigidas pelo UX-SPEC: senha **colável** (nenhum
 * bloqueio de `paste`/`copy` — o padrão antigo de proibir colar senha
 * piora a segurança, forçando senha mais fraca/reuso, e não é replicado
 * aqui) e rótulo sempre visível (nunca só placeholder).
 *
 * Botão de mostrar/ocultar é um `<button type="button">` nativo — Tab foca,
 * Enter e Espaço ativam sem handler de teclado escrito à mão. Erro nunca é
 * transmitido só por cor (DI-07): ícone **e** texto, mesmo padrão de
 * `CampoDeTexto`.
 */
import { forwardRef, useId, useState } from "react";
import type { InputHTMLAttributes } from "react";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";
import styles from "./CampoDeSenha.module.css";

export interface CampoDeSenhaProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, "id" | "type"> {
  rotulo: string;
  erro?: string;
  textoDeAjuda?: string;
  id?: string;
}

export const CampoDeSenha = forwardRef<HTMLInputElement, CampoDeSenhaProps>(
  function CampoDeSenha(
    { rotulo, erro, textoDeAjuda, id, style, className, ...rest },
    ref,
  ) {
    const generatedId = useId();
    const inputId = id ?? generatedId;
    const erroId = erro ? `${inputId}-erro` : undefined;
    const ajudaId = textoDeAjuda && !erro ? `${inputId}-ajuda` : undefined;
    const describedBy = [ajudaId, erroId].filter(Boolean).join(" ") || undefined;
    const [visivel, setVisivel] = useState(false);

    return (
      <div className={styles.campo}>
        <label htmlFor={inputId} className={styles.rotulo}>
          {rotulo}
        </label>
        <div className={styles.wrapper}>
          <input
            ref={ref}
            id={inputId}
            type={visivel ? "text" : "password"}
            className={[styles.input, className].filter(Boolean).join(" ")}
            style={{ ...TOUCH_TARGET_MIN_STYLE, ...style }}
            aria-invalid={erro ? true : undefined}
            aria-describedby={describedBy}
            // Deliberadamente sem onPaste/onCopy bloqueado: senha colável
            // (UX-SPEC §3.2).
            {...rest}
          />
          <button
            type="button"
            className={styles.alternarVisibilidade}
            style={TOUCH_TARGET_MIN_STYLE}
            onClick={() => setVisivel((atual) => !atual)}
            aria-pressed={visivel}
            aria-label={visivel ? `Ocultar ${rotulo}` : `Mostrar ${rotulo}`}
          >
            <span aria-hidden="true">{visivel ? "🙈" : "👁"}</span>
          </button>
        </div>
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

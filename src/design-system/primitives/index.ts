/**
 * TASK-025 — barrel dos primitivos de formulário do design system
 * (`Botao`, `CampoDeTexto`, `CampoDeSenha`, `SeletorDeHora`, `Alternador`).
 *
 * Escopo desta pasta: só os primitivos de formulário (UX-SPEC §3.2). Os
 * primitivos de sobreposição (`FolhaInferior`/`Dialogo`/`Toast`, TASK-026,
 * base Radix) vivem em outro diretório de `src/design-system/`, para as
 * duas tarefas paralelas do Lote 4 não escreverem no mesmo arquivo.
 */
export { Botao } from "./Botao";
export type { BotaoProps, BotaoVariante } from "./Botao";

export { CampoDeTexto } from "./CampoDeTexto";
export type { CampoDeTextoProps } from "./CampoDeTexto";

export { CampoDeSenha } from "./CampoDeSenha";
export type { CampoDeSenhaProps } from "./CampoDeSenha";

export { SeletorDeHora } from "./SeletorDeHora";
export type { SeletorDeHoraProps } from "./SeletorDeHora";

export { Alternador } from "./Alternador";
export type { AlternadorProps } from "./Alternador";

export { TOUCH_TARGET_MIN_STYLE } from "./touch-target";

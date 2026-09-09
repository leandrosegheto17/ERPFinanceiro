/**
 * TASK-040 — palavra exigida pelo UX-SPEC T-13 ("confirmação digitando a
 * palavra 'EXCLUIR'") para habilitar o botão de exclusão de conta em
 * `BlocoExcluirConta`.
 *
 * Vive em módulo próprio, sem nenhum import de React/CSS module, para que
 * `e2e/identity-erasure.spec.ts` (que roda fora de qualquer bundler, via
 * Playwright puro) consiga importar só a constante sem arrastar
 * `Botao.module.css`/`CampoDeTexto.module.css` (que o `.tsx` do componente
 * importa e que o runtime Node do Playwright não sabe interpretar).
 */
export const PALAVRA_DE_CONFIRMACAO_EXCLUSAO = "EXCLUIR";

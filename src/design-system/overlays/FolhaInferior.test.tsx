// @vitest-environment jsdom
/**
 * TASK-026 — teste de armadilha de foco e restauração de foco da
 * `FolhaInferior`. Mesma base Radix Dialog de `Dialogo.tsx`, mesmo tipo de
 * prova (ver comentário de decisão em `FolhaInferior.tsx`).
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { FolhaInferior } from "./FolhaInferior";

afterEach(cleanup);

function renderFolha() {
  return render(
    <FolhaInferior
      trigger={<button type="button">Abrir folha</button>}
      titulo="Concluir dia"
      descricao="Crie uma conta para guardar seu progresso entre aparelhos."
    >
      <button type="button">Criar conta</button>
      <button type="button">Agora não</button>
    </FolhaInferior>,
  );
}

describe("FolhaInferior", () => {
  it("prende o foco dentro do overlay enquanto aberta (Tab não escapa)", async () => {
    const user = userEvent.setup();
    renderFolha();

    await user.click(screen.getByRole("button", { name: "Abrir folha" }));
    const folha = await screen.findByRole("dialog");
    expect(folha).toHaveAttribute("aria-modal", "true");

    for (let i = 0; i < 8; i++) {
      await user.tab();
      expect(folha.contains(document.activeElement)).toBe(true);
    }
  });

  it("restaura o foco no elemento que abriu a folha, ao fechar com Esc", async () => {
    const user = userEvent.setup();
    renderFolha();

    const gatilho = screen.getByRole("button", { name: "Abrir folha" });
    await user.click(gatilho);
    await screen.findByRole("dialog");

    await user.keyboard("{Escape}");

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(gatilho).toHaveFocus();
  });

  it("restaura o foco no elemento que abriu a folha, ao fechar pelo botão Fechar", async () => {
    const user = userEvent.setup();
    renderFolha();

    const gatilho = screen.getByRole("button", { name: "Abrir folha" });
    await user.click(gatilho);
    await user.click(screen.getByRole("button", { name: "Fechar" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(gatilho).toHaveFocus();
  });
});

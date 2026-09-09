// @vitest-environment jsdom
/**
 * TASK-026 — teste de armadilha de foco e restauração de foco do `Dialogo`.
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Dialogo } from "./Dialogo";

afterEach(cleanup);

function renderDialogo() {
  return render(
    <Dialogo
      trigger={<button type="button">Abrir diálogo</button>}
      titulo="Convite de conta"
      descricao="Crie uma conta para não perder seu progresso."
    >
      <button type="button">Primeira ação</button>
      <button type="button">Segunda ação</button>
    </Dialogo>,
  );
}

describe("Dialogo", () => {
  it("prende o foco dentro do overlay enquanto aberto (Tab não escapa)", async () => {
    const user = userEvent.setup();
    renderDialogo();

    await user.click(screen.getByRole("button", { name: "Abrir diálogo" }));
    const dialogo = await screen.findByRole("dialog");
    expect(dialogo).toHaveAttribute("aria-modal", "true");

    // Tabula mais vezes do que o número de elementos focáveis dentro do
    // diálogo (3: "Primeira ação", "Segunda ação", botão fechar) — se
    // escapasse, algum desses Tabs levaria o foco para fora do overlay
    // (ex.: de volta ao botão "Abrir diálogo", que fica fora do overlay).
    for (let i = 0; i < 8; i++) {
      await user.tab();
      expect(dialogo.contains(document.activeElement)).toBe(true);
    }
  });

  it("restaura o foco no elemento que abriu o diálogo, ao fechar com Esc", async () => {
    const user = userEvent.setup();
    renderDialogo();

    const gatilho = screen.getByRole("button", { name: "Abrir diálogo" });
    await user.click(gatilho);
    await screen.findByRole("dialog");

    await user.keyboard("{Escape}");

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(gatilho).toHaveFocus();
  });

  it("restaura o foco no elemento que abriu o diálogo, ao fechar pelo botão Fechar", async () => {
    const user = userEvent.setup();
    renderDialogo();

    const gatilho = screen.getByRole("button", { name: "Abrir diálogo" });
    await user.click(gatilho);
    await user.click(screen.getByRole("button", { name: "Fechar" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(gatilho).toHaveFocus();
  });
});

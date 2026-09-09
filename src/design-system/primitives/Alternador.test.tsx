// @vitest-environment jsdom
/**
 * TASK-025 — teste de teclado e alvo de toque do primitivo `Alternador`.
 */
import { useState } from "react";
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Alternador } from "./Alternador";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";

afterEach(cleanup);

function AlternadorControlado({ inicial = false }: { inicial?: boolean }) {
  const [ligado, setLigado] = useState(inicial);
  return <Alternador rotulo="Lembrete diário" ligado={ligado} onChange={setLigado} />;
}

describe("Alternador", () => {
  it("recebe foco via Tab e alterna via Espaço", async () => {
    const user = userEvent.setup();
    render(<AlternadorControlado />);

    await user.tab();
    const alternador = screen.getByRole("switch", { name: "Lembrete diário" });
    expect(alternador).toHaveFocus();
    expect(alternador).toHaveAttribute("aria-checked", "false");

    await user.keyboard(" ");
    expect(alternador).toHaveAttribute("aria-checked", "true");
  });

  it("alterna via Enter", async () => {
    const user = userEvent.setup();
    render(<AlternadorControlado />);

    await user.tab();
    await user.keyboard("{Enter}");
    expect(screen.getByRole("switch", { name: "Lembrete diário" })).toHaveAttribute(
      "aria-checked",
      "true",
    );
  });

  it("chama onChange com o novo valor ao clicar", async () => {
    const user = userEvent.setup();
    const aoMudar = vi.fn();
    render(<Alternador rotulo="Lembrete diário" ligado={false} onChange={aoMudar} />);

    await user.click(screen.getByRole("switch", { name: "Lembrete diário" }));
    expect(aoMudar).toHaveBeenCalledWith(true);
  });

  it("comunica o estado por aria-checked e por rótulo textual, não só por cor (DI-07)", () => {
    render(<Alternador rotulo="Lembrete diário" ligado onChange={() => {}} />);
    const alternador = screen.getByRole("switch", { name: "Lembrete diário" });
    expect(alternador).toHaveAttribute("aria-checked", "true");
    expect(alternador).toHaveTextContent("Lembrete diário");
  });

  it("tem alvo de toque mínimo 44x44 (--touch-target-min)", () => {
    render(<Alternador rotulo="Lembrete diário" ligado={false} onChange={() => {}} />);
    const alternador = screen.getByRole("switch", { name: "Lembrete diário" });
    expect(alternador.style.minWidth).toBe(TOUCH_TARGET_MIN_STYLE.minWidth);
    expect(alternador.style.minHeight).toBe(TOUCH_TARGET_MIN_STYLE.minHeight);
  });
});

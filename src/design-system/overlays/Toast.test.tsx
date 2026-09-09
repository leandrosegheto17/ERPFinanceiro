// @vitest-environment jsdom
/**
 * TASK-026 — testes do `Toast`. Como documentado em `Toast.tsx`, Toast não
 * é modal: não há armadilha de foco a provar (o oposto — provamos que NÃO
 * prende o foco), e a restauração de foco só se aplica quando o usuário de
 * fato moveu o foco para dentro do toast antes de fechá-lo.
 */
import "@testing-library/jest-dom/vitest";
import { useState } from "react";
import { afterEach, beforeAll, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Toast, ToastProvider } from "./Toast";

// jsdom não implementa a Pointer Events API (usada pelo gesto de swipe do
// Radix Toast) — sem isto, `element.hasPointerCapture` não existe e o
// primeiro pointerdown simulado por `userEvent` lança exceção não tratada.
// Necessário só para o ambiente de teste; nenhum código de produção depende
// disto (navegadores reais implementam a API nativamente).
beforeAll(() => {
  if (!Element.prototype.hasPointerCapture) {
    Element.prototype.hasPointerCapture = () => false;
  }
  if (!Element.prototype.setPointerCapture) {
    Element.prototype.setPointerCapture = () => {};
  }
  if (!Element.prototype.releasePointerCapture) {
    Element.prototype.releasePointerCapture = () => {};
  }
});

afterEach(cleanup);

function Cenario() {
  const [open, setOpen] = useState(false);
  return (
    <div>
      <button type="button" onClick={() => setOpen(true)}>
        Salvar
      </button>
      <ToastProvider duration={100000}>
        <Toast
          open={open}
          onOpenChange={setOpen}
          titulo="Salvo"
          descricao="Suas alterações foram salvas."
          tipo="sucesso"
          acaoRotulo="Desfazer"
          onAcao={() => {}}
        />
      </ToastProvider>
      <button type="button">Depois do provedor de toast</button>
    </div>
  );
}

describe("Toast", () => {
  it("não move o foco automaticamente ao abrir — não é modal (decisão documentada)", async () => {
    const user = userEvent.setup();
    render(<Cenario />);

    const gatilho = screen.getByRole("button", { name: "Salvar" });
    await user.click(gatilho);

    await screen.findByText("Salvo");
    expect(gatilho).toHaveFocus();
  });

  it("não prende o Tab dentro do toast: a partir do último controle do toast, Tab alcança um elemento fora dele", async () => {
    const user = userEvent.setup();
    render(<Cenario />);

    await user.click(screen.getByRole("button", { name: "Salvar" }));
    const fechar = await screen.findByRole("button", { name: "Fechar notificação" });

    fechar.focus();
    expect(fechar).toHaveFocus();

    await user.tab();

    expect(fechar).not.toHaveFocus();
    expect(screen.getByRole("button", { name: "Depois do provedor de toast" })).toHaveFocus();
  });

  it("restaura o foco no elemento que abriu o toast, ao fechar a partir de dentro dele", async () => {
    const user = userEvent.setup();
    render(<Cenario />);

    const gatilho = screen.getByRole("button", { name: "Salvar" });
    await user.click(gatilho);

    const fechar = await screen.findByRole("button", { name: "Fechar notificação" });
    fechar.focus();
    await user.click(fechar);

    expect(screen.queryByText("Salvo")).not.toBeInTheDocument();
    expect(gatilho).toHaveFocus();
  });

  it("DI-07: o tipo do toast é comunicado por ícone e texto, não só cor (ícone é decorativo/aria-hidden)", async () => {
    const user = userEvent.setup();
    render(<Cenario />);

    await user.click(screen.getByRole("button", { name: "Salvar" }));
    const titulo = await screen.findByText("Salvo");

    // O ícone é puramente decorativo (aria-hidden) — quem carrega o
    // significado para tecnologia assistiva é o texto do título/descrição.
    const raizDoToast = titulo.closest(".eb-toast-root");
    expect(raizDoToast).not.toBeNull();
    const icone = raizDoToast!.querySelector(".eb-toast-icone");
    expect(icone).toHaveAttribute("aria-hidden", "true");
    expect(screen.getByText("Suas alterações foram salvas.")).toBeInTheDocument();
  });
});

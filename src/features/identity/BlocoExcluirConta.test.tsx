// @vitest-environment jsdom
/**
 * TASK-040 — teste de `BlocoExcluirConta` cobrindo os 4 estados de UX-SPEC
 * §4 (DI-06: carregando/formulário ("vazio"=N/A)/erro/sucesso) e a
 * fricção deliberada de digitar "EXCLUIR" antes de habilitar o botão.
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import type { ErasureRequestGateway } from "./erasure-request-gateway";
import { BlocoExcluirConta } from "./BlocoExcluirConta";

afterEach(cleanup);

function gatewaySemPedidoExistente(): ErasureRequestGateway {
  const maybeSingle = vi.fn(async () => ({ data: null, error: null }));
  const eq = vi.fn(() => ({ maybeSingle }));
  const select = vi.fn(() => ({ eq }));
  const single = vi.fn(async () => ({
    data: { requested_at: "2026-09-08T18:04:32.000Z", due_at: "2026-09-23T18:04:32.000Z" },
    error: null,
  }));
  const insertSelect = vi.fn(() => ({ single }));
  const insert = vi.fn(() => ({ select: insertSelect }));
  const from = vi.fn(() => ({ select, insert }));
  return { from } as unknown as ErasureRequestGateway;
}

function gatewayComPedidoExistente(): ErasureRequestGateway {
  const maybeSingle = vi.fn(async () => ({
    data: { requested_at: "2026-09-01T12:00:00.000Z", due_at: "2026-09-16T12:00:00.000Z" },
    error: null,
  }));
  const eq = vi.fn(() => ({ maybeSingle }));
  const select = vi.fn(() => ({ eq }));
  const from = vi.fn(() => ({ select }));
  return { from } as unknown as ErasureRequestGateway;
}

function gatewayComErroAoVerificar(): ErasureRequestGateway {
  const maybeSingle = vi.fn(async () => ({ data: null, error: { message: "network" } }));
  const eq = vi.fn(() => ({ maybeSingle }));
  const select = vi.fn(() => ({ eq }));
  const from = vi.fn(() => ({ select }));
  return { from } as unknown as ErasureRequestGateway;
}

function gatewayComErroAoEnviar(): ErasureRequestGateway {
  const maybeSingle = vi.fn(async () => ({ data: null, error: null }));
  const eq = vi.fn(() => ({ maybeSingle }));
  const select = vi.fn(() => ({ eq }));
  const single = vi.fn(async () => ({ data: null, error: { message: "insert failed" } }));
  const insertSelect = vi.fn(() => ({ single }));
  const insert = vi.fn(() => ({ select: insertSelect }));
  const from = vi.fn(() => ({ select, insert }));
  return { from } as unknown as ErasureRequestGateway;
}

describe("BlocoExcluirConta (TASK-040)", () => {
  it("carregando: mostra estado de carregamento ao montar, antes de resolver o status", () => {
    render(<BlocoExcluirConta erasureGateway={gatewaySemPedidoExistente()} userId="user-1" />);
    expect(screen.getByRole("status")).toHaveTextContent("Carregando");
  });

  it("formulário: sem pedido existente, mostra o formulário com botão desabilitado até digitar EXCLUIR", async () => {
    render(<BlocoExcluirConta erasureGateway={gatewaySemPedidoExistente()} userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /excluir conta e dados/i })).toBeInTheDocument(),
    );
    const botao = screen.getByRole("button", { name: /excluir conta e dados/i });
    expect(botao).toBeDisabled();

    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/digite "EXCLUIR" para confirmar/i), "excluir");
    expect(botao).toBeDisabled();

    await user.clear(screen.getByLabelText(/digite "EXCLUIR" para confirmar/i));
    await user.type(screen.getByLabelText(/digite "EXCLUIR" para confirmar/i), "EXCLUIR");
    expect(botao).toBeEnabled();
  });

  it("sucesso: após confirmar EXCLUIR, mostra o pedido registrado com a data-limite do servidor", async () => {
    const user = userEvent.setup();
    render(<BlocoExcluirConta erasureGateway={gatewaySemPedidoExistente()} userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /excluir conta e dados/i })).toBeInTheDocument(),
    );
    await user.type(screen.getByLabelText(/digite "EXCLUIR" para confirmar/i), "EXCLUIR");
    await user.click(screen.getByRole("button", { name: /excluir conta e dados/i }));

    await waitFor(() =>
      expect(screen.getByText(/exclusão solicitada/i)).toBeInTheDocument(),
    );
    expect(screen.getByTestId("erasure-due-at")).toHaveTextContent("23 de setembro de 2026");
  });

  it("sucesso: se já existe um pedido ao montar, mostra o estado 'exclusão solicitada' direto (sem exigir novo envio)", async () => {
    render(<BlocoExcluirConta erasureGateway={gatewayComPedidoExistente()} userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByText(/exclusão solicitada/i)).toBeInTheDocument(),
    );
    expect(screen.getByTestId("erasure-due-at")).toHaveTextContent("16 de setembro de 2026");
  });

  it("erro: falha ao verificar o status inicial mostra mensagem com ícone e texto (nunca só cor)", async () => {
    render(<BlocoExcluirConta erasureGateway={gatewayComErroAoVerificar()} userId="user-1" />);

    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
    expect(screen.getByRole("alert")).toHaveTextContent(/não foi possível verificar/i);
  });

  it("erro: falha ao enviar o pedido mantém o formulário disponível para tentar de novo", async () => {
    const user = userEvent.setup();
    render(<BlocoExcluirConta erasureGateway={gatewayComErroAoEnviar()} userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /excluir conta e dados/i })).toBeInTheDocument(),
    );
    await user.type(screen.getByLabelText(/digite "EXCLUIR" para confirmar/i), "EXCLUIR");
    await user.click(screen.getByRole("button", { name: /excluir conta e dados/i }));

    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
    expect(screen.getByRole("alert")).toHaveTextContent(/pedido continua pendente/i);
  });
});

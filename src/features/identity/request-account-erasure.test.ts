import { describe, expect, it, vi } from "vitest";

import type { ErasureRequestGateway } from "./erasure-request-gateway";
import { requestAccountErasure } from "./request-account-erasure";

function fakeGateway(result: {
  data: { requested_at: string; due_at: string } | null;
  error: { message: string } | null;
}) {
  const single = vi.fn(async () => result);
  const select = vi.fn(() => ({ single }));
  const insert = vi.fn(() => ({ select }));
  const from = vi.fn(() => ({ insert }));
  return { gateway: { from } as unknown as ErasureRequestGateway, insert, from, select };
}

describe("requestAccountErasure (TASK-040)", () => {
  it("insere em public.erasure_request só com user_id, nunca due_at", async () => {
    const { gateway, insert, from } = fakeGateway({
      data: { requested_at: "2026-09-08T18:04:32.000Z", due_at: "2026-09-23T18:04:32.000Z" },
      error: null,
    });

    const result = await requestAccountErasure(gateway, { userId: "user-1" });

    expect(from).toHaveBeenCalledWith("erasure_request");
    expect(insert).toHaveBeenCalledWith({ user_id: "user-1" });
    expect(insert).not.toHaveBeenCalledWith(
      expect.objectContaining({ due_at: expect.anything() }),
    );
    expect(result).toEqual({
      status: "requested",
      requestedAt: "2026-09-08T18:04:32.000Z",
      dueAt: "2026-09-23T18:04:32.000Z",
    });
  });

  it("retorna erro (sem lançar exceção) quando o insert falha (ex.: pedido duplicado)", async () => {
    const { gateway } = fakeGateway({
      data: null,
      error: { message: "duplicate key value violates unique constraint" },
    });

    const result = await requestAccountErasure(gateway, { userId: "user-2" });

    expect(result).toEqual({
      status: "error",
      message: "duplicate key value violates unique constraint",
    });
  });

  it("retorna erro quando o servidor não devolve dado (resposta vazia)", async () => {
    const { gateway } = fakeGateway({ data: null, error: null });

    const result = await requestAccountErasure(gateway, { userId: "user-3" });

    expect(result).toEqual({ status: "error", message: "Resposta vazia do servidor." });
  });

  it("o due_at calculado por 15 dias a partir de requested_at (contrato do servidor) chega intacto à UI", async () => {
    const requestedAt = new Date("2026-09-08T18:04:32.000Z");
    const dueAt = new Date(requestedAt.getTime() + 15 * 24 * 60 * 60 * 1000);
    const { gateway } = fakeGateway({
      data: { requested_at: requestedAt.toISOString(), due_at: dueAt.toISOString() },
      error: null,
    });

    const result = await requestAccountErasure(gateway, { userId: "user-4" });

    expect(result.status).toBe("requested");
    if (result.status === "requested") {
      const diffMs = new Date(result.dueAt).getTime() - new Date(result.requestedAt).getTime();
      expect(diffMs).toBe(15 * 24 * 60 * 60 * 1000);
    }
  });
});

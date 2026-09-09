import { describe, expect, it, vi } from "vitest";

import type { ErasureRequestGateway } from "./erasure-request-gateway";
import { getErasureRequestStatus } from "./get-erasure-request-status";

function fakeGateway(result: {
  data: { requested_at: string; due_at: string } | null;
  error: { message: string } | null;
}) {
  const maybeSingle = vi.fn(async () => result);
  const eq = vi.fn(() => ({ maybeSingle }));
  const select = vi.fn(() => ({ eq }));
  const from = vi.fn(() => ({ select }));
  return { gateway: { from } as unknown as ErasureRequestGateway, from, select, eq };
}

describe("getErasureRequestStatus (TASK-040)", () => {
  it("retorna status none quando o titular não tem pedido de exclusão", async () => {
    const { gateway, from, eq } = fakeGateway({ data: null, error: null });

    const result = await getErasureRequestStatus(gateway, "user-1");

    expect(from).toHaveBeenCalledWith("erasure_request");
    expect(eq).toHaveBeenCalledWith("user_id", "user-1");
    expect(result).toEqual({ status: "none" });
  });

  it("retorna requested com requestedAt/dueAt quando já existe um pedido", async () => {
    const { gateway } = fakeGateway({
      data: { requested_at: "2026-09-08T18:04:32.000Z", due_at: "2026-09-23T18:04:32.000Z" },
      error: null,
    });

    const result = await getErasureRequestStatus(gateway, "user-2");

    expect(result).toEqual({
      status: "requested",
      requestedAt: "2026-09-08T18:04:32.000Z",
      dueAt: "2026-09-23T18:04:32.000Z",
    });
  });

  it("retorna erro (sem lançar exceção) quando a consulta falha", async () => {
    const { gateway } = fakeGateway({ data: null, error: { message: "network error" } });

    const result = await getErasureRequestStatus(gateway, "user-3");

    expect(result).toEqual({ status: "error", message: "network error" });
  });
});

// @vitest-environment jsdom
/**
 * TASK-039 (Lote 6 — Identidade), Tela T-13 "Baixar meus dados" (CA-10.4).
 * `fake-indexeddb/auto` provê `indexedDB` para o Dexie real (`AppDatabase`)
 * funcionar em jsdom, mesmo padrão de `dexie-auth-storage.test.ts`.
 */
import "fake-indexeddb/auto";
import "@testing-library/jest-dom/vitest";

import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { AppDatabase } from "../../core/storage";

import { BlocoExportarDados } from "./BlocoExportarDados";

afterEach(cleanup);

describe("BlocoExportarDados (TASK-039)", () => {
  const openDatabases: AppDatabase[] = [];

  function open(): AppDatabase {
    const db = new AppDatabase(`bloco-exportar-dados-${crypto.randomUUID()}`);
    openDatabases.push(db);
    return db;
  }

  afterEach(async () => {
    while (openDatabases.length > 0) {
      const db = openDatabases.pop();
      db?.close();
      if (db) {
        await db.delete();
      }
    }
  });

  it("estado inicial: botão pronto, sem mensagem de erro/sucesso (UX-SPEC T-13 vazio = N/A)", async () => {
    const db = open();
    await db.open();
    render(<BlocoExportarDados db={db} />);

    expect(screen.getByRole("button", { name: "Exportar meus dados" })).toBeEnabled();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("carregando: desabilita o botão e mostra 'Preparando exportação…' enquanto coleta os dados", async () => {
    const db = open();
    await db.open();
    let resolveOnDownload: (() => void) | undefined;
    const onDownload = vi.fn(
      () =>
        new Promise<void>((resolve) => {
          resolveOnDownload = resolve;
        }),
    );

    const user = userEvent.setup();
    render(<BlocoExportarDados db={db} onDownload={onDownload} />);

    await user.click(screen.getByRole("button", { name: "Exportar meus dados" }));

    expect(await screen.findByRole("button", { name: "Preparando exportação…" })).toBeDisabled();
    resolveOnDownload?.();
  });

  it("sucesso: aciona onDownload com o payload real (progresso/preferências/esboços) e mostra 'Arquivo baixado'", async () => {
    const db = open();
    await db.open();
    await db.progress.add({
      planId: "plano-entrelacado-90",
      dayNumber: 1,
      completedAt: "2026-09-01T12:00:00.000Z",
    });
    await db.preferences.put({ key: "tema", value: "escuro", updatedAt: "2026-09-01T08:00:00.000Z" });

    const onDownload = vi.fn();
    const user = userEvent.setup();
    render(<BlocoExportarDados db={db} onDownload={onDownload} />);

    await user.click(screen.getByRole("button", { name: "Exportar meus dados" }));

    await waitFor(() => expect(onDownload).toHaveBeenCalledTimes(1));
    const [data, filename] = onDownload.mock.calls[0] as [Record<string, unknown>, string];
    expect(filename).toMatch(/^meus-dados-estudobiblico-\d{4}-\d{2}-\d{2}\.json$/);
    expect(data.progresso).toEqual([
      { planId: "plano-entrelacado-90", dayNumber: 1, completedAt: "2026-09-01T12:00:00.000Z" },
    ]);
    expect(data.preferencias).toEqual([
      { key: "tema", value: "escuro", updatedAt: "2026-09-01T08:00:00.000Z" },
    ]);
    expect(data.esbocos).toEqual([]);

    expect(await screen.findByRole("status")).toHaveTextContent(/Arquivo baixado/);
    expect(screen.getByRole("button", { name: "Exportar meus dados" })).toBeEnabled();
  });

  it("erro: mostra mensagem acessível (role=alert) quando a coleta falha, sem travar o botão", async () => {
    const db = open();
    await db.open();
    await db.close(); // banco fechado força falha real de leitura, não simulada.

    const user = userEvent.setup();
    render(<BlocoExportarDados db={db} />);

    await user.click(screen.getByRole("button", { name: "Exportar meus dados" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/Não conseguimos preparar/);
    expect(screen.getByRole("button", { name: "Exportar meus dados" })).toBeEnabled();
  });
});

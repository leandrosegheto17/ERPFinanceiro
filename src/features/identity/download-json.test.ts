// @vitest-environment jsdom
/**
 * TASK-039 (Lote 6 — Identidade): `downloadJsonFile` precisa de DOM real
 * (`Blob`/`URL.createObjectURL`/`<a>`) — mesmo padrão de override de
 * ambiente já usado por `dexie-auth-storage.test.ts`.
 */
import { afterEach, describe, expect, it, vi } from "vitest";

import { buildExportFilename, downloadJsonFile } from "./download-json";

describe("downloadJsonFile (TASK-039, CA-10.4)", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("cria um Blob JSON, aciona o clique de download com o nome de arquivo certo e limpa a URL do objeto", () => {
    const createObjectURL = vi
      .spyOn(URL, "createObjectURL")
      .mockReturnValue("blob:mock-url");
    const revokeObjectURL = vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => {});

    let clickedAnchor: HTMLAnchorElement | undefined;
    const appendChildSpy = vi.spyOn(document.body, "appendChild");
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, "click")
      .mockImplementation(() => {
        clickedAnchor = appendChildSpy.mock.calls.at(-1)?.[0] as HTMLAnchorElement;
      });

    const data = { ola: "mundo" };
    downloadJsonFile(data, "meus-dados.json");

    expect(createObjectURL).toHaveBeenCalledTimes(1);
    const blobArg = createObjectURL.mock.calls[0]?.[0] as Blob;
    expect(blobArg.type).toBe("application/json");

    expect(clickSpy).toHaveBeenCalledTimes(1);
    expect(clickedAnchor?.download).toBe("meus-dados.json");
    expect(clickedAnchor?.href).toBe("blob:mock-url");
    // Não deixa o `<a>` sintético pendurado no DOM.
    expect(document.body.contains(clickedAnchor ?? null)).toBe(false);

    expect(revokeObjectURL).toHaveBeenCalledWith("blob:mock-url");
  });

  it("serializa o conteúdo real em JSON legível (indentado, chaves preservadas)", async () => {
    const createObjectURL = vi
      .spyOn(URL, "createObjectURL")
      .mockReturnValue("blob:mock-url");
    vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => {});
    vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => {});

    const payload = { progresso: [{ dayNumber: 1 }], preferencias: [], esbocos: [] };
    downloadJsonFile(payload, "arquivo.json");

    const blobArg = createObjectURL.mock.calls[0]?.[0] as Blob;
    const text = await blobArg.text();
    expect(JSON.parse(text)).toEqual(payload);
  });

  it("buildExportFilename embute a data (AAAA-MM-DD) no nome do arquivo", () => {
    const filename = buildExportFilename(new Date("2026-09-08T10:00:00.000Z"));
    expect(filename).toBe("meus-dados-estudobiblico-2026-09-08.json");
  });
});

import { describe, expect, it } from "vitest";
import { contrastRatio, hexToRgb, meetsWcagLevel, relativeLuminance } from "./contrast";

describe("hexToRgb", () => {
  it("converte hex de 6 dígitos", () => {
    expect(hexToRgb("#ffffff")).toEqual({ r: 255, g: 255, b: 255 });
    expect(hexToRgb("#000000")).toEqual({ r: 0, g: 0, b: 0 });
    expect(hexToRgb("#0b5fff")).toEqual({ r: 11, g: 95, b: 255 });
  });

  it("converte hex de 3 dígitos (forma curta)", () => {
    expect(hexToRgb("#fff")).toEqual({ r: 255, g: 255, b: 255 });
    expect(hexToRgb("#000")).toEqual({ r: 0, g: 0, b: 0 });
  });

  it("rejeita hex inválido", () => {
    expect(() => hexToRgb("not-a-color")).toThrow();
    expect(() => hexToRgb("#ff")).toThrow();
  });
});

describe("relativeLuminance", () => {
  it("branco tem luminância 1 e preto tem luminância 0", () => {
    expect(relativeLuminance(hexToRgb("#ffffff"))).toBeCloseTo(1, 5);
    expect(relativeLuminance(hexToRgb("#000000"))).toBeCloseTo(0, 5);
  });
});

describe("contrastRatio", () => {
  it("preto sobre branco é 21:1 (contraste máximo)", () => {
    expect(contrastRatio("#000000", "#ffffff")).toBeCloseTo(21, 1);
  });

  it("uma cor contra ela mesma é 1:1 (sem contraste)", () => {
    expect(contrastRatio("#0b5fff", "#0b5fff")).toBeCloseTo(1, 5);
  });

  it("é simétrica na ordem dos argumentos", () => {
    expect(contrastRatio("#1c1c1e", "#ffffff")).toBeCloseTo(
      contrastRatio("#ffffff", "#1c1c1e"),
      10,
    );
  });
});

describe("meetsWcagLevel", () => {
  it("aprova um par que atinge o alvo AA-normal (>=4.5:1)", () => {
    expect(meetsWcagLevel("#1c1c1e", "#ffffff", "AA-normal")).toBe(true);
  });

  it("reprova um par cinza-claro sobre branco abaixo de 4.5:1", () => {
    expect(meetsWcagLevel("#cccccc", "#ffffff", "AA-normal")).toBe(false);
  });

  it("aceita um par mais fraco sob o alvo AA-ui (>=3:1) que falharia em AA-normal", () => {
    // #949494 sobre #ffffff fica perto de 3.5:1: passa AA-ui, falha AA-normal.
    expect(meetsWcagLevel("#949494", "#ffffff", "AA-ui")).toBe(true);
    expect(meetsWcagLevel("#949494", "#ffffff", "AA-normal")).toBe(false);
  });
});

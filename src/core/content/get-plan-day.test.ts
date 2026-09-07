import { describe, expect, it } from "vitest";
import {
  buildContentIndex,
  getNoteFromIndex,
  getPericopeFromIndex,
  getPlanDay,
  getPlanDayFromIndex,
} from "./get-plan-day";
import type { ContentBundle } from "./types";

const fixtureBundle: ContentBundle = {
  plan: {
    id: "plano-90-dias",
    name: "Plano Entrelaçado de 90 dias",
    dayCount: 3,
    validationReport: { passed: true, checkedAt: "2026-09-07T00:00:00.000Z", violations: [] },
  },
  days: [
    {
      dayNumber: 1,
      atPortion: {
        range: { bookId: "GEN", startChapter: 1, startVerse: 1, endChapter: 2, endVerse: 3 },
        category: "narrativo",
      },
      ntPortion: { bookId: "MAT", startChapter: 1, startVerse: 1, endChapter: 1, endVerse: 17 },
      noteIds: ["note-01"],
      hook: "O que Deus faz no sétimo dia?",
      wordCount: 2000,
    },
    {
      dayNumber: 2,
      atPortion: {
        range: { bookId: "GEN", startChapter: 2, startVerse: 4, endChapter: 3, endVerse: 24 },
        category: "narrativo",
      },
      ntPortion: { bookId: "MAT", startChapter: 1, startVerse: 18, endChapter: 2, endVerse: 12 },
      noteIds: ["note-02"],
      hook: "O que muda depois da queda?",
      wordCount: 2200,
    },
    {
      dayNumber: 90,
      atPortion: {
        range: { bookId: "MAL", startChapter: 4, startVerse: 1, endChapter: 4, endVerse: 6 },
        category: "profetico",
      },
      ntPortion: { bookId: "APO", startChapter: 22, startVerse: 1, endChapter: 22, endVerse: 21 },
      noteIds: ["note-90"],
      // Dia 90 não tem hook (C11, RF-20) — tem a tela de conclusão.
      wordCount: 2500,
    },
  ],
  pericopes: [
    {
      id: "per-gen-1-1-2-3",
      bookId: "GEN",
      startChapter: 1,
      startVerse: 1,
      endChapter: 2,
      endVerse: 3,
      title: "A criação",
    },
  ],
  notes: [
    {
      id: "note-01",
      pericopeId: "per-gen-1-1-2-3",
      body: "Corpo da nota do dia 1.",
      author: "Autor de Exemplo",
    },
  ],
};

describe("getPlanDay — TASK-015 (critério de aceite: dado o bundle fixture, retorna PlanDay por número)", () => {
  it("retorna o PlanDay correto por número", () => {
    const day = getPlanDay(fixtureBundle, 2);

    expect(day?.dayNumber).toBe(2);
    expect(day?.hook).toBe("O que muda depois da queda?");
  });

  it("retorna o dia 90 sem hook", () => {
    const day = getPlanDay(fixtureBundle, 90);

    expect(day?.dayNumber).toBe(90);
    expect(day?.hook).toBeUndefined();
  });

  it("retorna undefined para número de dia inexistente no bundle", () => {
    const day = getPlanDay(fixtureBundle, 42);

    expect(day).toBeUndefined();
  });

  it("buildContentIndex + getPlanDayFromIndex produzem o mesmo resultado que getPlanDay", () => {
    const index = buildContentIndex(fixtureBundle);

    expect(getPlanDayFromIndex(index, 1)).toEqual(getPlanDay(fixtureBundle, 1));
  });

  it("getPericopeFromIndex resolve perícope por id, undefined se não existir", () => {
    const index = buildContentIndex(fixtureBundle);

    expect(getPericopeFromIndex(index, "per-gen-1-1-2-3")?.title).toBe("A criação");
    expect(getPericopeFromIndex(index, "per-inexistente")).toBeUndefined();
  });

  it("getNoteFromIndex resolve nota por id, undefined se não existir", () => {
    const index = buildContentIndex(fixtureBundle);

    expect(getNoteFromIndex(index, "note-01")?.author).toBe("Autor de Exemplo");
    expect(getNoteFromIndex(index, "note-inexistente")).toBeUndefined();
  });
});

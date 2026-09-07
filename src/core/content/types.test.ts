import { describe, expect, it } from "vitest";
import type {
  AtCategory,
  ContentBundle,
  Note,
  Pericope,
  Plan,
  PlanDay,
  ScriptureRange,
  ValidationReport,
} from "./index";

/**
 * TASK-012 — critério de aceite: "Tipos exportados e usados por importer,
 * validador e `core/content`". Como estes são tipos sem lógica de runtime, o
 * teste que cabe aqui é de compilação/smoke: construir um literal de cada tipo
 * (via o barrel `./index`, o caminho de import real que TASK-013/014/015 vão
 * usar) e confirmar, em runtime, que os objetos resultantes têm exatamente o
 * shape esperado — se um campo obrigatório do SDD.md §5.3 for removido por
 * engano, o `tsc` já quebra antes do teste rodar; o `expect` abaixo é a
 * segunda camada, cobrindo o shape observável em runtime dos literais.
 */

describe("core/content — tipos compartilhados (TASK-012)", () => {
  it("ScriptureRange: literal válido tem os 5 campos de intervalo", () => {
    const range: ScriptureRange = {
      bookId: "ROM",
      startChapter: 8,
      startVerse: 28,
      endChapter: 8,
      endVerse: 30,
    };

    expect(range).toStrictEqual({
      bookId: "ROM",
      startChapter: 8,
      startVerse: 28,
      endChapter: 8,
      endVerse: 30,
    });
  });

  it("Pericope: estende ScriptureRange com id e título curto", () => {
    const pericope: Pericope = {
      id: "per-rom-8-28-30",
      bookId: "ROM",
      startChapter: 8,
      startVerse: 28,
      endChapter: 8,
      endVerse: 30,
      title: "Todas as coisas cooperam para o bem",
    };

    expect(pericope.id).toBe("per-rom-8-28-30");
    expect(pericope.title).toBeTypeOf("string");
  });

  it("Note: ancorada a uma perícope, com autor obrigatório e revisor opcional", () => {
    const semRevisor: Note = {
      id: "note-01",
      pericopeId: "per-rom-8-28-30",
      body: "Corpo da nota entre 120 e 200 palavras (contagem validada por tools/content-validate, TASK-014).",
      author: "Autor de Exemplo",
    };
    const comRevisor: Note = {
      ...semRevisor,
      id: "note-02",
      reviewer: "Revisor de Exemplo",
      reviewedAt: "2026-09-07T00:00:00.000Z",
    };

    expect(semRevisor.reviewer).toBeUndefined();
    expect(comRevisor.reviewer).toBe("Revisor de Exemplo");
  });

  it("PlanDay: dia comum tem hook; dia 90 (C11) pode omiti-lo", () => {
    const categoria: AtCategory = "narrativo";
    const diaComum: PlanDay = {
      dayNumber: 1,
      atPortion: {
        range: { bookId: "GEN", startChapter: 1, startVerse: 1, endChapter: 2, endVerse: 3 },
        category: categoria,
      },
      ntPortion: { bookId: "MAT", startChapter: 1, startVerse: 1, endChapter: 1, endVerse: 17 },
      noteIds: ["note-01"],
      hook: "O que Deus faz no sétimo dia?",
      wordCount: 2000,
    };
    const dia90: PlanDay = { ...diaComum, dayNumber: 90, hook: undefined };

    expect(diaComum.hook).toBeTypeOf("string");
    expect(dia90.hook).toBeUndefined();
    expect(diaComum.noteIds).toHaveLength(1);
  });

  it("Plan: carrega dayCount e um ValidationReport (CA-02.5)", () => {
    const validationReport: ValidationReport = {
      passed: true,
      checkedAt: "2026-09-07T00:00:00.000Z",
      violations: [],
    };
    const plan: Plan = {
      id: "plano-90-dias",
      name: "Plano Entrelaçado de 90 dias",
      dayCount: 90,
      validationReport,
    };

    expect(plan.dayCount).toBe(90);
    expect(plan.validationReport.passed).toBe(true);
  });

  it("ContentBundle: agrega plan/days/pericopes/notes num único objeto", () => {
    const bundle: ContentBundle = {
      plan: {
        id: "plano-90-dias",
        name: "Plano Entrelaçado de 90 dias",
        dayCount: 1,
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
          body: "Corpo da nota.",
          author: "Autor de Exemplo",
        },
      ],
    };

    // Confirma que o "PlanDay por número" que TASK-015 promete encontrar é
    // trivialmente resolvível a partir do shape deste agregado.
    const diaUm = bundle.days.find((day) => day.dayNumber === 1);
    expect(diaUm).toBeDefined();
    expect(bundle.pericopes[0]?.id).toBe(bundle.notes[0]?.pericopeId);
  });
});

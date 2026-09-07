import { describe, expect, it } from "vitest";
import type { ContentBundle, Note, PlanDay } from "../../core/content";
import { buildValidBundle } from "./__fixtures__/valid-bundle";
import { ContentValidationError, assertContentValid, validateContent } from "./validate-content";

function cloneBundle(bundle: ContentBundle): ContentBundle {
  return JSON.parse(JSON.stringify(bundle)) as ContentBundle;
}

function withDay(
  bundle: ContentBundle,
  dayNumber: number,
  updater: (day: PlanDay) => PlanDay,
): ContentBundle {
  const clone = cloneBundle(bundle);
  const index = clone.days.findIndex((day) => day.dayNumber === dayNumber);
  if (index === -1) {
    throw new Error(`fixture inválida: dia ${dayNumber} não encontrado`);
  }
  const mutableDays = clone.days as PlanDay[];
  mutableDays[index] = updater(mutableDays[index]);
  return clone;
}

function violationsFor(rule: string, bundle: ContentBundle): readonly string[] {
  return validateContent(bundle).violations.filter((v) => v.startsWith(`${rule}:`));
}

function withNote(
  bundle: ContentBundle,
  noteId: string,
  updater: (note: Note) => Note,
): ContentBundle {
  const clone = cloneBundle(bundle);
  const index = clone.notes.findIndex((note) => note.id === noteId);
  if (index === -1) {
    throw new Error(`fixture inválida: nota '${noteId}' não encontrada`);
  }
  const mutableNotes = clone.notes as Note[];
  mutableNotes[index] = updater(mutableNotes[index]);
  return clone;
}

function wordsOfLength(count: number): string {
  return Array.from({ length: count }, (_, i) => `palavra${i}`).join(" ");
}

describe("validateContent — fixture positiva (C1–C11)", () => {
  it("bundle de 90 dias construído para satisfazer todas as regras passa sem violação", () => {
    const report = validateContent(buildValidBundle());
    expect(report.violations).toEqual([]);
    expect(report.passed).toBe(true);
    expect(() => assertContentValid(buildValidBundle())).not.toThrow();
  });
});

describe("validateContent — C3 (critério de aceite da TASK-013)", () => {
  it("fixture violando C3 (AT não-narrativo na janela de ativação) falha com mensagem específica", () => {
    const bundle = withDay(buildValidBundle(), 3, (day) => ({
      ...day,
      atPortion: { ...day.atPortion, category: "legal-ritual" },
    }));

    const report = validateContent(bundle);

    expect(report.passed).toBe(false);
    expect(
      report.violations.some(
        (v) => v.startsWith("C3:") && v.includes("dia 3") && v.includes("legal-ritual"),
      ),
    ).toBe(true);
  });

  it("assertContentValid lança ContentValidationError citando C3 para o mesmo bundle", () => {
    const bundle = withDay(buildValidBundle(), 3, (day) => ({
      ...day,
      atPortion: { ...day.atPortion, category: "legal-ritual" },
    }));

    expect(() => assertContentValid(bundle)).toThrow(ContentValidationError);
    try {
      assertContentValid(bundle);
      throw new Error("deveria ter lançado");
    } catch (error) {
      expect(error).toBeInstanceOf(ContentValidationError);
      expect((error as Error).message).toContain("C3:");
    }
  });

  it("fixture violando C3 pela porção de NT (fora dos Evangelhos) também falha com mensagem específica", () => {
    const bundle = withDay(buildValidBundle(), 2, (day) => ({
      ...day,
      ntPortion: { ...day.ntPortion, bookId: "ACT" },
    }));

    const violations = violationsFor("C3", bundle);
    expect(violations.length).toBeGreaterThan(0);
    expect(violations.some((v) => v.includes("dia 2") && v.includes("Evangelho"))).toBe(true);
  });
});

describe("validateContent — demais regras C1–C11", () => {
  it("C1: dia ausente do bundle é reportado", () => {
    const base = buildValidBundle();
    const bundle: ContentBundle = { ...base, days: base.days.filter((day) => day.dayNumber !== 50) };

    const violations = violationsFor("C1", bundle);
    expect(violations.some((v) => v.includes("dia 50"))).toBe(true);
  });

  it("C2: dois dias consecutivos com AT árido é reportado", () => {
    const bundle = withDay(buildValidBundle(), 9, (day) => ({
      ...day,
      atPortion: { ...day.atPortion, category: "legal-ritual" },
    }));

    const violations = violationsFor("C2", bundle);
    expect(violations.some((v) => v.includes("dia 8") && v.includes("dia 9"))).toBe(true);
  });

  it("C4: dia fora da faixa de 1.800–3.200 palavras é reportado", () => {
    const bundle = withDay(buildValidBundle(), 10, (day) => ({ ...day, wordCount: 500 }));

    const violations = violationsFor("C4", bundle);
    expect(violations.some((v) => v.includes("dia 10") && v.includes("500"))).toBe(true);
  });

  it("C5: proporção de palavras do AT fora de 50%–65% é reportada quando o bundle traz a quebra completa", () => {
    const base = buildValidBundle();
    const bundle: ContentBundle = {
      ...base,
      days: base.days.map((day) => ({ ...day, atWordCount: 800, ntWordCount: 1700 })),
    };

    const violations = violationsFor("C5", bundle);
    expect(violations.length).toBe(1);
    expect(violations[0]).toContain("32.0%");
  });

  it("C5: é pulado (sem violação nem falso 'passou') quando algum dia não traz a quebra AT/NT", () => {
    const bundle = withDay(buildValidBundle(), 40, (day) => ({
      ...day,
      atWordCount: undefined,
      ntWordCount: undefined,
    }));

    const violations = violationsFor("C5", bundle);
    expect(violations).toEqual([]);
    // as demais regras continuam passando normalmente — C5 pulado não é "tudo passou por acidente"
    expect(validateContent(bundle).violations).toEqual([]);
  });

  it("C6: porção que não termina em fronteira de nenhuma perícope é reportada", () => {
    const bundle = withDay(buildValidBundle(), 15, (day) => ({
      ...day,
      atPortion: { ...day.atPortion, range: { ...day.atPortion.range, endVerse: 11 } },
    }));

    const violations = violationsFor("C6", bundle);
    expect(violations.some((v) => v.includes("dia 15"))).toBe(true);
  });

  it("C6: porção de NT que termina no último capítulo/versículo de um Evangelho passa por fronteira de capítulo, mesmo sem coincidir com nenhuma perícope do bundle", () => {
    const base = buildValidBundle();
    // Dia 28 termina em Mateus 28:20 (fim do Evangelho, ver `gospel-versification.ts`).
    // Desalinha a perícope correspondente para que a fronteira de perícope deixe de casar
    // — só a fronteira de capítulo (via `GOSPEL_FINAL_VERSE`) deve validar essa porção.
    const bundle: ContentBundle = {
      ...base,
      pericopes: base.pericopes.map((pericope) =>
        pericope.id === "nt-28" ? { ...pericope, endVerse: 19 } : pericope,
      ),
    };

    const violations = violationsFor("C6", bundle);
    expect(violations.some((v) => v.includes("dia 28"))).toBe(false);
  });

  it("C6: porção de NT que termina em capítulo comum (não-Evangelho ou não-final) sem perícope correspondente continua reportada — fronteira de capítulo fora dos Evangelhos não é verificável", () => {
    const base = buildValidBundle();
    // Dia 29 é Marcos 1 (não é o último capítulo de Marcos); desalinhar a perícope
    // não deve ser "salvo" por fronteira de capítulo, porque essa não é uma fronteira
    // de capítulo mecanicamente verificável nesta implementação (ver nota de módulo).
    const bundle: ContentBundle = {
      ...base,
      pericopes: base.pericopes.map((pericope) =>
        pericope.id === "nt-29" ? { ...pericope, endVerse: 14 } : pericope,
      ),
    };

    const violations = violationsFor("C6", bundle);
    expect(violations.some((v) => v.includes("dia 29"))).toBe(true);
  });

  it("C7: duas porções do mesmo trecho em dias diferentes são reportadas", () => {
    const base = buildValidBundle();
    const day5Range = base.days.find((day) => day.dayNumber === 5)!.atPortion.range;
    const bundle = withDay(base, 6, (day) => ({
      ...day,
      atPortion: { ...day.atPortion, range: { ...day5Range } },
    }));

    const violations = violationsFor("C7", bundle);
    expect(violations.some((v) => v.includes("dia 5") && v.includes("dia 6"))).toBe(true);
  });

  it("C8: dia árido sem nota cobrindo a porção de AT é reportado", () => {
    const bundle = withDay(buildValidBundle(), 8, (day) => ({ ...day, noteIds: [] }));

    const violations = violationsFor("C8", bundle);
    expect(violations.some((v) => v.includes("dia 8"))).toBe(true);
  });

  it("C9: dia 1 sem porção de NT de um Evangelho é reportado", () => {
    const bundle = withDay(buildValidBundle(), 1, (day) => ({
      ...day,
      ntPortion: { ...day.ntPortion, bookId: "ACT" },
    }));

    const violations = violationsFor("C9", bundle);
    expect(violations.some((v) => v.includes("dia 1") && v.includes("Evangelho"))).toBe(true);
  });

  it("C9: nenhum Evangelho completado integralmente é reportado", () => {
    const bundle = withDay(buildValidBundle(), 28, (day) => ({
      ...day,
      ntPortion: { ...day.ntPortion, endVerse: 15 },
    }));

    const violations = violationsFor("C9", bundle);
    expect(violations.some((v) => v.includes("nenhum Evangelho") || v.includes("completa"))).toBe(
      true,
    );
  });

  it("C10: nome do plano sugerindo cobertura integral da Bíblia é reportado", () => {
    const base = buildValidBundle();
    const bundle: ContentBundle = {
      ...base,
      plan: { ...base.plan, name: "Plano de cobertura integral da Bíblia" },
    };

    const violations = violationsFor("C10", bundle);
    expect(violations.length).toBe(1);
  });

  it("C11: dia sem gancho de continuidade (exceto o último) é reportado", () => {
    const bundle = withDay(buildValidBundle(), 50, (day) => ({ ...day, hook: undefined }));

    const violations = violationsFor("C11", bundle);
    expect(violations.some((v) => v.includes("dia 50"))).toBe(true);
  });

  it("C11: dia 90 com gancho de continuidade (deveria não ter) é reportado", () => {
    const bundle = withDay(buildValidBundle(), 90, (day) => ({ ...day, hook: "Gancho indevido" }));

    const violations = violationsFor("C11", bundle);
    expect(violations.some((v) => v.includes("dia 90"))).toBe(true);
  });
});

describe("validateContent — RN-13 (critério de aceite da TASK-014: nota fora do range falha)", () => {
  it("nota com corpo abaixo de 120 palavras é reportada", () => {
    const bundle = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      body: wordsOfLength(50),
    }));

    const violations = violationsFor("RN-13", bundle);
    expect(
      violations.some(
        (v) => v.includes("note-8") && v.includes("50 palavra") && v.includes("120–200"),
      ),
    ).toBe(true);
  });

  it("nota com corpo acima de 200 palavras é reportada", () => {
    const bundle = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      body: wordsOfLength(250),
    }));

    const violations = violationsFor("RN-13", bundle);
    expect(
      violations.some(
        (v) => v.includes("note-8") && v.includes("250 palavra") && v.includes("120–200"),
      ),
    ).toBe(true);
  });

  it("nota com corpo dentro do range (120–200 palavras) não é reportada por contagem de palavras", () => {
    const bundleTooLow = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      body: wordsOfLength(120),
    }));
    const bundleTooHigh = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      body: wordsOfLength(200),
    }));

    expect(violationsFor("RN-13", bundleTooLow)).toEqual([]);
    expect(violationsFor("RN-13", bundleTooHigh)).toEqual([]);
  });

  it("assertContentValid lança ContentValidationError citando RN-13 para nota fora do range", () => {
    const bundle = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      body: wordsOfLength(10),
    }));

    expect(() => assertContentValid(bundle)).toThrow(ContentValidationError);
    try {
      assertContentValid(bundle);
      throw new Error("deveria ter lançado");
    } catch (error) {
      expect(error).toBeInstanceOf(ContentValidationError);
      expect((error as Error).message).toContain("RN-13:");
    }
  });

  it("nota sem autoria (string vazia) é reportada", () => {
    const bundle = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      author: "",
    }));

    const violations = violationsFor("RN-13", bundle);
    expect(violations.some((v) => v.includes("note-8") && v.includes("autoria"))).toBe(true);
  });

  it("nota com autoria só de espaços em branco é reportada", () => {
    const bundle = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      author: "   ",
    }));

    const violations = violationsFor("RN-13", bundle);
    expect(violations.some((v) => v.includes("note-8") && v.includes("autoria"))).toBe(true);
  });

  it("nota sem revisor (campo ausente, 'quando houver') não é reportada", () => {
    const bundle = buildValidBundle();
    expect(bundle.notes.find((n) => n.id === "note-8")?.reviewer).toBeUndefined();

    const violations = violationsFor("RN-13", bundle);
    expect(violations.some((v) => v.includes("revisor"))).toBe(false);
  });

  it("nota com revisor identificado (não vazio) não é reportada", () => {
    const bundle = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      reviewer: "Revisor Fixture",
      reviewedAt: new Date(0).toISOString(),
    }));

    const violations = violationsFor("RN-13", bundle);
    expect(violations.some((v) => v.includes("revisor"))).toBe(false);
  });

  it("nota com campo de revisor presente mas vazio/só espaço é reportada", () => {
    const bundle = withNote(buildValidBundle(), "note-8", (note) => ({
      ...note,
      reviewer: "   ",
    }));

    const violations = violationsFor("RN-13", bundle);
    expect(violations.some((v) => v.includes("note-8") && v.includes("revisor"))).toBe(true);
  });
});

describe("validateContent — RN-13-cobertura (dias não-áridos precisam de pelo menos uma nota)", () => {
  it("dia não-árido sem nenhuma nota referenciada cobrindo AT ou NT é reportado", () => {
    // dia 10 (offset (10-8)%3=2 -> 'poetico', não-árido, ver aridCycleCategory)
    const bundle = withDay(buildValidBundle(), 10, (day) => ({ ...day, noteIds: [] }));

    const violations = violationsFor("RN-13-cobertura", bundle);
    expect(violations.some((v) => v.includes("dia 10"))).toBe(true);
  });

  it("dia não-árido com nota cobrindo pelo menos uma das porções (NT) não é reportado", () => {
    const bundle = buildValidBundle();
    const day10 = bundle.days.find((day) => day.dayNumber === 10)!;
    expect(day10.atPortion.category).not.toBe("legal-ritual");
    expect(day10.atPortion.category).not.toBe("genealogico");
    expect(day10.atPortion.category).not.toBe("profetico");
    expect(day10.noteIds.length).toBeGreaterThan(0);

    const violations = violationsFor("RN-13-cobertura", bundle);
    expect(violations.some((v) => v.includes("dia 10"))).toBe(false);
  });

  it("dia árido (já coberto por C8) não é duplicado em RN-13-cobertura", () => {
    const bundle = buildValidBundle();
    const day8 = bundle.days.find((day) => day.dayNumber === 8)!;
    expect(day8.atPortion.category).toBe("legal-ritual");

    const violations = violationsFor("RN-13-cobertura", bundle);
    expect(violations.some((v) => v.includes("dia 8"))).toBe(false);
  });
});

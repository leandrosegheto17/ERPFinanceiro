import { describe, expect, it, vi } from "vitest";
import {
  applyTheme,
  getStoredPreference,
  initTheme,
  resolveTheme,
  setStoredPreference,
  setThemePreference,
  THEME_STORAGE_KEY,
  type ThemeStorage,
  type ThemeTarget,
} from "./theme";

function createFakeTarget(): ThemeTarget & { attributes: Record<string, string> } {
  const attributes: Record<string, string> = {};
  return {
    attributes,
    setAttribute(name, value) {
      attributes[name] = value;
    },
    removeAttribute(name) {
      delete attributes[name];
    },
  };
}

function createFakeStorage(initial: Record<string, string> = {}): ThemeStorage {
  const store = { ...initial };
  return {
    getItem: (key) => (key in store ? store[key] : null),
    setItem: (key, value) => {
      store[key] = value;
    },
  };
}

describe("resolveTheme", () => {
  it("resolve 'system' para 'dark' quando o SO prefere escuro", () => {
    expect(resolveTheme("system", true)).toBe("dark");
  });

  it("resolve 'system' para 'light' quando o SO prefere claro", () => {
    expect(resolveTheme("system", false)).toBe("light");
  });

  it("ignora a preferência do SO quando há override manual", () => {
    expect(resolveTheme("light", true)).toBe("light");
    expect(resolveTheme("dark", false)).toBe("dark");
  });
});

describe("applyTheme", () => {
  it("define data-theme no alvo", () => {
    const target = createFakeTarget();
    applyTheme("dark", target);
    expect(target.attributes["data-theme"]).toBe("dark");
  });
});

describe("getStoredPreference / setStoredPreference", () => {
  it("retorna 'system' quando nada foi persistido", () => {
    const storage = createFakeStorage();
    expect(getStoredPreference(storage)).toBe("system");
  });

  it("retorna 'system' quando o valor persistido é inválido", () => {
    const storage = createFakeStorage({ [THEME_STORAGE_KEY]: "azul" });
    expect(getStoredPreference(storage)).toBe("system");
  });

  it("persiste e lê de volta uma preferência válida", () => {
    const storage = createFakeStorage();
    setStoredPreference(storage, "dark");
    expect(getStoredPreference(storage)).toBe("dark");
  });
});

describe("initTheme", () => {
  it("aplica o tema do sistema quando não há preferência persistida", () => {
    const target = createFakeTarget();
    const storage = createFakeStorage();

    const theme = initTheme({ target, storage, systemPrefersDark: true });

    expect(theme).toBe("dark");
    expect(target.attributes["data-theme"]).toBe("dark");
  });

  it("aplica o override manual persistido, ignorando o sistema", () => {
    const target = createFakeTarget();
    const storage = createFakeStorage({ [THEME_STORAGE_KEY]: "light" });

    const theme = initTheme({ target, storage, systemPrefersDark: true });

    expect(theme).toBe("light");
    expect(target.attributes["data-theme"]).toBe("light");
  });
});

describe("setThemePreference", () => {
  it("persiste a nova preferência e reaplica o tema resolvido", () => {
    const target = createFakeTarget();
    const storage = createFakeStorage();
    const setItemSpy = vi.spyOn(storage, "setItem");

    const theme = setThemePreference("dark", { target, storage, systemPrefersDark: false });

    expect(theme).toBe("dark");
    expect(target.attributes["data-theme"]).toBe("dark");
    expect(setItemSpy).toHaveBeenCalledWith(THEME_STORAGE_KEY, "dark");
    expect(getStoredPreference(storage)).toBe("dark");
  });
});

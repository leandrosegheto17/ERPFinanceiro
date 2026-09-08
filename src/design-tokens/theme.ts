/**
 * TASK-023 — mecanismo de alternância de tema (claro/escuro).
 *
 * Fonte de verdade em runtime: o atributo `data-theme` no elemento raiz
 * (ver `tokens.css`). A preferência efetiva é "sistema" por padrão
 * (`prefers-color-scheme`), com override manual persistido (T-11
 * Configurações).
 *
 * Este módulo não importa `document`/`window` diretamente — recebe
 * interfaces mínimas (duck typing) como parâmetro, para ser testável sem
 * jsdom (nenhuma dependência nova só para isto) e para não amarrar a lógica
 * de resolução de tema a um ambiente de DOM específico. `main.tsx` é quem
 * conecta este módulo ao `document`/`window`/`localStorage` reais.
 */

export type Theme = "light" | "dark";
export type ThemePreference = Theme | "system";

export const THEME_STORAGE_KEY = "estudobiblico:theme-preference";

/** Resolve a preferência ("light" | "dark" | "system") no tema efetivo. */
export function resolveTheme(preference: ThemePreference, systemPrefersDark: boolean): Theme {
  if (preference === "system") {
    return systemPrefersDark ? "dark" : "light";
  }
  return preference;
}

export interface ThemeTarget {
  setAttribute(qualifiedName: string, value: string): void;
  removeAttribute(qualifiedName: string): void;
}

/** Aplica o tema resolvido no elemento raiz via `data-theme`. */
export function applyTheme(theme: Theme, target: ThemeTarget): void {
  target.setAttribute("data-theme", theme);
}

export interface ThemeStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

function isThemePreference(value: string | null): value is ThemePreference {
  return value === "light" || value === "dark" || value === "system";
}

/** Lê a preferência persistida; "system" se nunca foi definida ou é inválida. */
export function getStoredPreference(storage: ThemeStorage): ThemePreference {
  const value = storage.getItem(THEME_STORAGE_KEY);
  return isThemePreference(value) ? value : "system";
}

/** Persiste a preferência escolhida pelo usuário (T-11 Configurações). */
export function setStoredPreference(storage: ThemeStorage, preference: ThemePreference): void {
  storage.setItem(THEME_STORAGE_KEY, preference);
}

export interface InitThemeOptions {
  target: ThemeTarget;
  storage: ThemeStorage;
  systemPrefersDark: boolean;
}

/**
 * Lê a preferência persistida, resolve o tema efetivo e aplica no elemento
 * raiz. Chamado uma vez no boot da aplicação (`main.tsx`) e de novo sempre
 * que o usuário troca a preferência em T-11.
 */
export function initTheme(options: InitThemeOptions): Theme {
  const preference = getStoredPreference(options.storage);
  const theme = resolveTheme(preference, options.systemPrefersDark);
  applyTheme(theme, options.target);
  return theme;
}

/** Troca e persiste a preferência de tema, aplicando o resultado de imediato. */
export function setThemePreference(
  preference: ThemePreference,
  options: InitThemeOptions,
): Theme {
  setStoredPreference(options.storage, preference);
  return initTheme(options);
}

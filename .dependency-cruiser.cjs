/**
 * Regra de dependência unidirecional (DI-02 / GUARDRAILS G-07, ADR-001,
 * ADR-012): `core/*` é TypeScript puro e nunca importa de `features/*`.
 * Verificado por lint no CI, nunca por revisão manual.
 *
 * `npm run lint:deps` roda esta config contra `src`. O teste automatizado em
 * `src/tools/dependency-rule.test.ts` também usa esta config (via API de
 * `dependency-cruiser`) contra uma fixture que viola a regra, para confirmar
 * que o lint efetivamente falha.
 */
module.exports = {
  forbidden: [
    {
      name: "no-features-from-core",
      comment:
        "core/* é TypeScript puro e nunca importa de features/* (DI-02, ADR-001, ADR-012)",
      severity: "error",
      from: { path: "(^|[\\\\/])core[\\\\/]" },
      to: { path: "(^|[\\\\/])features[\\\\/]" },
    },
  ],
  options: {
    tsPreCompilationDeps: true,
    tsConfig: { fileName: "tsconfig.app.json" },
    enhancedResolveOptions: {
      extensions: [".ts", ".tsx", ".js", ".jsx"],
    },
    doNotFollow: { path: "node_modules" },
  },
};

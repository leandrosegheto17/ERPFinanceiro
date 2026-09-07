# core/*

TypeScript puro, sem React nem DOM (DI-02 / ADR-001, ADR-012). Nunca importa de
`features/*`. Dependência unidirecional verificada por lint
(`npm run lint:deps`, config em `.dependency-cruiser.cjs`).

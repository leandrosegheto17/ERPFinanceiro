// TASK-034 (Lote 5 — Backend: Schema e Segurança): "Headers de segurança:
// CSP, HSTS, Permissions-Policy, Referrer-Policy (§7.5)". Critério de
// aceite: "Teste de headers HTTP confirma presença de todos."
//
// Duas camadas de teste, deliberadamente:
//   1. Valores exatos das diretivas (não só "a chave existe") — cobre o
//      texto literal exigido pelo SDD §7.5.
//   2. Resposta HTTP real: sobe um servidor `node:http` usando o mesmo
//      middleware aplicado por `vite dev`/`vite preview`
//      (`securityHeadersPlugin`) e faz `fetch()` de verdade contra ele,
//      confirmando que os headers chegam na resposta HTTP — não apenas que
//      a constante em memória está correta.
import { describe, expect, it, afterAll } from "vitest";
import { createServer, type Server } from "node:http";
import { AddressInfo } from "node:net";
import {
  applyDevSecurityHeaders,
  applySecurityHeaders,
  CONTENT_SECURITY_POLICY,
  PERMISSIONS_POLICY,
  REFERRER_POLICY,
  STRICT_TRANSPORT_SECURITY,
  SECURITY_HEADERS,
  buildHeadersFileContent,
} from "./securityHeaders";

describe("TASK-034: conteúdo exato dos headers de segurança (SDD §7.5)", () => {
  it("CSP: default-src 'self', script-src sem unsafe-inline, connect-src restrito ao backend, frame-ancestors 'none'", () => {
    expect(CONTENT_SECURITY_POLICY).toMatch(/(^|;\s*)default-src 'self'(;|$)/);
    expect(CONTENT_SECURITY_POLICY).toMatch(/frame-ancestors 'none'/);
    expect(CONTENT_SECURITY_POLICY).toMatch(
      /connect-src 'self' https:\/\/\*\.supabase\.co/,
    );
    // `script-src` é o risco real que CSP mitiga (XSS via injeção de
    // script): continua estrito, sem `unsafe-inline`/`unsafe-eval`.
    expect(CONTENT_SECURITY_POLICY).toMatch(/(^|;\s*)script-src 'self'(;|$)/);
    expect(CONTENT_SECURITY_POLICY).not.toMatch(/unsafe-eval/);
    // `style-src` permite `'unsafe-inline'` — correção pós-revisão inline de
    // TASK-034: os primitivos do Design System (Lote 4) usam `style={{...}}`
    // do React (ex.: TOUCH_TARGET_MIN_STYLE, DI-08), que vira atributo
    // `style=""` inline no DOM e é bloqueado por `style-src 'self'` estrito
    // sem essa concessão (sem nonce/hash configurado no projeto).
    expect(CONTENT_SECURITY_POLICY).toMatch(
      /(^|;\s*)style-src 'self' 'unsafe-inline'(;|$)/,
    );
    // A única diretiva que contém coringa é `connect-src`, e apenas para o
    // subdomínio do backend (`*.supabase.co`) — nenhuma outra diretiva
    // (script-src, style-src, img-src etc.) permite origem coringa (G-17:
    // nenhum destino de terceiro).
    const directivesWithWildcard = CONTENT_SECURITY_POLICY.split(";")
      .map((d) => d.trim())
      .filter((d) => d.includes("*"));
    expect(directivesWithWildcard).toEqual([
      "connect-src 'self' https://*.supabase.co",
    ]);
  });

  it("HSTS: max-age de pelo menos 1 ano, includeSubDomains e preload", () => {
    const match = STRICT_TRANSPORT_SECURITY.match(/max-age=(\d+)/);
    expect(match).not.toBeNull();
    const maxAge = Number(match![1]);
    expect(maxAge).toBeGreaterThanOrEqual(31536000); // 1 ano em segundos
    expect(STRICT_TRANSPORT_SECURITY).toMatch(/includeSubDomains/);
    expect(STRICT_TRANSPORT_SECURITY).toMatch(/preload/);
  });

  it("Referrer-Policy: no-referrer, exatamente", () => {
    expect(REFERRER_POLICY).toBe("no-referrer");
  });

  it("Permissions-Policy: nega câmera, microfone e geolocalização", () => {
    expect(PERMISSIONS_POLICY).toMatch(/camera=\(\)/);
    expect(PERMISSIONS_POLICY).toMatch(/microphone=\(\)/);
    expect(PERMISSIONS_POLICY).toMatch(/geolocation=\(\)/);
  });

  it("os 4 headers exigidos pelo SDD §7.5 estão todos presentes no conjunto", () => {
    const names = SECURITY_HEADERS.map(([name]) => name);
    expect(names).toContain("Content-Security-Policy");
    expect(names).toContain("Strict-Transport-Security");
    expect(names).toContain("Referrer-Policy");
    expect(names).toContain("Permissions-Policy");
  });

  it("buildHeadersFileContent() gera o formato Cloudflare Pages (regra /* + headers indentados)", () => {
    const content = buildHeadersFileContent();
    const lines = content.split("\n");
    expect(lines[0]).toBe("/*");
    for (const [name, value] of SECURITY_HEADERS) {
      expect(content).toContain(`  ${name}: ${value}`);
    }
  });
});

describe("TASK-034: resposta HTTP real confirma presença e valor de todos os headers", () => {
  let server: Server;
  let baseUrl: string;

  const startServer = () =>
    new Promise<void>((resolve) => {
      server = createServer((_req, res) => {
        applySecurityHeaders(res);
        res.statusCode = 200;
        res.end("ok");
      });
      server.listen(0, "127.0.0.1", () => {
        const { port } = server.address() as AddressInfo;
        baseUrl = `http://127.0.0.1:${port}`;
        resolve();
      });
    });

  afterAll(() => {
    server?.close();
  });

  it("GET / retorna os 5 headers de segurança com valor exato", async () => {
    await startServer();
    const response = await fetch(baseUrl + "/");
    expect(response.status).toBe(200);

    expect(response.headers.get("content-security-policy")).toBe(
      CONTENT_SECURITY_POLICY,
    );
    expect(response.headers.get("strict-transport-security")).toBe(
      STRICT_TRANSPORT_SECURITY,
    );
    expect(response.headers.get("referrer-policy")).toBe(REFERRER_POLICY);
    expect(response.headers.get("permissions-policy")).toBe(
      PERMISSIONS_POLICY,
    );
    expect(response.headers.get("x-content-type-options")).toBe("nosniff");
  });
});

describe("Correção pós-fechamento de TASK-034 (regressão TASK-035/Lote 6): headers de dev sem CSP", () => {
  let server: Server;
  let baseUrl: string;

  const startServer = () =>
    new Promise<void>((resolve) => {
      server = createServer((_req, res) => {
        applyDevSecurityHeaders(res);
        res.statusCode = 200;
        res.end("ok");
      });
      server.listen(0, "127.0.0.1", () => {
        const { port } = server.address() as AddressInfo;
        baseUrl = `http://127.0.0.1:${port}`;
        resolve();
      });
    });

  afterAll(() => {
    server?.close();
  });

  it("GET / não inclui Content-Security-Policy, mas inclui os demais 4 headers com valor exato", async () => {
    await startServer();
    const response = await fetch(baseUrl + "/");
    expect(response.status).toBe(200);

    // A causa raiz da regressão: CSP restrita em dev bloqueia o preamble
    // inline do React Fast Refresh (HMR). Em dev, não aplicamos CSP nenhuma.
    expect(response.headers.get("content-security-policy")).toBeNull();

    // Os demais headers não têm esse problema (nenhum bloqueia script/estilo
    // inline do HMR) e continuam aplicados normalmente em dev.
    expect(response.headers.get("strict-transport-security")).toBe(
      STRICT_TRANSPORT_SECURITY,
    );
    expect(response.headers.get("referrer-policy")).toBe(REFERRER_POLICY);
    expect(response.headers.get("permissions-policy")).toBe(
      PERMISSIONS_POLICY,
    );
    expect(response.headers.get("x-content-type-options")).toBe("nosniff");
  });
});

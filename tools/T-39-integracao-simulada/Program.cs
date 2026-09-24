using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Autofac;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace T39IntegracaoSimulada
{
    /// <summary>
    /// Harness descartável de T-39 (ADR-010, TASK.md Lote 8): sobe a API real (mesmo caminho de
    /// produção usado por T-30/T-31/T-32/T-34/T-37/T-38 — <see cref="ApiHost"/> +
    /// <see cref="Startup"/> + <see cref="CompositionRoot.Construir"/>) contra um Firebird
    /// embarcado real e novo, e faz o papel do "Vendas" disparando, via <see cref="HttpClient"/>
    /// real (não curl externo), os cenários do critério de aceite de T-39:
    ///   1. Cancelamento de venda Pendente -> 200.
    ///   2. Cancelamento de venda desconhecida -> 200 (cria+cancela, D-08).
    ///   3. Cancelamento de venda Quitada sem motivo -> 409 MOTIVO_OBRIGATORIO (D-06).
    ///   4. Reenvio após timeout (idempotência, P-2) -> mesmo resultado, sem duplicar histórico
    ///      — confirmado tanto pela resposta HTTP quanto por consulta direta ao banco
    ///      (VendaRepository/FinanceiroDbContext, mesmo padrão de T-38).
    ///   5. Erros 400 (payload inválido, vendaId ausente) e 401 (sem/errada X-Api-Key)
    ///      exercitados de fato.
    ///
    /// ============================================================================================
    /// SIMULAÇÃO — NÃO é integração real com o Vendas (Delphi). Este harness faz o papel do
    /// sistema Vendas contra a API local; não há acesso ao sistema Delphi real neste sandbox
    /// (ver ADR-010, .md/BLOCKERS.md Bloqueio 002). A integração real com o Vendas permanece
    /// pendência externa explícita.
    /// ============================================================================================
    /// </summary>
    public static class Program
    {
        private const string Banner =
            "SIMULAÇÃO — NÃO é integração real com o Vendas (Delphi). Harness HTTP fazendo o papel " +
            "do Vendas contra a API local (ADR-010).";

        public static int Main()
        {
            // Evita mojibake de acentos (á/ã/é/ç...) ao redirecionar a saída para arquivo de
            // evidência: o console do Windows usa um codepage OEM por padrão, divergente do
            // UTF-8 usado pelo restante do código-fonte/JSON da API.
            Console.OutputEncoding = new UTF8Encoding(false);

            Console.WriteLine("================================================================================");
            Console.WriteLine("[T39] " + Banner);
            Console.WriteLine("================================================================================");

            var configuracao = new ConfiguracaoBancoAppConfig();
            var apiKey = System.Configuration.ConfigurationManager.AppSettings["Api:ApiKey"];
            var porta = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Api:Porta"]);

            ApagarFdbAnterior(configuracao.CaminhoFdb);

            Console.WriteLine("[T39] Inicializando banco (.fdb) via DbInitializer...");
            new DbInitializer(configuracao).Inicializar();

            int falhas = 0;
            int total = 0;

            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var vendaIdPendente = $"V-T39-SIM-PEND-{ts}";
            var vendaIdDesconhecida = $"V-T39-SIM-DESC-{ts}";
            var vendaIdQuitada = $"V-T39-SIM-QUIT-{ts}";

            // Seed direto no banco real (mesma técnica de ApiHostVendasCancelamentoControllerTests,
            // T-32): venda Pendente e venda Quitada precisam existir antes do cenário de
            // cancelamento — não há endpoint público de "registrar Pendente" implementado nesta
            // sessão (POST /api/vendas é T-62, Tier B condicional, fora de escopo aqui).
            SemearVendaPendente(configuracao, vendaIdPendente);
            SemearVendaQuitada(configuracao, vendaIdQuitada);

            using (IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                host.Start(porta);

                if (host.Estado != EstadoApiHost.Ativa)
                {
                    Console.Error.WriteLine($"[T39] Falha ao subir a API. Estado: {host.Estado}, UltimoErro: {host.UltimoErro}");
                    return 1;
                }

                Console.WriteLine($"[T39] API ATIVA em http://localhost:{porta}/ (papel do Vendas simulado por este harness)");
                Console.WriteLine();

                using (var http = new HttpClient { BaseAddress = new Uri($"http://localhost:{porta}/api/vendas/") })
                {
                    http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                    // ---------------------------------------------------------------------
                    // Cenário 1 — Cancelamento de venda Pendente -> 200
                    // ---------------------------------------------------------------------
                    var payloadPendente = "{\"vendaId\":\"" + vendaIdPendente + "\"}";
                    var resp1 = ExecutarChamada(http, apiKey, "1. Cancelamento — venda Pendente (motivo opcional, ausente)", HttpMethod.Post, "cancelamento", payloadPendente);
                    total++;
                    if (resp1.StatusCode == 200 && StatusEsperado(resp1.Corpo, "Cancelada"))
                    {
                        Console.WriteLine("   [OK] HTTP 200, status=Cancelada.");
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 200 status=Cancelada, obtido HTTP " + resp1.StatusCode + " body=" + resp1.Corpo);
                        falhas++;
                    }

                    int historicoAntesReenvio = ContarHistorico(configuracao, vendaIdPendente);
                    Console.WriteLine($"   [DB]  Consulta direta ao banco (VendaHistorico) apos cenario 1: {historicoAntesReenvio} registro(s).");
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 2 — Cancelamento de venda desconhecida -> 200 (cria+cancela, D-08)
                    // ---------------------------------------------------------------------
                    var payloadDesconhecida = "{\"vendaId\":\"" + vendaIdDesconhecida + "\",\"motivo\":\"Cliente desistiu da compra\"}";
                    var resp2 = ExecutarChamada(http, apiKey, "2. Cancelamento — venda desconhecida (cria+cancela, D-08)", HttpMethod.Post, "cancelamento", payloadDesconhecida);
                    total++;
                    if (resp2.StatusCode == 200 && StatusEsperado(resp2.Corpo, "Cancelada"))
                    {
                        Console.WriteLine("   [OK] HTTP 200, status=Cancelada (venda criada já Cancelada).");
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 200 status=Cancelada, obtido HTTP " + resp2.StatusCode + " body=" + resp2.Corpo);
                        falhas++;
                    }
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 3 — Cancelamento de venda Quitada sem motivo -> 409 MOTIVO_OBRIGATORIO
                    // ---------------------------------------------------------------------
                    var payloadQuitadaSemMotivo = "{\"vendaId\":\"" + vendaIdQuitada + "\"}";
                    var resp3 = ExecutarChamada(http, apiKey, "3. Cancelamento — venda Quitada sem motivo (D-06)", HttpMethod.Post, "cancelamento", payloadQuitadaSemMotivo);
                    total++;
                    if (resp3.StatusCode == 409 && CodigoErroEsperado(resp3.Corpo, "MOTIVO_OBRIGATORIO"))
                    {
                        Console.WriteLine("   [OK] HTTP 409, erro.codigo=MOTIVO_OBRIGATORIO.");
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 409 MOTIVO_OBRIGATORIO, obtido HTTP " + resp3.StatusCode + " body=" + resp3.Corpo);
                        falhas++;
                    }
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 4 — Reenvio após timeout (idempotência, P-2): mesmo payload do
                    // cenário 1 (venda já Cancelada) reenviado como se o "Vendas" tivesse sofrido
                    // timeout na 1ª resposta e reenviado a mesma requisição sem saber se ela
                    // processou — mesmo resultado, sem duplicar histórico.
                    // ---------------------------------------------------------------------
                    var resp4 = ExecutarChamada(http, apiKey, "4. Reenvio pós-timeout — mesmo payload do cenário 1 (idempotência P-2)", HttpMethod.Post, "cancelamento", payloadPendente);
                    total++;
                    if (resp4.StatusCode == 200 && StatusEsperado(resp4.Corpo, "Cancelada"))
                    {
                        Console.WriteLine("   [OK] HTTP 200, status=Cancelada (mesmo resultado do cenário 1, reenvio idempotente).");
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 200 status=Cancelada, obtido HTTP " + resp4.StatusCode + " body=" + resp4.Corpo);
                        falhas++;
                    }

                    int historicoDepoisReenvio = ContarHistorico(configuracao, vendaIdPendente);
                    total++;
                    Console.WriteLine($"   [DB]  Consulta direta ao banco (VendaHistorico) apos reenvio: {historicoDepoisReenvio} registro(s) " +
                                       $"(antes do reenvio: {historicoAntesReenvio}).");
                    if (historicoDepoisReenvio == historicoAntesReenvio)
                    {
                        Console.WriteLine($"   [OK] Contagem de VendaHistorico inalterada ({historicoAntesReenvio} -> {historicoDepoisReenvio}) — reenvio pós-timeout NAO gerou novo histórico (P-2 confirmado por consulta direta ao banco).");
                    }
                    else
                    {
                        Console.WriteLine($"   [FALHA] Contagem de VendaHistorico mudou ({historicoAntesReenvio} -> {historicoDepoisReenvio}) — reenvio gerou histórico novo, violando P-2.");
                        falhas++;
                    }
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 5a — Erro 400: payload inválido (vendaId ausente)
                    // ---------------------------------------------------------------------
                    var payloadInvalido = "{\"motivo\":\"sem vendaId\"}";
                    var resp5a = ExecutarChamada(http, apiKey, "5a. Payload inválido — vendaId ausente (400)", HttpMethod.Post, "cancelamento", payloadInvalido);
                    total++;
                    if (resp5a.StatusCode == 400 && CodigoErroEsperado(resp5a.Corpo, "PAYLOAD_INVALIDO"))
                    {
                        Console.WriteLine("   [OK] HTTP 400, erro.codigo=PAYLOAD_INVALIDO.");
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 400 PAYLOAD_INVALIDO, obtido HTTP " + resp5a.StatusCode + " body=" + resp5a.Corpo);
                        falhas++;
                    }
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 5b — Erro 401: sem X-Api-Key
                    // ---------------------------------------------------------------------
                    total++;
                    var respSemHeader = ExecutarChamadaSemAutenticacao(porta, "5b. Sem X-Api-Key (401)", null, "{\"vendaId\":\"" + vendaIdPendente + "\"}");
                    if (respSemHeader.StatusCode == 401 && CodigoErroEsperado(respSemHeader.Corpo, "NAO_AUTORIZADO"))
                    {
                        Console.WriteLine("   [OK] HTTP 401, erro.codigo=NAO_AUTORIZADO (header ausente).");
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 401 NAO_AUTORIZADO, obtido HTTP " + respSemHeader.StatusCode + " body=" + respSemHeader.Corpo);
                        falhas++;
                    }
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 5c — Erro 401: X-Api-Key errada
                    // ---------------------------------------------------------------------
                    total++;
                    var respChaveErrada = ExecutarChamadaSemAutenticacao(porta, "5c. X-Api-Key errada (401)", "CHAVE_ERRADA_T39", "{\"vendaId\":\"" + vendaIdPendente + "\"}");
                    if (respChaveErrada.StatusCode == 401 && CodigoErroEsperado(respChaveErrada.Corpo, "NAO_AUTORIZADO"))
                    {
                        Console.WriteLine("   [OK] HTTP 401, erro.codigo=NAO_AUTORIZADO (chave inválida).");
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 401 NAO_AUTORIZADO, obtido HTTP " + respChaveErrada.StatusCode + " body=" + respChaveErrada.Corpo);
                        falhas++;
                    }

                    if (respSemHeader.StatusCode == 401 && respChaveErrada.StatusCode == 401 && respSemHeader.Corpo == respChaveErrada.Corpo)
                    {
                        Console.WriteLine("   [OK] Corpo do 401 idêntico nos dois casos (sem header / chave errada) — nenhuma distinção revelada ao chamador (T-35).");
                    }
                    Console.WriteLine();
                }

                host.Stop();
            }

            Console.WriteLine("================================================================================");
            Console.WriteLine($"[T39] Resultado: {total - falhas}/{total} verificações OK.");
            Console.WriteLine("[T39] " + Banner);
            Console.WriteLine("================================================================================");

            return falhas > 0 ? 1 : 0;
        }

        private static bool StatusEsperado(string json, string statusEsperado)
        {
            var obj = ParseJsonSemConverterDatas(json);
            return (string)obj["status"] == statusEsperado;
        }

        private static bool CodigoErroEsperado(string json, string codigoEsperado)
        {
            var obj = ParseJsonSemConverterDatas(json);
            return (string)obj["erro"]?["codigo"] == codigoEsperado;
        }

        /// <summary>
        /// Faz o parse do corpo JSON sem deixar o Newtonsoft.Json converter campos com "cara" de
        /// data automaticamente (mesma cautela de T-38, mesmo esta tarefa não comparando datas —
        /// mantido por consistência de padrão entre os dois harnesses).
        /// </summary>
        private static JObject ParseJsonSemConverterDatas(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
            {
                return JObject.Load(reader);
            }
        }

        private class RespostaHttp
        {
            public int StatusCode;
            public string Corpo;
        }

        private static RespostaHttp ExecutarChamada(HttpClient http, string apiKey, string titulo, HttpMethod metodo, string path, string corpoJson)
        {
            Console.WriteLine("--- Cenário: " + titulo + " ---");
            Console.WriteLine("REQUEST:");
            Console.WriteLine($"  {metodo} {http.BaseAddress}{path}");
            Console.WriteLine("  X-Api-Key: " + apiKey);
            if (corpoJson != null)
            {
                Console.WriteLine("  Content-Type: application/json");
                Console.WriteLine("  Body: " + corpoJson);
            }

            HttpResponseMessage respostaHttp;
            if (metodo == HttpMethod.Get)
            {
                respostaHttp = http.GetAsync(path).Result;
            }
            else
            {
                var conteudo = new StringContent(corpoJson ?? string.Empty, Encoding.UTF8, "application/json");
                respostaHttp = http.PostAsync(path, conteudo).Result;
            }

            var corpoResposta = respostaHttp.Content.ReadAsStringAsync().Result;
            Console.WriteLine("RESPONSE:");
            Console.WriteLine("  HTTP " + (int)respostaHttp.StatusCode);
            Console.WriteLine("  Body: " + corpoResposta);

            return new RespostaHttp { StatusCode = (int)respostaHttp.StatusCode, Corpo = corpoResposta };
        }

        /// <summary>
        /// Chamada dedicada aos cenários 5b/5c (401): usa um <see cref="HttpClient"/> próprio
        /// (sem o header <c>X-Api-Key</c> pré-configurado do cliente principal), para exercitar
        /// de fato "sem header" e "com chave errada" contra <see cref="ApiKeyHandler"/> (T-35).
        /// </summary>
        private static RespostaHttp ExecutarChamadaSemAutenticacao(int porta, string titulo, string chaveApiKeyOuNulo, string corpoJson)
        {
            Console.WriteLine("--- Cenário: " + titulo + " ---");
            using (var http = new HttpClient { BaseAddress = new Uri($"http://localhost:{porta}/api/vendas/") })
            {
                if (chaveApiKeyOuNulo != null)
                {
                    http.DefaultRequestHeaders.Add("X-Api-Key", chaveApiKeyOuNulo);
                }

                Console.WriteLine("REQUEST:");
                Console.WriteLine($"  POST {http.BaseAddress}cancelamento");
                Console.WriteLine("  X-Api-Key: " + (chaveApiKeyOuNulo ?? "(ausente)"));
                Console.WriteLine("  Content-Type: application/json");
                Console.WriteLine("  Body: " + corpoJson);

                var conteudo = new StringContent(corpoJson, Encoding.UTF8, "application/json");
                var respostaHttp = http.PostAsync("cancelamento", conteudo).Result;
                var corpoResposta = respostaHttp.Content.ReadAsStringAsync().Result;

                Console.WriteLine("RESPONSE:");
                Console.WriteLine("  HTTP " + (int)respostaHttp.StatusCode);
                Console.WriteLine("  Body: " + corpoResposta);

                return new RespostaHttp { StatusCode = (int)respostaHttp.StatusCode, Corpo = corpoResposta };
            }
        }

        private static int ContarHistorico(ConfiguracaoBancoAppConfig configuracao, string vendaId)
        {
            using (var conn = AbrirConexao(configuracao))
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var repo = new VendaRepository(ctx);
                var venda = repo.ObterPorVendaId(vendaId);
                return venda == null ? 0 : venda.Historico.Count;
            }
        }

        /// <summary>
        /// Seed direto no banco real de uma venda Pendente (mesma técnica de
        /// <c>ApiHostVendasCancelamentoControllerTests</c>, T-32) — não há endpoint público de
        /// "registrar Pendente" implementado nesta sessão (<c>POST /api/vendas</c> é T-62, Tier B
        /// condicional, fora de escopo).
        /// </summary>
        private static void SemearVendaPendente(ConfiguracaoBancoAppConfig configuracao, string vendaId)
        {
            using (var conn = AbrirConexao(configuracao))
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var agora = DateTime.UtcNow.AddHours(-1);
                var itens = new[] { new VendaItem("P-T39-A", 1, 55.00m) };
                var venda = Venda.CriarPendente(vendaId, "C-T39", 55.00m, itens, agora);
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        /// <summary>Seed direto no banco real de uma venda já Quitada — cenário 409 MOTIVO_OBRIGATORIO (D-06).</summary>
        private static void SemearVendaQuitada(ConfiguracaoBancoAppConfig configuracao, string vendaId)
        {
            using (var conn = AbrirConexao(configuracao))
            using (var ctx = new FinanceiroDbContext(conn))
            {
                var agora = DateTime.UtcNow.AddHours(-1);
                var itens = new[] { new VendaItem("P-T39-B", 1, 99.90m) };
                var venda = Venda.CriarPorQuitacao(vendaId, "C-T39-QUIT", 99.90m, itens, agora, agora);
                new VendaRepository(ctx).Adicionar(venda);
                new UnitOfWork(ctx).SalvarAlteracoes();
            }
        }

        private static FbConnection AbrirConexao(ConfiguracaoBancoAppConfig configuracao)
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = configuracao.CaminhoFdb,
                UserID = configuracao.Usuario,
                Password = configuracao.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return new FbConnection(csb.ConnectionString);
        }

        private static void ApagarFdbAnterior(string caminho)
        {
            try
            {
                if (File.Exists(caminho))
                {
                    File.Delete(caminho);
                }
            }
            catch
            {
                // Melhor esforço.
            }
        }
    }
}

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Autofac;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace T38IntegracaoSimulada
{
    /// <summary>
    /// Harness descartável de T-38 (ADR-010, TASK.md Lote 8): sobe a API real (mesmo caminho de
    /// produção usado por T-30/T-31/T-32/T-34/T-37 — <see cref="ApiHost"/> + <see cref="Startup"/>
    /// + <see cref="CompositionRoot.Construir"/>) contra um Firebird embarcado real e novo, e faz
    /// o papel do "Vendas" disparando, via <see cref="HttpClient"/> real (não curl externo), os
    /// cenários do critério de aceite de T-38:
    ///   1. Venda nova quitada -> 200.
    ///   2. Repetição da mesma quitação -> idempotente, sem novo histórico (P-2) — confirmado
    ///      tanto pela resposta HTTP (mesma dataQuitacao) quanto por consulta direta ao banco
    ///      (VendaRepository/FinanceiroDbContext) contando as entradas de VendaHistorico antes e
    ///      depois da repetição.
    ///   3. GET status reflete o resultado da quitação.
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
            Console.WriteLine("================================================================================");
            Console.WriteLine("[T38] " + Banner);
            Console.WriteLine("================================================================================");

            var configuracao = new ConfiguracaoBancoAppConfig();
            var apiKey = System.Configuration.ConfigurationManager.AppSettings["Api:ApiKey"];
            var porta = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Api:Porta"]);

            ApagarFdbAnterior(configuracao.CaminhoFdb);

            Console.WriteLine("[T38] Inicializando banco (.fdb) via DbInitializer...");
            new DbInitializer(configuracao).Inicializar();

            int falhas = 0;
            int total = 0;

            using (IContainer container = CompositionRoot.Construir())
            using (var host = new ApiHost(container))
            {
                host.Start(porta);

                if (host.Estado != EstadoApiHost.Ativa)
                {
                    Console.Error.WriteLine($"[T38] Falha ao subir a API. Estado: {host.Estado}, UltimoErro: {host.UltimoErro}");
                    return 1;
                }

                Console.WriteLine($"[T38] API ATIVA em http://localhost:{porta}/ (papel do Vendas simulado por este harness)");
                Console.WriteLine();

                using (var http = new HttpClient { BaseAddress = new Uri($"http://localhost:{porta}/api/vendas/") })
                {
                    http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                    var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var vendaId = $"V-T38-SIM-{ts}";

                    // ---------------------------------------------------------------------
                    // Cenário 1 — Venda nova quitada -> 200
                    // ---------------------------------------------------------------------
                    var payloadQuitacao =
                        "{\"vendaId\":\"" + vendaId + "\",\"clienteId\":\"C-T38-SIM-1\",\"valorTotal\":300.00," +
                        "\"itens\":[{\"produtoId\":\"P-T38-A\",\"quantidade\":2,\"precoUnitario\":100.00}," +
                        "{\"produtoId\":\"P-T38-B\",\"quantidade\":1,\"precoUnitario\":100.00}]}";

                    var resp1 = ExecutarChamada(http, apiKey, "1. Quitação — venda nova (caminho feliz)", HttpMethod.Post, "quitacao", payloadQuitacao);
                    total++;
                    string dataQuitacao1 = null;
                    if (resp1.StatusCode == 200)
                    {
                        var json1 = ParseJsonSemConverterDatas(resp1.Corpo);
                        dataQuitacao1 = (string)json1["dataQuitacao"];
                        if ((string)json1["status"] == "Quitada")
                        {
                            Console.WriteLine("   [OK] HTTP 200, status=Quitada, dataQuitacao=" + dataQuitacao1);
                        }
                        else
                        {
                            Console.WriteLine("   [FALHA] status inesperado: " + json1["status"]);
                            falhas++;
                        }
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 200, obtido HTTP " + resp1.StatusCode);
                        falhas++;
                    }

                    int historicoAntes = ContarHistorico(configuracao, vendaId);
                    Console.WriteLine($"   [DB]  Consulta direta ao banco (VendaHistorico) apos cenario 1: {historicoAntes} registro(s) " +
                                       "(esperado 2: Recebida + Quitacao, D-04/CriarPorQuitacao).");
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 2 — Repetição da mesma quitação -> idempotente, sem novo histórico (P-2)
                    // ---------------------------------------------------------------------
                    var resp2 = ExecutarChamada(http, apiKey, "2. Quitação — repetição do mesmo vendaId/payload (idempotência P-2)", HttpMethod.Post, "quitacao", payloadQuitacao);
                    total++;
                    if (resp2.StatusCode == 200)
                    {
                        var json2 = ParseJsonSemConverterDatas(resp2.Corpo);
                        var dataQuitacao2 = (string)json2["dataQuitacao"];
                        var statusOk = (string)json2["status"] == "Quitada";

                        // Comparação semântica (não string bruta): dataQuitacao1 é o valor
                        // devolvido no INSTANTE em que a venda foi criada/quitada (precisão de
                        // tick do .NET, ~7 dígitos de fração), dataQuitacao2 é o MESMO valor após
                        // um round-trip por TIMESTAMP do Firebird na repetição (o serviço relê a
                        // entidade do banco antes de checar Quitar() -> idempotente), e o Firebird
                        // TIMESTAMP tem precisão de só 4 dígitos de fração (100 microssegundos) —
                        // então a string bruta pode divergir nos últimos dígitos mesmo
                        // representando o MESMO instante gravado. Comparar strings brutas aqui
                        // seria um falso-negativo de teste, não uma violação real de P-2 (a
                        // verificação definitiva e inequívoca de P-2 é a contagem de
                        // VendaHistorico via consulta direta ao banco, abaixo). Tolerância de
                        // 1 ms cobre a diferença de precisão sem mascarar uma quitação
                        // genuinamente nova (que teria uma diferença de segundos/minutos, não
                        // sub-milissegundos).
                        var t1 = DateTime.Parse(dataQuitacao1, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
                        var t2 = DateTime.Parse(dataQuitacao2, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
                        var diferenca = (t2 - t1).Duration();
                        var dataOk = diferenca <= TimeSpan.FromMilliseconds(1);

                        if (statusOk && dataOk)
                        {
                            Console.WriteLine("   [OK] HTTP 200, status=Quitada, dataQuitacao semanticamente inalterada (diferença " +
                                               diferenca.TotalMilliseconds + " ms, dentro da tolerância de precisão TIMESTAMP do Firebird) " +
                                               "— confirma que Venda.Quitar() retornou false (idempotente, RN-04) sem sobrescrever DataQuitacao.");
                            if (dataQuitacao1 != dataQuitacao2)
                            {
                                Console.WriteLine("   [NOTA] DIVERGÊNCIA DE PRECISÃO (não é violação de P-2): dataQuitacao1='" + dataQuitacao1 +
                                                   "' (precisão de tick, valor recém-criado em memória) vs dataQuitacao2='" + dataQuitacao2 +
                                                   "' (mesmo instante, após round-trip por TIMESTAMP do Firebird, que só guarda 4 dígitos de " +
                                                   "fração de segundo) — mesmo instante, string diferente pelos dígitos finais.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("   [FALHA] resposta divergente do esperado para repetição idempotente. status=" + json2["status"] +
                                               " dataQuitacao1=" + dataQuitacao1 + " dataQuitacao2=" + dataQuitacao2 +
                                               " diferenca=" + diferenca.TotalMilliseconds + "ms");
                            falhas++;
                        }
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 200, obtido HTTP " + resp2.StatusCode);
                        falhas++;
                    }

                    int historicoDepois = ContarHistorico(configuracao, vendaId);
                    total++;
                    Console.WriteLine($"   [DB]  Consulta direta ao banco (VendaHistorico) apos cenario 2 (repeticao): {historicoDepois} registro(s).");
                    if (historicoDepois == historicoAntes)
                    {
                        Console.WriteLine($"   [OK] Contagem de VendaHistorico inalterada ({historicoAntes} -> {historicoDepois}) — repeticao NAO gerou novo historico (P-2 confirmado por consulta direta ao banco).");
                    }
                    else
                    {
                        Console.WriteLine($"   [FALHA] Contagem de VendaHistorico mudou ({historicoAntes} -> {historicoDepois}) — repeticao gerou historico novo, violando P-2.");
                        falhas++;
                    }
                    Console.WriteLine();

                    // ---------------------------------------------------------------------
                    // Cenário 3 — GET status reflete o resultado da quitação
                    // ---------------------------------------------------------------------
                    var resp3 = ExecutarChamada(http, apiKey, "3. GET status — reflete o resultado da quitação", HttpMethod.Get, $"{vendaId}/status", null);
                    total++;
                    if (resp3.StatusCode == 200)
                    {
                        var json3 = ParseJsonSemConverterDatas(resp3.Corpo);
                        var vendaIdOk = (string)json3["vendaId"] == vendaId;
                        var statusOk = (string)json3["status"] == "Quitada";
                        if (vendaIdOk && statusOk)
                        {
                            Console.WriteLine("   [OK] HTTP 200, vendaId e status refletem o resultado da quitação (Quitada).");
                        }
                        else
                        {
                            Console.WriteLine("   [FALHA] corpo divergente: " + resp3.Corpo);
                            falhas++;
                        }
                    }
                    else
                    {
                        Console.WriteLine("   [FALHA] esperado HTTP 200, obtido HTTP " + resp3.StatusCode);
                        falhas++;
                    }
                    Console.WriteLine();
                }

                host.Stop();
            }

            Console.WriteLine("================================================================================");
            Console.WriteLine($"[T38] Resultado: {total - falhas}/{total} verificações OK.");
            Console.WriteLine("[T38] " + Banner);
            Console.WriteLine("================================================================================");

            return falhas > 0 ? 1 : 0;
        }

        /// <summary>
        /// Faz o parse do corpo JSON sem deixar o Newtonsoft.Json converter campos com "cara" de
        /// data (ex.: <c>dataQuitacao</c>) para <see cref="JTokenType.Date"/> automaticamente
        /// (comportamento padrão do <c>JObject.Parse</c>). Sem isso, comparar duas leituras do
        /// mesmo campo via <c>(string)token</c> passaria pelo <c>DateTime.ToString()</c> da
        /// cultura corrente (arredondando/truncando subsegundos) e poderia mascarar uma
        /// diferença real entre as duas respostas — inaceitável para a evidência de idempotência
        /// (P-2) desta tarefa.
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

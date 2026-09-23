using System;
using System.IO;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Infrastructure.Persistencia;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// Teste de integração real (T-12): depende do motor Firebird embarcado real
    /// (DLLs nativas fbclient/fbembed — ver comentário no ERPFinanceiro.Tests.csproj,
    /// reaproveitadas de spikes/T-01-firebird-spike/FirebirdSpike/bin/Debug/net48/).
    ///
    /// Cobre os 3 cenários do critério de aceite de T-12: (1) criação do .fdb com as
    /// 3 tabelas quando ausente; (2) segunda execução é no-op; (3) .fdb bloqueado por
    /// outro handle gera exceção clara (RT-05).
    ///
    /// Se `dotnet test` não conseguir carregar as DLLs nativas neste ambiente (ex.:
    /// diretório do spike não existe localmente porque T-01 não rodou antes aqui):
    /// copie manualmente o conteúdo de
    /// spikes/T-01-firebird-spike/FirebirdSpike/bin/Debug/net48/ (DLLs nativas +
    /// firebird.conf/firebird.msg/plugins/intl) para bin/Debug/net48/ deste projeto
    /// e rode de novo.
    ///
    /// Executado de fato (3 cenários, 9/9 testes verdes) via `dotnet build` +
    /// `dotnet vstest ERPFinanceiro.Tests.dll`. Observação de ambiente (não é bug
    /// do teste nem do DbInitializer): rodando `dotnet test`/`dotnet vstest`
    /// diretamente de dentro de um caminho sincronizado pelo OneDrive, o .NET
    /// Framework recusa carregar `xunit.runner.visualstudio.testadapter.dll`
    /// (FileLoadException, HRESULT 0x80131515 — "Operação sem suporte", política de
    /// zona/trust em pasta sincronizada). Contorno: copiar a pasta `bin/Debug/net48`
    /// já compilada para um diretório local fora do OneDrive (ex.: um temp local) e
    /// rodar `dotnet vstest ERPFinanceiro.Tests.dll` a partir de lá — as DLLs
    /// nativas do Firebird embarcado carregam normalmente nesse caminho.
    /// </summary>
    public class DbInitializerTests : IDisposable
    {
        private readonly string _caminhoFdb;

        public DbInitializerTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T12_DbInitializerTests_" + Guid.NewGuid().ToString("N") + ".fdb");
        }

        public void Dispose()
        {
            TentarApagar(_caminhoFdb);
        }

        private sealed class ConfiguracaoBancoTeste : IConfiguracaoBanco
        {
            public string CaminhoFdb { get; set; }
            public string Usuario { get; set; } = "sysdba";
            public string Senha { get; set; } = "masterkey";
        }

        [Fact]
        public void Inicializar_CriaBancoComAsTresTabelas_QuandoArquivoAusente()
        {
            Assert.False(File.Exists(_caminhoFdb));

            var config = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };
            new DbInitializer(config).Inicializar();

            Assert.True(File.Exists(_caminhoFdb));
            Assert.Equal(3, ContarTabelas(config));
        }

        [Fact]
        public void Inicializar_SegundaExecucao_ENoOp()
        {
            var config = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };
            var inicializador = new DbInitializer(config);

            inicializador.Inicializar();
            Assert.True(File.Exists(_caminhoFdb));
            Assert.Equal(3, ContarTabelas(config));

            // Segunda execução: arquivo já existe -> não deve tentar recriar o schema nem lançar.
            var excecao = Record.Exception(() => inicializador.Inicializar());
            Assert.Null(excecao);
            Assert.Equal(3, ContarTabelas(config));
        }

        [Fact]
        public void Inicializar_ArquivoBloqueadoPorOutroHandle_LancaExcecaoComMensagemClara()
        {
            var config = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };
            new DbInitializer(config).Inicializar(); // garante o .fdb existente antes de bloquear
            Assert.True(File.Exists(_caminhoFdb));

            using (new FileStream(_caminhoFdb, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var excecao = Assert.Throws<BancoIndisponivelException>(
                    () => new DbInitializer(config).Inicializar());

                Assert.Contains("bloqueado", excecao.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static int ContarTabelas(IConfiguracaoBanco config)
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = config.CaminhoFdb,
                UserID = config.Usuario,
                Password = config.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };

            using (var conn = new FbConnection(csb.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT COUNT(*) FROM RDB$RELATIONS WHERE RDB$RELATION_NAME IN " +
                        "('FIN_VENDA', 'FIN_VENDA_ITEM', 'FIN_VENDA_HISTORICO')";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private static void TentarApagar(string caminho)
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
                // Melhor esforço em Dispose(); não deve mascarar falha do teste.
            }
        }
    }
}

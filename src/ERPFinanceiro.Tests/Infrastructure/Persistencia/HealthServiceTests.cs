using System;
using System.IO;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Infrastructure.Persistencia;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// Cobre o critério de aceite de T-22: com `.fdb` acessível retorna ok; com
    /// caminho inválido/banco inacessível retorna falha sem lançar (SDD.md Seção 2.6,
    /// P-5). O primeiro cenário é integração real contra Firebird embarcado (mesmo
    /// contorno de ambiente documentado em <see cref="DbInitializerTests"/> — copiar
    /// `bin/Debug/net48` para fora do OneDrive se as DLLs nativas não carregarem); o
    /// segundo não precisa de banco real (a própria inexistência do arquivo já é o
    /// cenário de falha).
    /// </summary>
    public class HealthServiceTests : IDisposable
    {
        private readonly string _caminhoFdb;

        public HealthServiceTests()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _caminhoFdb = Path.Combine(baseDir, "T22_HealthServiceTests_" + Guid.NewGuid().ToString("N") + ".fdb");
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
        public void ObterStatus_ComFdbAcessivel_RetornaOk()
        {
            var config = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };
            new DbInitializer(config).Inicializar(); // garante .fdb real com schema aplicado

            var resultado = new HealthService(config).ObterStatus();

            Assert.True(resultado.Ok);
            Assert.False(string.IsNullOrWhiteSpace(resultado.Mensagem));
        }

        [Fact]
        public void ObterStatus_ComCaminhoInvalido_RetornaFalhaSemLancar()
        {
            // Diretório inexistente (nunca criado por Inicializar()): abrir uma conexão
            // Embedded direto contra ele deve falhar dentro do próprio driver Firebird.
            string caminhoInvalido = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "diretorio-inexistente-" + Guid.NewGuid().ToString("N"),
                "banco.fdb");
            var config = new ConfiguracaoBancoTeste { CaminhoFdb = caminhoInvalido };

            ResultadoHealth resultado = null;
            var excecao = Record.Exception(() => resultado = new HealthService(config).ObterStatus());

            Assert.Null(excecao); // nunca lança para o chamador
            Assert.NotNull(resultado);
            Assert.False(resultado.Ok);
            Assert.False(string.IsNullOrWhiteSpace(resultado.Mensagem));
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

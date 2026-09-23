using System;
using ERPFinanceiro.Api;
using Newtonsoft.Json;
using Xunit;

namespace ERPFinanceiro.Tests.Api
{
    /// <summary>
    /// Critério de aceite de T-25 (contrato v1.1 Seção 2 / P-6): a serialização JSON
    /// configurada em <see cref="Startup"/> produz decimal como número (não string) e
    /// data em ISO 8601 UTC terminando em "Z" — comprovado serializando um DTO de
    /// teste diretamente com <see cref="Startup.CriarConfiguracaoJson"/>, sem subir o
    /// host OWIN (o endpoint de eco manual foi removido antes de fechar a tarefa).
    /// </summary>
    public class StartupTests
    {
        private class DtoDeTeste
        {
            public decimal Valor { get; set; }
            public DateTime Data { get; set; }
        }

        [Fact]
        public void CriarConfiguracaoJson_SerializaDecimalComoNumero()
        {
            var dto = new DtoDeTeste { Valor = 1250.5m, Data = DateTime.UtcNow };

            string json = JsonConvert.SerializeObject(dto, Startup.CriarConfiguracaoJson());

            Assert.Contains("\"valor\":1250.5", json);
            Assert.DoesNotContain("\"valor\":\"1250.5\"", json);
        }

        [Fact]
        public void CriarConfiguracaoJson_SerializaDataUtcTerminandoEmZ()
        {
            var dto = new DtoDeTeste
            {
                Valor = 1m,
                Data = new DateTime(2026, 9, 22, 14, 5, 0, DateTimeKind.Utc)
            };

            string json = JsonConvert.SerializeObject(dto, Startup.CriarConfiguracaoJson());

            Assert.Contains("\"data\":\"2026-09-22T14:05:00Z\"", json);
        }

        [Fact]
        public void CriarConfiguracaoJson_ConverteDataNaoUtcParaUtcAntesDeSerializar()
        {
            // Kind=Unspecified/Local também deve terminar em "Z" (DateTimeZoneHandling.Utc).
            var dto = new DtoDeTeste
            {
                Valor = 1m,
                Data = DateTime.SpecifyKind(new DateTime(2026, 9, 22, 14, 5, 0), DateTimeKind.Unspecified)
            };

            string json = JsonConvert.SerializeObject(dto, Startup.CriarConfiguracaoJson());

            Assert.EndsWith("Z\"}", json);
        }

        [Fact]
        public void CriarConfiguracaoJson_UsaCamelCaseNasPropriedades()
        {
            var dto = new DtoDeTeste { Valor = 1250.5m, Data = DateTime.UtcNow };

            string json = JsonConvert.SerializeObject(dto, Startup.CriarConfiguracaoJson());

            Assert.Contains("\"valor\":", json);
            Assert.Contains("\"data\":", json);
            Assert.DoesNotContain("\"Valor\":", json);
            Assert.DoesNotContain("\"Data\":", json);
        }
    }
}

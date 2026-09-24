using System;
using ERPFinanceiro.Desktop;
using ERPFinanceiro.Domain.Enums;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>
    /// Critério de aceite de T-41 (UX-SPEC 3: "Formatação centralizada em uma classe
    /// Formatadores... reutilizada por grid, detalhe e relatório"): moeda no padrão
    /// pt-BR (<c>1.250,00</c>), conversão de data UTC -&gt; hora local no formato
    /// <c>dd/MM/yyyy HH:mm</c>, e status sempre com texto + ícone (nunca só cor).
    /// <see cref="Formatadores"/> é uma classe pura, sem dependência de DevExpress/
    /// Windows Forms, então roda neste sandbox sem GUI (ver <see cref="ConfiguracaoVisual"/>
    /// para a parte de skin/SVG real, fora do escopo testável aqui).
    /// </summary>
    public class FormatadoresTests
    {
        [Theory]
        [InlineData(1250.00, "1.250,00")]
        [InlineData(0, "0,00")]
        [InlineData(1234567.8, "1.234.567,80")]
        [InlineData(-42.5, "-42,50")]
        public void FormatarMoeda_deve_usar_padrao_pt_BR_com_duas_casas(double valorDouble, string esperado)
        {
            var valor = (decimal)valorDouble;

            var resultado = Formatadores.FormatarMoeda(valor);

            Assert.Equal(esperado, resultado);
        }

        [Fact]
        public void FormatarDataHoraLocal_deve_converter_utc_para_hora_local_no_formato_esperado()
        {
            var dataUtc = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
            var esperadoLocal = TimeZoneInfo.ConvertTimeFromUtc(dataUtc, TimeZoneInfo.Local);
            var esperado = esperadoLocal.ToString("dd/MM/yyyy HH:mm");

            var resultado = Formatadores.FormatarDataHoraLocal(dataUtc);

            Assert.Equal(esperado, resultado);
        }

        [Fact]
        public void FormatarDataHoraLocal_deve_tratar_Unspecified_como_utc()
        {
            var dataUnspecified = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Unspecified);
            var dataUtcEquivalente = DateTime.SpecifyKind(dataUnspecified, DateTimeKind.Utc);

            var resultado = Formatadores.FormatarDataHoraLocal(dataUnspecified);
            var esperado = Formatadores.FormatarDataHoraLocal(dataUtcEquivalente);

            Assert.Equal(esperado, resultado);
        }

        [Fact]
        public void FormatarDataHoraLocal_deve_lancar_para_Kind_Local()
        {
            var dataLocal = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Local);

            Assert.Throws<ArgumentException>(() => Formatadores.FormatarDataHoraLocal(dataLocal));
        }

        [Fact]
        public void FormatarDataHoraLocal_nullable_deve_retornar_travessao_quando_nulo()
        {
            DateTime? dataUtc = null;

            var resultado = Formatadores.FormatarDataHoraLocal(dataUtc);

            Assert.Equal("—", resultado);
        }

        [Fact]
        public void FormatarDataHoraLocal_nullable_deve_formatar_quando_presente()
        {
            DateTime? dataUtc = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

            var resultado = Formatadores.FormatarDataHoraLocal(dataUtc);

            Assert.Equal(Formatadores.FormatarDataHoraLocal(dataUtc.Value), resultado);
        }

        [Fact]
        public void FormatarStatus_Quitada_deve_retornar_texto_e_icone_check()
        {
            var resultado = Formatadores.FormatarStatus(StatusVenda.Quitada);

            Assert.Equal("Quitada", resultado.Texto);
            Assert.Equal(IconeStatus.Check, resultado.Icone);
        }

        [Fact]
        public void FormatarStatus_Pendente_deve_retornar_texto_e_icone_relogio()
        {
            var resultado = Formatadores.FormatarStatus(StatusVenda.Pendente);

            Assert.Equal("Pendente", resultado.Texto);
            Assert.Equal(IconeStatus.Relogio, resultado.Icone);
        }

        [Fact]
        public void FormatarStatus_Cancelada_deve_retornar_texto_e_icone_x()
        {
            var resultado = Formatadores.FormatarStatus(StatusVenda.Cancelada);

            Assert.Equal("Cancelada", resultado.Texto);
            Assert.Equal(IconeStatus.X, resultado.Icone);
        }

        [Fact]
        public void FormatarStatus_valor_invalido_deve_lancar()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Formatadores.FormatarStatus((StatusVenda)99));
        }
    }
}

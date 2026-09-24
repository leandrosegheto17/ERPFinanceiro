using System;
using System.Collections.Generic;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Domain.Exceptions;
using Xunit;

namespace ERPFinanceiro.Tests.Domain
{
    /// <summary>
    /// T-63 (S-11 reduzido): matriz da maquina de estados de Venda (3 estados x 2 operacoes),
    /// regra de motivo (S-05) e venda desconhecida (D-08). Somente Domain, sem Moq.
    /// </summary>
    public class VendaMaquinaEstadosTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime T1 = T0.AddHours(1);
        private static readonly DateTime T2 = T0.AddHours(2);
        private const string Motivo = "Cliente desistiu";

        private static Venda Criar(StatusVenda status)
        {
            var itens = new List<VendaItem> { new VendaItem("PROD-1", 1, 10m) };
            var venda = Venda.CriarPendente("V-M", "CLI-1", 10m, itens, T0);
            if (status == StatusVenda.Quitada)
            {
                venda.Quitar(T1);
            }
            else if (status == StatusVenda.Cancelada)
            {
                venda.Cancelar(T1, Motivo);
            }
            return venda;
        }

        // Estado inicial x Quitar: resultado esperado (true/false/lanca).
        [Theory]
        [InlineData(StatusVenda.Pendente, true, false, StatusVenda.Quitada)]
        [InlineData(StatusVenda.Quitada, false, false, StatusVenda.Quitada)]
        [InlineData(StatusVenda.Cancelada, false, true, StatusVenda.Cancelada)]
        public void Matriz_Quitar(StatusVenda inicial, bool mudou, bool lanca, StatusVenda esperado)
        {
            var venda = Criar(inicial);
            var historicoAntes = venda.Historico.Count;

            if (lanca)
            {
                Assert.Throws<VendaJaCanceladaException>(() => venda.Quitar(T2));
                Assert.Equal(historicoAntes, venda.Historico.Count);
            }
            else
            {
                Assert.Equal(mudou, venda.Quitar(T2));
                Assert.Equal(historicoAntes + (mudou ? 1 : 0), venda.Historico.Count);
            }

            Assert.Equal(esperado, venda.Status);
        }

        // Estado inicial x Cancelar (com motivo informado): sempre termina Cancelada.
        [Theory]
        [InlineData(StatusVenda.Pendente, true)]
        [InlineData(StatusVenda.Quitada, true)]
        [InlineData(StatusVenda.Cancelada, false)]
        public void Matriz_Cancelar_ComMotivo(StatusVenda inicial, bool mudou)
        {
            var venda = Criar(inicial);
            var historicoAntes = venda.Historico.Count;

            Assert.Equal(mudou, venda.Cancelar(T2, "Outro motivo"));

            Assert.Equal(StatusVenda.Cancelada, venda.Status);
            Assert.Equal(historicoAntes + (mudou ? 1 : 0), venda.Historico.Count);
            if (!mudou)
            {
                // idempotente: dados da primeira quitacao/cancelamento preservados
                Assert.Equal(T1, venda.DataCancelamento);
                Assert.Equal(Motivo, venda.MotivoCancelamento);
            }
        }

        // Estado inicial x Cancelar (sem motivo): so Quitada rejeita.
        [Theory]
        [InlineData(StatusVenda.Pendente, null)]
        [InlineData(StatusVenda.Pendente, "")]
        [InlineData(StatusVenda.Cancelada, null)]
        [InlineData(StatusVenda.Cancelada, "  ")]
        public void Matriz_Cancelar_SemMotivo_NaoLanca(StatusVenda inicial, string motivo)
        {
            var venda = Criar(inicial);

            venda.Cancelar(T2, motivo);

            Assert.Equal(StatusVenda.Cancelada, venda.Status);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Cancelar_Quitada_MotivoVazio_LancaSemAlterarEstado(string motivo)
        {
            var venda = Criar(StatusVenda.Quitada);
            var historicoAntes = venda.Historico.Count;

            Assert.Throws<MotivoObrigatorioException>(() => venda.Cancelar(T2, motivo));

            Assert.Equal(StatusVenda.Quitada, venda.Status);
            Assert.Null(venda.DataCancelamento);
            Assert.Null(venda.MotivoCancelamento);
            Assert.Equal(T1, venda.DataQuitacao);
            Assert.Equal(historicoAntes, venda.Historico.Count);
        }

        [Fact]
        public void Cancelar_Quitada_ComMotivo_RegistraStatusAnteriorQuitada()
        {
            var venda = Criar(StatusVenda.Quitada);

            venda.Cancelar(T2, Motivo, "corr-x");

            var reg = venda.Historico[venda.Historico.Count - 1];
            Assert.Equal(StatusVenda.Quitada, reg.StatusAnterior);
            Assert.Equal(Motivo, reg.Motivo);
            Assert.Equal(Motivo, venda.MotivoCancelamento);
            Assert.Equal(T2, venda.DataCancelamento);
        }

        [Fact]
        public void Quitar_Pendente_NaoAlteraDataCancelamentoEMantemDataQuitacao()
        {
            var venda = Criar(StatusVenda.Pendente);

            venda.Quitar(T1);
            venda.Quitar(T2);

            Assert.Equal(T1, venda.DataQuitacao);
            Assert.Null(venda.DataCancelamento);
        }

        [Fact]
        public void VendaDesconhecida_CriarCancelada_QuitarLanca()
        {
            var venda = Venda.CriarCancelada("V-D", "nao localizada", T0);

            var ex = Assert.Throws<VendaJaCanceladaException>(() => venda.Quitar(T1));

            Assert.Equal("V-D", ex.VendaId);
            Assert.Single(venda.Historico);
        }

        [Fact]
        public void VendaDesconhecida_CriarCancelada_CancelarEIdempotente()
        {
            var venda = Venda.CriarCancelada("V-D", "nao localizada", T0);

            Assert.False(venda.Cancelar(T1, "outro"));

            Assert.Equal("nao localizada", venda.MotivoCancelamento);
            Assert.Equal(T0, venda.DataCancelamento);
            Assert.Single(venda.Historico);
        }

        [Fact]
        public void Cancelada_EstadoTerminal_NaoVoltaPendenteNemQuitada()
        {
            var venda = Criar(StatusVenda.Cancelada);

            Assert.Throws<VendaJaCanceladaException>(() => venda.Quitar(T2));
            Assert.False(venda.Cancelar(T2));

            Assert.Equal(StatusVenda.Cancelada, venda.Status);
            Assert.Null(venda.DataQuitacao);
        }
    }
}

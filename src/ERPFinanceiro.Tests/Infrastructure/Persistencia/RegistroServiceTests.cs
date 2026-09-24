using System;
using System.Collections.Generic;
using System.IO;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Servicos;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Infrastructure.Persistencia;
using Xunit;

namespace ERPFinanceiro.Tests.Infrastructure.Persistencia
{
    /// <summary>
    /// Integracao real (T-61) de <see cref="RegistroService"/> contra Firebird embarcado
    /// (mesmo mecanismo de CancelamentoServiceTests). CA-04.1/04.2 (RF-04, D-03/P-9).
    /// </summary>
    public class RegistroServiceTests : IDisposable
    {
        private readonly string _caminhoFdb;
        private readonly IConfiguracaoBanco _configuracao;
        private readonly RelogioFixo _relogio = new RelogioFixo(new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc));

        public RegistroServiceTests()
        {
            _caminhoFdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "T61_RegistroServiceTests_" + Guid.NewGuid().ToString("N") + ".fdb");
            _configuracao = new ConfiguracaoBancoTeste { CaminhoFdb = _caminhoFdb };
            new DbInitializer(_configuracao).Inicializar();
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_caminhoFdb))
                {
                    File.Delete(_caminhoFdb);
                }
            }
            catch
            {
            }
        }

        private sealed class ConfiguracaoBancoTeste : IConfiguracaoBanco
        {
            public string CaminhoFdb { get; set; }
            public string Usuario { get; set; } = "sysdba";
            public string Senha { get; set; } = "masterkey";
        }

        private sealed class RelogioFixo : IClock
        {
            public RelogioFixo(DateTime utcNow) { UtcNow = utcNow; }
            public DateTime UtcNow { get; set; }
        }

        private sealed class CorrelacaoFixa : ICorrelationContext
        {
            public string CorrelationId { get { return "corr-t61-q"; } }
        }

        private RegistroService CriarServico()
        {
            return new RegistroService(new FabricaEscopoOperacaoEf(_configuracao), _relogio);
        }

        private static List<ItemVendaCommand> Itens()
        {
            return new List<ItemVendaCommand>
            {
                new ItemVendaCommand { ProdutoId = "PROD-T61", Quantidade = 1, PrecoUnitario = 100.0000m }
            };
        }

        private static RegistrarVendaCommand Comando(string vendaId)
        {
            return new RegistrarVendaCommand
            {
                VendaId = vendaId,
                ClienteId = "CLI-T61",
                ValorTotal = 100.00m,
                Itens = Itens(),
                CorrelationId = "corr-t61"
            };
        }

        private Venda Ler(string vendaId)
        {
            using (var escopo = new FabricaEscopoOperacaoEf(_configuracao).Abrir())
            {
                var v = escopo.Repositorio.ObterPorVendaId(vendaId);
                var materializa = v.Historico.Count + v.Itens.Count;
                Assert.True(materializa >= 0);
                return v;
            }
        }

        [Fact]
        public void Registrar_VendaNova_CriaPendenteComUmHistoricoRecebida()
        {
            var resultado = CriarServico().Registrar(Comando("V-T61-NOVA"));

            Assert.Equal(StatusVenda.Pendente, resultado.Status);

            var lida = Ler("V-T61-NOVA");
            Assert.Equal(StatusVenda.Pendente, lida.Status);
            Assert.Equal("CLI-T61", lida.ClienteId);
            Assert.Single(lida.Itens);
            var h = Assert.Single(lida.Historico);
            Assert.Equal(Operacao.Recebida, h.Operacao);
            Assert.Equal("corr-t61", h.CorrelationId);
        }

        [Fact]
        public void Registrar_Repeticao_IdempotenteSemNovoHistorico()
        {
            CriarServico().Registrar(Comando("V-T61-REP"));
            var segundo = CriarServico().Registrar(Comando("V-T61-REP"));

            Assert.Equal(StatusVenda.Pendente, segundo.Status);
            Assert.Single(Ler("V-T61-REP").Historico);
        }

        [Fact]
        public void Registrar_VendaJaQuitada_DevolveStatusAtualSemAlterar()
        {
            CriarServico().Registrar(Comando("V-T61-QUIT"));
            var quitacao = new QuitacaoService(new FabricaEscopoOperacaoEf(_configuracao), _relogio, new CorrelacaoFixa());
            quitacao.Executar(new QuitarVendaCommand
            {
                VendaId = "V-T61-QUIT",
                ClienteId = "CLI-T61",
                ValorTotal = 100.00m,
                Itens = Itens()
            });

            var resultado = CriarServico().Registrar(Comando("V-T61-QUIT"));

            Assert.Equal(StatusVenda.Quitada, resultado.Status);
            var lida = Ler("V-T61-QUIT");
            Assert.Equal(StatusVenda.Quitada, lida.Status);
            Assert.Equal(2, lida.Historico.Count);
        }

        [Fact]
        public void Registrar_PayloadInvalido_LancaValidacaoException()
        {
            var c = Comando("V-T61-INV");
            c.Itens = new List<ItemVendaCommand>();
            Assert.Throws<ValidacaoException>(() => CriarServico().Registrar(c));
        }
    }
}

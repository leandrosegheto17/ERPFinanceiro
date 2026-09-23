using System;
using System.Linq;
using System.Reflection;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using Moq;
using Xunit;

namespace ERPFinanceiro.Tests.Application
{
    /// <summary>
    /// Testes estruturais dos tipos de Application definidos em T-13. Não exercitam
    /// Firebird/EF (nenhuma dependência de Infrastructure); confirmam apenas a forma
    /// do contrato: IVendaRepository expõe só busca + Adicionar (sem update genérico),
    /// e os DTOs/comandos carregam os campos esperados pelo contrato-v1.1.md.
    /// </summary>
    public class ApplicationContratosTests
    {
        [Fact]
        public void IVendaRepository_NaoExpoeMetodoDeAtualizacaoGenerica()
        {
            var metodos = typeof(IVendaRepository)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.Name)
                .ToArray();

            Assert.Contains("ObterPorVendaId", metodos);
            Assert.Contains("Adicionar", metodos);
            Assert.DoesNotContain(metodos, nome => nome.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0
                                                    || nome.IndexOf("Atualizar", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.Equal(2, metodos.Length);
        }

        [Fact]
        public void IVendaRepository_MockConsegueSimularBuscaEAdicionar()
        {
            var venda = Venda.CriarPendente(
                "V-001", "CLI-1", 10m,
                new[] { new VendaItem("PROD-1", 1, 10m) },
                new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc));

            var repositorioMock = new Mock<IVendaRepository>();
            repositorioMock.Setup(r => r.ObterPorVendaId("V-001")).Returns(venda);
            repositorioMock.Setup(r => r.ObterPorVendaId("INEXISTENTE")).Returns((Venda)null);

            Assert.Same(venda, repositorioMock.Object.ObterPorVendaId("V-001"));
            Assert.Null(repositorioMock.Object.ObterPorVendaId("INEXISTENTE"));

            repositorioMock.Object.Adicionar(venda);
            repositorioMock.Verify(r => r.Adicionar(venda), Times.Once);
        }

        [Fact]
        public void IUnitOfWork_ExposeSalvarAlteracoesSincrono()
        {
            var metodo = typeof(IUnitOfWork).GetMethod("SalvarAlteracoes");

            Assert.NotNull(metodo);
            Assert.Equal(typeof(void), metodo.ReturnType);
            Assert.Empty(metodo.GetParameters());
        }

        [Fact]
        public void IClock_ExposePropriedadeUtcNowSomenteLeitura()
        {
            var clockMock = new Mock<IClock>();
            var instante = new DateTime(2026, 9, 22, 15, 30, 0, DateTimeKind.Utc);
            clockMock.SetupGet(c => c.UtcNow).Returns(instante);

            Assert.Equal(instante, clockMock.Object.UtcNow);

            var propriedade = typeof(IClock).GetProperty("UtcNow");
            Assert.False(propriedade.CanWrite);
        }

        [Fact]
        public void IAppLogger_RegistrarRecebeMensagemECorrelationId()
        {
            var loggerMock = new Mock<IAppLogger>();

            loggerMock.Object.Registrar("falha ao processar", "corr-123");

            loggerMock.Verify(l => l.Registrar("falha ao processar", "corr-123"), Times.Once);
        }

        [Fact]
        public void ICorrelationContext_ExposeCorrelationIdSomenteLeitura()
        {
            var propriedade = typeof(ICorrelationContext).GetProperty("CorrelationId");

            Assert.NotNull(propriedade);
            Assert.Equal(typeof(string), propriedade.PropertyType);
            Assert.False(propriedade.CanWrite);
        }

        [Fact]
        public void QuitarVendaCommand_CarregaCamposDoContratoV11Secao31()
        {
            var comando = new QuitarVendaCommand
            {
                VendaId = "V-000123",
                ClienteId = "C-4521",
                ValorTotal = 1250.50m,
                Itens = new System.Collections.Generic.List<ItemVendaCommand>
                {
                    new ItemVendaCommand { ProdutoId = "P-01", Quantidade = 2, PrecoUnitario = 500.00m },
                    new ItemVendaCommand { ProdutoId = "P-02", Quantidade = 1, PrecoUnitario = 250.50m }
                },
                CorrelationId = "corr-abc"
            };

            Assert.Equal("V-000123", comando.VendaId);
            Assert.Equal("C-4521", comando.ClienteId);
            Assert.Equal(1250.50m, comando.ValorTotal);
            Assert.Equal(2, comando.Itens.Count);
            Assert.Equal("corr-abc", comando.CorrelationId);
        }

        [Fact]
        public void CancelarVendaCommand_MotivoEOpcionalNoTipo()
        {
            var comando = new CancelarVendaCommand { VendaId = "V-000123" };

            Assert.Equal("V-000123", comando.VendaId);
            Assert.Null(comando.Motivo);
        }

        [Fact]
        public void RegistrarVendaCommand_MesmoFormatoDeQuitarVendaCommand()
        {
            var comando = new RegistrarVendaCommand
            {
                VendaId = "V-000200",
                ClienteId = "C-01",
                ValorTotal = 100m,
                Itens = new System.Collections.Generic.List<ItemVendaCommand>
                {
                    new ItemVendaCommand { ProdutoId = "P-9", Quantidade = 1, PrecoUnitario = 100m }
                }
            };

            Assert.Equal("V-000200", comando.VendaId);
            Assert.Single(comando.Itens);
        }

        [Fact]
        public void FiltroVendas_TodosOsCamposSaoOpcionais()
        {
            var filtro = new FiltroVendas();

            Assert.Null(filtro.PeriodoInicioUtc);
            Assert.Null(filtro.PeriodoFimUtc);
            Assert.Null(filtro.ClienteId);
            Assert.Null(filtro.Status);
        }
    }
}

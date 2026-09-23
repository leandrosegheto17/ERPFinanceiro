using System.Collections.Generic;
using System.Linq;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Validacao;
using Xunit;

namespace ERPFinanceiro.Tests.Application
{
    /// <summary>
    /// Tabela de casos para <see cref="ValidadorVendaCommand"/> (T-17), cobrindo
    /// CA-01.5 (payload inválido) e CA-01.6 (tolerância de valorTotal, P-7/RN-02).
    /// Não exercita Firebird/EF (Application pura) — sem dependência de Infrastructure.
    /// </summary>
    public class ValidadorVendaCommandTests
    {
        private static QuitarVendaCommand ComandoValido()
        {
            return new QuitarVendaCommand
            {
                VendaId = "V-000123",
                ClienteId = "C-4521",
                ValorTotal = 1250.50m,
                Itens = new List<ItemVendaCommand>
                {
                    new ItemVendaCommand { ProdutoId = "P-01", Quantidade = 2, PrecoUnitario = 500.00m },
                    new ItemVendaCommand { ProdutoId = "P-02", Quantidade = 1, PrecoUnitario = 250.50m }
                }
            };
        }

        [Fact]
        public void Validar_ComandoValido_RetornaSucesso()
        {
            var resultado = ValidadorVendaCommand.Validar(ComandoValido());

            Assert.True(resultado.Sucesso);
            Assert.Empty(resultado.Erros);
        }

        [Fact]
        public void Validar_VendaIdVazio_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.VendaId = "";

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_VendaIdNulo_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.VendaId = null;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_ClienteIdVazio_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.ClienteId = "   ";

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_ItensVazio_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.Itens = new List<ItemVendaCommand>();

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_ItensNulo_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.Itens = null;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_QuantidadeZero_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.Itens[0].Quantidade = 0;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_QuantidadeNegativa_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.Itens[0].Quantidade = -1;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_PrecoUnitarioNegativo_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.Itens[0].PrecoUnitario = -0.01m;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_PrecoUnitarioZero_EValido()
        {
            var comando = ComandoValido();
            comando.Itens[0].PrecoUnitario = 0m;
            comando.ValorTotal = comando.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.True(resultado.Sucesso);
        }

        [Fact]
        public void Validar_ProdutoIdVazio_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.Itens[0].ProdutoId = "";

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_VendaIdAcimaDoLimiteDe50Caracteres_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.VendaId = new string('V', 51);

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_VendaIdComExatamente50Caracteres_EValido()
        {
            var comando = ComandoValido();
            comando.VendaId = new string('V', 50);

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.True(resultado.Sucesso);
        }

        [Fact]
        public void Validar_MaisDeMilItens_RetornaPayloadInvalido()
        {
            var comando = ComandoValido();
            comando.Itens = Enumerable.Range(1, 1001)
                .Select(i => new ItemVendaCommand { ProdutoId = $"P-{i}", Quantidade = 1, PrecoUnitario = 1m })
                .ToList();
            comando.ValorTotal = 1001m;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_ComMilItens_NaoViolaLimite()
        {
            var comando = ComandoValido();
            comando.Itens = Enumerable.Range(1, 1000)
                .Select(i => new ItemVendaCommand { ProdutoId = $"P-{i}", Quantidade = 1, PrecoUnitario = 1m })
                .ToList();
            comando.ValorTotal = 1000m;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.True(resultado.Sucesso);
        }

        [Fact]
        public void Validar_DiferencaDeUmCentavo_EAceita()
        {
            var comando = ComandoValido();
            // soma dos itens = 1250.50; valorTotal com diferença de exatamente 0,01
            comando.ValorTotal = 1250.51m;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.True(resultado.Sucesso);
        }

        [Fact]
        public void Validar_DiferencaDeDoisCentavos_ERejeitadaComoValorTotalDivergente()
        {
            var comando = ComandoValido();
            // soma dos itens = 1250.50; valorTotal com diferença de 0,02
            comando.ValorTotal = 1250.52m;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "VALOR_TOTAL_DIVERGENTE");
        }

        [Fact]
        public void Validar_ValorTotalMenorQueSomaAlemDaTolerancia_ERejeitado()
        {
            var comando = ComandoValido();
            comando.ValorTotal = 1000.00m;

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "VALOR_TOTAL_DIVERGENTE");
        }

        [Fact]
        public void Validar_RegistrarVendaCommand_MesmasRegrasDeQuitarVendaCommand()
        {
            var comando = new RegistrarVendaCommand
            {
                VendaId = "V-000200",
                ClienteId = "C-01",
                ValorTotal = 100m,
                Itens = new List<ItemVendaCommand>
                {
                    new ItemVendaCommand { ProdutoId = "P-9", Quantidade = 1, PrecoUnitario = 100m }
                }
            };

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.True(resultado.Sucesso);
        }

        [Fact]
        public void Validar_RegistrarVendaCommandComItensVazio_RetornaPayloadInvalido()
        {
            var comando = new RegistrarVendaCommand
            {
                VendaId = "V-000200",
                ClienteId = "C-01",
                ValorTotal = 0m,
                Itens = new List<ItemVendaCommand>()
            };

            var resultado = ValidadorVendaCommand.Validar(comando);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }

        [Fact]
        public void Validar_ComandoNulo_RetornaPayloadInvalido()
        {
            var resultado = ValidadorVendaCommand.Validar((QuitarVendaCommand)null);

            Assert.False(resultado.Sucesso);
            Assert.Contains(resultado.Erros, e => e.Codigo == "PAYLOAD_INVALIDO");
        }
    }
}

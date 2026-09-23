using System.Collections.Generic;
using System.Linq;
using ERPFinanceiro.Application.Commands;

namespace ERPFinanceiro.Application.Validacao
{
    /// <summary>
    /// Validação de payload e total (T-17) para <see cref="QuitarVendaCommand"/> e
    /// <see cref="RegistrarVendaCommand"/> (mesmo formato de payload, contrato-v1.1.md
    /// Seções 3.1/3.4). Cobre CA-01.5 (IDs vazios/`itens` vazio/`quantidade`&lt;=0/
    /// `precoUnitario`&lt;0) e CA-01.6 (tolerância de 0,01 em `valorTotal`, P-7/RN-02).
    /// Nunca lança exceção para erro de validação esperado — devolve
    /// <see cref="ResultadoValidacao"/> tipado; a tradução para HTTP 400 é
    /// responsabilidade da Api (T-30/T-32).
    /// </summary>
    /// <remarks>
    /// Interpretação adotada (desvio pequeno, documentado conforme guardrail do
    /// Executor) para os limites "50/500/1000" citados na linha T-17 do TASK.md: o
    /// texto da própria tarefa não detalha a qual campo cada número se aplica; a fonte
    /// primária é <c>SDD.md</c> Seção 7 ("Entrada"): tamanho máximo de 50 caracteres
    /// para IDs (`vendaId`/`clienteId`/`produtoId`), 500 para `motivo` (campo que só
    /// existe em <see cref="CancelarVendaCommand"/>, fora do escopo de itens/total desta
    /// tarefa — não validado aqui) e no máximo 1000 itens por payload. O limite de
    /// "corpo &lt;= 1 MB" do mesmo trecho do SDD.md é responsabilidade de camada HTTP
    /// (Api/OWIN), não da Application, e também não é tratado aqui.
    /// </remarks>
    public static class ValidadorVendaCommand
    {
        private const string CodigoPayloadInvalido = "PAYLOAD_INVALIDO";
        private const string CodigoValorTotalDivergente = "VALOR_TOTAL_DIVERGENTE";

        private const int TamanhoMaximoId = 50;
        private const int LimiteMaximoItens = 1000;
        private const decimal ToleranciaValorTotal = 0.01m;

        public static ResultadoValidacao Validar(QuitarVendaCommand comando)
        {
            if (comando == null)
            {
                return ResultadoValidacao.ComErros(new[]
                {
                    new ErroValidacao(CodigoPayloadInvalido, "O corpo da requisição é obrigatório.")
                });
            }

            return ValidarInterno(comando.VendaId, comando.ClienteId, comando.ValorTotal, comando.Itens);
        }

        public static ResultadoValidacao Validar(RegistrarVendaCommand comando)
        {
            if (comando == null)
            {
                return ResultadoValidacao.ComErros(new[]
                {
                    new ErroValidacao(CodigoPayloadInvalido, "O corpo da requisição é obrigatório.")
                });
            }

            return ValidarInterno(comando.VendaId, comando.ClienteId, comando.ValorTotal, comando.Itens);
        }

        private static ResultadoValidacao ValidarInterno(
            string vendaId,
            string clienteId,
            decimal valorTotal,
            List<ItemVendaCommand> itens)
        {
            var erros = new List<ErroValidacao>();

            if (string.IsNullOrWhiteSpace(vendaId))
            {
                erros.Add(new ErroValidacao(CodigoPayloadInvalido, "O campo 'vendaId' é obrigatório."));
            }
            else if (vendaId.Length > TamanhoMaximoId)
            {
                erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O campo 'vendaId' excede o tamanho máximo de {TamanhoMaximoId} caracteres."));
            }

            if (string.IsNullOrWhiteSpace(clienteId))
            {
                erros.Add(new ErroValidacao(CodigoPayloadInvalido, "O campo 'clienteId' é obrigatório."));
            }
            else if (clienteId.Length > TamanhoMaximoId)
            {
                erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O campo 'clienteId' excede o tamanho máximo de {TamanhoMaximoId} caracteres."));
            }

            if (itens == null || itens.Count == 0)
            {
                erros.Add(new ErroValidacao(CodigoPayloadInvalido, "O campo 'itens' não pode ser vazio."));
            }
            else
            {
                if (itens.Count > LimiteMaximoItens)
                {
                    erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O campo 'itens' excede o limite máximo de {LimiteMaximoItens} itens."));
                }

                for (var i = 0; i < itens.Count; i++)
                {
                    var item = itens[i];

                    if (item == null)
                    {
                        erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O item na posição {i} é inválido."));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(item.ProdutoId))
                    {
                        erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O campo 'itens[{i}].produtoId' é obrigatório."));
                    }
                    else if (item.ProdutoId.Length > TamanhoMaximoId)
                    {
                        erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O campo 'itens[{i}].produtoId' excede o tamanho máximo de {TamanhoMaximoId} caracteres."));
                    }

                    if (item.Quantidade <= 0)
                    {
                        erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O campo 'itens[{i}].quantidade' deve ser maior que zero."));
                    }

                    if (item.PrecoUnitario < 0)
                    {
                        erros.Add(new ErroValidacao(CodigoPayloadInvalido, $"O campo 'itens[{i}].precoUnitario' não pode ser negativo."));
                    }
                }

                var somaItens = itens
                    .Where(item => item != null)
                    .Sum(item => item.Quantidade * item.PrecoUnitario);
                var diferenca = valorTotal - somaItens;
                if (diferenca < 0)
                {
                    diferenca = -diferenca;
                }

                if (diferenca > ToleranciaValorTotal)
                {
                    erros.Add(new ErroValidacao(
                        CodigoValorTotalDivergente,
                        $"valorTotal ({valorTotal}) diverge da soma dos itens ({somaItens}) além da tolerância de 0,01."));
                }
            }

            return ResultadoValidacao.ComErros(erros);
        }
    }
}

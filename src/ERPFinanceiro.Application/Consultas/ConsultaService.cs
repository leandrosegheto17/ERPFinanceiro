using System;
using System.Collections.Generic;
using System.Linq;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Domain.Entities;
using ERPFinanceiro.Domain.Exceptions;

namespace ERPFinanceiro.Application.Consultas
{
    /// <summary>
    /// Serviço de leitura sobre o agregado Venda (T-13/T-23/T-24). Criado por T-24 com
    /// <see cref="ObterDetalhe"/>/<see cref="ObterStatus"/>; T-23 (paralela, mesmo lote)
    /// adiciona <see cref="Listar"/> a esta mesma classe (ADR-008: a tela usa este
    /// serviço em processo, nunca HTTP).
    /// </summary>
    public class ConsultaService
    {
        /// <summary>Limite de linhas da listagem (T-23/UX-SPEC 2.1). O relatório (T-48) não usa este limite.</summary>
        public const int LimiteListagem = 5000;

        private readonly IVendaRepository _repositorio;
        private readonly IVendaConsultaLeitura _leitura;

        /// <summary>
        /// Construtor original de T-24 (<c>ObterDetalhe</c>/<c>ObterStatus</c>), preservado
        /// como está para não quebrar os testes/mocks já escritos contra ele
        /// (<c>Mock&lt;IVendaRepository&gt;</c>, <c>ApplicationContratosTests</c>).
        /// <paramref name="leitura"/> é opcional (nulo por padrão): só é exigido por
        /// <see cref="Listar"/> (T-23). Na composição real (Autofac, T-40), a mesma
        /// instância de <c>VendaRepository</c> implementa as duas interfaces e deve ser
        /// passada para ambos os parâmetros.
        /// </summary>
        public ConsultaService(IVendaRepository repositorio, IVendaConsultaLeitura leitura = null)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _leitura = leitura;
        }

        /// <summary>
        /// Lista vendas ordenadas por <c>DataRecebimento</c> desc, no máximo
        /// <see cref="LimiteListagem"/> itens, com nulos preservados no DTO (a tela
        /// decide exibir "—", não este serviço). Pede <c>LimiteListagem + 1</c> à camada
        /// de leitura para detectar truncamento sem uma segunda consulta de contagem.
        /// <para>
        /// <paramref name="filtro"/> é deliberadamente ignorado por ora — a aplicação
        /// real de <see cref="FiltroVendas"/> (período, cliente, status) é T-57 (Tier B).
        /// Até lá, <see cref="Listar"/> sempre devolve tudo, respeitando só
        /// ordenação/limite/truncamento.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Este <see cref="ConsultaService"/> foi construído sem <see cref="IVendaConsultaLeitura"/>
        /// (parâmetro <c>leitura</c> do construtor).
        /// </exception>
        public ResultadoListagemVendas Listar(FiltroVendas filtro)
        {
            if (_leitura == null)
            {
                throw new InvalidOperationException(
                    "ConsultaService.Listar requer IVendaConsultaLeitura (parâmetro 'leitura' do construtor, T-23).");
            }

            IReadOnlyList<Venda> vendas = _leitura.ListarMaisRecentes(LimiteListagem + 1);

            bool truncado = vendas.Count > LimiteListagem;
            IEnumerable<Venda> pagina = truncado ? vendas.Take(LimiteListagem) : vendas;

            List<VendaListagemDto> itens = pagina.Select(MapearParaListagemDto).ToList();

            return new ResultadoListagemVendas(itens, truncado);
        }

        /// <summary>
        /// Lista vendas para o relatório financeiro (T-48/UX-SPEC 2.3), ordenadas por
        /// <c>DataRecebimento</c> desc, **sem** o limite de 5.000 (TASK.md Seção 1 regra
        /// 15 — usa <see cref="IVendaConsultaLeitura.ListarTodas"/>, não
        /// <see cref="ListarMaisRecentes"/>). "Total listado" e a contagem de vendas com
        /// valor nulo (nota de rodapé) são calculados **da mesma lista** devolvida
        /// (<see cref="ResultadoRelatorioVendas"/>), nunca por uma segunda consulta.
        /// <para>
        /// Mesma decisão já tomada em <see cref="Listar"/> (T-23): <paramref name="filtro"/>
        /// é deliberadamente ignorado por ora — a aplicação real de
        /// <see cref="FiltroVendas"/> é T-57 (Tier B). Até lá, sempre devolve tudo.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Este <see cref="ConsultaService"/> foi construído sem <see cref="IVendaConsultaLeitura"/>
        /// (parâmetro <c>leitura</c> do construtor).
        /// </exception>
        public ResultadoRelatorioVendas ListarParaRelatorio(FiltroVendas filtro)
        {
            if (_leitura == null)
            {
                throw new InvalidOperationException(
                    "ConsultaService.ListarParaRelatorio requer IVendaConsultaLeitura (parâmetro 'leitura' do construtor, T-23/T-48).");
            }

            IReadOnlyList<Venda> vendas = _leitura.ListarTodas();

            List<LinhaRelatorio> linhas = vendas.Select(MapearParaLinhaRelatorio).ToList();

            decimal totalListado = linhas.Sum(l => l.ValorTotal ?? 0m);
            int quantidadeSemValor = linhas.Count(l => l.ValorTotal == null);

            return new ResultadoRelatorioVendas(linhas, totalListado, quantidadeSemValor);
        }

        private static LinhaRelatorio MapearParaLinhaRelatorio(Venda venda)
        {
            return new LinhaRelatorio
            {
                VendaId = venda.VendaId,
                ClienteId = venda.ClienteId,
                ValorTotal = venda.ValorTotal,
                Status = venda.Status,
                DataRecebimento = venda.DataRecebimento,
                DataQuitacao = venda.DataQuitacao,
                DataCancelamento = venda.DataCancelamento
            };
        }

        private static VendaListagemDto MapearParaListagemDto(Venda venda)
        {
            return new VendaListagemDto
            {
                VendaId = venda.VendaId,
                ClienteId = venda.ClienteId,
                ValorTotal = venda.ValorTotal,
                Status = venda.Status,
                DataRecebimento = venda.DataRecebimento,
                DataQuitacao = venda.DataQuitacao,
                DataCancelamento = venda.DataCancelamento
            };
        }

        /// <summary>
        /// Cabeçalho + itens (com subtotal calculado) de uma venda. Histórico é
        /// carregado por <see cref="IVendaRepository.ObterPorVendaId"/> (inclui
        /// itens+histórico, T-14) mas deliberadamente **não** exposto no DTO de saída
        /// (RF-03, Tier C — aba de histórico fora da tela de detalhe, T-44).
        /// <para>
        /// Decisão de padrão (venda inexistente): lança <see cref="VendaNaoEncontradaException"/>
        /// em vez de devolver <c>null</c>. Motivo: a exceção já existe (criada por
        /// antecipação em T-28) especificamente para este cenário e já está mapeada
        /// pelo <c>GlobalExceptionHandler</c> para 404 <c>VENDA_NAO_ENCONTRADA</c> — reaproveitar
        /// o mecanismo evita que cada controller (T-31/T-32) precise checar `null` e
        /// traduzir para 404 na mão. O critério de aceite de T-24 aceita ambos os
        /// padrões ("nulo/exceção"); este método escolhe exceção por consistência com
        /// T-28/T-31, que já giram em torno dela.
        /// </para>
        /// <para>
        /// Venda cancelada sem itens (D-08/ADR-007, ex.: V-1003 do seed T-06) não é erro:
        /// <see cref="VendaDetalheDto.Itens"/> volta como lista vazia e
        /// <see cref="VendaDetalheDto.TotalItens"/> como 0.
        /// </para>
        /// </summary>
        /// <exception cref="VendaNaoEncontradaException">Venda com o <paramref name="vendaId"/> informado não existe.</exception>
        public VendaDetalheDto ObterDetalhe(string vendaId)
        {
            var venda = _repositorio.ObterPorVendaId(vendaId);
            if (venda == null)
            {
                throw new VendaNaoEncontradaException(vendaId);
            }

            var itens = venda.Itens
                .Select(item => new VendaItemDetalheDto
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    Subtotal = item.Quantidade * item.PrecoUnitario
                })
                .ToList();

            return new VendaDetalheDto
            {
                VendaId = venda.VendaId,
                ClienteId = venda.ClienteId,
                ValorTotal = venda.ValorTotal,
                Status = venda.Status,
                DataRecebimento = venda.DataRecebimento,
                DataQuitacao = venda.DataQuitacao,
                DataCancelamento = venda.DataCancelamento,
                MotivoCancelamento = venda.MotivoCancelamento,
                Itens = itens,
                TotalItens = itens.Sum(item => item.Subtotal)
            };
        }

        /// <summary>
        /// Versão enxuta de <see cref="ObterDetalhe"/>, só com <c>VendaId</c>+<c>Status</c> —
        /// insumo do futuro <c>GET /api/vendas/{vendaId}/status</c> (T-31). Mesma decisão
        /// de padrão para venda inexistente: <see cref="VendaNaoEncontradaException"/>
        /// (ver comentário de <see cref="ObterDetalhe"/>), que já é exatamente o
        /// comportamento pedido pelo critério de aceite de T-31 ("inexistente 404
        /// VENDA_NAO_ENCONTRADA").
        /// </summary>
        /// <exception cref="VendaNaoEncontradaException">Venda com o <paramref name="vendaId"/> informado não existe.</exception>
        public VendaStatusDto ObterStatus(string vendaId)
        {
            var venda = _repositorio.ObterPorVendaId(vendaId);
            if (venda == null)
            {
                throw new VendaNaoEncontradaException(vendaId);
            }

            return new VendaStatusDto
            {
                VendaId = venda.VendaId,
                Status = venda.Status
            };
        }
    }
}

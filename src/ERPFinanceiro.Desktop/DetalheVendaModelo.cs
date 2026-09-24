using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Desktop
{
    /// <summary>Linha da aba Itens de F-3 (T-44): preço unitário com 4 casas, subtotal com 2 (UX-SPEC 2.2).</summary>
    public sealed class LinhaItemDetalhe
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public LinhaItemDetalhe(VendaItemDetalheDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            ProdutoId = dto.ProdutoId;
            Quantidade = dto.Quantidade;
            PrecoUnitario = dto.PrecoUnitario.ToString("N4", PtBr);
            Subtotal = Formatadores.FormatarMoeda(dto.Subtotal);
        }

        public string ProdutoId { get; }
        public int Quantidade { get; }
        public string PrecoUnitario { get; }
        public string Subtotal { get; }
    }

    /// <summary>Cabeçalho de F-3 (UX-SPEC 2.2). Montável a partir da linha do grid (abertura imediata) ou do DTO completo.</summary>
    public sealed class CabecalhoDetalheVenda
    {
        public const string TextoNulo = "—";

        private CabecalhoDetalheVenda() { }

        public string Titulo { get; private set; }
        public string Cliente { get; private set; }
        public string Status { get; private set; }
        public string Valor { get; private set; }
        public string Recebida { get; private set; }
        public string Quitada { get; private set; }
        public string CancelamentoMotivo { get; private set; }

        public static CabecalhoDetalheVenda De(LinhaConsultaVenda linha)
        {
            if (linha == null) throw new ArgumentNullException(nameof(linha));
            return new CabecalhoDetalheVenda
            {
                Titulo = "Venda " + linha.VendaId,
                Cliente = linha.ClienteId,
                Status = linha.StatusTexto,
                Valor = linha.ValorTotalTexto,
                Recebida = linha.DataRecebimentoTexto,
                Quitada = linha.DataQuitacaoTexto,
                CancelamentoMotivo = linha.DataCancelamento.HasValue ? linha.DataCancelamentoTexto : TextoNulo
            };
        }

        public static CabecalhoDetalheVenda De(VendaDetalheDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            string status = Formatadores.FormatarStatus(dto.Status).Texto;
            if (dto.Status == StatusVenda.Cancelada && !dto.DataQuitacao.HasValue) status += "*";
            string cancel = TextoNulo;
            if (dto.DataCancelamento.HasValue)
            {
                cancel = Formatadores.FormatarDataHoraLocal(Utc(dto.DataCancelamento.Value));
                if (!string.IsNullOrEmpty(dto.MotivoCancelamento)) cancel += " / " + dto.MotivoCancelamento;
            }
            return new CabecalhoDetalheVenda
            {
                Titulo = "Venda " + dto.VendaId,
                Cliente = string.IsNullOrEmpty(dto.ClienteId) ? TextoNulo : dto.ClienteId,
                Status = status,
                Valor = dto.ValorTotal.HasValue ? Formatadores.FormatarMoeda(dto.ValorTotal.Value) : TextoNulo,
                Recebida = Formatadores.FormatarDataHoraLocal(Utc(dto.DataRecebimento)),
                Quitada = dto.DataQuitacao.HasValue ? Formatadores.FormatarDataHoraLocal(Utc(dto.DataQuitacao.Value)) : TextoNulo,
                CancelamentoMotivo = cancel
            };
        }

        private static DateTime Utc(DateTime v)
        {
            return v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc);
        }
    }

    /// <summary>Modelo de exibição de F-3 pronto para a view (itens, total, texto de venda sem itens).</summary>
    public sealed class ModeloDetalheVenda
    {
        /// <summary>Texto exato da UX-SPEC 2.2 para venda cancelada sem itens.</summary>
        public const string TextoSemItens = "Sem itens registrados (venda cancelada sem quitação prévia)";

        public ModeloDetalheVenda(CabecalhoDetalheVenda cabecalho, IReadOnlyList<LinhaItemDetalhe> itens, string totalItens)
        {
            Cabecalho = cabecalho;
            Itens = itens;
            TotalItensTexto = "Total itens: " + totalItens;
        }

        public CabecalhoDetalheVenda Cabecalho { get; }
        public IReadOnlyList<LinhaItemDetalhe> Itens { get; }
        public string TotalItensTexto { get; }

        /// <summary>Verdadeiro quando não há itens (não é erro).</summary>
        public bool SemItens => Itens.Count == 0;
    }

    /// <summary>
    /// Presenter de F-3 (T-44): carrega <see cref="ConsultaService.ObterDetalhe"/> em <see cref="Task"/>
    /// (regra 5). Exceções (inclusive <c>VendaNaoEncontradaException</c>) propagam ao chamador,
    /// que as apresenta como estado de erro com "Tentar novamente" sem fechar a janela.
    /// Histórico não existe (Tier C).
    /// </summary>
    public sealed class DetalheVendaPresenter
    {
        /// <summary>Mensagem do estado de erro (aba Itens).</summary>
        public const string TextoErro = "Não foi possível carregar os itens da venda.";

        private readonly Func<string, VendaDetalheDto> _carregador;

        private readonly Action<Exception> _aoFalhar;

        /// <param name="aoFalhar">RL9-02: recebe a exceção de carga (log) antes de propagar. Opcional.</param>
        public DetalheVendaPresenter(Func<string, VendaDetalheDto> carregador, Action<Exception> aoFalhar = null)
        {
            _carregador = carregador ?? throw new ArgumentNullException(nameof(carregador));
            _aoFalhar = aoFalhar;
        }

        /// <summary>Escopo Autofac novo por operação (regra 5/RT-08).</summary>
        public static Func<string, VendaDetalheDto> CarregadorViaContainer(IContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            return vendaId =>
            {
                using (ILifetimeScope escopo = container.BeginLifetimeScope())
                {
                    return escopo.Resolve<ConsultaService>().ObterDetalhe(vendaId);
                }
            };
        }

        public Task<ModeloDetalheVenda> CarregarAsync(string vendaId)
        {
            return Task.Run(() =>
            {
                try
                {
                    return Montar(_carregador(vendaId));
                }
                catch (Exception ex)
                {
                    ConsultaVendasPresenter.NotificarFalha(_aoFalhar, ex);
                    throw;
                }
            });
        }

        public static ModeloDetalheVenda Montar(VendaDetalheDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            var itens = (dto.Itens ?? new List<VendaItemDetalheDto>()).Select(i => new LinhaItemDetalhe(i)).ToList();
            return new ModeloDetalheVenda(CabecalhoDetalheVenda.De(dto), itens, Formatadores.FormatarMoeda(dto.TotalItens));
        }
    }
}

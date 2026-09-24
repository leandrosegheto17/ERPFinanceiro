using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Linha de exibição do grid de F-1 (T-42, UX-SPEC 2.1). Guarda os valores tipados
    /// (usados para ordenação do grid) e os textos já formatados por
    /// <see cref="Formatadores"/> (usados na exibição), sem nenhum tipo de UI — 100%
    /// testável por xUnit puro.
    /// </summary>
    public sealed class LinhaConsultaVenda
    {
        /// <summary>Texto exibido para valores nulos (ADR-007, UX-SPEC 2.1).</summary>
        public const string TextoNulo = "—";

        /// <summary>Tooltip da linha "Cancelada*" (UX-SPEC 2.1).</summary>
        public const string TooltipCanceladaSemQuitacao = "Cancelada sem quitação registrada; sem cliente/valor";

        public LinhaConsultaVenda(VendaListagemDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            VendaId = dto.VendaId;
            ClienteId = string.IsNullOrEmpty(dto.ClienteId) ? TextoNulo : dto.ClienteId;
            ValorTotal = dto.ValorTotal;
            ValorTotalTexto = dto.ValorTotal.HasValue ? Formatadores.FormatarMoeda(dto.ValorTotal.Value) : TextoNulo;

            StatusFormatado status = Formatadores.FormatarStatus(dto.Status);
            // "Cancelada*": cancelada sem quitação prévia (ADR-007) = sem quitação registrada.
            CanceladaSemQuitacao = dto.Status == StatusVenda.Cancelada && !dto.DataQuitacao.HasValue;
            StatusTexto = CanceladaSemQuitacao ? status.Texto + "*" : status.Texto;
            StatusIcone = status.Icone;

            DataRecebimento = dto.DataRecebimento;
            DataRecebimentoTexto = Formatadores.FormatarDataHoraLocal(DataUtc(dto.DataRecebimento));
            DataQuitacao = dto.DataQuitacao;
            DataQuitacaoTexto = Formatadores.FormatarDataHoraLocal(dto.DataQuitacao.HasValue ? DataUtc(dto.DataQuitacao.Value) : (DateTime?)null);
            DataCancelamento = dto.DataCancelamento;
            DataCancelamentoTexto = Formatadores.FormatarDataHoraLocal(dto.DataCancelamento.HasValue ? DataUtc(dto.DataCancelamento.Value) : (DateTime?)null);
        }

        private static DateTime DataUtc(DateTime valor)
        {
            // O banco/EF devolve Kind=Unspecified; a convenção (regra 4) é UTC.
            return valor.Kind == DateTimeKind.Local ? valor.ToUniversalTime() : DateTime.SpecifyKind(valor, DateTimeKind.Utc);
        }

        public string VendaId { get; }
        public string ClienteId { get; }
        public decimal? ValorTotal { get; }
        public string ValorTotalTexto { get; }
        public string StatusTexto { get; }
        public IconeStatus StatusIcone { get; }
        public bool CanceladaSemQuitacao { get; }
        public DateTime DataRecebimento { get; }
        public string DataRecebimentoTexto { get; }
        public DateTime? DataQuitacao { get; }
        public string DataQuitacaoTexto { get; }
        public DateTime? DataCancelamento { get; }
        public string DataCancelamentoTexto { get; }

        /// <summary>Tooltip da linha; nulo quando não há o que explicar.</summary>
        public string Tooltip => CanceladaSemQuitacao ? TooltipCanceladaSemQuitacao : null;
    }

    /// <summary>Estados de tela de F-1 (T-43, UX-SPEC 4).</summary>
    public enum EstadoConsulta
    {
        Inicial,
        Carregando,
        Vazio,
        Erro,
        Dados
    }

    /// <summary>Textos literais dos estados de F-1 (UX-SPEC 2.1 e 4). Sem tipos de UI.</summary>
    public static class TextosConsulta
    {
        public const string Vazio = "Nenhuma venda registrada ainda. As vendas aparecem aqui quando o ERP Vendas as enviar.";
        public const string Carregando = "Carregando vendas...";
        public const string Erro = "Não foi possível consultar o banco.";
        public const string BotaoTentarNovamente = "Tentar novamente";
        public const string AvisoTruncado = "Mostrando as 5.000 mais recentes; refine o filtro";
    }

    /// <summary>Resultado pronto para a view: linhas, contador "N vendas" e aviso de truncamento.</summary>
    public sealed class ModeloConsultaVendas
    {
        public ModeloConsultaVendas(IReadOnlyList<LinhaConsultaVenda> linhas, bool truncado)
        {
            Linhas = linhas;
            Truncado = truncado;
        }

        public IReadOnlyList<LinhaConsultaVenda> Linhas { get; }

        /// <summary>Verdadeiro se a consulta foi limitada às 5.000 mais recentes (aviso é T-43).</summary>
        public bool Truncado { get; }

        /// <summary>Contador do rodapé (UX-SPEC 2.1): "1 venda" / "N vendas".</summary>
        public string TextoContador => Linhas.Count == 1 ? "1 venda" : Linhas.Count + " vendas";

        /// <summary>Estado resultante: Vazio (sem linhas) ou Dados.</summary>
        public EstadoConsulta Estado => Linhas.Count == 0 ? EstadoConsulta.Vazio : EstadoConsulta.Dados;

        /// <summary>Aviso das 5.000 mais recentes; nulo quando não truncado.</summary>
        public string AvisoTruncado => Truncado ? TextosConsulta.AvisoTruncado : null;
    }

    /// <summary>
    /// Presenter de F-1 (T-42): carrega via <see cref="ConsultaService.Listar"/> em
    /// <see cref="Task"/> (regra 5: UI thread nunca bloqueada) e monta o modelo de
    /// exibição. Sem dependência de Windows Forms/DevExpress.
    /// </summary>
    public sealed class ConsultaVendasPresenter
    {
        /// <summary>Nome da coluna e ordenação padrão do grid (UX-SPEC 2.1: Recebida em, decrescente).</summary>
        public const string CampoOrdenacaoPadrao = nameof(LinhaConsultaVenda.DataRecebimento);

        private readonly Func<ResultadoListagemVendas> _carregador;

        private readonly Action<Exception> _aoFalhar;

        /// <param name="aoFalhar">
        /// RL9-02: recebe a exceção de carga (log via <c>IAppLogger</c>, fiado no composition root)
        /// antes de ela propagar ao formulário, que a apresenta sem stack. Opcional.
        /// </param>
        public ConsultaVendasPresenter(Func<ResultadoListagemVendas> carregador, Action<Exception> aoFalhar = null)
        {
            _carregador = carregador ?? throw new ArgumentNullException(nameof(carregador));
            _aoFalhar = aoFalhar;
        }

        /// <summary>
        /// Carregador de produção: abre um escopo Autofac novo por operação (regra 5/RT-08 — nunca
        /// compartilha <c>DbContext</c> entre operações), resolve <see cref="ConsultaService"/> e lista.
        /// </summary>
        public static Func<ResultadoListagemVendas> CarregadorViaContainer(IContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            return () =>
            {
                using (ILifetimeScope escopo = container.BeginLifetimeScope())
                {
                    return escopo.Resolve<ConsultaService>().Listar(new FiltroVendas());
                }
            };
        }

        /// <summary>Repassa a falha ao callback de log; falha do próprio log nunca mascara a exceção original.</summary>
        internal static void NotificarFalha(Action<Exception> aoFalhar, Exception ex)
        {
            try { aoFalhar?.Invoke(ex); }
            catch (Exception) { /* log é melhor esforço */ }
        }

        public Task<ModeloConsultaVendas> CarregarAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    return Montar(_carregador());
                }
                catch (Exception ex)
                {
                    NotificarFalha(_aoFalhar, ex);
                    throw;
                }
            });
        }

        public static ModeloConsultaVendas Montar(ResultadoListagemVendas resultado)
        {
            if (resultado == null) throw new ArgumentNullException(nameof(resultado));
            var linhas = resultado.Itens
                .Select(d => new LinhaConsultaVenda(d))
                .OrderByDescending(l => l.DataRecebimento) // ordenação padrão (o serviço já entrega assim)
                .ToList();
            return new ModeloConsultaVendas(linhas, resultado.Truncado);
        }
    }
}

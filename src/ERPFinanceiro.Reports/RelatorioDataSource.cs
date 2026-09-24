using System;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Reports
{
    /// <summary>
    /// Fonte de dados do relatório financeiro de vendas (T-48/UX-SPEC 2.3), consumida
    /// pelo layout FastReport `.frx` (T-49) e pelo fluxo "Emitir relatório" (F-5, T-50).
    /// Camada fina sobre <see cref="ConsultaService"/> (SDD 2.1: `Reports` referencia só
    /// `Application`, nunca EF/Infrastructure diretamente) — reaproveita o **mesmo**
    /// <see cref="ConsultaService"/> e o **mesmo** <see cref="FiltroVendas"/> usados pela
    /// tela (ADR-008: tela e relatório em processo, sem HTTP), mas chama
    /// <see cref="ConsultaService.ListarParaRelatorio"/>, não
    /// <see cref="ConsultaService.Listar"/> — **sem** o limite de 5.000 da grade F-1
    /// (TASK.md Seção 1 regra 15).
    /// <para>
    /// "Total listado" e a contagem de vendas com valor nulo (nota de rodapé, UX-SPEC
    /// 2.3: "Vendas com valor nulo... entram como 0 e são contadas") já vêm calculados
    /// por <see cref="ConsultaService.ListarParaRelatorio"/>, **da mesma lista** —
    /// <see cref="RelatorioDataSource"/> não recalcula nem faz segunda consulta.
    /// </para>
    /// </summary>
    public class RelatorioDataSource
    {
        private readonly ConsultaService _consultaService;

        public RelatorioDataSource(ConsultaService consultaService)
        {
            _consultaService = consultaService ?? throw new ArgumentNullException(nameof(consultaService));
        }

        /// <summary>
        /// Obtém os dados do relatório para o filtro corrente da tela (mesmo
        /// <see cref="FiltroVendas"/> aplicado na grid F-1/F-2, UX-SPEC 2.3). Lista vazia
        /// (nenhuma venda no filtro) devolve <see cref="ResultadoRelatorioVendas.Linhas"/>
        /// vazia e <see cref="ResultadoRelatorioVendas.TotalListado"/> = 0,00 (T-49 decide
        /// a mensagem "Nenhuma venda no período" a partir disso).
        /// </summary>
        public ResultadoRelatorioVendas Obter(FiltroVendas filtro)
        {
            return _consultaService.ListarParaRelatorio(filtro ?? new FiltroVendas());
        }

        /// <summary>
        /// Totais por status para o bloco de totais do relatório (S-07/CA-07.2, UX-SPEC 2.3),
        /// calculados da <b>mesma lista</b> de <see cref="Obter"/> (nulos = 0), de modo que
        /// quitado + cancelado + pendente = <see cref="ResultadoRelatorioVendas.TotalListado"/>
        /// (CA-07.3). <paramref name="incluirPendente"/> (D-03, A VALIDAR): com D-03 rejeitada,
        /// passar <c>false</c> omite a linha pendente (<see cref="TotaisRelatorioVendas.Pendente"/>
        /// nulo); nesse caso a soma dos totais exibidos não inclui as vendas pendentes.
        /// </summary>
        public TotaisRelatorioVendas ObterTotais(FiltroVendas filtro, bool incluirPendente = true)
        {
            ResultadoRelatorioVendas resultado = Obter(filtro);

            TotalPorStatus Somar(StatusVenda status)
            {
                decimal total = 0m;
                int quantidade = 0;
                foreach (LinhaRelatorio linha in resultado.Linhas)
                {
                    if (linha.Status != status) continue;
                    total += linha.ValorTotal ?? 0m;
                    quantidade++;
                }
                return new TotalPorStatus(total, quantidade);
            }

            return new TotaisRelatorioVendas(
                Somar(StatusVenda.Quitada),
                Somar(StatusVenda.Cancelada),
                incluirPendente ? Somar(StatusVenda.Pendente) : null,
                resultado.TotalListado);
        }
    }

    /// <summary>Soma (nulos = 0) e contagem de vendas de um status.</summary>
    public sealed class TotalPorStatus
    {
        public decimal Valor { get; }
        public int Quantidade { get; }

        public TotalPorStatus(decimal valor, int quantidade)
        {
            Valor = valor;
            Quantidade = quantidade;
        }
    }

    /// <summary>Totais por status + Total listado (UX-SPEC 2.3). <see cref="Pendente"/> nulo = linha omitida (D-03).</summary>
    public sealed class TotaisRelatorioVendas
    {
        public TotalPorStatus Quitado { get; }
        public TotalPorStatus Cancelado { get; }
        public TotalPorStatus Pendente { get; }
        public decimal TotalListado { get; }

        public TotaisRelatorioVendas(TotalPorStatus quitado, TotalPorStatus cancelado, TotalPorStatus pendente, decimal totalListado)
        {
            Quitado = quitado;
            Cancelado = cancelado;
            Pendente = pendente;
            TotalListado = totalListado;
        }
    }
}

using System;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Desktop
{
    /// <summary>Textos literais do painel de filtros de F-2 (UX-SPEC 2.1/4). Sem tipos de UI.</summary>
    public static class TextosFiltros
    {
        public const string VazioComFiltro = "Nenhuma venda para os filtros informados.";
        public const string LimparFiltros = "Limpar filtros";
        public const string PeriodoInvalido = "O período inicial não pode ser posterior ao período final.";
        public const string SemFiltros = "Filtros: nenhum";
    }

    /// <summary>Valores digitados no painel (DateEdit x2, Cliente, Status). Datas sem hora, no fuso local.</summary>
    public sealed class CamposFiltro
    {
        public DateTime? DataInicial { get; set; }
        public DateTime? DataFinal { get; set; }
        public string Cliente { get; set; }
        public StatusVenda? Status { get; set; }
    }

    /// <summary>Resultado de <see cref="FiltrosConsultaModelo.Aplicar"/>.</summary>
    public sealed class ResultadoAplicarFiltro
    {
        /// <summary>Verdadeiro se o filtro é válido e a consulta deve ser refeita.</summary>
        public bool Valido => ErroPeriodo == null;
        /// <summary>Mensagem inline junto aos campos de período; nulo se válido.</summary>
        public string ErroPeriodo { get; set; }
    }

    /// <summary>
    /// Lógica do painel de filtros (T-58, F-2), sem UI: monta o <see cref="FiltroVendas"/> a partir dos
    /// campos, valida o período ANTES de consultar, guarda o filtro corrente (usado pela grid e pelo
    /// relatório T-48/T-50) e produz o resumo da barra e o texto do estado vazio.
    /// <para>
    /// Conversão de datas: os DateEdit são datas locais (sem hora). Início = 00:00:00 local do dia inicial;
    /// Fim = fim do dia final local (início do dia seguinte menos 1 tick), ou seja, o dia final é
    /// INCLUSIVO. Ambos convertidos para UTC via <see cref="TimeZoneInfo"/> (o filtro compara
    /// <c>DataRecebimento</c> em UTC, inclusive nas duas pontas).
    /// </para>
    /// </summary>
    public sealed class FiltrosConsultaModelo
    {
        private readonly TimeZoneInfo _fuso;
        private FiltroVendas _corrente = new FiltroVendas();
        private string _resumo = TextosFiltros.SemFiltros;

        public FiltrosConsultaModelo(TimeZoneInfo fuso = null)
        {
            _fuso = fuso ?? TimeZoneInfo.Local;
        }

        /// <summary>Filtro atualmente aplicado (nunca nulo). Fonte única para grid e relatório.</summary>
        public FiltroVendas Corrente => _corrente;

        public bool FiltroAtivo => !_corrente.SemCriterios;

        /// <summary>Resumo dos filtros aplicados para a barra de status (UX-SPEC 4).</summary>
        public string Resumo => _resumo;

        /// <summary>Texto central do estado vazio: com filtro ativo, o de F-2; sem filtro, o de F-1.</summary>
        public string TextoVazio => FiltroAtivo ? TextosFiltros.VazioComFiltro : TextosConsulta.Vazio;

        /// <summary>Monta o filtro (sem validar nem guardar). Cliente = "contém" (D-10, ClienteContem).</summary>
        public FiltroVendas Montar(CamposFiltro campos)
        {
            campos = campos ?? new CamposFiltro();
            string cliente = string.IsNullOrWhiteSpace(campos.Cliente) ? null : campos.Cliente.Trim();
            return new FiltroVendas
            {
                PeriodoInicioUtc = campos.DataInicial.HasValue ? InicioDiaUtc(campos.DataInicial.Value) : (DateTime?)null,
                PeriodoFimUtc = campos.DataFinal.HasValue ? FimDiaUtc(campos.DataFinal.Value) : (DateTime?)null,
                ClienteId = cliente,
                ClienteContem = cliente != null,
                Status = campos.Status
            };
        }

        /// <summary>
        /// Valida e, se válido, torna o filtro corrente. Período invertido: devolve a mensagem inline e NÃO
        /// altera o filtro corrente (a tela não deve consultar).
        /// </summary>
        public ResultadoAplicarFiltro Aplicar(CamposFiltro campos)
        {
            if (campos != null && campos.DataInicial.HasValue && campos.DataFinal.HasValue
                && campos.DataInicial.Value.Date > campos.DataFinal.Value.Date)
            {
                return new ResultadoAplicarFiltro { ErroPeriodo = TextosFiltros.PeriodoInvalido };
            }
            _corrente = Montar(campos);
            _resumo = MontarResumo(campos);
            return new ResultadoAplicarFiltro();
        }

        /// <summary>Limpar: volta ao filtro vazio (consulta refeita pela tela).</summary>
        public void Limpar()
        {
            _corrente = new FiltroVendas();
            _resumo = TextosFiltros.SemFiltros;
        }

        private static string MontarResumo(CamposFiltro c)
        {
            if (c == null) return TextosFiltros.SemFiltros;
            var partes = new System.Collections.Generic.List<string>();
            if (c.DataInicial.HasValue || c.DataFinal.HasValue)
            {
                partes.Add("período " + (c.DataInicial.HasValue ? c.DataInicial.Value.ToString("dd/MM/yyyy") : "...")
                    + " a " + (c.DataFinal.HasValue ? c.DataFinal.Value.ToString("dd/MM/yyyy") : "..."));
            }
            if (!string.IsNullOrWhiteSpace(c.Cliente)) partes.Add("cliente contém \"" + c.Cliente.Trim() + "\"");
            if (c.Status.HasValue) partes.Add("status " + c.Status.Value);
            return partes.Count == 0 ? TextosFiltros.SemFiltros : "Filtros: " + string.Join("; ", partes);
        }

        private DateTime InicioDiaUtc(DateTime d)
        {
            var local = DateTime.SpecifyKind(d.Date, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, _fuso);
        }

        private DateTime FimDiaUtc(DateTime d)
        {
            var local = DateTime.SpecifyKind(d.Date.AddDays(1), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, _fuso).AddTicks(-1);
        }
    }
}

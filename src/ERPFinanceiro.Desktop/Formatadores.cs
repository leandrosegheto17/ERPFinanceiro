using System;
using System.Globalization;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Identifica o ícone associado a um status de venda, sem renderizar nada (T-41,
    /// UX-SPEC 3). A renderização real (SVG 16 px via <c>ImageCollection</c> do
    /// DevExpress) fica para quando o pacote DevExpress WinForms estiver disponível
    /// neste ambiente (ver nota em <see cref="ConfiguracaoVisual"/> e
    /// ERPFinanceiro.Desktop.csproj). Cada valor aqui é só um nome estável de recurso
    /// que o código de UI (T-42+) usa para escolher o SVG correspondente — nunca uma
    /// cor: a cor do status é reforço vindo da skin DX (UX-SPEC 3), nunca hardcoded
    /// aqui.
    /// </summary>
    public enum IconeStatus
    {
        /// <summary>Quitada: ícone "check".</summary>
        Check,

        /// <summary>Pendente: ícone "relógio".</summary>
        Relogio,

        /// <summary>Cancelada: ícone "x".</summary>
        X
    }

    /// <summary>
    /// Par texto + identificador de ícone para um status de venda (UX-SPEC 3: "Status
    /// sempre ícone + texto, cor só reforça" — TASK.md Seção 1, regra 13). Não carrega
    /// nenhuma cor: quem decide a cor de reforço é a skin DX (<see cref="ConfiguracaoVisual"/>),
    /// nunca este tipo.
    /// </summary>
    public struct StatusFormatado
    {
        public StatusFormatado(string texto, IconeStatus icone)
        {
            Texto = texto;
            Icone = icone;
        }

        /// <summary>Texto exibido junto ao ícone (nunca cor isolada).</summary>
        public string Texto { get; }

        /// <summary>Identificador do ícone SVG 16 px correspondente (ver <see cref="IconeStatus"/>).</summary>
        public IconeStatus Icone { get; }
    }

    /// <summary>
    /// Formatação centralizada de moeda, data e status (T-41, UX-SPEC 3: "Formatação
    /// centralizada em uma classe Formatadores... reutilizada por grid, detalhe e
    /// relatório"). Classe estática pura — sem dependência de DevExpress nem de
    /// Windows Forms — para ser 100% testável (xUnit) mesmo neste sandbox sem GUI
    /// interativa (mesma limitação de ambiente já registrada em T-02/T-04 para o
    /// pacote DevExpress WinForms trial).
    ///
    /// Cultura fixada em pt-BR (TASK.md Seção 1, regra 13 / UX-SPEC 5: "Idioma/formatos:
    /// cultura pt-BR fixada para datas e moeda") — não depende da cultura da thread/SO.
    /// </summary>
    public static class Formatadores
    {
        private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");

        /// <summary>
        /// Formata um valor monetário no padrão pt-BR, duas casas decimais, sem símbolo
        /// de moeda (grid/relatório mostram só o número, alinhado à direita — UX-SPEC
        /// 2.1/3). Ex.: <c>1250.00m</c> -&gt; <c>"1.250,00"</c>.
        /// </summary>
        public static string FormatarMoeda(decimal valor)
        {
            return valor.ToString("N2", CulturaPtBr);
        }

        /// <summary>
        /// Converte um instante UTC (como persistido no banco/API — TASK.md Seção 1,
        /// regra 4) para a hora local da máquina e formata como <c>dd/MM/yyyy HH:mm</c>
        /// (UX-SPEC 3). Usa <see cref="TimeZoneInfo.Local"/>: nenhuma outra convenção de
        /// fuso para a camada de apresentação foi definida em tarefa anterior (T-13/IClock
        /// cobre só UTC para Domain/Application); T-45 (barra de status) não introduz
        /// fuso próprio, então esta é a decisão de implementação adotada aqui, documentada
        /// conforme TASK.md Seção 1 ("desvio pequeno resolve e documenta a interpretação").
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="dataUtc"/> tem <see cref="DateTime.Kind"/> igual a
        /// <see cref="DateTimeKind.Local"/> (ambíguo: exige Utc ou Unspecified-tratado-como-Utc).
        /// </exception>
        public static string FormatarDataHoraLocal(DateTime dataUtc)
        {
            if (dataUtc.Kind == DateTimeKind.Local)
            {
                throw new ArgumentException(
                    "dataUtc não pode ter Kind=Local; o valor deve vir em UTC (TASK.md Seção 1, regra 4).",
                    nameof(dataUtc));
            }

            var utc = dataUtc.Kind == DateTimeKind.Utc
                ? dataUtc
                : DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc);

            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
            return local.ToString("dd/MM/yyyy HH:mm", CulturaPtBr);
        }

        /// <summary>
        /// Formata um instante UTC opcional (ex.: "Quitada em" nula quando a venda ainda
        /// está pendente) como <c>"—"</c> quando nulo (UX-SPEC 2.1) ou pelo mesmo formato
        /// de <see cref="FormatarDataHoraLocal(DateTime)"/> quando presente.
        /// </summary>
        public static string FormatarDataHoraLocal(DateTime? dataUtc)
        {
            return dataUtc.HasValue ? FormatarDataHoraLocal(dataUtc.Value) : "—";
        }

        /// <summary>
        /// Mapeia um <see cref="StatusVenda"/> para texto + identificador de ícone
        /// (UX-SPEC 3: Quitada = "check", Pendente = "relógio", Cancelada = "x"). Nunca
        /// retorna cor: a cor de reforço vem da skin DX, escolhida por quem consome este
        /// resultado (T-42+), nunca por este método.
        /// </summary>
        public static StatusFormatado FormatarStatus(StatusVenda status)
        {
            switch (status)
            {
                case StatusVenda.Quitada:
                    return new StatusFormatado("Quitada", IconeStatus.Check);
                case StatusVenda.Pendente:
                    return new StatusFormatado("Pendente", IconeStatus.Relogio);
                case StatusVenda.Cancelada:
                    return new StatusFormatado("Cancelada", IconeStatus.X);
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "StatusVenda não mapeado em Formatadores.FormatarStatus.");
            }
        }
    }
}

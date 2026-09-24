using System;
using System.Text;
using DevExpress.LookAndFeel;
using DevExpress.Utils;
using DevExpress.Utils.Svg;

namespace ERPFinanceiro.Desktop
{
    /// <summary>
    /// Configuração visual DevExpress (T-41 / UX-SPEC 3), entregue junto com T-42 depois que o
    /// pacote trial passou a ser instalável (Bloqueio 003): skin única e coleção de ícones
    /// SVG 16 px de status. Nenhuma cor hardcoded no código de UI: os SVGs são formas
    /// preenchidas que referenciam a classe de paleta <c>Black</c> do DevExpress, resolvida pela
    /// <c>SvgPalette</c> da skin ativa (RL9-01).
    /// </summary>
    internal static class ConfiguracaoVisual
    {
        /// <summary>Classe de cor da paleta SVG DevExpress (mapeada pela skin, não uma cor literal).</summary>
        internal const string ClassePaleta = "Black";

        public const string NomeSkin = "Office 2019 Colorful";

        private static bool _skinAplicada;

        /// <summary>T-51 (UX-SPEC 5): declara o processo DPI-aware (DPI 125/150%). Chamar antes do primeiro Form.</summary>
        public static void AplicarAcessibilidadeDpi()
        {
            DevExpress.XtraEditors.WindowsFormsSettings.SetDPIAware();
        }

        /// <summary>Aplica a skin única da aplicação (idempotente). Chamar antes de criar qualquer Form.</summary>
        public static void AplicarSkin()
        {
            if (_skinAplicada) return;
            UserLookAndFeel.Default.SetSkinStyle(NomeSkin);
            _skinAplicada = true;
        }

        /// <summary>Coleção com 3 SVGs 16x16, índices = valor de <see cref="IconeStatus"/> (check, relógio, x).</summary>
        public static SvgImageCollection CriarIconesStatus()
        {
            var colecao = new SvgImageCollection();
            // Formas preenchidas com class="Black": a classe é resolvida pela paleta SVG da skin
            // (SvgPalette), nunca por cor literal (RL9-01).
            colecao.Add(CriarSvg("<polygon class=\"" + ClassePaleta + "\" points=\"2.3,8.2 3.7,6.8 6.5,9.6 12.3,3.6 13.7,5 6.5,12.4\"/>"));
            colecao.Add(CriarSvg("<path class=\"" + ClassePaleta + "\" fill-rule=\"evenodd\" " +
                                 "d=\"M8 1.5 A6.5 6.5 0 1 0 8.001 1.5 Z M8 2.8 A5.2 5.2 0 1 1 7.999 2.8 Z\"/>" +
                                 "<path class=\"" + ClassePaleta + "\" d=\"M7.4 4.5 H8.6 V7.7 L10.6 8.9 L10 9.9 L7.4 8.3 Z\"/>"));
            colecao.Add(CriarSvg("<polygon class=\"" + ClassePaleta + "\" points=\"4,5.4 5.4,4 8,6.6 10.6,4 12,5.4 9.4,8 12,10.6 10.6,12 8,9.4 5.4,12 4,10.6 6.6,8\"/>"));
            return colecao;
        }

        private static SvgImage CriarSvg(string corpo)
        {
            string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" viewBox=\"0 0 16 16\">" + corpo + "</svg>";
            return SvgImage.FromStream(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(svg)));
        }
    }
}

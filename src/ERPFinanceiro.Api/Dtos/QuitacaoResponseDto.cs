using System;

namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de saída 200 de <c>POST /api/vendas/quitacao</c> (T-30, `docs/contrato-v1.1.md`
    /// Seção 3.1): <c>{status,dataQuitacao}</c>. Mapeado manualmente a partir de
    /// <see cref="ERPFinanceiro.Application.Servicos.ResultadoQuitacao"/> pelo controller.
    /// </summary>
    public class QuitacaoResponseDto
    {
        public string Status { get; set; }

        /// <summary>UTC — serializado em ISO 8601 terminando em "Z" (P-6, Startup.CriarConfiguracaoJson).</summary>
        public DateTime DataQuitacao { get; set; }
    }
}

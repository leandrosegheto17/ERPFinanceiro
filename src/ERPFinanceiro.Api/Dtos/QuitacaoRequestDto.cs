using System.Collections.Generic;

namespace ERPFinanceiro.Api.Dtos
{
    /// <summary>
    /// DTO de entrada de <c>POST /api/vendas/quitacao</c> (T-30, `docs/contrato-v1.1.md`
    /// Seção 3.1): espelha o JSON de entrada campo a campo (o formatter JSON da Api,
    /// <see cref="Startup.CriarConfiguracaoJson"/>, converte para/de camelCase). Nunca é a
    /// entidade de Domain — o controller mapeia manualmente para
    /// <see cref="ERPFinanceiro.Application.Commands.QuitarVendaCommand"/> (Seção 1, regra 2).
    /// </summary>
    public class QuitacaoRequestDto
    {
        public string VendaId { get; set; }

        public string ClienteId { get; set; }

        public decimal ValorTotal { get; set; }

        public List<ItemQuitacaoDto> Itens { get; set; }
    }

    /// <summary>Item do payload de entrada (`itens[].produtoId/quantidade/precoUnitario`).</summary>
    public class ItemQuitacaoDto
    {
        public string ProdutoId { get; set; }

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }
    }
}

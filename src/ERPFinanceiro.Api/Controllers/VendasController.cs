using System;
using System.Linq;
using System.Web.Http;
using ERPFinanceiro.Api.Dtos;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Servicos;

namespace ERPFinanceiro.Api.Controllers
{
    /// <summary>
    /// Controller dos endpoints de venda do contrato v1.1 (Seção 3): <c>POST /api/vendas/quitacao</c>
    /// (T-30, esta tarefa). Decisão de reaproveitamento (T-30): <c>GET /api/vendas/{vendaId}/status</c>
    /// (T-31) e <c>POST /api/vendas/cancelamento</c> (T-32) adicionam actions a este mesmo
    /// controller (mesmo <c>[RoutePrefix]</c> "api/vendas"), em vez de um controller por
    /// endpoint — evita duplicar o prefixo de rota e mantém os 3 endpoints de venda juntos,
    /// como o SDD/UX-SPEC os agrupam.
    /// <para>
    /// Controller fino (Seção 1, regra 2): mapeia DTO de entrada -> comando de Application,
    /// chama o serviço de Application (resolvido via Autofac, T-26/T-30), mapeia o
    /// resultado -> DTO de saída. Toda regra de negócio/validação fica em Domain/Application;
    /// exceções de Application/Domain não capturadas aqui propagam para o
    /// <see cref="ERPFinanceiro.Api.Erros.GlobalExceptionHandler"/> (T-28), que as traduz
    /// para o envelope <c>{erro:{codigo,mensagem}}</c>.
    /// </para>
    /// </summary>
    [RoutePrefix("api/vendas")]
    public class VendasController : ApiController
    {
        private readonly QuitacaoService _quitacaoService;

        public VendasController(QuitacaoService quitacaoService)
        {
            _quitacaoService = quitacaoService ?? throw new ArgumentNullException(nameof(quitacaoService));
        }

        /// <summary>
        /// <c>POST /api/vendas/quitacao</c> (contrato-v1.1.md Seção 3.1). 200 com
        /// <c>{status,dataQuitacao}</c> para venda inexistente (cria+quita), Pendente
        /// (quita) e já Quitada (idempotente, P-2, mesma data). Payload inválido/total
        /// divergente -> <see cref="ERPFinanceiro.Application.Exceptions.ValidacaoException"/>
        /// (400, via <see cref="ERPFinanceiro.Api.Erros.GlobalExceptionHandler"/>). Venda
        /// Cancelada -> <c>VendaJaCanceladaException</c> (409 <c>VENDA_JA_CANCELADA</c>).
        /// </summary>
        [HttpPost]
        [Route("quitacao")]
        public IHttpActionResult Quitacao([FromBody] QuitacaoRequestDto dto)
        {
            var comando = new QuitarVendaCommand
            {
                VendaId = dto?.VendaId,
                ClienteId = dto?.ClienteId,
                ValorTotal = dto?.ValorTotal ?? 0m,
                Itens = dto?.Itens?
                    .Select(item => new ItemVendaCommand
                    {
                        ProdutoId = item?.ProdutoId,
                        Quantidade = item?.Quantidade ?? 0,
                        PrecoUnitario = item?.PrecoUnitario ?? 0m
                    })
                    .ToList()
            };

            ResultadoQuitacao resultado = _quitacaoService.Executar(comando);

            var resposta = new QuitacaoResponseDto
            {
                Status = resultado.Status,
                DataQuitacao = resultado.DataQuitacaoUtc
            };

            return Ok(resposta);
        }
    }
}

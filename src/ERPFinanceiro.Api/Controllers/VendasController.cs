using System;
using System.Linq;
using System.Web.Http;
using ERPFinanceiro.Api.Dtos;
using ERPFinanceiro.Application.Commands;
using ERPFinanceiro.Application.Consultas;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using ERPFinanceiro.Application.Servicos;
using ERPFinanceiro.Application.Validacao;

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
    /// <para>
    /// T-33 (D-05/P-8, "A VALIDAR" com o Vendas): cada action tem um segundo
    /// <see cref="RouteAttribute"/> absoluto (prefixo <c>~/</c>, ignora o
    /// <see cref="RoutePrefixAttribute"/> "api/vendas") apontando para o alias
    /// <c>/api/v1/vendas/...</c> — <c>RouteAttribute</c> permite múltiplas
    /// ocorrências na mesma action (<c>AllowMultiple = true</c>), então as duas
    /// rotas chamam exatamente o mesmo método, sem duplicar controller/DTO/lógica.
    /// Se D-05 for rejeitada pelo Vendas: remover só a linha
    /// <c>[Route("~/api/v1/vendas/...")]</c> de cada uma das 3 actions abaixo —
    /// desativa o alias sem apagar o controller nem as rotas <c>/api/vendas/...</c>.
    /// </para>
    /// </summary>
    [RoutePrefix("api/vendas")]
    public class VendasController : ApiController
    {
        private readonly QuitacaoService _quitacaoService;
        private readonly ConsultaService _consultaService;
        private readonly CancelamentoService _cancelamentoService;
        private readonly ICorrelationContext _correlationContext;

        public VendasController(
            QuitacaoService quitacaoService,
            ConsultaService consultaService,
            CancelamentoService cancelamentoService,
            ICorrelationContext correlationContext)
        {
            _quitacaoService = quitacaoService ?? throw new ArgumentNullException(nameof(quitacaoService));
            _consultaService = consultaService ?? throw new ArgumentNullException(nameof(consultaService));
            _cancelamentoService = cancelamentoService ?? throw new ArgumentNullException(nameof(cancelamentoService));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
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
        [Route("~/api/v1/vendas/quitacao")]
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

        /// <summary>
        /// <c>GET /api/vendas/{vendaId}/status</c> (contrato-v1.1.md): 200 com
        /// <c>{vendaId,status}</c>, <c>status</c> em PascalCase (conversão do enum
        /// <see cref="ERPFinanceiro.Domain.Enums.StatusVenda"/> feita aqui, na Api —
        /// mesma decisão de T-19/T-24). Venda inexistente ->
        /// <see cref="ERPFinanceiro.Application.Exceptions.VendaNaoEncontradaException"/>
        /// (404 <c>VENDA_NAO_ENCONTRADA</c>, via <see cref="ERPFinanceiro.Api.Erros.GlobalExceptionHandler"/>).
        /// </summary>
        [HttpGet]
        [Route("{vendaId}/status")]
        [Route("~/api/v1/vendas/{vendaId}/status")]
        public IHttpActionResult Status(string vendaId)
        {
            VendaStatusDto resultado = _consultaService.ObterStatus(vendaId);

            var resposta = new VendaStatusResponseDto
            {
                VendaId = resultado.VendaId,
                Status = resultado.Status.ToString()
            };

            return Ok(resposta);
        }

        /// <summary>
        /// <c>POST /api/vendas/cancelamento</c> (contrato-v1.1.md Seção 3.2, T-32). 200
        /// com <c>{status}</c> para Pendente cancelada, já Cancelada (idempotente, P-2) e
        /// desconhecida (cria já Cancelada, D-08). <c>vendaId</c> ausente/vazio ->
        /// <see cref="ValidacaoException"/> (400 <c>PAYLOAD_INVALIDO</c>) — validado aqui
        /// porque <see cref="ValidadorVendaCommand"/> (T-17) não cobre
        /// <see cref="CancelarVendaCommand"/> (só <c>QuitarVendaCommand</c>/
        /// <c>RegistrarVendaCommand</c>) e <see cref="CancelamentoService.Cancelar"/>
        /// (T-19) só garante essa pré-condição via <see cref="ArgumentException"/> (erro
        /// de programação do chamador, não um cenário de negócio do contrato — por isso
        /// a Api valida antes de chamar o serviço). Cancelar venda Quitada sem
        /// <c>motivo</c> -> <c>MotivoObrigatorioException</c> (409 <c>MOTIVO_OBRIGATORIO</c>,
        /// via <see cref="ERPFinanceiro.Api.Erros.GlobalExceptionHandler"/>, D-06).
        /// </summary>
        [HttpPost]
        [Route("cancelamento")]
        [Route("~/api/v1/vendas/cancelamento")]
        public IHttpActionResult Cancelamento([FromBody] CancelamentoRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.VendaId))
            {
                throw new ValidacaoException(new ErroValidacao("PAYLOAD_INVALIDO", "O campo 'vendaId' é obrigatório."));
            }

            var comando = new CancelarVendaCommand
            {
                VendaId = dto.VendaId,
                Motivo = dto.Motivo,
                CorrelationId = _correlationContext.CorrelationId
            };

            ResultadoCancelamento resultado = _cancelamentoService.Cancelar(comando);

            var resposta = new CancelamentoResponseDto
            {
                Status = resultado.Status.ToString()
            };

            return Ok(resposta);
        }
    }
}

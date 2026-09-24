using System;
using System.Net;
using System.Web.Http;
using ERPFinanceiro.Api.Dtos;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Api.Controllers
{
    /// <summary>
    /// Controller do endpoint de infraestrutura <c>GET /api/health</c> (T-36,
    /// `docs/contrato-v1.1.md` Seção 3.5, P-5) — sem relação com o domínio de vendas,
    /// por isso um controller dedicado, separado de <see cref="VendasController"/>.
    /// Controller fino (Seção 1, regra 2): delega integralmente a checagem de banco a
    /// <see cref="IHealthService.ObterStatus"/> (T-22, já pronta, nunca lança), só
    /// traduz o resultado tipado para o corpo/status HTTP do contrato.
    /// <para>
    /// Sem autenticação (P-5): esta action, por si só, não exige nem lê nenhum
    /// header — a isenção de <c>X-Api-Key</c> é responsabilidade do
    /// <c>ApiKeyHandler</c> (T-35, isentando a rota <c>/api/health</c>), não deste
    /// controller. Hoje, sem <c>ApiKeyHandler</c> registrado no pipeline, qualquer
    /// requisição (com ou sem o header) já é atendida normalmente.
    /// </para>
    /// </summary>
    [RoutePrefix("api")]
    public class HealthController : ApiController
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
        {
            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        }

        /// <summary>
        /// <c>GET /api/health</c>: 200 <c>{status:"ok",banco:"ok"}</c> quando
        /// <see cref="ResultadoHealth.Ok"/> é <c>true</c>; 503
        /// <c>{status:"degradado",banco:"falha"}</c> quando é <c>false</c>. Usa
        /// <see cref="ApiController.Content{T}(HttpStatusCode, T)"/> (não
        /// <see cref="ApiController.Ok{T}(T)"/>, que só cobre 200) para poder devolver
        /// 503 com corpo JSON passando pela mesma negociação de conteúdo/formatter
        /// configurado em <see cref="Startup"/> (camelCase, T-25).
        /// </summary>
        [HttpGet]
        [Route("health")]
        public IHttpActionResult Health()
        {
            ResultadoHealth resultado = _healthService.ObterStatus();

            var resposta = new HealthResponseDto
            {
                Status = resultado.Ok ? "ok" : "degradado",
                Banco = resultado.Ok ? "ok" : "falha"
            };

            HttpStatusCode statusCode = resultado.Ok ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

            return Content(statusCode, resposta);
        }
    }
}

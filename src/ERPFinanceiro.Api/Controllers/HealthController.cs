using System;
using System.Net;
using System.Web.Http;
using ERPFinanceiro.Api.Dtos;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Api.Controllers
{
    /// <summary>
    /// <c>GET /api/health</c> (T-36, S-09, contrato v1.1 Seção 3.5). Público: isento de
    /// <c>X-Api-Key</c> (P-5, tratado no ApiKeyHandler, T-35). Nunca ecoa
    /// <c>ResultadoHealth.Mensagem</c> (pode conter detalhe de infraestrutura — nota do
    /// SECURITY-REVIEW do Lote 5); só os campos fixos do contrato.
    /// </summary>
    [RoutePrefix("api/health")]
    public class HealthController : ApiController
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
        {
            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            ResultadoHealth resultado = _healthService.ObterStatus();

            if (resultado != null && resultado.Ok)
            {
                return Content(HttpStatusCode.OK, new HealthResponseDto { Status = "ok", Banco = "ok" });
            }

            return Content(HttpStatusCode.ServiceUnavailable, new HealthResponseDto { Status = "degradado", Banco = "falha" });
        }
    }
}

using System;
using ERPFinanceiro.Application.Interfaces;
using FirebirdSql.Data.FirebirdClient;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// Implementação de <see cref="IHealthService"/> (T-22, SDD.md Seção 2.6, P-5):
    /// abre uma conexão Firebird própria (curta, fechada ao final do método — não
    /// reaproveita a conexão de <see cref="FinanceiroDbContext"/>, que fica com o
    /// repositório/UoW por transação) e executa <c>SELECT 1 FROM RDB$DATABASE</c>,
    /// a tabela de sistema sempre presente em qualquer banco Firebird (equivalente ao
    /// `SELECT 1` de outros SGBDs, que não tem tabela sem FROM).
    ///
    /// Contrato de <see cref="ObterStatus"/>: nunca lança. Qualquer exceção de
    /// conexão/consulta (caminho inválido, arquivo ausente, banco bloqueado por outro
    /// processo etc.) é capturada e resumida em <see cref="ResultadoHealth.ComFalha"/>
    /// (só <see cref="Exception.Message"/>, sem stack trace) — consumida tanto pelo
    /// `GET /api/health` (T-36, 200/503) quanto pela barra de status da tela (T-45),
    /// em processo, sem HTTP.
    /// </summary>
    public sealed class HealthService : IHealthService
    {
        private const string ConsultaVerificacao = "SELECT 1 FROM RDB$DATABASE";

        private readonly IConfiguracaoBanco _configuracao;

        public HealthService(IConfiguracaoBanco configuracao)
        {
            _configuracao = configuracao ?? throw new ArgumentNullException(nameof(configuracao));
        }

        public ResultadoHealth ObterStatus()
        {
            try
            {
                using (var connection = new FbConnection(MontarConnectionString()))
                {
                    connection.Open();

                    using (var command = new FbCommand(ConsultaVerificacao, connection))
                    {
                        command.ExecuteScalar();
                    }
                }

                return ResultadoHealth.ComSucesso();
            }
            catch (Exception ex)
            {
                // Mensagem resumida, sem stack trace (guardrail da tarefa) — o chamador
                // (Api/tela) decide o que expor ao usuário final.
                return ResultadoHealth.ComFalha(ex.Message);
            }
        }

        private string MontarConnectionString()
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = _configuracao.CaminhoFdb,
                UserID = _configuracao.Usuario,
                Password = _configuracao.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return csb.ConnectionString;
        }
    }
}

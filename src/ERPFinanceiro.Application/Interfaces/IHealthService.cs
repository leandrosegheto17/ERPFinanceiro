namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Verificação de disponibilidade do banco (T-22, SDD.md Seção 2.6, P-5). Usada em
    /// dois pontos, sempre a mesma implementação/instância de contrato: `GET /api/health`
    /// (T-36, via HTTP) e a barra de status da tela (T-45), esta última chamando
    /// <see cref="ObterStatus"/> diretamente em processo, sem round-trip HTTP.
    ///
    /// Implementação real (Infrastructure, EF6/Firebird) nunca lança exceção para o
    /// chamador: qualquer falha de conexão/consulta (`.fdb` inacessível, caminho
    /// inválido, timeout etc.) é traduzida em <see cref="ResultadoHealth.ComFalha"/>.
    /// </summary>
    public interface IHealthService
    {
        /// <summary>
        /// Executa a verificação (`SELECT 1`/equivalente Firebird) e devolve o
        /// resultado tipado — nunca lança.
        /// </summary>
        ResultadoHealth ObterStatus();
    }
}

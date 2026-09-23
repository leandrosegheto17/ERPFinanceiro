using System;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// Lançada pelo <see cref="DbInitializer"/> quando o arquivo .fdb existe
    /// mas não pode ser aberto — tipicamente porque está bloqueado por outro
    /// handle/processo (RT-05). Não é uma <c>DomainException</c> (não é regra
    /// de negócio): é um problema de infraestrutura, tratado por T-46
    /// (splash abre `FrmConsulta` em estado de erro, nunca fecha silencioso).
    /// </summary>
    public sealed class BancoIndisponivelException : Exception
    {
        public BancoIndisponivelException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

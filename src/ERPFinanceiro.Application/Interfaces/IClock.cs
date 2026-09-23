using System;

namespace ERPFinanceiro.Application.Interfaces
{
    /// <summary>
    /// Abstração de data/hora (T-13). Datas em UTC no banco/API (TASK.md Seção 1, regra
    /// 4): Domain/Application nunca chamam <c>DateTime.Now</c>/<c>DateTime.UtcNow</c>
    /// diretamente — sempre através desta interface, injetada, o que também permite
    /// controlar o tempo em teste (ex.: <c>Mock&lt;IClock&gt;</c>). Implementação real
    /// em Infrastructure (wrapper trivial de <c>DateTime.UtcNow</c>).
    /// </summary>
    public interface IClock
    {
        /// <summary>Instante atual em UTC.</summary>
        DateTime UtcNow { get; }
    }
}

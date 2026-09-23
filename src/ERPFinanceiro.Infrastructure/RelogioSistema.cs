using System;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Infrastructure
{
    /// <summary>
    /// Implementação de <see cref="IClock"/> (T-26): relógio real do sistema, sempre
    /// UTC (TASK.md Seção 1, regra 4 — "Datas em UTC no banco/API; <c>IClock</c>,
    /// nunca <c>DateTime.Now</c> no Domain/Application"). Sem estado, sem
    /// dependências — <c>SingleInstance</c> é seguro no composition root.
    /// </summary>
    public sealed class RelogioSistema : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

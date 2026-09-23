using System;

namespace ERPFinanceiro.Application.Servicos
{
    /// <summary>
    /// Resultado de <see cref="QuitacaoService.Executar"/> (T-18), espelhando o corpo
    /// 200 de `docs/contrato-v1.1.md` Seção 3.1 (<c>{status,dataQuitacao}</c>). Toda
    /// execução bem-sucedida de <see cref="QuitacaoService.Executar"/> termina com a
    /// venda em <c>Quitada</c> (inexistente criada+quitada / Pendente quitada / já
    /// Quitada idempotente) — <see cref="Status"/> é sempre a string fixa
    /// <c>"Quitada"</c>, sem variação (o controller de T-30 serializa diretamente).
    /// </summary>
    public class ResultadoQuitacao
    {
        public ResultadoQuitacao(DateTime dataQuitacaoUtc)
        {
            DataQuitacaoUtc = dataQuitacaoUtc;
        }

        public string Status => "Quitada";

        public DateTime DataQuitacaoUtc { get; }
    }
}

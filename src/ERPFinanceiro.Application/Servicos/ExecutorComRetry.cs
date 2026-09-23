using System;
using ERPFinanceiro.Application.Exceptions;

namespace ERPFinanceiro.Application.Servicos
{
    /// <summary>
    /// Helper de Application (TASK.md T-16, regra 9 da Seção 1: "Conflito de
    /// concorrência: rollback, reler e reavaliar, máximo 3 tentativas, depois 409
    /// <c>CONFLITO_CONCORRENCIA</c>"). Executa uma operação (tipicamente "reler a venda,
    /// reavaliar as regras de negócio e tentar salvar de novo") e, se ela lançar
    /// <see cref="ConcorrenciaException"/>, tenta novamente — no máximo 3 tentativas ao
    /// todo. Se a 3ª tentativa também falhar por conflito, a exceção é deixada propagar
    /// (quem a traduz para HTTP 409 <c>CONFLITO_CONCORRENCIA</c> é a Api, T-28).
    /// </summary>
    public static class ExecutorComRetry
    {
        /// <summary>Número máximo de tentativas antes de propagar o conflito (regra 9).</summary>
        public const int MaximoTentativas = 3;

        /// <summary>
        /// Executa <paramref name="operacao"/>, repetindo em caso de
        /// <see cref="ConcorrenciaException"/> até <see cref="MaximoTentativas"/> vezes.
        /// </summary>
        /// <typeparam name="T">Tipo de retorno da operação.</typeparam>
        /// <param name="operacao">
        /// Callback que reler o estado atual, reavalia as regras e tenta salvar. Deve
        /// lançar <see cref="ConcorrenciaException"/> quando o commit falhar por
        /// conflito de concorrência.
        /// </param>
        /// <returns>O resultado da operação assim que uma tentativa tiver sucesso.</returns>
        /// <exception cref="ConcorrenciaException">
        /// Propagada quando todas as <see cref="MaximoTentativas"/> tentativas falharem
        /// por conflito de concorrência.
        /// </exception>
        public static T Executar<T>(Func<T> operacao)
        {
            if (operacao == null)
            {
                throw new ArgumentNullException(nameof(operacao));
            }

            var tentativa = 0;

            while (true)
            {
                tentativa++;

                try
                {
                    return operacao();
                }
                catch (ConcorrenciaException) when (tentativa < MaximoTentativas)
                {
                    // Rollback já ocorreu na fronteira transacional (IUnitOfWork, T-15).
                    // Reler e reavaliar acontece na próxima chamada de `operacao`.
                }
            }
        }

        /// <summary>
        /// Sobrecarga sem retorno, para operações representadas por <see cref="Action"/>.
        /// </summary>
        public static void Executar(Action operacao)
        {
            if (operacao == null)
            {
                throw new ArgumentNullException(nameof(operacao));
            }

            Executar<object>(() =>
            {
                operacao();
                return null;
            });
        }
    }
}

using System;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Servicos;
using Xunit;

namespace ERPFinanceiro.Tests.Application
{
    /// <summary>
    /// Testes de <see cref="ExecutorComRetry"/> (T-16, regra 9 do TASK.md Seção 1):
    /// lógica pura de retry sobre um callback fake — sem Firebird real.
    /// </summary>
    public class ExecutorComRetryTests
    {
        [Fact]
        public void Executar_ConflitaDuasVezesEPassaNaTerceira_CompletaComSucessoSemPropagar()
        {
            var tentativas = 0;

            var resultado = ExecutorComRetry.Executar(() =>
            {
                tentativas++;
                if (tentativas < 3)
                {
                    throw new ConcorrenciaException("conflito simulado", new Exception("inner"));
                }

                return "ok";
            });

            Assert.Equal("ok", resultado);
            Assert.Equal(3, tentativas);
        }

        [Fact]
        public void Executar_ConflitaSempre_PropagaConcorrenciaExceptionApos3Tentativas()
        {
            var tentativas = 0;

            var excecao = Assert.Throws<ConcorrenciaException>(() =>
            {
                ExecutorComRetry.Executar<string>(() =>
                {
                    tentativas++;
                    throw new ConcorrenciaException("conflito simulado", new Exception("inner"));
                });
            });

            Assert.NotNull(excecao);
            Assert.Equal(ExecutorComRetry.MaximoTentativas, tentativas);
        }

        [Fact]
        public void Executar_ActionSemRetorno_TambemAplicaRetry()
        {
            var tentativas = 0;
            var executouComSucesso = false;

            ExecutorComRetry.Executar(() =>
            {
                tentativas++;
                if (tentativas < 2)
                {
                    throw new ConcorrenciaException("conflito simulado", new Exception("inner"));
                }

                executouComSucesso = true;
            });

            Assert.True(executouComSucesso);
            Assert.Equal(2, tentativas);
        }
    }
}

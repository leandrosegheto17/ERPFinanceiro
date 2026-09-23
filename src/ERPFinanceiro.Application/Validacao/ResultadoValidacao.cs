using System.Collections.Generic;
using System.Linq;

namespace ERPFinanceiro.Application.Validacao
{
    /// <summary>
    /// Resultado tipado de uma validação (T-17). Nunca lança exceção para erro de
    /// validação esperado — o chamador (serviço de aplicação futuro, T-XX) inspeciona
    /// <see cref="Sucesso"/>/<see cref="Erros"/> e decide o que fazer; a tradução para
    /// HTTP 400 fica com a Api (T-30/T-32).
    /// </summary>
    public class ResultadoValidacao
    {
        private static readonly IReadOnlyList<ErroValidacao> SemErros = new ErroValidacao[0];

        private ResultadoValidacao(IReadOnlyList<ErroValidacao> erros)
        {
            Erros = erros;
        }

        public bool Sucesso => Erros.Count == 0;

        public IReadOnlyList<ErroValidacao> Erros { get; }

        public static ResultadoValidacao ComSucesso()
        {
            return new ResultadoValidacao(SemErros);
        }

        public static ResultadoValidacao ComErros(IEnumerable<ErroValidacao> erros)
        {
            var lista = (erros ?? Enumerable.Empty<ErroValidacao>()).ToArray();
            return lista.Length == 0
                ? ComSucesso()
                : new ResultadoValidacao(lista);
        }
    }
}

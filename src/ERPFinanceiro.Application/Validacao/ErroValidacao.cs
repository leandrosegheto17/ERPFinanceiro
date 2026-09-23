namespace ERPFinanceiro.Application.Validacao
{
    /// <summary>
    /// Uma violação de validação (T-17), no formato código+mensagem que espelha o
    /// envelope de erro do <c>docs/contrato-v1.1.md</c> Seção 2 (<c>{erro:{codigo,mensagem}}</c>).
    /// A tradução deste código para HTTP 400 é responsabilidade da Api (T-30/T-32) — este
    /// tipo não carrega status HTTP.
    /// </summary>
    public class ErroValidacao
    {
        public ErroValidacao(string codigo, string mensagem)
        {
            Codigo = codigo;
            Mensagem = mensagem;
        }

        public string Codigo { get; }

        public string Mensagem { get; }
    }
}

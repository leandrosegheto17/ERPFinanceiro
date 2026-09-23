namespace ERPFinanceiro.Api.Erros
{
    /// <summary>
    /// Envelope único de erro do contrato v1.1 (Seção 2): <c>{erro:{codigo,mensagem}}</c>.
    /// Propriedades em PascalCase aqui — o formatter JSON da Api (<see cref="ERPFinanceiro.Api.Startup.CriarConfiguracaoJson"/>)
    /// converte para camelCase na serialização (mesmo mecanismo dos demais DTOs, T-25).
    /// </summary>
    public class ErroEnvelopeDto
    {
        public ErroDto Erro { get; set; }
    }

    /// <summary>Código/mensagem de um erro (nunca stack trace/mensagem bruta de exceção — TASK.md Seção 1, regra 7).</summary>
    public class ErroDto
    {
        public string Codigo { get; set; }

        public string Mensagem { get; set; }
    }
}

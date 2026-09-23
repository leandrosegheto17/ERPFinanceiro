using System;
using System.Configuration;
using ERPFinanceiro.Application.Interfaces;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// Implementação de <see cref="IConfiguracaoBanco"/> que lê
    /// <c>Banco:CaminhoFdb</c>/<c>Banco:Usuario</c>/<c>Banco:Senha</c> do
    /// <c>appSettings</c> do <c>App.config</c> do processo host (Desktop/Tests
    /// — ver App.config.example). Único ponto da solução que acessa
    /// <see cref="ConfigurationManager"/> para essas chaves; Domain/Application
    /// só conhecem a interface (regra da tarefa T-12).
    /// </summary>
    public sealed class ConfiguracaoBancoAppConfig : IConfiguracaoBanco
    {
        public string CaminhoFdb { get; }
        public string Usuario { get; }
        public string Senha { get; }

        public ConfiguracaoBancoAppConfig()
        {
            CaminhoFdb = ObterObrigatoria("Banco:CaminhoFdb");
            Usuario = ObterObrigatoria("Banco:Usuario");
            Senha = ObterObrigatoria("Banco:Senha");
        }

        private static string ObterObrigatoria(string chave)
        {
            string valor = ConfigurationManager.AppSettings[chave];
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new InvalidOperationException(
                    $"Configuração obrigatória ausente: appSettings/{chave} (App.config). " +
                    "Veja App.config.example.");
            }

            return valor;
        }
    }
}

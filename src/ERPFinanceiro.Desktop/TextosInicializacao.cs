using System;

namespace ERPFinanceiro.Desktop
{
    /// <summary>Textos de F-7 (UX-SPEC 2.7): splash, aviso de segunda instância, diálogo de API.</summary>
    public static class TextosInicializacao
    {
        public const string Splash = "Iniciando banco e API...";
        public const string TituloApp = "ERP Financeiro";
        public const string SegundaInstancia = "Já existe uma instância em execução";
        public const string TituloDialogoApi = "API indisponível";

        /// <summary>Causa + ação (trocar a porta em App.config, fechar outra instância) + escolha Continuar/Sair.</summary>
        public static string DialogoApi(ResultadoInicializacao r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            string causa = r.PortaEmUso
                ? "A porta " + r.Porta + " já está em uso por outro processo (possivelmente outra instância do aplicativo)."
                : "O servidor da API não pôde ser iniciado na porta " + r.Porta + ".";
            return causa + Environment.NewLine + Environment.NewLine +
                   "O que fazer: feche a outra instância ou o programa que usa a porta, ou altere a chave "
                   + "\"Api:Porta\" no App.config e reinicie." + Environment.NewLine + Environment.NewLine +
                   "Sim = continuar sem a API (consulta local segue funcionando; sistemas externos não conseguem enviar vendas)."
                   + Environment.NewLine + "Não = sair.";
        }
    }
}

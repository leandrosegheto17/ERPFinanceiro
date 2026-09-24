using System;
using ERPFinanceiro.Desktop;
using Xunit;

namespace ERPFinanceiro.Tests.Desktop
{
    /// <summary>RL10-02: exceção não tratada -> log + mensagem sem stack; falha de encerramento chega ao log.</summary>
    public class TratadorExcecoesTests
    {
        [Fact]
        public void Tratar_registra_no_log_e_mostra_mensagem_sem_stack()
        {
            string contexto = null; Exception logada = null; string msg = null;
            var t = new TratadorExcecoesNaoTratadas((c, e) => { contexto = c; logada = e; }, m => msg = m);
            var ex = new InvalidOperationException("boom");

            t.Tratar("UI", ex);

            Assert.Equal("UI", contexto);
            Assert.Same(ex, logada);
            Assert.Equal(TratadorExcecoesNaoTratadas.TextoMensagem, msg);
            Assert.DoesNotContain("boom", msg);
            Assert.DoesNotContain(" at ", msg);
        }

        [Fact]
        public void Tratar_nao_propaga_falha_do_log_nem_do_dialogo()
        {
            var t = new TratadorExcecoesNaoTratadas((c, e) => throw new Exception("x"), m => throw new Exception("y"));
            t.Tratar("UI", new Exception("z"));
        }

        [Fact]
        public void Encerrar_falha_do_mutex_apos_container_chega_ao_aoFalhar_e_nao_e_perdida()
        {
            var ordem = new System.Collections.Generic.List<string>();
            Exception falha = null;
            var enc = new EncerramentoAplicacao(() => ordem.Add("barra"), () => ordem.Add("api"),
                () => ordem.Add("container"), () => throw new InvalidOperationException("mutex"), ex => falha = ex);

            enc.Encerrar();

            Assert.Equal(new[] { "barra", "api", "container" }, ordem);
            Assert.Equal("mutex", falha?.Message);
        }
    }
}

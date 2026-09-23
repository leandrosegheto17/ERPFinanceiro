using System;
using ERPFinanceiro.Domain.Enums;

namespace ERPFinanceiro.Domain.Entities
{
    /// <summary>
    /// Registro de histórico append-only de uma venda. Espelha FIN_VENDA_HISTORICO
    /// (database/01-schema.sql). TASK.md Seção 1, regra 8: só INSERT — repositório
    /// expõe apenas Adicionar; sem métodos de alteração aqui.
    /// </summary>
    public class VendaHistorico
    {
        /// <summary>PK técnica; atribuída pelo banco (identity). 0 enquanto não persistida.</summary>
        public long Id { get; internal set; }

        public Operacao Operacao { get; private set; }

        /// <summary>NULL na criação da venda (primeiro registro do histórico).</summary>
        public StatusVenda? StatusAnterior { get; private set; }

        public StatusVenda StatusNovo { get; private set; }

        public DateTime DataHora { get; private set; }

        public string Motivo { get; private set; }

        public string CorrelationId { get; private set; }

        /// <summary>Uso exclusivo do EF (T-11)/testes. Não usar para criar registro novo.</summary>
        protected VendaHistorico()
        {
        }

        internal VendaHistorico(
            Operacao operacao,
            StatusVenda? statusAnterior,
            StatusVenda statusNovo,
            DateTime dataHoraUtc,
            string motivo,
            string correlationId)
        {
            Operacao = operacao;
            StatusAnterior = statusAnterior;
            StatusNovo = statusNovo;
            DataHora = dataHoraUtc;
            Motivo = motivo;
            CorrelationId = correlationId;
        }
    }
}

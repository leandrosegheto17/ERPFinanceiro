using System;
using System.Collections.Generic;
using ERPFinanceiro.Domain.Enums;
using ERPFinanceiro.Domain.Exceptions;

namespace ERPFinanceiro.Domain.Entities
{
    /// <summary>
    /// Agregado raiz Venda. Espelha FIN_VENDA (database/01-schema.sql) e as regras de
    /// ADR-007/D-08: ClienteId e ValorTotal são anuláveis (NULL só nasce hoje pela
    /// venda de cancelamento desconhecido — regra em si é responsabilidade de T-09/
    /// do serviço de cancelamento; aqui as duas fábricas sempre preenchem os dois
    /// campos, "NOT NULL lógico" para vendas nascidas por quitação/pendente, SDD 5).
    ///
    /// Transições de estado: Quitar()/Cancelar() (T-08, RN-03/RN-04). Cancelada é
    /// terminal: qualquer operação (exceto o próprio Cancelar) sobre uma venda
    /// Cancelada lança VendaJaCanceladaException. Repetição da mesma transição é
    /// idempotente (não duplica histórico, sinaliza "sem mudança" via retorno bool).
    /// Regra de `motivo` obrigatório em Quitada->Cancelada (S-05) implementada em T-09.
    /// </summary>
    public class Venda
    {
        private readonly List<VendaItem> _itens = new List<VendaItem>();
        private readonly List<VendaHistorico> _historico = new List<VendaHistorico>();

        /// <summary>PK técnica; atribuída pelo banco (identity). 0 enquanto não persistida.</summary>
        public long Id { get; internal set; }

        public string VendaId { get; private set; }

        /// <summary>Anulável (ADR-007/D-08). NULL só ocorre na venda de cancelamento desconhecido (T-09).</summary>
        public string ClienteId { get; private set; }

        /// <summary>Anulável (ADR-007/D-08). NULL só ocorre na venda de cancelamento desconhecido (T-09).</summary>
        public decimal? ValorTotal { get; private set; }

        public StatusVenda Status { get; private set; }

        public DateTime DataRecebimento { get; private set; }

        public DateTime? DataQuitacao { get; private set; }

        public DateTime? DataCancelamento { get; private set; }

        public string MotivoCancelamento { get; private set; }

        /// <summary>Concorrência otimista; incrementada pela aplicação (TASK.md Seção 1, regra 8), nunca por trigger.</summary>
        public int Versao { get; internal set; }

        public IReadOnlyList<VendaItem> Itens => _itens;

        public IReadOnlyList<VendaHistorico> Historico => _historico;

        /// <summary>
        /// Uso exclusivo do mapeamento Fluent do EF6 (T-11, <c>FinanceiroDbContext</c>).
        /// EF6 clássico precisa de uma propriedade de navegação do tipo <see cref="ICollection{T}"/>
        /// (com Add/Clear) para materializar/rastrear a coleção; <see cref="Itens"/> é
        /// deliberadamente somente-leitura (IReadOnlyList) para não expor mutação pública do
        /// agregado. Esta propriedade interna aponta para o mesmo backing field (List&lt;T&gt;
        /// implementa ICollection&lt;T&gt;), visível só para ERPFinanceiro.Infrastructure via
        /// InternalsVisibleTo (ERPFinanceiro.Domain.csproj) — a API pública do domínio (Itens)
        /// não muda.
        /// </summary>
        internal ICollection<VendaItem> ItensEf => _itens;

        /// <summary>Mesma decisão de <see cref="ItensEf"/>, para <see cref="Historico"/>.</summary>
        internal ICollection<VendaHistorico> HistoricoEf => _historico;

        /// <summary>Uso exclusivo do EF (T-11)/testes. Não usar para criar venda nova.</summary>
        protected Venda()
        {
        }

        private Venda(string vendaId, string clienteId, decimal? valorTotal, IEnumerable<VendaItem> itens, DateTime dataRecebimentoUtc)
        {
            if (string.IsNullOrWhiteSpace(vendaId))
            {
                throw new ArgumentException("VendaId é obrigatório.", nameof(vendaId));
            }

            VendaId = vendaId;
            ClienteId = clienteId;
            ValorTotal = valorTotal;
            DataRecebimento = dataRecebimentoUtc;
            Versao = 0;

            if (itens != null)
            {
                _itens.AddRange(itens);
            }
        }

        /// <summary>
        /// Nasce Pendente: fluxo normal de recepção de venda (RegistroService, SDD 2.2/2.3;
        /// POST /api/vendas). Gera um único registro de histórico (Recebida, sem status
        /// anterior).
        /// </summary>
        public static Venda CriarPendente(
            string vendaId,
            string clienteId,
            decimal valorTotal,
            IEnumerable<VendaItem> itens,
            DateTime dataRecebimentoUtc,
            string correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(clienteId))
            {
                throw new ArgumentException("ClienteId é obrigatório para venda Pendente.", nameof(clienteId));
            }

            var venda = new Venda(vendaId, clienteId, valorTotal, itens, dataRecebimentoUtc)
            {
                Status = StatusVenda.Pendente
            };

            venda._historico.Add(new VendaHistorico(
                Operacao.Recebida,
                statusAnterior: null,
                statusNovo: StatusVenda.Pendente,
                dataHoraUtc: dataRecebimentoUtc,
                motivo: null,
                correlationId: correlationId));

            return venda;
        }

        /// <summary>
        /// Nasce Quitada: fluxo de quitação de venda desconhecida (SDD 2.2, "inexistente:
        /// cria Venda + itens, Quitar()"). Como a venda nunca existiu antes, o agregado
        /// nasce já no estado final, com o histórico completo (Recebida seguida de
        /// Quitação) para preservar a auditoria (TASK.md Seção 1, regra 8).
        /// </summary>
        public static Venda CriarPorQuitacao(
            string vendaId,
            string clienteId,
            decimal valorTotal,
            IEnumerable<VendaItem> itens,
            DateTime dataRecebimentoUtc,
            DateTime dataQuitacaoUtc,
            string correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(clienteId))
            {
                throw new ArgumentException("ClienteId é obrigatório para venda Quitada.", nameof(clienteId));
            }

            if (dataQuitacaoUtc < dataRecebimentoUtc)
            {
                throw new ArgumentOutOfRangeException(nameof(dataQuitacaoUtc), dataQuitacaoUtc, "DataQuitacao não pode ser anterior à DataRecebimento.");
            }

            var venda = new Venda(vendaId, clienteId, valorTotal, itens, dataRecebimentoUtc)
            {
                Status = StatusVenda.Quitada,
                DataQuitacao = dataQuitacaoUtc
            };

            venda._historico.Add(new VendaHistorico(
                Operacao.Recebida,
                statusAnterior: null,
                statusNovo: StatusVenda.Pendente,
                dataHoraUtc: dataRecebimentoUtc,
                motivo: null,
                correlationId: correlationId));

            venda._historico.Add(new VendaHistorico(
                Operacao.Quitacao,
                statusAnterior: StatusVenda.Pendente,
                statusNovo: StatusVenda.Quitada,
                dataHoraUtc: dataQuitacaoUtc,
                motivo: null,
                correlationId: correlationId));

            return venda;
        }

        /// <summary>
        /// Nasce Cancelada: cenário D-08/ADR-007 "cancelamento chega para venda
        /// desconhecida" (T-10). Como este sistema nunca teve conhecimento prévio
        /// da venda (só recebemos vendaId/motivo do cancelamento), o agregado nasce
        /// direto no estado terminal Cancelada, sem itens e com ClienteId/ValorTotal
        /// NULL (ADR-007: "Sem itens", colunas anuláveis). Histórico com uma única
        /// entrada Cancelamento (StatusAnterior NULL) — sem uma entrada Recebida
        /// separada, conforme decisão já ratificada e documentada no comentário de
        /// database/02-seed.sql sobre V-1003 (T-06), que segue o ADR-007 em vez do
        /// exemplo ilustrativo genérico da linha T-06 do TASK.md. DataRecebimento
        /// reaproveita a própria dataCancelamentoUtc (mesma lógica do seed).
        /// </summary>
        public static Venda CriarCancelada(
            string vendaId,
            string motivo,
            DateTime dataCancelamentoUtc,
            string correlationId = null)
        {
            var venda = new Venda(vendaId, clienteId: null, valorTotal: null, itens: null, dataRecebimentoUtc: dataCancelamentoUtc)
            {
                Status = StatusVenda.Cancelada,
                DataCancelamento = dataCancelamentoUtc,
                MotivoCancelamento = motivo
            };

            venda._historico.Add(new VendaHistorico(
                Operacao.Cancelamento,
                statusAnterior: null,
                statusNovo: StatusVenda.Cancelada,
                dataHoraUtc: dataCancelamentoUtc,
                motivo: motivo,
                correlationId: correlationId));

            return venda;
        }

        /// <summary>
        /// Transição Pendente->Quitada (RN-04). Idempotente: se a venda já está
        /// Quitada, não altera nada e devolve false ("sem mudança"). Se a venda está
        /// Cancelada (estado terminal), lança VendaJaCanceladaException. Adiciona um
        /// VendaHistorico (Quitacao) quando efetivamente transiciona.
        /// </summary>
        /// <returns>true se houve transição de estado; false se já estava Quitada (idempotente).</returns>
        public bool Quitar(DateTime dataQuitacaoUtc, string correlationId = null)
        {
            if (Status == StatusVenda.Cancelada)
            {
                throw new VendaJaCanceladaException(VendaId);
            }

            if (Status == StatusVenda.Quitada)
            {
                return false;
            }

            var statusAnterior = Status;
            Status = StatusVenda.Quitada;
            DataQuitacao = dataQuitacaoUtc;

            _historico.Add(new VendaHistorico(
                Operacao.Quitacao,
                statusAnterior: statusAnterior,
                statusNovo: StatusVenda.Quitada,
                dataHoraUtc: dataQuitacaoUtc,
                motivo: null,
                correlationId: correlationId));

            return true;
        }

        /// <summary>
        /// Transição Pendente->Cancelada ou Quitada->Cancelada (RN-03/RN-04). Cancelada
        /// é terminal e o próprio Cancelar é idempotente sobre ela: não lança, não gera
        /// novo histórico, devolve false ("sem mudança"). Regra S-05 (T-09): cancelar uma
        /// venda Quitada exige `motivo` não vazio, senão lança MotivoObrigatorioException
        /// sem alterar nenhum estado (Status/Versao/histórico inalterados); cancelar uma
        /// venda Pendente continua aceitando motivo nulo/vazio (opcional nesse caso). Se
        /// motivo for informado, é guardado em MotivoCancelamento.
        /// </summary>
        /// <returns>true se houve transição de estado; false se já estava Cancelada (idempotente).</returns>
        /// <exception cref="MotivoObrigatorioException">
        /// Venda Quitada cancelada sem motivo (ou motivo em branco).
        /// </exception>
        public bool Cancelar(DateTime dataCancelamentoUtc, string motivo = null, string correlationId = null)
        {
            if (Status == StatusVenda.Cancelada)
            {
                return false;
            }

            if (Status == StatusVenda.Quitada && string.IsNullOrWhiteSpace(motivo))
            {
                throw new MotivoObrigatorioException();
            }

            var statusAnterior = Status;
            Status = StatusVenda.Cancelada;
            DataCancelamento = dataCancelamentoUtc;

            if (!string.IsNullOrWhiteSpace(motivo))
            {
                MotivoCancelamento = motivo;
            }

            _historico.Add(new VendaHistorico(
                Operacao.Cancelamento,
                statusAnterior: statusAnterior,
                statusNovo: StatusVenda.Cancelada,
                dataHoraUtc: dataCancelamentoUtc,
                motivo: motivo,
                correlationId: correlationId));

            return true;
        }
    }
}

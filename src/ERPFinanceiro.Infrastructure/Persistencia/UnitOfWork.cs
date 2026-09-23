using System;
using System.Data.Entity.Infrastructure;
using ERPFinanceiro.Application.Exceptions;
using ERPFinanceiro.Application.Interfaces;
using FirebirdSql.Data.FirebirdClient;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// Implementação EF6 (T-15) de <see cref="IUnitOfWork"/> contra
    /// <see cref="FinanceiroDbContext"/> (T-11). Venda + itens + histórico ficam atômicos
    /// (TASK.md Seção 1, regra 8) por um único <c>DbContext.SaveChanges()</c>: o EF6, por
    /// padrão, já agrupa toda a árvore de mudanças rastreadas (a venda raiz + os itens e o
    /// histórico adicionados ao mesmo grafo via <see cref="VendaRepository.Adicionar"/> ou
    /// via mutação do agregado já carregado) numa única transação de banco implícita quando
    /// há exatamente uma chamada a <c>SaveChanges()</c> por unidade de trabalho — não há
    /// <c>TransactionScope</c> manual aqui de propósito, para não abrir uma segunda conexão/
    /// transação por cima da que o EF já controla (o que quebraria a atomicidade em vez de
    /// reforçá-la). Ver <see cref="SalvarAlteracoes"/> para a regra que garante isso.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FinanceiroDbContext _contexto;

        public UnitOfWork(FinanceiroDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Importante para manter a atomicidade (regra 8): esta classe expõe **só** este
        /// único ponto de commit por instância de <see cref="UnitOfWork"/>/<see cref="FinanceiroDbContext"/>.
        /// Quem consome esta classe (Application, T-18/T-19) deve compor toda a operação de
        /// negócio (registrar/mutar venda + itens + histórico) no mesmo <see cref="FinanceiroDbContext"/>
        /// e chamar <see cref="SalvarAlteracoes"/> **uma única vez** ao final; chamar
        /// <c>SaveChanges()</c> diretamente no contexto, ou chamar este método mais de uma
        /// vez para a mesma operação, quebraria a garantia de "mesma transação" (cada
        /// chamada a <c>SaveChanges()</c> abre/fecha sua própria transação implícita do
        /// EF6). Não há como impedir isso só por tipo aqui (o contexto é injetado de fora,
        /// T-14/T-18); a garantia é de contrato/uso, documentada também em
        /// <see cref="IUnitOfWork"/>.
        /// </remarks>
        /// <exception cref="ConcorrenciaException">
        /// Conflito de concorrência: (1) <see cref="DbUpdateConcurrencyException"/> — 0
        /// linhas afetadas porque a <c>VERSAO</c> lida já mudou; ou (2) violação da
        /// constraint <c>UNIQUE(VENDA_ID)</c> (duas operações concorrentes criando a mesma
        /// venda nova, upsert RN-06). Qualquer outra falha de <see cref="DbUpdateException"/>
        /// (ex.: truncamento de string, CHECK de item) propaga como está — não é conflito de
        /// concorrência, e o rollback automático do EF6 já desfez a operação inteira.
        /// </exception>
        public void SalvarAlteracoes()
        {
            try
            {
                _contexto.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcorrenciaException(
                    "Conflito de concorrência: a VERSAO da venda foi alterada por outra operação antes deste commit.",
                    ex);
            }
            catch (DbUpdateException ex) when (EhViolacaoDeUniqueVendaId(ex))
            {
                throw new ConcorrenciaException(
                    "Conflito de concorrência: já existe uma venda com o mesmo VENDA_ID (violação de UNIQUE(VENDA_ID)).",
                    ex);
            }
        }

        /// <summary>
        /// Percorre a cadeia de <see cref="Exception.InnerException"/> até achar uma
        /// <see cref="FbException"/> que indique violação de PRIMARY/UNIQUE KEY envolvendo a
        /// constraint <c>UQ_FIN_VENDA_VENDA_ID</c> (database/01-schema.sql, T-05).
        /// Sinal primário, independente de idioma: <see cref="FbException.ErrorCode"/> ==
        /// <see cref="IscUniqueKeyViolation"/> (gds 335544665, isc_unique_key_violation; o
        /// provider 10.3.4 não tem constante nomeada em <c>IscCodes</c>) ou
        /// <see cref="FbException.SQLSTATE"/> == <see cref="SqlStateIntegridade"/> ("23000",
        /// classe integridade). O nome da constraint não é traduzido, então continua sendo
        /// checado na mensagem para não confundir com outra UNIQUE do schema. Fallback: se
        /// nenhum código bater, o texto em inglês do engine ("violation of PRIMARY or UNIQUE
        /// KEY constraint...") ainda é aceito.
        /// </summary>
        internal static bool EhViolacaoDeUniqueVendaId(Exception excecao)
        {
            for (var atual = excecao; atual != null; atual = atual.InnerException)
            {
                var fbExcecao = atual as FbException;
                if (fbExcecao != null && IndicaViolacaoDeUniqueVendaId(fbExcecao))
                {
                    return true;
                }
            }

            return false;
        }

        private const int IscUniqueKeyViolation = 335544665;
        private const string SqlStateIntegridade = "23000";

        private static bool IndicaViolacaoDeUniqueVendaId(FbException fb)
        {
            string mensagem = fb.Message;
            if (string.IsNullOrEmpty(mensagem))
            {
                return false;
            }

            bool ehConstraintDaVendaId = mensagem.IndexOf("UQ_FIN_VENDA_VENDA_ID", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!ehConstraintDaVendaId)
            {
                return false;
            }

            if (fb.ErrorCode == IscUniqueKeyViolation || fb.SQLSTATE == SqlStateIntegridade)
            {
                return true;
            }

            return MensagemIndicaViolacaoDeUnique(mensagem);
        }

        private static bool MensagemIndicaViolacaoDeUnique(string mensagem)
        {
            bool ehViolacaoDeChave = mensagem.IndexOf("violation of PRIMARY or UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase) >= 0
                || mensagem.IndexOf("violation of FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase) < 0
                   && mensagem.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0
                   && mensagem.IndexOf("violat", StringComparison.OrdinalIgnoreCase) >= 0;

            bool ehConstraintDaVendaId = mensagem.IndexOf("UQ_FIN_VENDA_VENDA_ID", StringComparison.OrdinalIgnoreCase) >= 0;

            return ehViolacaoDeChave && ehConstraintDaVendaId;
        }
    }
}

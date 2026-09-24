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
        /// Gds-code estável do Firebird para violação de PRIMARY/UNIQUE KEY
        /// (<c>isc_unique_key_violation</c> no include C do engine). Confirmado
        /// empiricamente (RL4-01) via <see cref="FbException.ErrorCode"/> forçando a
        /// violação real de <c>UQ_FIN_VENDA_VENDA_ID</c> contra Firebird embarcado
        /// (mesmo harness de <c>UnitOfWorkTests.SalvarAlteracoes_ViolacaoDeUniqueVendaId_LancaConcorrenciaException</c>,
        /// T-15): <c>ErrorCode=335544665</c>, <c>SQLSTATE=23000</c>. O provider
        /// gerenciado (FirebirdSql.Data.FirebirdClient 10.3.4) não expõe esse valor como
        /// constante nomeada em <c>IscCodes</c>, mas o valor numérico é estável — é o
        /// gds-code isc_* interno do engine, não uma string de mensagem sujeita a locale.
        /// Nota importante do mesmo experimento: <c>SQLSTATE</c> sozinho **não** é
        /// específico o bastante — a violação de CHECK constraint
        /// (<c>CK_FIN_VENDA_HIST_OPER</c>, coberta por
        /// <c>SalvarAlteracoes_FalhaInjetadaNoHistorico_FazRollbackDaVendaInteira</c>)
        /// também retorna <c>SQLSTATE=23000</c> (mesma classe "integrity constraint
        /// violation"), com <c>ErrorCode=335544558</c> diferente. Por isso o sinal
        /// primário aqui é o <c>ErrorCode</c>, não o <c>SQLSTATE</c>.
        /// </summary>
        private const int GdsCodeViolacaoDeChaveUnica = 335544665;

        /// <summary>
        /// Percorre a cadeia de <see cref="Exception.InnerException"/> até achar uma
        /// <see cref="FbException"/> que sinalize violação de PRIMARY/UNIQUE KEY
        /// envolvendo a constraint <c>UQ_FIN_VENDA_VENDA_ID</c> (database/01-schema.sql,
        /// T-05). Sinal primário, locale-independente: <see cref="FbException.ErrorCode"/>
        /// igual a <see cref="GdsCodeViolacaoDeChaveUnica"/> (ver doc do campo acima).
        /// Fallback (mantido por segurança, RL4-01): se o <c>ErrorCode</c> não bater —
        /// versão futura do provider que renumere o gds-code, por exemplo — cai para o
        /// texto em inglês da mensagem padrão do engine, como antes desta tarefa. Em
        /// ambos os casos, checar o nome da constraint na mensagem evita confundir com
        /// uma eventual UNIQUE diferente adicionada no futuro a outra tabela do mesmo
        /// schema (o <c>ErrorCode</c> por si só não identifica qual constraint violou).
        /// </summary>
        private static bool EhViolacaoDeUniqueVendaId(Exception excecao)
        {
            for (var atual = excecao; atual != null; atual = atual.InnerException)
            {
                var fbExcecao = atual as FbException;
                if (fbExcecao == null)
                {
                    continue;
                }

                bool ehConstraintDaVendaId = fbExcecao.Message != null
                    && fbExcecao.Message.IndexOf("UQ_FIN_VENDA_VENDA_ID", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!ehConstraintDaVendaId)
                {
                    continue;
                }

                bool ehViolacaoDeChave = fbExcecao.ErrorCode == GdsCodeViolacaoDeChaveUnica
                    || MensagemIndicaViolacaoDeChaveUnica(fbExcecao.Message);

                if (ehViolacaoDeChave)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fallback (RL4-01) baseado no texto em inglês da mensagem padrão do Firebird,
        /// usado só quando <see cref="FbException.ErrorCode"/> não bate com
        /// <see cref="GdsCodeViolacaoDeChaveUnica"/>. Sinal original desta checagem, antes
        /// de RL4-01 introduzir o <c>ErrorCode</c> como sinal primário.
        /// </summary>
        private static bool MensagemIndicaViolacaoDeChaveUnica(string mensagem)
        {
            if (string.IsNullOrEmpty(mensagem))
            {
                return false;
            }

            return mensagem.IndexOf("violation of PRIMARY or UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase) >= 0
                || mensagem.IndexOf("violation of FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase) < 0
                   && mensagem.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0
                   && mensagem.IndexOf("violat", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

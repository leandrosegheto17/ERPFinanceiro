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
        /// <see cref="FbException"/> cuja mensagem indique violação de PRIMARY/UNIQUE KEY
        /// envolvendo a constraint <c>UQ_FIN_VENDA_VENDA_ID</c> (database/01-schema.sql,
        /// T-05). Firebird não expõe um código de erro público/estável específico para
        /// "unique violation" na API gerenciada (<c>FbException.ErrorCode</c> carrega o
        /// gds-code isc_* interno do provider, sem constante pública equivalente na versão
        /// 10.3.4 usada aqui — verificado por reflexão contra o assembly do NuGet); a
        /// mensagem padrão do engine ("violation of PRIMARY or UNIQUE KEY constraint...")
        /// é o sinal estável documentado pelo próprio Firebird. Checar o nome da constraint
        /// evita confundir com uma eventual UNIQUE diferente adicionada no futuro a outra
        /// tabela do mesmo schema.
        /// </summary>
        private static bool EhViolacaoDeUniqueVendaId(Exception excecao)
        {
            for (var atual = excecao; atual != null; atual = atual.InnerException)
            {
                var fbExcecao = atual as FbException;
                if (fbExcecao != null && MensagemIndicaViolacaoDeUnique(fbExcecao.Message))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MensagemIndicaViolacaoDeUnique(string mensagem)
        {
            if (string.IsNullOrEmpty(mensagem))
            {
                return false;
            }

            bool ehViolacaoDeChave = mensagem.IndexOf("violation of PRIMARY or UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase) >= 0
                || mensagem.IndexOf("violation of FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase) < 0
                   && mensagem.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0
                   && mensagem.IndexOf("violat", StringComparison.OrdinalIgnoreCase) >= 0;

            bool ehConstraintDaVendaId = mensagem.IndexOf("UQ_FIN_VENDA_VENDA_ID", StringComparison.OrdinalIgnoreCase) >= 0;

            return ehViolacaoDeChave && ehConstraintDaVendaId;
        }
    }
}

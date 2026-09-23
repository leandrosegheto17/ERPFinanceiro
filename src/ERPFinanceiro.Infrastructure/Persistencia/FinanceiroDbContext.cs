using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using ERPFinanceiro.Domain.Entities;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// DbContext EF6 (T-11) das 3 entidades do agregado Venda. Mapeamento Fluent
    /// (<see cref="OnModelCreating"/>) batendo exatamente com os nomes de tabela/coluna de
    /// <c>database/01-schema.sql</c> (fonte da verdade — mesma convenção já usada pelo schema
    /// embutido de T-12/<see cref="DbInitializer"/>).
    ///
    /// Sem migrations (TASK.md Seção 1, regra 10): o schema é criado/gerenciado só pelo
    /// <c>01-schema.sql</c> (via T-12); este contexto nunca deve criar/alterar tabelas —
    /// <see cref="Database.SetInitializer{TContext}"/> é desligado no construtor estático.
    ///
    /// Padrão de conexão idêntico ao spike validado em ADR-009/T-01 (<c>SpikeContext</c>):
    /// recebe a <see cref="DbConnection"/> (Firebird embarcado) já aberta/pronta de fora —
    /// quem decide a connection string/credenciais é o chamador (T-14 repositório, T-26
    /// composition root), este contexto só mapeia o modelo.
    /// </summary>
    public class FinanceiroDbContext : DbContext
    {
        static FinanceiroDbContext()
        {
            // DDL manual via 01-schema.sql (T-05/T-12); o EF nunca cria/valida schema sozinho.
            Database.SetInitializer<FinanceiroDbContext>(null);
        }

        public FinanceiroDbContext(DbConnection connection, bool contextOwnsConnection = true)
            : base(connection, contextOwnsConnection)
        {
            // Entidades de domínio (Venda/VendaItem/VendaHistorico) não são proxies dinâmicos
            // (construtores protected/internal set — T-07/T-08) e a tela é somente leitura via
            // ConsultaService com AsNoTracking (T-14/T-23); lazy loading fica desligado para
            // evitar acesso ao banco fora de escopo (TASK.md Seção 1, regra 5).
            Configuration.ProxyCreationEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<Venda> Vendas { get; set; }

        public DbSet<VendaItem> VendaItens { get; set; }

        public DbSet<VendaHistorico> VendaHistoricos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            MapearVenda(modelBuilder);
            MapearVendaItem(modelBuilder);
            MapearVendaHistorico(modelBuilder);
        }

        private static void MapearVenda(DbModelBuilder modelBuilder)
        {
            var venda = modelBuilder.Entity<Venda>();
            venda.ToTable("FIN_VENDA");
            venda.HasKey(v => v.Id);

            venda.Property(v => v.Id)
                .HasColumnName("ID")
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            venda.Property(v => v.VendaId)
                .HasColumnName("VENDA_ID")
                .IsRequired()
                .HasMaxLength(50);

            // Anuláveis (ADR-007/D-08): venda de cancelamento desconhecido.
            venda.Property(v => v.ClienteId)
                .HasColumnName("CLIENTE_ID")
                .IsOptional()
                .HasMaxLength(50);

            venda.Property(v => v.ValorTotal)
                .HasColumnName("VALOR_TOTAL")
                .IsOptional()
                .HasPrecision(18, 2);

            venda.Property(v => v.Status)
                .HasColumnName("STATUS")
                .IsRequired();

            venda.Property(v => v.DataRecebimento)
                .HasColumnName("DATA_RECEBIMENTO")
                .IsRequired();

            venda.Property(v => v.DataQuitacao)
                .HasColumnName("DATA_QUITACAO")
                .IsOptional();

            venda.Property(v => v.DataCancelamento)
                .HasColumnName("DATA_CANCELAMENTO")
                .IsOptional();

            venda.Property(v => v.MotivoCancelamento)
                .HasColumnName("MOTIVO_CANCELAMENTO")
                .IsOptional()
                .HasMaxLength(500);

            // Concorrência otimista (ADR-005/T-01): incrementada pela aplicação, sem trigger
            // (TASK.md Seção 1, regra 8) — mesmo comportamento comprovado no spike (ADR-009).
            venda.Property(v => v.Versao)
                .HasColumnName("VERSAO")
                .IsConcurrencyToken();

            // API pública do agregado (IReadOnlyList) não é mapeável como navegação EF6
            // (precisa de ICollection<T> mutável) — mapeamento real usa as propriedades
            // internal ItensEf/HistoricoEf (ver Venda.cs); Itens/Historico ficam fora do modelo.
            venda.Ignore(v => v.Itens);
            venda.Ignore(v => v.Historico);

            // Associação independente (sem propriedade de FK no lado filho — VendaItem/
            // VendaHistorico não têm "VendaId"): EF6 cria a coluna de FK sombra e o Fluent Map
            // só precisa dizer o nome real da coluna (VENDA_REF), igual ao DDL. FK CASCADE já
            // está no schema (ON DELETE CASCADE); WithRequired() reflete NOT NULL de VENDA_REF.
            venda.HasMany(v => v.ItensEf)
                .WithRequired()
                .Map(m => m.MapKey("VENDA_REF"));

            venda.HasMany(v => v.HistoricoEf)
                .WithRequired()
                .Map(m => m.MapKey("VENDA_REF"));
        }

        private static void MapearVendaItem(DbModelBuilder modelBuilder)
        {
            var item = modelBuilder.Entity<VendaItem>();
            item.ToTable("FIN_VENDA_ITEM");
            item.HasKey(i => i.Id);

            item.Property(i => i.Id)
                .HasColumnName("ID")
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            item.Property(i => i.ProdutoId)
                .HasColumnName("PRODUTO_ID")
                .IsRequired()
                .HasMaxLength(50);

            item.Property(i => i.Quantidade)
                .HasColumnName("QUANTIDADE")
                .IsRequired();

            // DECIMAL(18,4) — precisão diferente de VALOR_TOTAL (18,2); é justamente o caso não
            // exercitado no spike T-01 (ADR-009, item 2) e o foco do critério de aceite de T-11.
            item.Property(i => i.PrecoUnitario)
                .HasColumnName("PRECO_UNITARIO")
                .IsRequired()
                .HasPrecision(18, 4);
        }

        private static void MapearVendaHistorico(DbModelBuilder modelBuilder)
        {
            var historico = modelBuilder.Entity<VendaHistorico>();
            historico.ToTable("FIN_VENDA_HISTORICO");
            historico.HasKey(h => h.Id);

            historico.Property(h => h.Id)
                .HasColumnName("ID")
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            historico.Property(h => h.Operacao)
                .HasColumnName("OPERACAO")
                .IsRequired();

            // NULL no primeiro registro (criação da venda) — ver Venda.CriarPendente/CriarPorQuitacao.
            historico.Property(h => h.StatusAnterior)
                .HasColumnName("STATUS_ANTERIOR")
                .IsOptional();

            historico.Property(h => h.StatusNovo)
                .HasColumnName("STATUS_NOVO")
                .IsRequired();

            historico.Property(h => h.DataHora)
                .HasColumnName("DATA_HORA")
                .IsRequired();

            historico.Property(h => h.Motivo)
                .HasColumnName("MOTIVO")
                .IsOptional()
                .HasMaxLength(500);

            historico.Property(h => h.CorrelationId)
                .HasColumnName("CORRELATION_ID")
                .IsOptional()
                .HasMaxLength(50);
        }
    }
}

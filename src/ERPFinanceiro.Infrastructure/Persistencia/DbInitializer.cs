using System;
using System.IO;
using System.Reflection;
using ERPFinanceiro.Application.Interfaces;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;

namespace ERPFinanceiro.Infrastructure.Persistencia
{
    /// <summary>
    /// Cria o .fdb embarcado se ausente e aplica <c>database/01-schema.sql</c>
    /// sem passo manual do operador (CA-05.5, T-12). Credenciais vêm de
    /// <see cref="IConfiguracaoBanco"/> (nunca hardcoded, nunca lidas
    /// diretamente do App.config fora da Infrastructure).
    ///
    /// Decisão de empacotamento (T-12/T-53): o script é embutido como
    /// Embedded Resource (<c>Persistencia/Schema/01-schema.sql</c>, linkado a
    /// partir do único arquivo-fonte em <c>database/01-schema.sql</c> — ver
    /// ERPFinanceiro.Infrastructure.csproj) em vez de copiado/lido de um
    /// caminho relativo em disco: fica embutido no assembly, não depende da
    /// estrutura de pastas do pacote de instalação nem do diretório de
    /// trabalho do processo, e não há dois arquivos para manter sincronizados
    /// (o schema tem uma única fonte, no repositório).
    ///
    /// Reaproveita o parser/executor de script isql (FbScript/FbBatchExecution)
    /// já validado nos harnesses de T-05/T-06 (mesmo mecanismo, entende
    /// SET TERM).
    /// </summary>
    public sealed class DbInitializer
    {
        private const string LogicalNameSchema =
            "ERPFinanceiro.Infrastructure.Persistencia.Schema._01_schema.sql";

        private readonly IConfiguracaoBanco _configuracao;

        public DbInitializer(IConfiguracaoBanco configuracao)
        {
            _configuracao = configuracao ?? throw new ArgumentNullException(nameof(configuracao));
        }

        /// <summary>
        /// Garante que o banco exista e tenha o schema aplicado. Idempotente:
        /// se o .fdb já existe, é no-op (não tenta recriar tabelas). Se o
        /// arquivo existe mas não pode ser aberto (bloqueado por outro
        /// handle/processo), lança <see cref="BancoIndisponivelException"/>
        /// com mensagem clara (RT-05).
        /// </summary>
        public void Inicializar()
        {
            string caminhoFdb = _configuracao.CaminhoFdb;

            if (File.Exists(caminhoFdb))
            {
                GarantirAcessivel(caminhoFdb);
                return; // no-op: banco já existe, schema já aplicado anteriormente.
            }

            string diretorio = Path.GetDirectoryName(caminhoFdb);
            if (!string.IsNullOrEmpty(diretorio) && !Directory.Exists(diretorio))
            {
                Directory.CreateDirectory(diretorio);
            }

            CriarBanco(caminhoFdb);
            AplicarSchema(caminhoFdb);
        }

        private string MontarConnectionString(string caminhoFdb)
        {
            var csb = new FbConnectionStringBuilder
            {
                Database = caminhoFdb,
                UserID = _configuracao.Usuario,
                Password = _configuracao.Senha,
                ServerType = FbServerType.Embedded,
                Charset = "UTF8",
                Pooling = false
            };
            return csb.ConnectionString;
        }

        private void CriarBanco(string caminhoFdb)
        {
            string connectionString = MontarConnectionString(caminhoFdb);
            try
            {
                FbConnection.CreateDatabase(connectionString, 8192, forcedWrites: true, overwrite: false);
            }
            catch (Exception ex)
            {
                throw new BancoIndisponivelException(
                    $"Não foi possível criar o banco de dados em '{caminhoFdb}'. " +
                    "Verifique permissões de escrita no diretório e se o caminho está correto.",
                    ex);
            }
        }

        private void AplicarSchema(string caminhoFdb)
        {
            string script = LerScriptSchemaEmbutido();
            string connectionString = MontarConnectionString(caminhoFdb);

            try
            {
                var fbScript = new FbScript(script);
                fbScript.Parse();

                using (var connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    var execucao = new FbBatchExecution(connection);
                    execucao.AppendSqlStatements(fbScript);
                    execucao.Execute();
                }
            }
            catch (Exception ex)
            {
                // Falha ao aplicar o schema num .fdb recém-criado: apaga para não deixar
                // um banco "meio criado" que o próximo Inicializar() trataria como já pronto (no-op).
                TentarApagar(caminhoFdb);
                throw new BancoIndisponivelException(
                    $"Falha ao aplicar database/01-schema.sql no banco recém-criado em '{caminhoFdb}'.",
                    ex);
            }
        }

        private void GarantirAcessivel(string caminhoFdb)
        {
            string connectionString = MontarConnectionString(caminhoFdb);
            try
            {
                using (var connection = new FbConnection(connectionString))
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {
                throw new BancoIndisponivelException(
                    $"O banco de dados em '{caminhoFdb}' existe mas não pôde ser aberto. " +
                    "Ele pode estar bloqueado por outro processo/handle (RT-05): feche outras " +
                    "instâncias do aplicativo e tente novamente.",
                    ex);
            }
        }

        private static string LerScriptSchemaEmbutido()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(LogicalNameSchema))
            {
                if (stream == null)
                {
                    string disponiveis = string.Join(", ", assembly.GetManifestResourceNames());
                    throw new InvalidOperationException(
                        $"Recurso embutido '{LogicalNameSchema}' não encontrado no assembly " +
                        $"{assembly.FullName}. Recursos disponíveis: {disponiveis}");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static void TentarApagar(string caminhoFdb)
        {
            try
            {
                if (File.Exists(caminhoFdb))
                {
                    File.Delete(caminhoFdb);
                }
            }
            catch
            {
                // Melhor esforço: se não conseguir apagar, a exceção original já reporta o problema real.
            }
        }
    }
}

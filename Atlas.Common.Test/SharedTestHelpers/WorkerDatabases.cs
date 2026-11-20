using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Transactions;
using NUnit.Framework.Internal;

namespace Atlas.Common.Test.SharedTestHelpers
{
    /// <summary>
    /// Gives each NUnit worker its own copy of the databases of an integration test project, so that fixtures can run at the same time
    /// without seeing each other's data: each worker runs its fixtures one after another, as a run with one worker does, in databases of its own.
    ///
    /// Once the project's global set-up has migrated and reset its databases, <see cref="Create"/> backs them up. The first time a worker
    /// reads a connection string, its copy is restored from the backup, and from then on <see cref="AddWorkerDatabases"/> gives it the copy's
    /// connection string. Code outside any fixture, such as the global set-up, gets the configured databases.
    /// The copies are named after the configured databases and the worker, and are not dropped: dropping a database waits for the
    /// connections to it to close. The next run replaces them.
    /// </summary>
    public static class WorkerDatabases
    {
        private static readonly ConcurrentDictionary<string, Func<string, string>> RestoreByConnectionString = new();
        private static readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<string, string>>> CopiesByWorker = new();

        /// <summary>
        /// Backs up the given databases, to restore a copy of each for every worker that uses it.
        /// </summary>
        /// <param name="connectionStrings">The configured connection strings of the databases; the same database may appear more than once.</param>
        public static void Create(IEnumerable<string> connectionStrings)
        {
            foreach (var connectionString in connectionStrings.Distinct())
            {
                RestoreByConnectionString[connectionString] = Backup(connectionString);
            }
        }

        /// <summary>
        /// The connection string of the current worker's copy of a configured database, or the configured one outside any fixture,
        /// before <see cref="Create"/>, or for a database it was not given.
        /// </summary>
        public static string ConnectionString(string configured)
        {
            var worker = TestExecutionContext.CurrentContext.TestWorker?.Name;
            if (configured == null || worker == null || TestFixtureScopes.CurrentFixture() is not TestFixture
                || !RestoreByConnectionString.ContainsKey(configured))
            {
                return configured;
            }

            // All of a worker's copies are restored at once, the first time it reads any of them: that is usually in the constructor
            // or one-time set-up of its first fixture, rather than within a test's transaction, which a restore must not join.
            return CopiesByWorker.GetOrAdd(worker, _ => new Lazy<IReadOnlyDictionary<string, string>>(() =>
            {
                using var noTransaction = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
                return RestoreByConnectionString.ToDictionary(r => r.Key, r => r.Value(worker));
            })).Value[configured];
        }

        /// <summary>
        /// Makes the given connection strings of the configuration being built read the current worker's copies.
        /// The configuration must already hold them when this is called.
        /// </summary>
        public static IConfigurationBuilder AddWorkerDatabases(this IConfigurationBuilder builder, params string[] connectionStringNames)
        {
            return builder.Add(new WorkerDatabasesSource(builder.Build(), connectionStringNames));
        }

        /// <summary>
        /// Backs up a database, and returns a function that restores a copy of it for a worker, and gives the copy's connection string.
        /// </summary>
        private static Func<string, string> Backup(string connectionString)
        {
            var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            var backupFile = $"{database}_WorkerTemplate.bak";
            List<(string Name, bool IsLog)> files;
            using (var connection = Open(connectionString))
            {
                // The copies keep no log backups, so their logs must be truncated at each checkpoint.
                Execute(connection, $"ALTER DATABASE [{database}] SET RECOVERY SIMPLE");
                files = Query(connection, "SELECT name, type FROM sys.database_files", r => (r.GetString(0), r.GetByte(1) == 1));
                // A restore creates each file at its full size, so the files are shrunk first.
                foreach (var file in files)
                {
                    Execute(connection, $"CHECKPOINT; DBCC SHRINKFILE ([{file.Name}], 1) WITH NO_INFOMSGS");
                }
                Execute(connection, $"BACKUP DATABASE [{database}] TO DISK = N'{backupFile}' WITH INIT, COPY_ONLY");
            }

            return worker =>
            {
                var copy = $"{database}_{Regex.Replace(worker, "[^A-Za-z0-9]", "")}";
                using (var master = Open(new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString))
                {
                    var dataPath = Query(master, "SELECT CONVERT(nvarchar(260), SERVERPROPERTY('InstanceDefaultDataPath'))", r => r.GetString(0)).Single();
                    var moves = string.Join(", ", files.Select(f => $"MOVE N'{f.Name}' TO N'{dataPath}{copy}{(f.IsLog ? "_log.ldf" : ".mdf")}'"));
                    Execute(master, $"RESTORE DATABASE [{copy}] FROM DISK = N'{backupFile}' WITH {moves}, REPLACE");
                }

                return new SqlConnectionStringBuilder(connectionString) { InitialCatalog = copy }.ConnectionString;
            };
        }

        private static SqlConnection Open(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static void Execute(SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
            command.ExecuteNonQuery();
        }

        private static List<T> Query<T>(SqlConnection connection, string sql, Func<SqlDataReader, T> read)
        {
            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            var rows = new List<T>();
            while (reader.Read())
            {
                rows.Add(read(reader));
            }
            return rows;
        }

        /// <summary>
        /// Reads the given connection strings from the current worker's copies, and everything else from the configuration it was made with.
        /// Configuration is read each time a value is asked for, so each service gets the copy of the worker that makes it.
        /// </summary>
        private class WorkerDatabasesSource(IConfiguration configured, IReadOnlyCollection<string> connectionStringNames) : IConfigurationSource
        {
            public IConfigurationProvider Build(IConfigurationBuilder builder) => new Provider(configured, connectionStringNames);

            private class Provider(IConfiguration configured, IReadOnlyCollection<string> connectionStringNames) : ConfigurationProvider
            {
                private const string Section = "ConnectionStrings:";

                public override bool TryGet(string key, out string value)
                {
                    value = null;
                    if (!key.StartsWith(Section, StringComparison.OrdinalIgnoreCase)
                        || !connectionStringNames.Contains(key[Section.Length..], StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    value = ConnectionString(configured[key]);
                    return value != null;
                }

                public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) => earlierKeys;
            }
        }
    }
}

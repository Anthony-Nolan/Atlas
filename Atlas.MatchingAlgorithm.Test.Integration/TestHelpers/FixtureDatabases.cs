using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers
{
    /// <summary>
    /// Gives a fixture its own copies of the transient (A, B) and persistent databases, so that it can expect an empty database,
    /// clear it, or change data refresh records, while other fixtures run at the same time.
    /// The copies are restored from backups of the databases taken after the global set-up, when the fixture first reads a connection string
    /// (which can be in its constructor, before any set-up), and dropped after its one-time tear-down: NUnit runs the one-time tear-down
    /// of a base class after that of the fixture, which may still clear its databases.
    /// </summary>
    public abstract class FixtureWithOwnDatabases
    {
        [OneTimeTearDown]
        public void DropOwnDatabases()
        {
            FixtureDatabases.Drop(DependencyInjection.DependencyInjection.CurrentFixtureId());
        }
    }

    internal static class FixtureDatabases
    {
        private const string ConnectionStringsSection = "ConnectionStrings";

        /// <summary>
        /// The connection strings the copies are made of: those of the transient databases and of the persistent database.
        /// </summary>
        private static readonly string[] ConnectionStringKeys = { "SqlA", "SqlB", "PersistentSql" };

        /// <summary>
        /// The connection strings of each fixture that has its own databases, by fixture id and connection string key.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<string, string>>> ConnectionStringsByFixture = new();

        private static IReadOnlyDictionary<string, Template> templates;

        private record Template(string ConnectionString, string Database, string BackupFile, IReadOnlyList<(string LogicalName, string Extension)> Files);

        /// <summary>
        /// The connection string with the given key for the current fixture: its own database if it has one, else the shared one.
        /// </summary>
        public static string ConnectionString(IServiceProvider serviceProvider, string key)
        {
            var fixture = DependencyInjection.DependencyInjection.CurrentFixture();
            var hasOwnDatabases = fixture is TestFixture && fixture.TypeInfo.Type.IsAssignableTo(typeof(FixtureWithOwnDatabases));
            return hasOwnDatabases
                && ConnectionStringsByFixture.GetOrAdd(fixture.Id, id => new Lazy<IReadOnlyDictionary<string, string>>(() => Create(id))).Value
                    .TryGetValue(key, out var own)
                ? own
                : serviceProvider.GetService<IConfiguration>().GetSection(ConnectionStringsSection)[key];
        }

        /// <summary>
        /// Backs up the shared databases, once they are migrated and hold the global test data, as the templates of the fixtures' copies.
        /// Its files are shrunk first, as a restore creates each file at its full size, and every fixture with its own databases has a copy.
        /// </summary>
        public static void CreateTemplates(IConfiguration configuration)
        {
            templates = ConnectionStringKeys.ToDictionary(key => key, key =>
            {
                var connectionString = configuration.GetSection(ConnectionStringsSection)[key];
                var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
                using var connection = new SqlConnection(connectionString);
                // The copies keep no log backups, so their logs must be truncated at each checkpoint.
                connection.Execute($"ALTER DATABASE [{database}] SET RECOVERY SIMPLE");
                var files = connection.Query<(string Name, int Type)>("SELECT name, type FROM sys.database_files").ToList();
                foreach (var file in files)
                {
                    connection.Execute($"CHECKPOINT; DBCC SHRINKFILE ([{file.Name}], 1) WITH NO_INFOMSGS", commandTimeout: 0);
                }

                var backupFile = $"{database}_FixtureTemplate.bak";
                connection.Execute($"BACKUP DATABASE [{database}] TO DISK = @backupFile WITH INIT, COPY_ONLY", new { backupFile }, commandTimeout: 0);
                return new Template(connectionString, database, backupFile, files.Select(f => (f.Name, f.Type == 1 ? "_log.ldf" : ".mdf")).ToList());
            });
        }

        private static IReadOnlyDictionary<string, string> Create(string fixtureId)
        {
            return templates.ToDictionary(t => t.Key, t =>
            {
                var template = t.Value;
                var database = DatabaseName(template, fixtureId);
                using var connection = new SqlConnection(MasterConnectionString(template));
                var dataPath = connection.ExecuteScalar<string>("SELECT CONVERT(nvarchar(260), SERVERPROPERTY('InstanceDefaultDataPath'))");
                var moves = string.Join(", ", template.Files.Select(f => $"MOVE N'{f.LogicalName}' TO N'{dataPath}{database}{f.Extension}'"));
                connection.Execute($"RESTORE DATABASE [{database}] FROM DISK = @backupFile WITH {moves}, REPLACE", new { template.BackupFile }, commandTimeout: 0);
                return new SqlConnectionStringBuilder(template.ConnectionString) { InitialCatalog = database }.ConnectionString;
            });
        }

        /// <summary>
        /// Drops the copies of a fixture, as all of them at once would not fit in a SQL Server whose databases are kept in memory.
        /// </summary>
        public static void Drop(string fixtureId)
        {
            if (ConnectionStringsByFixture.TryRemove(fixtureId, out var connectionStrings) && connectionStrings.IsValueCreated)
            {
                Drop(connectionStrings.Value.Values);
            }
        }

        /// <summary>
        /// Drops the copies of fixtures that did not drop theirs, e.g. because their one-time set-up failed.
        /// </summary>
        public static void DropAll()
        {
            if (templates == null)
            {
                return;
            }

            foreach (var fixtureId in ConnectionStringsByFixture.Keys)
            {
                Drop(fixtureId);
            }
        }

        private static void Drop(IEnumerable<string> connectionStrings)
        {
            using var master = new SqlConnection(MasterConnectionString(templates.Values.First()));
            foreach (var connectionString in connectionStrings)
            {
                // Pooled connections would otherwise keep the database in use.
                using (var pooled = new SqlConnection(connectionString))
                {
                    SqlConnection.ClearPool(pooled);
                }

                var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
                master.Execute($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]");
            }
        }

        private static string DatabaseName(Template template, string fixtureId) => $"{template.Database}_{fixtureId.Replace('-', '_')}";

        private static string MasterConnectionString(Template template) =>
            new SqlConnectionStringBuilder(template.ConnectionString) { InitialCatalog = "master" }.ConnectionString;
    }
}

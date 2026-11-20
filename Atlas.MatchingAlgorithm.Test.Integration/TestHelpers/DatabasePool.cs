using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers
{
    /// <summary>
    /// A fixture that needs databases to itself: it expects them empty, clears them, checks whole tables, or changes the data refresh records,
    /// which decide the active database for every test. It runs at the same time as other fixtures, on a set of databases from
    /// <see cref="DatabasePool"/> that no other fixture uses meanwhile. Other fixtures share the databases of the configuration.
    /// </summary>
    public abstract class FixtureWithPooledDatabases
    {
        // NUnit runs the one-time tear-down of a base class after that of the fixture, which may still use the databases.
        [OneTimeTearDown]
        public void ReturnPooledDatabases()
        {
            DatabasePool.Return(DependencyInjection.DependencyInjection.CurrentFixtureId());
        }
    }

    /// <summary>
    /// Sets of the transient (A, B) and persistent databases, one for each NUnit worker, so that a fixture that needs databases to itself
    /// never waits for one while a worker is free. The sets are restored once, at the start of the run, from backups of the configured databases
    /// taken after the global set-up; they are not dropped, as dropping a database waits for the connections to it to close.
    /// A set is reset when a fixture takes it, to what the global set-up leaves: empty, with one successful data refresh record.
    /// </summary>
    internal static class DatabasePool
    {
        private const string ConnectionStringsSection = "ConnectionStrings";

        /// <summary>
        /// The connection strings a set has its own copy of. Any other connection string is the configured one.
        /// </summary>
        private static readonly string[] ConnectionStringKeys = { "SqlA", "SqlB", "PersistentSql" };

        private static readonly ConcurrentDictionary<string, Lazy<int>> SetByFixture = new();
        private static readonly ConcurrentBag<int> FreeSets = new();
        private static SemaphoreSlim freeSetCount;
        private static List<IReadOnlyDictionary<string, string>> sets;

        /// <summary>
        /// The connection string with the given key for the current fixture: that of its set if it is a <see cref="FixtureWithPooledDatabases"/>,
        /// which takes a set the first time (possibly in its constructor, before any set-up), else the configured one.
        /// </summary>
        public static string ConnectionString(IServiceProvider serviceProvider, string key)
        {
            var fixture = DependencyInjection.DependencyInjection.CurrentFixture();
            if (sets != null && fixture is TestFixture && fixture.TypeInfo.Type.IsAssignableTo(typeof(FixtureWithPooledDatabases)))
            {
                var set = SetByFixture.GetOrAdd(fixture.Id, _ => new Lazy<int>(Take)).Value;
                if (sets[set].TryGetValue(key, out var connectionString))
                {
                    return connectionString;
                }
            }

            return serviceProvider.GetService<IConfiguration>().GetSection(ConnectionStringsSection)[key];
        }

        /// <summary>
        /// Backs up the configured databases, which the global set-up has migrated and reset, and restores a set from them for each NUnit worker.
        /// Their files are shrunk first, as a restore creates each file at its full size.
        /// </summary>
        public static void Create(IConfiguration configuration)
        {
            var size = WorkerCount();
            var copies = ConnectionStringKeys.ToDictionary(key => key, key => Backup(configuration.GetSection(ConnectionStringsSection)[key]));
            sets = Enumerable.Range(0, size).Select(_ => (IReadOnlyDictionary<string, string>)null).ToList();
            Parallel.For(0, size, set => sets[set] = ConnectionStringKeys.ToDictionary(key => key, key => copies[key](set)));

            freeSetCount = new SemaphoreSlim(size, size);
            for (var set = 0; set < size; set++)
            {
                FreeSets.Add(set);
            }
            TestContext.Progress.WriteLine($"Database pool: {size} sets, one for each NUnit worker");
        }

        public static void Return(string fixtureId)
        {
            if (SetByFixture.TryRemove(fixtureId, out var set) && set.IsValueCreated)
            {
                FreeSets.Add(set.Value);
                freeSetCount.Release();
            }
        }

        private static int Take()
        {
            freeSetCount.Wait();
            if (!FreeSets.TryTake(out var set))
            {
                throw new InvalidOperationException("The database pool counted a free set, but had none");
            }

            var connectionStrings = sets[set];
            DatabaseManager.ResetDatabases(connectionStrings["SqlA"], connectionStrings["SqlB"], connectionStrings["PersistentSql"]);
            return set;
        }

        /// <summary>
        /// The number of fixtures NUnit runs at the same time: NUnit.NumberOfTestWorkers, or by default the number of cores.
        /// </summary>
        private static int WorkerCount() =>
            TestExecutionContext.CurrentContext.Dispatcher is ParallelWorkItemDispatcher dispatcher ? dispatcher.LevelOfParallelism : 1;

        /// <summary>
        /// Backs up a database, and returns a function that restores the set with a given number from the backup, and gives its connection string.
        /// </summary>
        private static Func<int, string> Backup(string connectionString)
        {
            var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            List<(string Name, int Type)> files;
            var backupFile = $"{database}_PoolTemplate.bak";
            using (var connection = new SqlConnection(connectionString))
            {
                // The sets keep no log backups, so their logs must be truncated at each checkpoint.
                connection.Execute($"ALTER DATABASE [{database}] SET RECOVERY SIMPLE");
                files = connection.Query<(string Name, int Type)>("SELECT name, type FROM sys.database_files").ToList();
                foreach (var file in files)
                {
                    connection.Execute($"CHECKPOINT; DBCC SHRINKFILE ([{file.Name}], 1) WITH NO_INFOMSGS", commandTimeout: 0);
                }
                connection.Execute($"BACKUP DATABASE [{database}] TO DISK = @backupFile WITH INIT, COPY_ONLY", new { backupFile }, commandTimeout: 0);
            }

            return set =>
            {
                var copy = $"{database}_Pool{set}";
                using var master = new SqlConnection(new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString);
                var dataPath = master.ExecuteScalar<string>("SELECT CONVERT(nvarchar(260), SERVERPROPERTY('InstanceDefaultDataPath'))");
                var moves = string.Join(", ", files.Select(f => $"MOVE N'{f.Name}' TO N'{dataPath}{copy}{(f.Type == 1 ? "_log.ldf" : ".mdf")}'"));
                master.Execute($"RESTORE DATABASE [{copy}] FROM DISK = @backupFile WITH {moves}, REPLACE", new { backupFile }, commandTimeout: 0);
                return new SqlConnectionStringBuilder(connectionString) { InitialCatalog = copy }.ConnectionString;
            };
        }
    }
}

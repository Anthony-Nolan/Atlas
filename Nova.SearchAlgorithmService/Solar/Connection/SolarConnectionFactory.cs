using System.Data;
using Nova.Utils.Common;
using Oracle.ManagedDataAccess.Client;

namespace Nova.SearchAlgorithmService.Solar.Connection
{
    public interface ISolarConnectionFactory
    {
        IDbConnection GetConnection();
    }

    public class SolarConnectionFactory : ISolarConnectionFactory
    {
        private readonly SolarConnectionSettings settings;

        public SolarConnectionFactory(SolarConnectionSettings settings)
        {
            this.settings = settings.AssertArgumentNotNull(nameof(settings));
        }

        public IDbConnection GetConnection()
        {
            var conn = new OracleConnection(settings.ConnectionString);
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            return conn;
        }
    }
}
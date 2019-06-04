using System;
using System.Linq;
using Nova.SearchAlgorithm.Data.Persistent.Models;

namespace Nova.SearchAlgorithm.Data.Persistent.Repositories
{
    public interface IDataRefreshHistoryRepository
    {
        /// <returns>The transient database for which the refresh job was most recently completed</returns>
        TransientDatabase? GetActiveDatabase();
    }
    
    public class DataRefreshHistoryRepository : IDataRefreshHistoryRepository
    {
        private readonly SearchAlgorithmPersistentContext context;

        public DataRefreshHistoryRepository(SearchAlgorithmPersistentContext context)
        {
            this.context = context;
        }
        
        public TransientDatabase? GetActiveDatabase()
        {
            var lastCompletedRecord = GetLastCompletedRecord();
            if (lastCompletedRecord == null)
            {
                return null;
            }
            return (TransientDatabase) Enum.Parse(typeof(TransientDatabase), lastCompletedRecord.Database);
        }

        private DataRefreshRecord GetLastCompletedRecord()
        {
            return context.DataRefreshRecords
                .Where(r => r.RefreshEndUtc != null)
                .OrderByDescending(r => r.RefreshEndUtc)
                .FirstOrDefault();
        }
    }
}
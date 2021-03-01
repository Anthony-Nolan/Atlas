using System.Threading.Tasks;
using Atlas.RepeatSearch.Data.Context;
using Atlas.RepeatSearch.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.RepeatSearch.Data.Repositories
{
    public interface IRepeatSearchHistoryRepository
    {
        Task RecordRepeatSearchRequest(RepeatSearchHistoryRecord repeatSearchHistoryRecord);
    }

    public class RepeatSearchHistoryRepository : IRepeatSearchHistoryRepository
    {
        private readonly RepeatSearchContext context;
        private DbSet<RepeatSearchHistoryRecord> Entities => context.RepeatSearchHistoryRecords;

        public RepeatSearchHistoryRepository(RepeatSearchContext context)
        {
            this.context = context;
        }

        public async Task RecordRepeatSearchRequest(RepeatSearchHistoryRecord repeatSearchHistoryRecord)
        {
            await Entities.AddAsync(repeatSearchHistoryRecord);
            await context.SaveChangesAsync();
        }
    }
}
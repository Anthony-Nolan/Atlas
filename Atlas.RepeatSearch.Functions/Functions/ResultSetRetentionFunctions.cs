using System.Threading;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.RepeatSearch.Functions.Functions;

public class ResultSetRetentionFunctions
{
    private readonly IExpiredResultSetCleaner expiredResultSetCleaner;

    public ResultSetRetentionFunctions(IExpiredResultSetCleaner expiredResultSetCleaner)
    {
        this.expiredResultSetCleaner = expiredResultSetCleaner;
    }

    [Function(nameof(DeleteExpiredResultSets))]
    public async Task DeleteExpiredResultSets(
        [TimerTrigger("%RepeatSearch:ResultsDeleteCronSchedule%")] TimerInfo _,
        CancellationToken cancellationToken)
    {
        await expiredResultSetCleaner.DeleteExpiredResultSets(cancellationToken);
    }
}

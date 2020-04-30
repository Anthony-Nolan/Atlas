namespace Atlas.Utils.Core.ApplicationInsights
{
    /* This interface allows us to isolate the logger for an entity DbContext, where the logging occurs in a seperate thread.
     * See the EntityContextLogger for details.
     */
    public interface IEntityLogger : ILogger
    {
    }
}
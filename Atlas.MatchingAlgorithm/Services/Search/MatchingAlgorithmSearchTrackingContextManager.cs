using Atlas.MatchingAlgorithm.Models;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface IMatchingAlgorithmSearchTrackingContextManager
    {
        void Set(MatchingAlgorithmSearchTrackingContext context);

        MatchingAlgorithmSearchTrackingContext Retrieve();
    }

    public class MatchingAlgorithmSearchTrackingContextManager(MatchingAlgorithmSearchTrackingContext currentContext)
        : IMatchingAlgorithmSearchTrackingContextManager
    {
        public void Set(MatchingAlgorithmSearchTrackingContext context)
        {
            currentContext.SearchRequestId = context.SearchRequestId;
            currentContext.AttemptNumber = context.AttemptNumber;
        }

        public MatchingAlgorithmSearchTrackingContext Retrieve()
        {
            return currentContext;
        }
    }
}
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
            currentContext.SearchIdentifier = context.SearchIdentifier;
            currentContext.OriginalSearchIdentifier = context.OriginalSearchIdentifier;
            currentContext.AttemptNumber = context.AttemptNumber;
        }

        public MatchingAlgorithmSearchTrackingContext Retrieve()
        {
            return currentContext;
        }
    }
}
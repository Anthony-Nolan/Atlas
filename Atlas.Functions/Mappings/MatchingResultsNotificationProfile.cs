using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Functions.Models;
using AutoMapper;

namespace Atlas.Functions.Mappings;

public class MatchingResultsNotificationProfile : Profile
{
    public MatchingResultsNotificationProfile()
    {
        // Maps only the request identity fields; failure-specific details are assembled
        // on demand (see SearchOrchestrationFunctions.SendFailureNotification).
        CreateMap<MatchingResultsNotification, SearchRequestIdentifiers>();
    }
}
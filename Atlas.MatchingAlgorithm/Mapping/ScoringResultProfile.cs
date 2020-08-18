using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using AutoMapper;

namespace Atlas.MatchingAlgorithm.Mapping
{
    public class ScoringResultProfile : Profile
    {
        public ScoringResultProfile()
        {
            CreateMap<LocusScoreDetails, LocusSearchResult>()
                .ForMember(l => l.IsLocusMatchCountIncludedInTotal, opt => opt.MapFrom(src => true))
                .ForMember(l => l.MatchCount, opt => opt.MapFrom(m => m.MatchCount()))
                .ForMember(l => l.ScoreDetailsAtPositionOne, opt => opt.MapFrom(m => m.ScoreDetailsAtPosition1))
                .ForMember(l => l.ScoreDetailsAtPositionTwo, opt => opt.MapFrom(m => m.ScoreDetailsAtPosition2));
            
            CreateMap<ScoreResult, ScoringResult>()
                .ForMember(s => s.TotalMatchCount, opt => opt.MapFrom(m => m.AggregateScoreDetails.MatchCount))
                .ForMember(s => s.PotentialMatchCount, opt => opt.MapFrom(m => m.AggregateScoreDetails.PotentialMatchCount))
                .ForMember(s => s.GradeScore, opt => opt.MapFrom(m => m.AggregateScoreDetails.GradeScore))
                .ForMember(s => s.ConfidenceScore, opt => opt.MapFrom(m => m.AggregateScoreDetails.ConfidenceScore))
                .ForMember(s => s.OverallMatchConfidence, opt => opt.MapFrom(m => m.AggregateScoreDetails.OverallMatchConfidence))
                .ForMember(s => s.SearchResultAtLocusA, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.A)))
                .ForMember(s => s.SearchResultAtLocusB, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.B)))
                .ForMember(s => s.SearchResultAtLocusC, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.C)))
                .ForMember(s => s.SearchResultAtLocusDpb1, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.Dpb1)))
                .ForMember(s => s.SearchResultAtLocusDqb1, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.Dqb1)))
                .ForMember(s => s.SearchResultAtLocusDrb1, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.Drb1)));
        }
    }
}
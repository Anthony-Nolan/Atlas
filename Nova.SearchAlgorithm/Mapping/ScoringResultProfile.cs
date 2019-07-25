using AutoMapper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Mapping
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
                .ForMember(s => s.TotalMatchCount, opt => opt.MapFrom(m => m.MatchCount))
                .ForMember(s => s.SearchResultAtLocusA, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.A)))
                .ForMember(s => s.SearchResultAtLocusB, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.B)))
                .ForMember(s => s.SearchResultAtLocusC, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.C)))
                .ForMember(s => s.SearchResultAtLocusDpb1, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.Dpb1)))
                .ForMember(s => s.SearchResultAtLocusDqb1, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.Dqb1)))
                .ForMember(s => s.SearchResultAtLocusDrb1, opt => opt.MapFrom(m => m.ScoreDetailsForLocus(Locus.Drb1)));
        }
    }
}
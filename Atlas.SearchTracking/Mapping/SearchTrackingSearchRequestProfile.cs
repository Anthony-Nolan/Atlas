using System.Reflection;
using Atlas.Debug.Client.Models.SearchTracking;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Data.Models;
using AutoMapper;

namespace Atlas.SearchTracking.Mapping
{
    public class SearchTrackingSearchRequestProfile : Profile
    {
        public SearchTrackingSearchRequestProfile()
        {
            CreateMap<SearchRequest, SearchTrackingSearchRequest>()
                .ForMember(dest => dest.ResultsSent,
                    opt => opt.MapFrom(s => s.ResultsSent))
                .ForMember(dest => dest.ResultsSentTimeUtc,
                    opt => opt.MapFrom(s => s.ResultsSentTimeUtc))
                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.IsSuccessful,
                //    opt => opt.MapFrom<CustomResolver>())
                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.IsSuccessful,
                //    opt => opt.MapFrom(src =>
                //    (bool)src.MatchingAlgorithm_IsSuccessful ? src.MatchingAlgorithm_IsSuccessful : null))

                .ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.IsSuccessful,
                    opt =>
                {
                    opt.Condition(src => src.Source.MatchingAlgorithm_IsSuccessful != null);
                    opt.MapFrom(src => src.MatchingAlgorithm_IsSuccessful ?? null);
                })
                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.IsSuccessful,
                //    opt => opt.MapFrom(s => s.MatchingAlgorithm_IsSuccessful))

                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.TotalAttemptsNumber,
                //    opt => opt.MapFrom(s => s.MatchingAlgorithm_TotalAttemptsNumber))
                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.NumberOfResults,
                //    opt => opt.MapFrom(s => s.MatchingAlgorithm_NumberOfResults))
                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.HlaNomenclatureVersion,
                //    opt => opt.MapFrom(s => s.MatchingAlgorithm_HlaNomenclatureVersion))
                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.ResultsSent,
                //    opt => opt.MapFrom(s => s.MatchingAlgorithm_ResultsSent))
                //.ForPath(dest => dest.SearchTrackingMatchingAlgorithmInfo.ResultsSentTimeUtc,
                //    opt => opt.MapFrom(s => s.MatchingAlgorithm_ResultsSentTimeUtc))
                .ForMember(dest => dest.SearchRequestMatchPredictionDetails,
                    opt => opt.MapFrom(s => s.SearchRequestMatchPrediction))
                .ForMember(dest => dest.SearchRequestMatchingAlgorithmAttemptDetails, 
                    opt => opt.MapFrom(s => s.SearchRequestMatchingAlgorithmAttempts));

            CreateMap<SearchRequestMatchingAlgorithmAttempts, SearchTrackingMatchingAlgorithmAttemptDetails>()
                .ForPath(dest => dest.AlgorithmCoreMatchingTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_Matching_EndTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreMatchingTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_Matching_StartTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreScoringTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_Scoring_EndTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreScoringTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_Scoring_StartTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.PersistingResults_EndTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.PersistingResults_StartTimeUtc));

            CreateMap<SearchRequestMatchPrediction, SearchTrackingMatchPredictionDetails>()
                .ForPath(dest => dest.AlgorithmCoreRunningBatchesTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_RunningBatches_EndTimeUtc))
                .ForPath(dest => dest.AlgorithmCoreRunningBatchesTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.AlgorithmCore_RunningBatches_StartTimeUtc))
                .ForPath(dest => dest.PrepareBatchesTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.PrepareBatches_EndTimeUtc))
                .ForPath(dest => dest.PrepareBatchesTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.PrepareBatches_StartTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.EndTimeUtc,
                    opt => opt.MapFrom(s => s.PersistingResults_EndTimeUtc))
                .ForPath(dest => dest.PersistingResultsTiming.StartTimeUtc,
                    opt => opt.MapFrom(s => s.PersistingResults_StartTimeUtc))
                .ForMember(dest => dest.FailureInfo,
                    opt => opt.Condition((src, dest, srcMember) => srcMember != null))
                .ForPath(dest => dest.FailureInfo.ExceptionStacktrace,
                    opt => opt.MapFrom(s => s.FailureInfo_ExceptionStacktrace))
                .ForPath(dest => dest.FailureInfo.Message,
                    opt => opt.MapFrom(s => s.FailureInfo_Message))
                .ForPath(dest => dest.FailureInfo.Type,
                    opt => opt.MapFrom(s => s.FailureInfo_Type));

            CreateMap<MatchingAlgorithmFailureType, SearchTrackingMatchingAlgorithmFailureType>();

            CreateMap<MatchPredictionFailureType, SearchTrackingMatchPredictionFailureType>();

            CreateMap<SearchTrackingMatchPredictionInfo, SearchTrackingMatchPredictionFailureInfo>()
                .ForMember(dest => dest.ExceptionStacktrace,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.ExceptionStacktrace))
                .ForMember(dest => dest.Message,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.Message))
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.Type));

            CreateMap<SearchTrackingMatchingAlgorithmInfo, SearchTrackingMatchingAlgorithmFailureInfo>()
                .ForMember(dest => dest.ExceptionStacktrace,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.ExceptionStacktrace))
                .ForMember(dest => dest.Message,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.Message))
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.Type));

            CreateMap<SearchTrackingMatchingAlgorithmInfo, SearchTrackingRepeatSearchMatchingAlgorithmDetails>()
                .ForMember(dest => dest.AddedResultCount,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.RepeatDetails.AddedResultCount))
                .ForMember(dest => dest.RemovedResultCount,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.RepeatDetails.RemovedResultCount))
                .ForMember(dest => dest.UpdatedResultCount,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.RepeatDetails.UpdatedResultCount));

            CreateMap<SearchTrackingMatchingAlgorithmAttemptDetails, SearchTrackingMatchingAlgorithmFailureInfo>()
                .ForMember(dest => dest.ExceptionStacktrace,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.ExceptionStacktrace))
                .ForMember(dest => dest.Message,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.Message))
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(s => (bool)s.IsSuccessful ? null : s.FailureInfo.Type));

            CreateMap<SearchTrackingSearchRequest, SearchTrackingMatchPredictionInfo>()
                .ForMember(dest => dest.FailureInfo,
                    opt => opt.MapFrom(s => s.IsMatchPredictionRun ? s.SearchTrackingMatchPredictionInfo.FailureInfo : null));
        }

        //public interface IValueResolver<in TSource, in TDestination, TDestMember>
        //{
        //    TDestMember Resolve(TSource source, TDestination destination, TDestMember destMember, ResolutionContext context);
        //}

        //public class CustomResolver : IValueResolver<Source, Destination, bool?>
        //{
        //    public bool? Resolve(Source source, Destination destination, bool? destMember, ResolutionContext context)
        //    {
        //        // Skip mapping if parent object is null or IsSuccessful is null
        //        if (destination.SearchTrackingMatchingAlgorithmInfo.IsSuccessful == null)
        //        {
        //            return null; // Preserve null (no mapping occurs)
        //        }

        //        // Map true to true, false to false, null to null
        //        return source.MatchingAlgorithm_IsSuccessful;
        //    }
        //}
    }
}
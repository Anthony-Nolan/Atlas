using Atlas.Debug.Client.Models.SearchTracking;
using Atlas.SearchTracking.Data.Models;
using AutoMapper;

namespace Atlas.SearchTracking.Mapping
{
    public class SearchTrackingSearchRequestProfile : Profile
    {
        public SearchTrackingSearchRequestProfile()
        {
            CreateMap<SearchRequest, SearchTrackingSearchRequest>()
                .ForMember(dest => dest.DonorRegistryCodes, opt =>
                {
                    opt.PreCondition(src => src.DonorRegistryCodes != null);
                    opt.MapFrom(src => src.DonorRegistryCodes);
                })
                .ForMember(dest => dest.MatchingAlgorithmInfo, opt =>
                {
                    opt.PreCondition(src => src.MatchingAlgorithm_IsSuccessful != null);
                    opt.MapFrom(src => new SearchTrackingMatchingAlgorithmInfo
                    {
                        IsSuccessful = src.MatchingAlgorithm_IsSuccessful.Value,
                        TotalAttemptsNumber = src.MatchingAlgorithm_TotalAttemptsNumber,
                        NumberOfResults = src.MatchingAlgorithm_NumberOfResults,
                        HlaNomenclatureVersion = src.MatchingAlgorithm_HlaNomenclatureVersion,
                        ResultsSent = src.MatchingAlgorithm_ResultsSent,
                        ResultsSentTimeUtc = src.MatchingAlgorithm_ResultsSentTimeUtc,
                        FailureInfo = src.MatchingAlgorithm_IsSuccessful == true
                            ? null
                            : new SearchTrackingMatchingAlgorithmFailureInfo
                            {
                                Message = src.MatchingAlgorithm_FailureInfo_Message,
                                ExceptionStacktrace = src.MatchingAlgorithm_FailureInfo_ExceptionStacktrace,
                                Type = (SearchTrackingMatchingAlgorithmFailureType?)src.MatchingAlgorithm_FailureInfo_Type
                            },
                        RepeatDetails = src.IsRepeatSearch == true && src.MatchingAlgorithm_IsSuccessful == true
                            ? new SearchTrackingRepeatSearchMatchingAlgorithmDetails
                            {
                                AddedResultCount = src.MatchingAlgorithm_RepeatSearch_AddedResultCount,
                                RemovedResultCount = src.MatchingAlgorithm_RepeatSearch_RemovedResultCount,
                                UpdatedResultCount = src.MatchingAlgorithm_RepeatSearch_UpdatedResultCount
                            }
                            : null,
                    });
                })
                .ForMember(dest => dest.MatchPredictionInfo, opt =>
                {
                    opt.PreCondition(src => src.MatchPrediction_IsSuccessful != null);
                    opt.MapFrom(src => new SearchTrackingMatchPredictionInfo()
                    {
                        IsSuccessful = src.MatchPrediction_IsSuccessful.Value,
                        DonorsPerBatch = src.MatchPrediction_DonorsPerBatch,
                        TotalNumberOfBatches = src.MatchPrediction_TotalNumberOfBatches,
                        FailureInfo = src.MatchPrediction_IsSuccessful == true
                            ? null
                            : new SearchTrackingMatchPredictionFailureInfo()
                            {
                                Message = src.MatchPrediction_FailureInfo_Message,
                                ExceptionStacktrace = src.MatchPrediction_FailureInfo_ExceptionStacktrace,
                                Type = (SearchTrackingMatchPredictionFailureType?)src.MatchPrediction_FailureInfo_Type
                            },
                    });
                })
                .ForMember(dest => dest.MatchPredictionDetails, opt => opt.MapFrom(src => src.MatchPrediction))
                .ForMember(dest => dest.MatchingAlgorithmAttemptDetails,
                    opt => opt.MapFrom(src => src.MatchingAlgorithmAttempts));

            CreateMap<SearchRequestMatchingAlgorithmAttempts, SearchTrackingMatchingAlgorithmAttemptDetails>()
                .ForMember(dest => dest.AlgorithmCoreMatchingTiming, opt => opt.MapFrom(src => new SearchTrackingTimingInfo
                {
                    StartTimeUtc = src.AlgorithmCore_Matching_StartTimeUtc,
                    EndTimeUtc = src.AlgorithmCore_Matching_EndTimeUtc
                }))
                .ForMember(dest => dest.AlgorithmCoreScoringTiming, opt => opt.MapFrom(src => new SearchTrackingTimingInfo
                {
                    StartTimeUtc = src.AlgorithmCore_Scoring_StartTimeUtc,
                    EndTimeUtc = src.AlgorithmCore_Scoring_EndTimeUtc
                }))
                .ForMember(dest => dest.PersistingResultsTiming, opt => opt.MapFrom(src => new SearchTrackingTimingInfo
                {
                    StartTimeUtc = src.PersistingResults_StartTimeUtc,
                    EndTimeUtc = src.PersistingResults_EndTimeUtc
                }))
                .ForMember(dest => dest.FailureInfo, opt =>
                {
                    opt.PreCondition(src => src.IsSuccessful == false);
                    opt.MapFrom(src => new SearchTrackingMatchingAlgorithmFailureInfo
                    {
                        Message = src.FailureInfo_Message!,
                        ExceptionStacktrace = src.FailureInfo_ExceptionStacktrace,
                        Type = (SearchTrackingMatchingAlgorithmFailureType?)src.FailureInfo_Type
                    });
                });
            
            CreateMap<SearchRequestMatchPrediction, SearchTrackingMatchPredictionDetails>()
                .ForMember(dest => dest.AlgorithmCoreRunningBatchesTiming, opt => opt.MapFrom(src => new SearchTrackingTimingInfo
                {
                    StartTimeUtc = src.AlgorithmCore_RunningBatches_StartTimeUtc,
                    EndTimeUtc = src.AlgorithmCore_RunningBatches_EndTimeUtc
                }))
                .ForMember(dest => dest.PrepareBatchesTiming, opt => opt.MapFrom(src => new SearchTrackingTimingInfo
                {
                    StartTimeUtc = src.PrepareBatches_StartTimeUtc,
                    EndTimeUtc = src.PrepareBatches_EndTimeUtc
                }))
                .ForMember(dest => dest.PersistingResultsTiming, opt => opt.MapFrom(src => new SearchTrackingTimingInfo
                {
                    StartTimeUtc = src.PersistingResults_StartTimeUtc,
                    EndTimeUtc = src.PersistingResults_EndTimeUtc
                }))
                .ForMember(dest => dest.FailureInfo, opt =>
                {
                    opt.PreCondition(src => src.IsSuccessful == false);
                    opt.MapFrom(src => new SearchTrackingMatchPredictionFailureInfo
                    {
                        Message = src.FailureInfo_Message,
                        ExceptionStacktrace = src.FailureInfo_ExceptionStacktrace,
                        Type = (SearchTrackingMatchPredictionFailureType?)src.FailureInfo_Type
                    });
                });
        }
    }
}